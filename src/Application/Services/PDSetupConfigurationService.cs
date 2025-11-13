using System;
using System.Text.Json;
using Application.Abstractions.Data;
using Application.DTOs.PD;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.Services;

/// <summary>
/// Service for managing PDSetup configuration data with caching to avoid repeated database reads and JSON parsing
/// </summary>
public interface IPDSetupConfigurationService
{
    /// <summary>
    /// Gets the current PDSetup configuration, using cache if available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The current PDSetup configuration or null if not found</returns>
    Task<PDSetupConfiguration?> GetCurrentConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the PDSetup configuration from the default JSON file (PD_wizad_data.json)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The PDSetup configuration from the file or null if not found</returns>
    Task<PDSetupConfiguration?> GetConfigurationFromFileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the bucket labels from step3 configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bucket labels in order</returns>
    Task<List<string>> GetBucketLabelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the worst (last) bucket label from step3 configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The worst bucket label or null if not found</returns>
    Task<string?> GetWorstBucketAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the segment configurations from step6 configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of segment configurations</returns>
    Task<List<PDConfiguration>> GetSegmentConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the frequency from step4 configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The frequency value or empty string if not found</returns>
    Task<string> GetFrequencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the comparison period is compatible with the frequency
    /// </summary>
    /// <param name="frequency">The frequency from step4</param>
    /// <param name="comparisonPeriod">The comparison period from step6</param>
    /// <returns>True if compatible, false otherwise</returns>
    bool ValidateComparisonPeriodCompatibility(string frequency, string comparisonPeriod);

    /// <summary>
    /// Clears the cached configuration, forcing a reload on next access
    /// </summary>
    void ClearCache();
}

/// <summary>
/// Implementation of PDSetupConfigurationService with caching
/// </summary>
public sealed class PDSetupConfigurationService : IPDSetupConfigurationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<PDSetupConfigurationService> _logger;
    
    // Cache fields
    private PDSetupConfiguration? _cachedConfiguration;
    private DateTime? _cacheTimestamp;
    private readonly object _cacheLock = new();
    private const int CacheExpirationMinutes = 30; // Cache for 30 minutes
    
    // Relative path for PD wizard configuration from application root
    private const string DefaultConfigurationFilePath = @"Data\PD_wizad_data.json";
    
    // Static JsonSerializerOptions to avoid creating new instances
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PDSetupConfigurationService(
        IApplicationDbContext dbContext,
        ILogger<PDSetupConfigurationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PDSetupConfiguration?> GetCurrentConfigurationAsync(CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            // Check if cache is valid
            if (_cachedConfiguration != null && 
                _cacheTimestamp.HasValue && 
                DateTime.UtcNow.Subtract(_cacheTimestamp.Value).TotalMinutes < CacheExpirationMinutes)
            {
                _logger.LogDebug("Returning cached PDSetup configuration");
                return _cachedConfiguration;
            }
        }

        try
        {
            _logger.LogInformation("Loading PDSetup configuration from database");

            // Get the latest PDTempData from database
            Domain.PDTempData.PDTempData? pdTempData = await _dbContext.PDTempDatas
                .OrderByDescending(pd => pd.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (pdTempData == null)
            {
                _logger.LogWarning("No PDTempData found in database");
                return null;
            }

            // Deserialize JSON to strongly typed configuration
            PDSetupConfiguration? configuration = await DeserializeConfigurationAsync(pdTempData.PDSetupJson);

            if (configuration != null)
            {
                lock (_cacheLock)
                {
                    _cachedConfiguration = configuration;
                    _cacheTimestamp = DateTime.UtcNow;
                }

                _logger.LogInformation("PDSetup configuration loaded and cached successfully");
            }

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading PDSetup configuration: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("Failed to load PDSetup configuration. See inner exception for details.", ex);
        }
    }

    public async Task<PDSetupConfiguration?> GetConfigurationFromFileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the absolute path from the relative path
            string absolutePath = GetAbsoluteFilePath();
            
            if (!File.Exists(absolutePath))
            {
                _logger.LogWarning("Configuration file not found at path: {FilePath}", absolutePath);
                throw new FileNotFoundException($"Configuration file not found at path: {absolutePath}");
            }

            _logger.LogInformation("Loading PDSetup configuration from file: {FilePath}", absolutePath);

            // Read the JSON file
            string jsonContent = await File.ReadAllTextAsync(absolutePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Configuration file is empty: {FilePath}", absolutePath);
                return null;
            }

            // Deserialize JSON to strongly typed configuration
            PDSetupConfiguration? configuration = await DeserializeConfigurationAsync(jsonContent);

            if (configuration is not null)
            {
                _logger.LogInformation("PDSetup configuration loaded successfully from file");
            }

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading PDSetup configuration from file");
            throw new InvalidOperationException("Failed to load PDSetup configuration from file", ex);
        }
    }

    public async Task<List<string>> GetBucketLabelsAsync(CancellationToken cancellationToken = default)
    {
        PDSetupConfiguration? configuration = await GetConfigurationFromFileAsync(cancellationToken);

        if (configuration?.Step3?.DatePassedDueBuckets == null)
        {
            _logger.LogWarning("No step3 datePassedDueBuckets found in configuration");
            return new List<string>();
        }

        var bucketLabels = configuration.Step3.DatePassedDueBuckets
            .Where(bucket => !string.IsNullOrWhiteSpace(bucket.BucketLabel))
            .Select(bucket => bucket.BucketLabel)
            .ToList();

        _logger.LogDebug("Extracted {Count} bucket labels from configuration", bucketLabels.Count);
        return bucketLabels;
    }

    public async Task<string?> GetWorstBucketAsync(CancellationToken cancellationToken = default)
    {
        List<string> bucketLabels = await GetBucketLabelsAsync(cancellationToken);

        if (bucketLabels.Count == 0)
        {
            _logger.LogWarning("No bucket labels found, cannot determine worst bucket");
            return null;
        }

        // The worst bucket is the last one in the ordered list
        string worstBucket = bucketLabels[bucketLabels.Count - 1];
        
        _logger.LogDebug("Found worst bucket: '{WorstBucket}' from {TotalBuckets} total buckets", 
            worstBucket, bucketLabels.Count);

        return worstBucket;
    }

    public async Task<List<PDConfiguration>> GetSegmentConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        PDSetupConfiguration? configuration = await GetConfigurationFromFileAsync(cancellationToken);

        if (configuration?.Step6?.PDConfiguration == null)
        {
            _logger.LogWarning("No step6 PDConfiguration found in configuration");
            return new List<PDConfiguration>();
        }

        var segmentConfigurations = configuration.Step6.PDConfiguration.ToList();
        _logger.LogDebug("Extracted {Count} segment configurations from step6", segmentConfigurations.Count);
        return segmentConfigurations;
    }

    public async Task<string> GetFrequencyAsync(CancellationToken cancellationToken = default)
    {
        PDSetupConfiguration? configuration = await GetConfigurationFromFileAsync(cancellationToken);

        if (configuration?.Step4 == null)
        {
            _logger.LogWarning("No step4 configuration found");
            return string.Empty;
        }

        string frequency = configuration.Step4.Frequency ?? string.Empty;
        _logger.LogDebug("Found frequency: '{Frequency}' from step4 configuration", frequency);
        return frequency;
    }

    public bool ValidateComparisonPeriodCompatibility(string frequency, string comparisonPeriod)
    {
        if (string.IsNullOrWhiteSpace(frequency) || string.IsNullOrWhiteSpace(comparisonPeriod))
        {
            return false;
        }

        return frequency.ToUpperInvariant() switch
        {
            "YEARLY" => comparisonPeriod.ToUpperInvariant() is "YEARLY",
            "QUARTERLY" => comparisonPeriod.ToUpperInvariant() is "QUARTERLY" or "YEARLY",
            "MONTHLY" => comparisonPeriod.ToUpperInvariant() is "MONTHLY" or "QUARTERLY" or "YEARLY",
            _ => false
        };
    }

    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _cachedConfiguration = null;
            _cacheTimestamp = null;
            _logger.LogDebug("PDSetup configuration cache cleared");
        }
    }

    /// <summary>
    /// Gets the absolute file path from the relative path based on application base directory
    /// </summary>
    /// <returns>The absolute file path</returns>
    private static string GetAbsoluteFilePath()
    {
        // Get the base directory of the application
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        // Combine with the relative path to get absolute path
        string absolutePath = Path.Combine(baseDirectory, DefaultConfigurationFilePath);
        
        return absolutePath;
    }

    /// <summary>
    /// Deserializes the PDSetupJson string to strongly typed configuration
    /// </summary>
    /// <param name="pdSetupJson">The JSON string to deserialize</param>
    /// <returns>The deserialized configuration or null if deserialization fails</returns>
    private async Task<PDSetupConfiguration?> DeserializeConfigurationAsync(string pdSetupJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pdSetupJson))
            {
                _logger.LogWarning("PDSetupJson is null or empty");
                return null;
            }

            PDSetupConfiguration? configuration = JsonSerializer.Deserialize<PDSetupConfiguration>(pdSetupJson, s_jsonOptions);

            if (configuration == null)
            {
                _logger.LogWarning("Failed to deserialize PDSetupJson - result is null");
                return null;
            }

            _logger.LogDebug("Successfully deserialized PDSetupJson to strongly typed configuration");
            return await Task.FromResult(configuration);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize PDSetupJson: {Error}", ex.Message);
            throw new InvalidOperationException("Invalid PDSetupJson format", ex);
        }
    }
}
