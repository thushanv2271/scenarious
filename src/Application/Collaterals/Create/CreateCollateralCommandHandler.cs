using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Collaterals;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Collaterals.Create;

internal sealed class CreateCollateralCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateCollateralCommand, CreateCollateralResponse>
{
    public async Task<Result<CreateCollateralResponse>> Handle(
        CreateCollateralCommand command,
        CancellationToken cancellationToken)
    {
        // Validate input array
        if (command.Names is null || command.Names.Length == 0)
        {
            return Result.Failure<CreateCollateralResponse>(
                CollateralErrors.EmptyArray);
        }

        // Filter and validate names
        var validNames = new List<string>();
        var skippedNames = new List<string>();

        foreach (string name in command.Names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                skippedNames.Add(name ?? string.Empty);
                continue;
            }

            string trimmedName = name.Trim();
            if (trimmedName.Length > 255)
            {
                skippedNames.Add(trimmedName);
                continue;
            }

            validNames.Add(trimmedName);
        }

        // If no valid names, return success with skipped items
        if (validNames.Count == 0)
        {
            return Result.Success(new CreateCollateralResponse(
                Success: true,
                TotalProcessed: command.Names.Length,
                CreatedCount: 0,
                SkippedCount: skippedNames.Count,
                CreatedCollaterals: Array.Empty<CreatedCollateral>(),
                SkippedNames: skippedNames
            ));
        }

        // Get existing collateral names (case-insensitive)
        List<string> existingNames = await context.Collaterals
            .Where(c => validNames.Contains(c.Name))
            .Select(c => c.Name.ToUpperInvariant())
            .ToListAsync(cancellationToken);

        // Filter out existing names (case-insensitive comparison)
        var namesToCreate = validNames
            .Where(name => !existingNames.Contains(name.ToUpperInvariant()))
            .ToList();

        // Add existing names to skipped
        var duplicateNames = validNames
            .Where(name => existingNames.Contains(name.ToUpperInvariant()))
            .ToList();
        skippedNames.AddRange(duplicateNames);

        var createdCollaterals = new List<CreatedCollateral>();

        // Create new collaterals
        if (namesToCreate.Count > 0)
        {
            DateTime now = dateTimeProvider.UtcNow;
            var collateralsToAdd = namesToCreate.Select(name => new Collateral
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            context.Collaterals.AddRange(collateralsToAdd);
            await context.SaveChangesAsync(cancellationToken);

            createdCollaterals.AddRange(collateralsToAdd.Select(c => new CreatedCollateral(
                c.Id,
                c.Name,
                c.CreatedAt
            )));
        }

        return Result.Success(new CreateCollateralResponse(
            Success: true,
            TotalProcessed: command.Names.Length,
            CreatedCount: createdCollaterals.Count,
            SkippedCount: skippedNames.Count,
            CreatedCollaterals: createdCollaterals,
            SkippedNames: skippedNames
        ));
    }
}