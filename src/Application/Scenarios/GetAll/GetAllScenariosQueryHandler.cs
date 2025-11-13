using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Scenarios.GetAll;

/// <summary>
/// Optimized handler that retrieves scenarios grouped by product category and segment
/// Uses eager loading to avoid N+1 query problems
/// </summary>
internal sealed class GetAllScenariosQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetAllScenariosQuery, List<ProductCategoryWithScenariosResponse>>
{
    public async Task<Result<List<ProductCategoryWithScenariosResponse>>> Handle(
        GetAllScenariosQuery request,
        CancellationToken cancellationToken)
    {
        // Build the base query with filters
        IQueryable<Domain.ProductCategories.ProductCategory> query = context.ProductCategories
            .AsNoTracking();

        // Filter by ProductCategoryId if provided
        if (request.ProductCategoryId.HasValue)
        {
            query = query.Where(pc => pc.Id == request.ProductCategoryId.Value);
        }

        // Eager load the entire graph in ONE database query
        List<ProductCategoryWithScenariosResponse> result = await query
            .Include(pc => pc.Segments.Where(seg =>
                !request.SegmentId.HasValue || seg.Id == request.SegmentId.Value))
                .ThenInclude(seg => seg.Scenarios)
                .ThenInclude(sc => sc.UploadedFile)
            .OrderBy(pc => pc.Name)
            .Select(pc => new ProductCategoryWithScenariosResponse
            {
                ProductCategoryId = pc.Id,
                ProductCategoryName = pc.Name,
                Segments = pc.Segments
                    .OrderBy(seg => seg.Name)
                    .Select(seg => new SegmentWithScenariosResponse
                    {
                        SegmentId = seg.Id,
                        SegmentName = seg.Name,
                        Scenarios = seg.Scenarios
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
                                UploadedFile = sc.UploadedFile != null
                                    ? new UploadedFileInfo
                                    {
                                        Id = sc.UploadedFile.Id,
                                        OriginalFileName = sc.UploadedFile.OriginalFileName,
                                        StoredFileName = sc.UploadedFile.StoredFileName,
                                        ContentType = sc.UploadedFile.ContentType,
                                        Size = sc.UploadedFile.Size,
                                        Url = sc.UploadedFile.PublicUrl,
                                        UploadedBy = sc.UploadedFile.UploadedBy,
                                        UploadedAt = sc.UploadedFile.UploadedAt
                                    }
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
