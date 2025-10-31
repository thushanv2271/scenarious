namespace Application.MasterData.SegmentMasterData;

public sealed class SegmentListResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<string> SubSegments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
