using System.Text.Json;
using Application.Abstractions.Data;
using Application.PD.DTOs;
using Application.PD.Services;
using Domain.PDProgressTrackings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.PD;

/// <summary>
/// Publisher service that sends PD progress messages to Redis stream.
/// This service is responsible for publishing progress updates during PD calculation execution.
/// Messages are sent to a Redis Stream where the consumer service picks them up for processing.
/// </summary>
internal sealed class PDProgressPublisher : IPDProgressPublisher
{
    // Redis stream key where all progress messages are published
    private const string StreamKey = "pd-progress-stream";
    private readonly IConnectionMultiplexer? _redis;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PDProgressPublisher> _logger;

    public PDProgressPublisher(IApplicationDbContext context, ILogger<PDProgressPublisher> logger, IConnectionMultiplexer? redis = null)
    {
        _context = context;
        _logger = logger;
        _redis = redis;
        
        if (_redis == null)
        {
            _logger.LogWarning("PDProgressPublisher initialized without Redis connection. Progress updates will not be published.");
        }
    }

    /// <summary>
    /// Publishes a progress update for a specific subtask.
    /// This is the single entry point for all progress updates (Started, Completed, Failed).
    /// </summary>
    public async Task PublishProgress(
        Guid sessionId, 
        int stepOrder, 
        int subTaskOrder, 
        PDProgressStatus status, 
        string? errorMessage = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PublishProgress called for SessionId: {SessionId}, Step: {StepOrder}, SubTask: {SubTaskOrder}, Status: {Status}", 
            sessionId, stepOrder, subTaskOrder, status);
        
        // Retrieve the progress record from database to get step/subtask names
        PDProgressTracking? record = await GetProgressRecord(sessionId, stepOrder, subTaskOrder, cancellationToken);
        
        if (record == null)
        {
            _logger.LogWarning("Progress record not found for SessionId: {SessionId}, Step: {StepOrder}, SubTask: {SubTaskOrder}", 
                sessionId, stepOrder, subTaskOrder);
            return;
        }

        // Create message with the specified statu
        PDProgressMessage message = new(
            SessionId: sessionId,
            StepOrder: stepOrder,
            StepName: record.StepName,
            SubTaskOrder: subTaskOrder,
            SubTaskName: record.SubTaskName,
            Status: status,
            ErrorMessage: errorMessage,
            Timestamp: DateTime.UtcNow
        );

        // Publish to Redis stream for consumer to process
        await PublishToRedis(message);
    }

    /// <summary>
    /// Retrieves the progress tracking record from the database.
    /// This is needed to get the step and subtask names for the progress message.
    /// </summary>
    private async Task<PDProgressTracking?> GetProgressRecord(Guid sessionId, int stepOrder, int subTaskOrder, CancellationToken cancellationToken)
    {
        return await _context.PDProgressTrackings
            .Where(p => p.SessionId == sessionId
                     && p.StepOrder == stepOrder
                     && p.SubTaskOrder == subTaskOrder
                     && p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Publishes a message to the Redis stream.
    /// The message is serialized to JSON and added to the stream as a name-value pair.
    /// Redis Streams guarantees message ordering and persistence until acknowledged.
    /// </summary>
    private async Task PublishToRedis(PDProgressMessage message)
    {
        if (_redis == null)
        {
            _logger.LogWarning("Cannot publish to Redis - connection not available. Message: {Message}", JsonSerializer.Serialize(message));
            return;
        }
        
        try
        {
            IDatabase db = _redis.GetDatabase();
            
            // Serialize message to JSON
            string json = JsonSerializer.Serialize(message);
            
            _logger.LogInformation("Publishing to Redis stream {StreamKey}: {Json}", StreamKey, json);
            
            // Create stream entry with "data" field containing the JSON
            NameValueEntry[] fields = 
            [
                new NameValueEntry("data", json)
            ];

            // Add message to Redis stream (fire-and-forget, consumer will pick it up)
            RedisValue messageId = await db.StreamAddAsync(StreamKey, fields);
            
            _logger.LogInformation("Published progress to Redis - SessionId: {SessionId}, Status: {Status}, MessageId: {MessageId}", 
                message.SessionId, message.Status, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to Redis for SessionId: {SessionId}", message.SessionId);
        }
    }
}
