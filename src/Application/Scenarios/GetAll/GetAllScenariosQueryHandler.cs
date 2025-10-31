using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Scenarios.GetAll;

/// <summary>
/// Handles retrieving scenarios grouped by product category and segment
/// </summary>
internal sealed class GetAllScenariosQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetAllScenariosQuery, List<ProductCategoryWithScenariosResponse>>
{
    public async Task<Result<List<ProductCategoryWithScenariosResponse>>> Handle(
        GetAllScenariosQuery request,
        CancellationToken cancellationToken)
    {
        // Start with product categories query
        IQueryable<Domain.ProductCategories.ProductCategory> productCategoriesQuery = context.ProductCategories.AsQueryable();

        // Filter by ProductCategoryId if provided
        if (request.ProductCategoryId.HasValue)
        {
            productCategoriesQuery = productCategoriesQuery
                .Where(pc => pc.Id == request.ProductCategoryId.Value);
        }

        // Get all product categories with their segments and scenarios
        List<ProductCategoryWithScenariosResponse> result = await productCategoriesQuery
            .OrderBy(pc => pc.Name)
            .Select(pc => new ProductCategoryWithScenariosResponse
            {
                ProductCategoryId = pc.Id,
                ProductCategoryName = pc.Name,
                Segments = context.Segments
                    .Where(seg => seg.ProductCategoryId == pc.Id)
                    .Where(seg => !request.SegmentId.HasValue || seg.Id == request.SegmentId.Value)
                    .OrderBy(seg => seg.Name)
                    .Select(seg => new SegmentWithScenariosResponse
                    {
                        SegmentId = seg.Id,
                        SegmentName = seg.Name,
                        Scenarios = context.Scenarios
                            .Where(sc => sc.SegmentId == seg.Id)
                            .OrderBy(sc => sc.ScenarioName)
                            .Select(sc => new ScenarioDetailResponse
                            {
                                Id = sc.Id,
                                ScenarioName = sc.ScenarioName,
                                Probability = sc.Probability,
                                ContractualCashFlowsEnabled = sc.ContractualCashFlowsEnabled,
                                LastQuarterCashFlowsEnabled = sc.LastQuarterCashFlowsEnabled,
                                OtherCashFlowsEnabled = sc.OtherCashFlowsEnabled,
                                CollateralValueEnabled = sc.CollateralValueEnabled,
                                UploadedFileId = sc.UploadedFileId,
                                UploadedFile = sc.UploadedFileId != null
                                    ? context.UploadedFiles
                                        .Where(f => f.Id == sc.UploadedFileId)
                                        .Select(f => new UploadedFileInfo
                                        {
                                            Id = f.Id,
                                            OriginalFileName = f.OriginalFileName,
                                            StoredFileName = f.StoredFileName,
                                            ContentType = f.ContentType,
                                            Size = f.Size,
                                            Url = f.PublicUrl,
                                            UploadedBy = f.UploadedBy,
                                            UploadedAt = f.UploadedAt
                                        })
                                        .FirstOrDefault()
                                    : null,
                                CreatedAt = sc.CreatedAt,
                                UpdatedAt = sc.UpdatedAt
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        // Remove product categories with no segments (when filtered by SegmentId)
        if (request.SegmentId.HasValue)
        {
            result = result.Where(pc => pc.Segments.Any()).ToList();
        }

        return Result.Success(result);
    }
}
