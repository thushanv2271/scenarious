using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;

namespace Application.Files.DeleteFile;
/// <summary>
/// Command to delete an uploaded file, including both binary data and metadata.
/// </summary>
/// <param name="Id">The unique identifier of the file to delete.</param>
/// <param name="DeletedBy">The unique identifier of the user requesting the deletion.</param>
public sealed record DeleteFileCommand(
    List<Guid> Ids,
    Guid DeletedBy
) : ICommand<List<DeleteFileResponse>>;
