using System.Text.RegularExpressions;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private readonly Dictionary<(string Pattern, RegexOptions Options), Regex> _regexCache = [];
    private readonly Dictionary<string, RegexOptions> _regexModesCache = [];

    private RuntimeValue EvaluateRegexIsMatchBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXISMATCH", pattern, modes, location);
        return BuiltinRegexHelpers.IsMatch(regex, text, location, ReportBuiltInArgumentError, "REGEXISMATCH");
    }

    private RuntimeValue EvaluateRegexMatchBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXMATCH", pattern, modes, location);
        return BuiltinRegexHelpers.Match(regex, text, location, ReportBuiltInArgumentError, "REGEXMATCH");
    }

    private RuntimeValue EvaluateRegexExtractBuiltin(
        string text,
        string pattern,
        int group,
        string modes,
        TextLocation location)
    {
        if (group < 0)
        {
            ReportBuiltInArgumentError("REGEXEXTRACT", "group", "не может быть отрицательным.", location);
            throw new RuntimeFailureException();
        }

        var regex = CreateRegex("REGEXEXTRACT", pattern, modes, location);
        return BuiltinRegexHelpers.Extract(regex, text, group, location, ReportBuiltInArgumentError, "REGEXEXTRACT", CreateStringArrayValue);
    }

    private RuntimeValue EvaluateRegexMatchesBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXMATCHES", pattern, modes, location);
        return BuiltinRegexHelpers.Matches(regex, text, location, ReportBuiltInArgumentError, "REGEXMATCHES", CreateStringArrayValue);
    }

    private RuntimeValue EvaluateRegexExtractAllBuiltin(
        string text,
        string pattern,
        int group,
        string modes,
        TextLocation location)
    {
        if (group < 0)
        {
            ReportBuiltInArgumentError("REGEXEXTRACTALL", "group", "не может быть отрицательным.", location);
            throw new RuntimeFailureException();
        }

        var regex = CreateRegex("REGEXEXTRACTALL", pattern, modes, location);
        return BuiltinRegexHelpers.ExtractAll(regex, text, group, location, ReportBuiltInArgumentError, "REGEXEXTRACTALL", CreateStringArrayValue);
    }

    private RuntimeValue EvaluateRegexSplitBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXSPLIT", pattern, modes, location);
        return BuiltinRegexHelpers.Split(regex, text, location, ReportBuiltInArgumentError, "REGEXSPLIT", CreateStringArrayValue);
    }

    private RuntimeValue EvaluateRegexReplaceBuiltin(
        RuntimeValue textValue,
        RuntimeValue patternValue,
        RuntimeValue replacementValue,
        string modes,
        int? count,
        TextLocation location)
    {
        ValidateRegexReplaceCount(count, location);

        if (count == 0)
        {
            return textValue;
        }

        var text = GetStringUnchecked(textValue);
        var regex = CreateRegex("REGEXREPLACE", GetStringUnchecked(patternValue), modes, location);
        return EvaluateRegexReplaceWithCachedRegex(
            textValue,
            text,
            regex,
            GetStringUnchecked(replacementValue),
            count,
            location);
    }

    private string EvaluateRegexReplaceCore(
        string text,
        string pattern,
        string replacement,
        string modes,
        int? count,
        TextLocation location)
    {
        ValidateRegexReplaceCount(count, location);

        if (count == 0)
        {
            return text;
        }

        var regex = CreateRegex("REGEXREPLACE", pattern, modes, location);
        return EvaluateRegexReplaceWithCachedRegex(text, regex, replacement, count, location);
    }

    private RuntimeValue EvaluateRegexReplaceWithCachedRegex(
        RuntimeValue textValue,
        string text,
        Regex regex,
        string replacement,
        int? count,
        TextLocation location)
    {
        try
        {
            if (IsRegexReplaceGuaranteedNoOp(text, regex, replacement, count))
            {
                return textValue;
            }

            var result = BuiltinRegexHelpers.Replace(regex, text, replacement, count, location, ReportBuiltInArgumentError, "REGEXREPLACE");

            return string.Equals(result, text, StringComparison.Ordinal)
                ? textValue
                : RuntimeValue.FromString(result);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportBuiltInArgumentError("REGEXREPLACE", "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
        catch (ArgumentException ex)
        {
            ReportBuiltInArgumentError(
                "REGEXREPLACE",
                "replacement",
                $"содержит недопустимую ссылку на группу: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private string EvaluateRegexReplaceWithCachedRegex(
        string text,
        Regex regex,
        string replacement,
        int? count,
        TextLocation location)
    {
        try
        {
            return IsRegexReplaceGuaranteedNoOp(text, regex, replacement, count)
                ? text
                : BuiltinRegexHelpers.Replace(regex, text, replacement, count, location, ReportBuiltInArgumentError, "REGEXREPLACE");
        }
        catch (RegexMatchTimeoutException)
        {
            ReportBuiltInArgumentError("REGEXREPLACE", "pattern", "выполняется слишком долго.", location);
            throw new RuntimeFailureException();
        }
        catch (ArgumentException ex)
        {
            ReportBuiltInArgumentError(
                "REGEXREPLACE",
                "replacement",
                $"содержит недопустимую ссылку на группу: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexCountBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXCOUNT", pattern, modes, location);
        return RuntimeValue.FromInteger(BuiltinRegexHelpers.Count(regex, text, location, ReportBuiltInArgumentError, "REGEXCOUNT"));
    }

    private RuntimeValue EvaluateRegexReplaceBuiltin(
        string text,
        string pattern,
        string replacement,
        string modes,
        int? count,
        TextLocation location)
    {
        return RuntimeValue.FromString(EvaluateRegexReplaceCore(text, pattern, replacement, modes, count, location));
    }

    private void ValidateRegexReplaceCount(int? count, TextLocation location)
    {
        if (count is < 0)
        {
            ReportBuiltInArgumentError("REGEXREPLACE", "count", "не может быть отрицательным.", location);
            throw new RuntimeFailureException();
        }
    }

    private Regex CreateRegex(string functionName, string pattern, string modes, TextLocation location)
    {
        var options = ParseRegexModes(functionName, modes, location);
        var cacheKey = (pattern, options);

        if (_regexCache.TryGetValue(cacheKey, out var cachedRegex))
        {
            return cachedRegex;
        }

        var regex = BuiltinRegexHelpers.CreateRegex(pattern, options, location, functionName, ReportBuiltInArgumentError);
        _regexCache[cacheKey] = regex;
        return regex;
    }

    private RegexOptions ParseRegexModes(string functionName, string modes, TextLocation location)
    {
        if (modes.Length == 0)
        {
            return RegexOptions.CultureInvariant;
        }

        if (_regexModesCache.TryGetValue(modes, out var cachedOptions))
        {
            return cachedOptions;
        }

        var options = BuiltinRegexHelpers.ParseRegexModes(modes, location, functionName, ReportBuiltInArgumentError);
        _regexModesCache[modes] = options;
        return options;
    }

    private static bool IsRegexReplaceGuaranteedNoOp(string text, Regex regex, string replacement, int? count)
    {
        if (ReplacementDependsOnMatch(replacement))
        {
            return false;
        }

        var remaining = count ?? int.MaxValue;
        var match = regex.Match(text);

        if (!match.Success)
        {
            return true;
        }

        while (match.Success && remaining > 0)
        {
            if (!string.Equals(match.Value, replacement, StringComparison.Ordinal))
            {
                return false;
            }

            remaining--;
            if (remaining == 0)
            {
                return true;
            }

            match = match.NextMatch();
        }

        return true;
    }

    private static bool ReplacementDependsOnMatch(string replacement) => replacement.IndexOf('$') >= 0;
}
