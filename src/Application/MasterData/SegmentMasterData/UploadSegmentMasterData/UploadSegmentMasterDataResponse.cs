namespace Application.MasterData.SegmentMasterData.UploadSegmentMasterData;

public sealed record UploadSegmentMasterDataResponse(
    string FileName,
    string FilePath,
    long Size,
    int RecordsLoaded,
    bool DatabaseRefreshed
);