using System.Text;
using System.Text.RegularExpressions;
using Axiom.Core;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinRegexHelpers
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static RegexOptions ParseRegexModes(
        string modes,
        TextLocation location,
        string functionName,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        var options = RegexOptions.CultureInvariant;

        foreach (var mode in modes)
        {
            options |= char.ToLowerInvariant(mode) switch
            {
                'i' => RegexOptions.IgnoreCase,
                'm' => RegexOptions.Multiline,
                's' => RegexOptions.Singleline,
                'x' => RegexOptions.IgnorePatternWhitespace,
                _ => throw ReportAndThrowInvalidMode(functionName, mode, location, reportArgumentError)
            };
        }

        return options;
    }

    public static Regex CreateRegex(
        string pattern,
        RegexOptions options,
        TextLocation location,
        string functionName,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        try
        {
            return new Regex(pattern, options, DefaultTimeout);
        }
        catch (ArgumentException ex)
        {
            reportArgumentError(
                functionName,
                "pattern",
                $"содержит недопустимое регулярное выражение: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue IsMatch(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError, string functionName)
    {
        try
        {
            return RuntimeValue.FromBoolean(regex.IsMatch(text));
        }
        catch (RegexMatchTimeoutException)
        {
            reportArgumentError(functionName, "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue Match(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError, string functionName)
    {
        try
        {
            var match = regex.Match(text);
            return RuntimeValue.FromString(match.Success ? match.Value : string.Empty);
        }
        catch (RegexMatchTimeoutException)
        {
            reportArgumentError(functionName, "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue Extract(Regex regex, string text, int group, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError, string functionName,
        Func<string[], RuntimeValue> createStringArray)
    {
        try
        {
            var match = regex.Match(text);

            if (!match.Success || group >= match.Groups.Count)
            {
                return RuntimeValue.FromString(string.Empty);
            }

            var capturedGroup = match.Groups[group];
            return RuntimeValue.FromString(capturedGroup.Success ? capturedGroup.Value : string.Empty);
        }
        catch (RegexMatchTimeoutException)
        {
            reportArgumentError(functionName, "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue Matches(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError, string functionName,
        Func<string[], RuntimeValue> createStringArray)
    {
        try
        {
            var values = regex
                .Matches(text)
                .Select(static match => match.Value)
                .ToArray();

            return createStringArray(values);
        }
        catch (RegexMatchTimeoutException)
        {
            reportArgumentError(functionName, "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue ExtractAll(Regex regex, string text, int group, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError, string functionName,
        Func<string[], RuntimeValue> createStringArray)
    {
        try
        {
            var values = regex
                .Matches(text)
                .Select(match =>
                {
                    if (group >= match.Groups.Count)
                    {
                        return string.Empty;
                    }

                    var capturedGroup = match.Groups[group];
                    return capturedGroup.Success ? capturedGroup.Value : string.Empty;
                })
                .ToArray();

            return createStringArray(values);
        }
        catch (RegexMatchTimeoutException)
        {
            reportArgumentError(functionName, "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue Split(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError, string functionName,
        Func<string[], RuntimeValue> createStringArray)
    {
        try
        {
            return text.Length == 0
                ? createStringArray([])
                : createStringArray(regex.Split(text));
        }
        catch (RegexMatchTimeoutException)
        {
            reportArgumentError(functionName, "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
    }

    public static string Replace(Regex regex, string text, string replacement, int? count, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError, string functionName)
    {
        try
        {
            return count is null
                ? regex.Replace(text, replacement)
                : ReplaceRegexMatches(regex, text, replacement, count.Value);
        }
        catch (RegexMatchTimeoutException)
        {
            reportArgumentError(functionName, "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
        catch (ArgumentException ex)
        {
            reportArgumentError(
                functionName,
                "replacement",
                $"содержит недопустимую ссылку на группу: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static int Count(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError, string functionName)
    {
        try
        {
            return regex.Matches(text).Count;
        }
        catch (RegexMatchTimeoutException)
        {
            reportArgumentError(functionName, "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
    }

    public static string ReplaceRegexMatches(Regex regex, string text, string replacement, int count)
    {
        var matches = regex.Matches(text);

        if (matches.Count == 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        var cursor = 0;
        var replaced = 0;

        foreach (Match match in matches)
        {
            if (replaced >= count)
            {
                break;
            }

            builder.Append(text, cursor, match.Index - cursor);
            builder.Append(match.Result(replacement));

            cursor = match.Index + match.Length;
            replaced++;
        }

        builder.Append(text, cursor, text.Length - cursor);
        return builder.ToString();
    }

    private static RuntimeFailureException ReportAndThrowInvalidMode(
        string functionName, char mode, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        reportArgumentError(
            functionName,
            "modes",
            $"содержит неизвестный режим '{mode}'. Допустимы только i, m, s, x.",
            location);
        return new RuntimeFailureException();
    }
}
