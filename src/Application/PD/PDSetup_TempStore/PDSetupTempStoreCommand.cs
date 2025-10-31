using Application.Abstractions.Messaging;
using System.Text.Json.Nodes;

namespace Application.PD.PDSetup_TempStore;

/// <summary>
/// Command to temporarily store PD setup steps as JSON.
/// </summary>
/// <param name="StepsJson">The JSON object representing the steps.</param>
public sealed record PDSetupTempStoreCommand(JsonObject StepsJson, Guid userId) : ICommand;
