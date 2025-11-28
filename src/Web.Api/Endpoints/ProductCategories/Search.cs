using Application.ProductCategories;
using Domain.ProductCategories;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.ProductCategories;

internal sealed class Search : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("product-categories/search", async (
            string? type,
            [FromServices] SearchProductCategoriesAndSegmentsHandler handler,
            CancellationToken cancellationToken) =>
        {
            // Validate type parameter if provided
            if (!string.IsNullOrWhiteSpace(type) && !Enum.TryParse<ProductCategoryType>(type, true, out _))
            {
                var validationResult = Result.Failure<SearchProductCategoriesAndSegmentsResponse>(ProductCategoryErrors.InvalidType(type));
                return CustomResults.Problem(validationResult);
            }

            var command = new SearchProductCategoriesAndSegmentsCommand(type);
            Result<SearchProductCategoriesAndSegmentsResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.ProductCategories)
        .WithDescription("Search product categories and segments with optional type filter (PD or LGD)")
        .WithSummary("Search Product Categories and Segments");
    }
}