using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.RiskEvaluations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.RiskEvaluations.CreateEvaluation;

// Handles creation of a risk evaluation
internal sealed class CreateRiskEvaluationCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateRiskEvaluationCommand, Guid>
{
    // Allowed values for indicator evaluation
    private static readonly HashSet<string> ValidValues = new(StringComparer.OrdinalIgnoreCase)
        { "Yes", "No", "N/A" };

    public async Task<Result<Guid>> Handle(
        CreateRiskEvaluationCommand command,
        CancellationToken cancellationToken)
    {
        // Check if customer exists in loan_details table
        bool customerExists = await context.Database
            .SqlQueryRaw<int>(
                "SELECT 1 FROM loan_details WHERE customer_number = {0} LIMIT 1",
                command.CustomerNumber)
            .AnyAsync(cancellationToken);

        if (!customerExists)
        {
            return Result.Failure<Guid>(
                RiskEvaluationErrors.CustomerNotFound(command.CustomerNumber));
        }

        // Prevent duplicate evaluation for same customer and date
        bool duplicateExists = await context.CustomerRiskEvaluations
            .AnyAsync(e =>
                e.CustomerNumber == command.CustomerNumber &&
                e.EvaluationDate.Date == command.EvaluationDate.Date,
                cancellationToken);

        if (duplicateExists)
        {
            return Result.Failure<Guid>(RiskEvaluationErrors.DuplicateEvaluation);
        }

        // Validate requested indicator IDs
        var indicatorIds = command.IndicatorEvaluations.Select(i => i.IndicatorId).ToList();

        List<Guid> existingIndicatorIds = await context.RiskIndicators
            .Where(r => indicatorIds.Contains(r.IndicatorId) && r.IsActive)
            .Select(r => r.IndicatorId)
            .ToListAsync(cancellationToken);

        var missingIndicators = indicatorIds.Except(existingIndicatorIds).ToList();
        if (missingIndicators.Any())
        {
            return Result.Failure<Guid>(
                RiskEvaluationErrors.IndicatorNotFound(default)); // simplified
        }

        // Validate values (Yes / No / N/A)
        foreach (IndicatorEvaluationItem item in command.IndicatorEvaluations)
        {
            if (!ValidValues.Contains(item.Value))
            {
                return Result.Failure<Guid>(RiskEvaluationErrors.InvalidValue(item.Value));
            }
        }

        // Build evaluation root entity
        var evaluation = new CustomerRiskEvaluation
        {
            EvaluationId = Guid.NewGuid(),
            CustomerNumber = command.CustomerNumber,
            EvaluationDate = command.EvaluationDate,
            EvaluatedBy = userContext.UserId,
            CreatedAt = dateTimeProvider.UtcNow
        };

        // Add evaluation details
        foreach (IndicatorEvaluationItem item in command.IndicatorEvaluations)
        {
            evaluation.IndicatorEvaluations.Add(new CustomerRiskIndicatorEvaluation
            {
                EvalDetailId = Guid.NewGuid(),
                IndicatorId = item.IndicatorId,
                Value = item.Value,
                CreatedAt = dateTimeProvider.UtcNow
            });
        }

        // Raise domain event for evaluation creation
        evaluation.Raise(new RiskEvaluationCreatedDomainEvent(evaluation.EvaluationId));

        // Save to database
        context.CustomerRiskEvaluations.Add(evaluation);
        await context.SaveChangesAsync(cancellationToken);

        // Return created evaluation ID
        return Result.Success(evaluation.EvaluationId);
    }
}
