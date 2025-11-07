using Application.ProductCategories;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.ProductCategories;

internal sealed class UploadCsv : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("product-categories/upload-csv", async (
            IFormFile file,
            UploadProductCategoriesAndSegmentsHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest("Please provide a valid CSV file.");
            }

            if (!file.ContentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("Only CSV files are allowed.");
            }

            using Stream stream = file.OpenReadStream();
            Result<CsvUploadResult> result = await handler.Handle(stream, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithTags(Tags.ProductCategories)
        .WithDescription("Upload CSV file with product categories and segments")
        .WithSummary("Upload Product Categories and Segments from CSV");
    }
}