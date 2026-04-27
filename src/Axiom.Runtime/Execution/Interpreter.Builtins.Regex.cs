using System.Text;
using System.Text.RegularExpressions;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private static readonly TimeSpan RegexExecutionLimit = TimeSpan.FromSeconds(5);

    private RuntimeValue EvaluateRegexIsMatch(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXISMATCH", pattern, modes, modesArgumentIndex);

        try
        {
            return RuntimeValue.FromBoolean(regex.IsMatch(text));
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout(expression, "REGEXISMATCH");
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexMatch(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXMATCH", pattern, modes, modesArgumentIndex);

        try
        {
            var match = regex.Match(text);
            return RuntimeValue.FromString(match.Success ? match.Value : string.Empty);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout(expression, "REGEXMATCH");
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexExtract(
        BoundCallExpression expression,
        string text,
        string pattern,
        int group,
        string modes,
        int modesArgumentIndex)
    {
        if (group < 0)
        {
            ReportBuiltInArgumentError(
                "REGEXEXTRACT",
                "group",
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }

        var regex = CreateRegex(expression, "REGEXEXTRACT", pattern, modes, modesArgumentIndex);

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
            ReportRegexTimeout(expression, "REGEXEXTRACT");
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexMatches(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXMATCHES", pattern, modes, modesArgumentIndex);

        try
        {
            var values = regex
                .Matches(text)
                .Select(static match => match.Value)
                .ToArray();

            return CreateStringArrayValue(values);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout(expression, "REGEXMATCHES");
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexExtractAll(
        BoundCallExpression expression,
        string text,
        string pattern,
        int group,
        string modes,
        int modesArgumentIndex)
    {
        if (group < 0)
        {
            ReportBuiltInArgumentError(
                "REGEXEXTRACTALL",
                "group",
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }

        var regex = CreateRegex(expression, "REGEXEXTRACTALL", pattern, modes, modesArgumentIndex);

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

            return CreateStringArrayValue(values);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout(expression, "REGEXEXTRACTALL");
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexSplit(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXSPLIT", pattern, modes, modesArgumentIndex);

        try
        {
            return text.Length == 0
                ? CreateStringArrayValue([])
                : CreateStringArrayValue(regex.Split(text));
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout(expression, "REGEXSPLIT");
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexReplace(
        BoundCallExpression expression,
        string text,
        string pattern,
        string replacement,
        string modes,
        int? count,
        int modesArgumentIndex,
        int countArgumentIndex)
    {
        if (count is < 0)
        {
            ReportBuiltInArgumentError(
                "REGEXREPLACE",
                "count",
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, countArgumentIndex));
            throw new RuntimeFailureException();
        }

        if (count == 0)
        {
            return RuntimeValue.FromString(text);
        }

        var regex = CreateRegex(expression, "REGEXREPLACE", pattern, modes, modesArgumentIndex);

        try
        {
            var result = count is null
                ? regex.Replace(text, replacement)
                : ReplaceRegexMatches(regex, text, replacement, count.Value);

            return RuntimeValue.FromString(result);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout(expression, "REGEXREPLACE");
            throw new RuntimeFailureException();
        }
        catch (ArgumentException ex)
        {
            ReportBuiltInArgumentError(
                "REGEXREPLACE",
                "replacement",
                $"содержит недопустимую ссылку на группу: {ex.Message}",
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexCount(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXCOUNT", pattern, modes, modesArgumentIndex);

        try
        {
            return RuntimeValue.FromInteger(regex.Matches(text).Count);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout(expression, "REGEXCOUNT");
            throw new RuntimeFailureException();
        }
    }

    private Regex CreateRegex(
        BoundCallExpression expression,
        string functionName,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var options = ParseRegexModes(expression, functionName, modes, modesArgumentIndex);

        try
        {
            return new Regex(pattern, options, RegexExecutionLimit);
        }
        catch (ArgumentException ex)
        {
            ReportBuiltInArgumentError(
                functionName,
                "pattern",
                $"содержит недопустимое регулярное выражение: {ex.Message}",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }
    }

    private RegexOptions ParseRegexModes(
        BoundCallExpression expression,
        string functionName,
        string modes,
        int modesArgumentIndex)
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
                _ => ReportInvalidRegexMode(expression, functionName, mode, modesArgumentIndex)
            };
        }

        return options;
    }

    private RegexOptions ReportInvalidRegexMode(
        BoundCallExpression expression,
        string functionName,
        char mode,
        int modesArgumentIndex)
    {
        ReportBuiltInArgumentError(
            functionName,
            "modes",
            $"содержит неизвестный режим '{mode}'. Допустимы только i, m, s, x.",
            GetBuiltInArgumentLocation(expression, modesArgumentIndex));
        throw new RuntimeFailureException();
    }

    private void ReportRegexTimeout(BoundCallExpression expression, string functionName)
    {
        ReportBuiltInArgumentError(
            functionName,
            "pattern",
            "выполняется слишком долго.",
            GetBuiltInArgumentLocation(expression, 1));
    }

    private static string ReplaceRegexMatches(Regex regex, string text, string replacement, int count)
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
}
