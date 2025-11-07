using Application.Abstractions.Messaging;

namespace Application.Industries.Create;

/// <summary>
/// Command to create multiple industries from a list of names.
/// </summary>
public sealed record CreateIndustryCommand(
    string[] Names
) : ICommand<CreateIndustryResponse>;