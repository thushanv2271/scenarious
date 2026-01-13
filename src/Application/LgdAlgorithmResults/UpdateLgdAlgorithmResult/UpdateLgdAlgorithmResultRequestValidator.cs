using FluentValidation;

namespace Application.LgdAlgorithmResults.UpdateLgdAlgorithmResult;

/// <summary>
/// Validator for UpdateLgdAlgorithmResultRequest
/// </summary>
internal sealed class UpdateLgdAlgorithmResultRequestValidator : AbstractValidator<UpdateLgdAlgorithmResultRequest>
{
    public UpdateLgdAlgorithmResultRequestValidator()
    {
        RuleFor(x => x.LgdAlgorithmResultData)
            .NotEmpty()
            .WithMessage("LGD Algorithm Result data is required")
            .Must(BeValidJson)
            .WithMessage("LGD Algorithm Result data must be valid JSON");
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}