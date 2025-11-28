using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.CashFlowProjections.GetCustomerFacilities;

/// <summary>
/// Handler to retrieve all facilities for a customer
/// Uses repository to encapsulate data access
/// </summary>
internal sealed class GetCustomerFacilitiesQueryHandler(
    ILoanDetailsRepository loanRepository,
    ILogger<GetCustomerFacilitiesQueryHandler> logger)
    : IQueryHandler<GetCustomerFacilitiesQuery, List<CustomerFacilityResponse>>
{
    public async Task<Result<List<CustomerFacilityResponse>>> Handle(
        GetCustomerFacilitiesQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get facilities from repository
            List<CustomerFacilityDetail> facilities = await loanRepository
                .GetCustomerFacilitiesAsync(query.CustomerNumber, cancellationToken);

            if (facilities.Count == 0)
            {
                logger.LogWarning("No facilities found for customer: {CustomerNumber}", query.CustomerNumber);
                return Result.Failure<List<CustomerFacilityResponse>>(
                    Error.NotFound("CustomerFacilities.NotFound",
                        $"No facilities found for customer {query.CustomerNumber}"));
            }

            // Map to response
            var response = facilities.Select(f => new CustomerFacilityResponse
            {
                CustomerNumber = f.CustomerNumber,
                FacilityNumber = f.FacilityNumber,
                ProductCategory = f.ProductCategory,
                Segment = f.Segment,
                Branch = f.Branch,
                TotalOutstanding = f.TotalOutstanding,
                InterestRate = f.InterestRate,
                GrantDate = f.GrantDate,
                MaturityDate = f.MaturityDate,
                DaysPastDue = f.DaysPastDue,
                BucketLabel = f.BucketLabel
            }).ToList();

            logger.LogInformation("Found {Count} facilities for customer {CustomerNumber}",
                facilities.Count, query.CustomerNumber);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving facilities for customer {CustomerNumber}",
                query.CustomerNumber);
            return Result.Failure<List<CustomerFacilityResponse>>(
                Error.Failure("CustomerFacilities.RetrievalError",
                    $"Error retrieving facilities: {ex.Message}"));
        }
    }
}
