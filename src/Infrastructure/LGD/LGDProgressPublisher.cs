using System.Text.Json;
using Application.Abstractions.Data;
using Application.LGD.DTOs;
using Application.LGD.Services;
using Domain.LGDProgressTrackings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.LGD;

/// <summary>
/// Publisher service that sends LGD progress messages to Redis stream.
/// This service is responsible for publishing progress updates during LGD calculation execution.
/// Messages are sent to a Redis Stream where the consumer service picks them up for processing.
/// </summary>
internal sealed class LgdProgressPublisher : ILgdProgressPublisher
{
    // Redis stream key where all LGD progress messages are published
    private const string StreamKey = "lgd-progress-stream";
    private readonly IConnectionMultiplexer? _redis;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LgdProgressPublisher> _logger;

    public LgdProgressPublisher(IApplicationDbContext context, ILogger<LgdProgressPublisher> logger, IConnectionMultiplexer? redis = null)
    {
        _context = context;
        _logger = logger;
        _redis = redis;

        if (_redis == null)
        {
            _logger.LogWarning("LGDProgressPublisher initialized without Redis connection. Progress updates will not be published.");
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
        LgdProgressStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PublishProgress called for SessionId: {SessionId}, Step: {StepOrder}, SubTask: {SubTaskOrder}, Status: {Status}",
            sessionId, stepOrder, subTaskOrder, status);

        // Retrieve the progress record from database to get step/subtask names
        LgdProgressTracking? record = await GetProgressRecord(sessionId, stepOrder, subTaskOrder, cancellationToken);

        if (record == null)
        {
            _logger.LogWarning("Progress record not found for SessionId: {SessionId}, Step: {StepOrder}, SubTask: {SubTaskOrder}",
                sessionId, stepOrder, subTaskOrder);
            return;
        }

        // Create message with the specified status
        LgdProgressMessage message = new(
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
    private async Task<LgdProgressTracking?> GetProgressRecord(Guid sessionId, int stepOrder, int subTaskOrder, CancellationToken cancellationToken)
    {
        return await _context.LgdProgressTrackings
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
    private async Task PublishToRedis(LgdProgressMessage message)
    {
        if (_redis is null)
        {
            _logger.LogWarning("Cannot publish LGD progress message: Redis connection is not available");
            return;
        }

        try
        {
            IDatabase database = _redis.GetDatabase();
            string serializedMessage = JsonSerializer.Serialize(message);

            NameValueEntry[] fields = { new("data", serializedMessage) };

            RedisValue messageId = await database.StreamAddAsync(StreamKey, fields);

            _logger.LogInformation("Published LGD progress message to Redis stream. MessageId: {MessageId}, SessionId: {SessionId}, Step: {StepOrder}, SubTask: {SubTaskOrder}, Status: {Status}",
                messageId, message.SessionId, message.StepOrder, message.SubTaskOrder, message.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing LGD progress message to Redis stream for SessionId: {SessionId}, Step: {StepOrder}, SubTask: {SubTaskOrder}",
                message.SessionId, message.StepOrder, message.SubTaskOrder);
            throw new InvalidOperationException($"Failed to publish LGD progress message to Redis stream for session {message.SessionId}", ex);
        }
    }
}