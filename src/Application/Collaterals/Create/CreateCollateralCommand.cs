using Application.Abstractions.Messaging;

namespace Application.Collaterals.Create;

/// <summary>
/// Command to create multiple collaterals from a list of names.
/// </summary>
public sealed record CreateCollateralCommand(
    string[] Names
) : ICommand<CreateCollateralResponse>;