using SharedKernel;

namespace Domain.FacilityCashFlowTypes;

/// <summary>
/// Error definitions for facility cash flow type operations
/// </summary>
public static class FacilityCashFlowTypeErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "FacilityCashFlowType.NotFound",
        $"The facility cash flow type with ID '{id}' was not found");

    public static Error FacilityNotFound(string facilityNumber) => Error.NotFound(
        "FacilityCashFlowType.FacilityNotFound",
        $"The facility with number '{facilityNumber}' was not found in loan_details");

    public static Error SegmentNotFound(Guid segmentId) => Error.NotFound(
        "FacilityCashFlowType.SegmentNotFound",
        $"The segment with ID '{segmentId}' was not found");

    public static Error ScenarioNotFound(Guid scenarioId) => Error.NotFound(
        "FacilityCashFlowType.ScenarioNotFound",
        $"The scenario with ID '{scenarioId}' was not found");

    public static Error ScenarioNotLinkedToSegment => Error.Problem(
        "FacilityCashFlowType.ScenarioNotLinkedToSegment",
        "The specified scenario is not linked to the facility's segment");

    public static Error DuplicateActiveCashFlowType => Error.Conflict(
        "FacilityCashFlowType.DuplicateActiveCashFlowType",
        "An active cash flow type already exists for this facility and scenario combination");

    public static Error InvalidConfiguration(string message) => Error.Problem(
        "FacilityCashFlowType.InvalidConfiguration",
        $"Invalid configuration: {message}");

    public static Error FacilitySegmentMismatch => Error.Problem(
        "FacilityCashFlowType.FacilitySegmentMismatch",
        "The facility's segment in loan_details does not match the provided segment");
}
