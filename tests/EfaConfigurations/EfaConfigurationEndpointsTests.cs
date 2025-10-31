using Domain.EfaConfigs;
using IntegrationTests.Common;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;

namespace IntegrationTests.EfaConfigurations;

public class EfaConfigurationEndpointsTests : BaseIntegrationTest
{
    private const string BaseUrl = "efa-configurations";

    public EfaConfigurationEndpointsTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    #region Create Tests

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnOk_WhenSingleItemIsValid()
    {
        // Arrange
        var request = new[]
        {
            new { Year = 2025, EfaRate = 45.75m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<EfaConfigResponse>? result = await response.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Id.Should().NotBeEmpty();
        result[0].Year.Should().Be(2025);
        result[0].EfaRate.Should().Be(45.75m);
        result[0].UpdatedBy.Should().Be(TestUserId);
        result[0].UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify in database
        EfaConfiguration? efaConfig = await DbContext.EfaConfigurations
            .FirstOrDefaultAsync(e => e.Id == result[0].Id);
        efaConfig.Should().NotBeNull();
        efaConfig!.Year.Should().Be(2025);
        efaConfig.EfaRate.Should().Be(45.75m);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnOk_WhenMultipleItemsAreValid()
    {
        // Arrange
        var request = new[]
        {
            new { Year = 2025, EfaRate = 45.75m },
            new { Year = 2026, EfaRate = 50.20m },
            new { Year = 2027, EfaRate = 55.00m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? result = await response.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        result![0].Year.Should().Be(2025);
        result[0].EfaRate.Should().Be(45.75m);
        result[1].Year.Should().Be(2026);
        result[1].EfaRate.Should().Be(50.20m);
        result[2].Year.Should().Be(2027);
        result[2].EfaRate.Should().Be(55.00m);

        // Verify all are in database
        List<EfaConfiguration> dbConfigs = await DbContext.EfaConfigurations
            .Where(e => e.Year >= 2025 && e.Year <= 2027)
            .ToListAsync();
        dbConfigs.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnBadRequest_WhenYearIsInvalid()
    {
        // Arrange
        var request = new[]
        {
            new { Year = 1800, EfaRate = 10m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnBadRequest_WhenEfaRateIsNegative()
    {
        // Arrange
        var request = new[]
        {
            new { Year = 2025, EfaRate = -5m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnBadRequest_WhenDuplicateYearsInRequest()
    {
        // Arrange
        var request = new[]
        {
            new { Year = 2025, EfaRate = 10m },
            new { Year = 2025, EfaRate = 15m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnBadRequest_WhenEmptyArray()
    {
        // Arrange
        object[] request = Array.Empty<object>();

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get All Tests

    [Fact]
    public async Task GetAllEfaConfigurations_ShouldReturnOk_WithEmptyList_WhenNoConfigurationsExist()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? result = await response.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllEfaConfigurations_ShouldReturnOk_WithAllConfigurations()
    {
        // Arrange
        EfaConfiguration[] configs = new[]
        {
            new EfaConfiguration
            {
                Id = Guid.CreateVersion7(),
                Year = 2023,
                EfaRate = 8.5m,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = TestUserId
            },
            new EfaConfiguration
            {
                Id = Guid.CreateVersion7(),
                Year = 2024,
                EfaRate = 9.25m,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = TestUserId
            },
            new EfaConfiguration
            {
                Id = Guid.CreateVersion7(),
                Year = 2025,
                EfaRate = 10.75m,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = TestUserId
            }
        };
        DbContext.EfaConfigurations.AddRange(configs);
        await DbContext.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? result = await response.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Verify ordered by year descending
        result![0].Year.Should().Be(2025);
        result[1].Year.Should().Be(2024);
        result[2].Year.Should().Be(2023);
    }

    #endregion

    #region Edit Tests

    [Fact]
    public async Task EditEfaConfiguration_ShouldReturnOk_WhenOnlyEfaRateIsChanged()
    {
        // Arrange
        var existingConfig = new EfaConfiguration
        {
            Id = Guid.CreateVersion7(),
            Year = 2025,
            EfaRate = 45.75m,
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = TestUserId
        };
        DbContext.EfaConfigurations.Add(existingConfig);
        await DbContext.SaveChangesAsync();

        var request = new
        {
            Year = 2025,
            EfaRate = 50.00m
        };

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{existingConfig.Id}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        EditResponse? result = await response.Content.ReadFromJsonAsync<EditResponse>();
        result.Should().NotBeNull();
        result!.EfaRate.Should().Be(50.00m);
        result.Year.Should().Be(2025);
    }

    [Fact]
    public async Task EditEfaConfiguration_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        // Arrange
        var request = new
        {
            Year = 2025,
            EfaRate = 45.75m
        };

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEfaConfiguration_ShouldReturnConflict_WhenYearAlreadyExistsForDifferentRecord()
    {
        // Arrange
        var existingConfig1 = new EfaConfiguration
        {
            Id = Guid.CreateVersion7(),
            Year = 2024,
            EfaRate = 40m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = TestUserId
        };
        var existingConfig2 = new EfaConfiguration
        {
            Id = Guid.CreateVersion7(),
            Year = 2025,
            EfaRate = 45m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = TestUserId
        };
        DbContext.EfaConfigurations.AddRange(existingConfig1, existingConfig2);
        await DbContext.SaveChangesAsync();

        var request = new
        {
            Year = 2024, // Trying to change to year that exists in config1
            EfaRate = 50m
        };

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{existingConfig2.Id}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteEfaConfiguration_ShouldReturnOk_WhenIdExists()
    {
        // Arrange
        var existingConfig = new EfaConfiguration
        {
            Id = Guid.CreateVersion7(),
            Year = 2026,
            EfaRate = 50.20m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = TestUserId
        };
        DbContext.EfaConfigurations.Add(existingConfig);
        await DbContext.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{existingConfig.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        DeleteResponse? result = await response.Content.ReadFromJsonAsync<DeleteResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingConfig.Id);
        result.Year.Should().Be(2026);
        result.EfaRate.Should().Be(50.20m);

        // Verify deleted from database
        EfaConfiguration? deletedConfig = await DbContext.EfaConfigurations
            .FirstOrDefaultAsync(e => e.Id == existingConfig.Id);
        deletedConfig.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEfaConfiguration_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Response DTOs

    private sealed record EfaConfigResponse(
        Guid Id,
        int Year,
        decimal EfaRate,
        DateTime UpdatedAt,
        Guid UpdatedBy);

    private sealed record EditResponse(
        Guid Id,
        int Year,
        decimal EfaRate,
        DateTime UpdatedAt,
        Guid UpdatedBy);

    private sealed record DeleteResponse(
        Guid Id,
        int Year,
        decimal EfaRate,
        DateTime DeletedAt,
        Guid DeletedBy);

    #endregion
}
