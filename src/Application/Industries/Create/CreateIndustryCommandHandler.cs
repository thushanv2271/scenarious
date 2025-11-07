using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Industries;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System.Globalization;

namespace Application.Industries.Create;

internal sealed class CreateIndustryCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateIndustryCommand, CreateIndustryResponse>
{
    public async Task<Result<CreateIndustryResponse>> Handle(
        CreateIndustryCommand command,
        CancellationToken cancellationToken)
    {
        // Validate input array
        if (command.Names is null || command.Names.Length == 0)
        {
            return Result.Failure<CreateIndustryResponse>(
                IndustryErrors.EmptyArray);
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
            return Result.Success(new CreateIndustryResponse(
                Success: true,
                TotalProcessed: command.Names.Length,
                CreatedCount: 0,
                SkippedCount: skippedNames.Count,
                CreatedIndustries: Array.Empty<CreatedIndustry>(),
                SkippedNames: skippedNames
            ));
        }

        // Get existing industry names (case-insensitive)
        List<string> existingNames = await context.Industries
            .Where(i => validNames.Contains(i.Name))
            .Select(i => i.Name.ToUpperInvariant())
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

        var createdIndustries = new List<CreatedIndustry>();

        // Create new industries
        if (namesToCreate.Count > 0)
        {
            DateTime now = dateTimeProvider.UtcNow;
            var industriesToAdd = namesToCreate.Select(name => new Industry
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            context.Industries.AddRange(industriesToAdd);
            await context.SaveChangesAsync(cancellationToken);

            createdIndustries.AddRange(industriesToAdd.Select(i => new CreatedIndustry(
                i.Id,
                i.Name,
                i.CreatedAt
            )));
        }

        return Result.Success(new CreateIndustryResponse(
            Success: true,
            TotalProcessed: command.Names.Length,
            CreatedCount: createdIndustries.Count,
            SkippedCount: skippedNames.Count,
            CreatedIndustries: createdIndustries,
            SkippedNames: skippedNames
        ));
    }
}