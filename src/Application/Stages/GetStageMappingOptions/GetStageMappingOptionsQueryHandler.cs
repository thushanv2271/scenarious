using Application.Abstractions.Messaging;
using Domain.Stages;
using SharedKernel;

namespace Application.Stages.GetStageMappingOptions;

/// <summary>
/// Handler for getting stage mapping options
/// </summary>
internal sealed class GetStageMappingOptionsQueryHandler 
    : IQueryHandler<GetStageMappingOptionsQuery, GetStageMappingOptionsResponse>
{
    public Task<Result<GetStageMappingOptionsResponse>> Handle(
        GetStageMappingOptionsQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<StageMappingOptionDto> options = StageMappingOption.All
            .Select(option => new StageMappingOptionDto(
                MapStageToValue(option.Value),
                option.Label));

        var response = new GetStageMappingOptionsResponse(options);
        
        return Task.FromResult(Result.Success(response));
    }

    private static string MapStageToValue(Stage stage) => stage switch
    {
        Stage.One => "stage1",
        Stage.Two => "stage2", 
        Stage.Three => "stage3",
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Invalid stage value")
    };
}