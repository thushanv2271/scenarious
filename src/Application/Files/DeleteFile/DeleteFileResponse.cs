using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Files.DeleteFile;
/// <summary>
/// Response returned after successfully deleting a file.
/// Contains details of the deleted file for confirmation.
/// </summary>
public sealed record DeleteFileResponse(
    Guid Id,
    string OriginalFileName,
    string StoredFileName,
    long Size,
    string PhysicalPath,
    DateTime DeletedAt,
    Guid DeletedBy
);
