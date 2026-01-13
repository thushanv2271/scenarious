using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.LGDCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.LgdAlgorithmResults.GetLgdAlgorithmResult;

/// <summary>
/// Handler to retrieve the latest LGD Algorithm Result
/// </summary>
internal sealed class GetLgdAlgorithmResultQueryHandler(
    IApplicationDbContext context,
    ILogger<GetLgdAlgorithmResultQueryHandler> logger)
    : IQueryHandler<GetLgdAlgorithmResultQuery, LgdAlgorithmResultResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<LgdAlgorithmResultResponse>> Handle(
        GetLgdAlgorithmResultQuery query,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving latest LGD Algorithm Result");

        LgdAlgorithmResult? result = await context.LgdAlgorithmResults
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            logger.LogWarning("No LGD Algorithm Results found in database");
            return Result.Failure<LgdAlgorithmResultResponse>(
                LgdAlgorithmResultErrors.NoResultsFound);
        }

        LgdAlgorithmData? data = JsonSerializer.Deserialize<LgdAlgorithmData>(
            result.LgdAlgorithmResultData,
            JsonOptions);

        if (data is null)
        {
            logger.LogError("Failed to deserialize LGD Algorithm Result data for ID: {Id}", result.Id);
            return Result.Failure<LgdAlgorithmResultResponse>(
                LgdAlgorithmResultErrors.InvalidData);
        }

        // Return the complete LGD Algorithm Result data
        var response = new LgdAlgorithmResultResponse
        {
            Id = result.Id,
            Data = data,
            CreatedAt = result.CreatedAt,
            CreatedBy = result.CreatedBy,
            UpdatedAt = result.UpdatedAt,
            UpdatedBy = result.UpdatedBy
        };

        logger.LogInformation("Successfully retrieved LGD Algorithm Result with ID: {Id}", result.Id);

        return Result.Success(response);
    }
}