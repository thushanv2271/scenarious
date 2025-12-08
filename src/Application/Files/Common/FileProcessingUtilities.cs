using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharedKernel;

namespace Application.Files.Common;

public static class FileProcessingUtilities
{
    public static string SanitizeFileNameWithoutExtension(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] sanitized = new char[name.Length];
        int idx = 0;

        foreach (char c in name)
        {
            sanitized[idx++] = invalid.Contains(c) ? '_' : c;
        }

        string result = new string(sanitized, 0, idx).Trim();
        return string.IsNullOrWhiteSpace(result) ? "upload" : result;
    }

    public static string ReplaceWhitespaceWithUnderscore(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);
        bool lastWasUnderscore = false;

        foreach (char ch in input)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasUnderscore)
                {
                    sb.Append('_');
                    lastWasUnderscore = true;
                }
            }
            else
            {
                sb.Append(ch);
                lastWasUnderscore = false;
            }
        }

        return sb.ToString();
    }

    public static Result<string> ValidateTimePeriod(string timePeriod, string configJson)
    {
        try
        {
            var configDoc = JsonDocument.Parse(configJson);
            JsonElement root = configDoc.RootElement;

            if (!root.TryGetProperty("pdSetup", out JsonElement pdSetup))
            {
                return Result.Failure<string>(Error.Problem(
                    "Config.Invalid",
                    "Configuration does not contain pdSetup section."));
            }

            if (!pdSetup.TryGetProperty("frequency", out JsonElement frequencyElement))
            {
                return Result.Failure<string>(Error.Problem(
                    "Config.NoFrequency",
                    "Configuration does not specify frequency."));
            }

            string frequency = frequencyElement.GetString() ?? "";

            return frequency.ToUpperInvariant() switch
            {
                "YEARLY" => ValidateYearlyTimePeriod(timePeriod, pdSetup),
                "QUARTERLY" => ValidateQuarterlyTimePeriod(timePeriod, pdSetup),
                "MONTHLY" => ValidateMonthlyTimePeriod(timePeriod, pdSetup),
                _ => Result.Failure<string>(Error.Problem(
                    "Config.UnsupportedFrequency",
                    $"Unsupported frequency: {frequency}"))
            };
        }
        catch (JsonException)
        {
            return Result.Failure<string>(Error.Problem(
                "Config.InvalidJson",
                "Configuration contains invalid JSON."));
        }
    }

    public static string CreateTimePeriodFolderPath(string parameterFolder, string timePeriod, string configJson)
    {
        try
        {
            var configDoc = JsonDocument.Parse(configJson);
            JsonElement root = configDoc.RootElement;

            if (!root.TryGetProperty("pdSetup", out JsonElement pdSetup) ||
                !pdSetup.TryGetProperty("frequency", out JsonElement frequencyElement))
            {
                return Path.Combine(parameterFolder, timePeriod);
            }

            string frequency = frequencyElement.GetString()?.ToUpperInvariant() ?? "";

            return frequency switch
            {
                "YEARLY" => Path.Combine(parameterFolder, timePeriod),
                "QUARTERLY" => CreateQuarterlyFolderPath(parameterFolder, timePeriod),
                "MONTHLY" => CreateMonthlyFolderPath(parameterFolder, timePeriod),
                _ => Path.Combine(parameterFolder, timePeriod)
            };
        }
        catch (JsonException)
        {
            return Path.Combine(parameterFolder, timePeriod);
        }
    }

    private static Result<string> ValidateYearlyTimePeriod(string timePeriod, JsonElement pdSetup)
    {
        if (!Regex.IsMatch(timePeriod, @"^\d{4}$"))
        {
            return Result.Failure<string>(Error.Problem(
                "TimePeriod.InvalidFormat",
                "For yearly frequency, time period must be in format YYYY (e.g., '2025')."));
        }

        return ValidateTimePeriodRange(timePeriod, pdSetup);
    }

    private static Result<string> ValidateQuarterlyTimePeriod(string timePeriod, JsonElement pdSetup)
    {
        if (!Regex.IsMatch(timePeriod, @"^\d{4}-Q[1-4]$"))
        {
            return Result.Failure<string>(Error.Problem(
                "TimePeriod.InvalidFormat",
                "For quarterly frequency, time period must be in format YYYY-QX (e.g., '2025-Q3')."));
        }

        return ValidateTimePeriodRange(timePeriod, pdSetup);
    }

    private static Result<string> ValidateMonthlyTimePeriod(string timePeriod, JsonElement pdSetup)
    {
        if (!Regex.IsMatch(timePeriod, @"^\d{4}-\d{2}$"))
        {
            return Result.Failure<string>(Error.Problem(
                "TimePeriod.InvalidFormat",
                "For monthly frequency, time period must be in format YYYY-MM (e.g., '2025-12')."));
        }

        return ValidateTimePeriodRange(timePeriod, pdSetup);
    }

    private static Result<string> ValidateTimePeriodRange(string timePeriod, JsonElement pdSetup)
    {
        if (!pdSetup.TryGetProperty("timePeriod", out JsonElement timePeriodConfig))
        {
            return Result.Success(timePeriod);
        }

        if (!timePeriodConfig.TryGetProperty("from", out JsonElement fromElement) ||
            !timePeriodConfig.TryGetProperty("to", out JsonElement toElement))
        {
            return Result.Success(timePeriod);
        }

        string fromPeriod = fromElement.GetString() ?? "";
        string toPeriod = toElement.GetString() ?? "";

        if (string.Compare(timePeriod, toPeriod, StringComparison.Ordinal) < 0 ||
            string.Compare(timePeriod, fromPeriod, StringComparison.Ordinal) > 0)
        {
            return Result.Failure<string>(Error.Problem(
                "TimePeriod.OutOfRange",
                $"Time period '{timePeriod}' is outside the configured range from '{fromPeriod}' to '{toPeriod}'."));
        }

        return Result.Success(timePeriod);
    }

    private static string CreateQuarterlyFolderPath(string parameterFolder, string timePeriod)
    {
        Match match = Regex.Match(timePeriod, @"^(\d{4})-Q([1-4])$");
        if (match.Success)
        {
            string year = match.Groups[1].Value;
            string quarter = $"Q{match.Groups[2].Value}";
            return Path.Combine(parameterFolder, year, quarter);
        }

        return Path.Combine(parameterFolder, timePeriod);
    }

    private static string CreateMonthlyFolderPath(string parameterFolder, string timePeriod)
    {
        Match match = Regex.Match(timePeriod, @"^(\d{4})-(\d{2})$");
        if (match.Success && int.TryParse(match.Groups[2].Value, out int monthNumber))
        {
            string year = match.Groups[1].Value;
            string monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(monthNumber);
            string monthAbbrev = monthName.Length > 3 ? monthName[..3] : monthName;
            return Path.Combine(parameterFolder, year, monthAbbrev);
        }

        return Path.Combine(parameterFolder, timePeriod);
    }
}