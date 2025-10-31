using Application.Abstractions.Messaging;

namespace Application.MasterData.SegmentMasterData.UploadSegmentMasterData;

/// <summary>
/// Represents a command to upload the SegmentMasterData.xlsx file.
/// </summary>
/// <remarks>This command encapsulates the details required to upload the segment master data Excel file,
/// including the uploader's identifier and the binary content of the file. The file will be saved to the 
/// configured SegmentMasterDataPath location.</remarks>
/// <param name="UploadedBy">The unique identifier of the user uploading the file.</param>
/// <param name="FileName">The name of the file being uploaded (should be SegmentMasterData.xlsx).</param>
/// <param name="Content">The binary content of the Excel file as a byte array.</param>
public sealed record UploadSegmentMasterDataCommand(
    Guid UploadedBy,
    string FileName,
    byte[] Content
) : ICommand<UploadSegmentMasterDataResponse>;