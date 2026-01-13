using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.IndividualImpairment;

/// <summary>
/// Endpoint to retrieve calculation history for a customer or facility
/// </summary>
internal sealed class GetCalculationHistory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("individual-impairment/history", async (
            string? customerNumber,
            string? facilityNumber,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            IApplicationDbContext context,
            CancellationToken cancellationToken) =>
        {
            IQueryable<Domain.IndividualImpairment.IndividualImpairmentCalculation> query =
                context.IndividualImpairmentCalculations.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(customerNumber))
            {
                query = query.Where(c => c.CustomerNumber == customerNumber);
            }

            if (!string.IsNullOrWhiteSpace(facilityNumber))
            {
                query = query.Where(c => c.FacilityNumber == facilityNumber);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(c => c.CalculationDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(c => c.CalculationDate <= toDate.Value);
            }

            int totalCount = await query.CountAsync(cancellationToken);

            var calculations = await query
                .OrderByDescending(c => c.CalculationDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.FacilityNumber,
                    c.CustomerNumber,
                    c.CalculationDate,
                    c.InterestRate,
                    c.AmortizedCost,
                    c.SumOfPVOfCashFlows,
                    c.ImpairmentAmount,
                    c.ImpairmentPercentage,
                    c.CalculatedBy,
                    c.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var response = new
            {
                Data = calculations,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisAccess)
        .WithTags(Tags.IndividualImpairment)
        .WithName("GetCalculationHistory")
        .WithDescription("Retrieve calculation history with pagination and filtering");
    }
}
