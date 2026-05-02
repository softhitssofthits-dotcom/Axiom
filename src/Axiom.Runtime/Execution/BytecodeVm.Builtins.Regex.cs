using System.Text.RegularExpressions;
using Axiom.Core;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static readonly TimeSpan RegexExecutionLimit = TimeSpan.FromSeconds(5);
    private readonly Dictionary<(string Pattern, RegexOptions Options), Regex> _regexCache = [];
    private readonly Dictionary<string, RegexOptions> _regexModesCache = [];

    private RuntimeValue EvaluateRegexIsMatchBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXISMATCH", pattern, modes, location);

        try
        {
            return RuntimeValue.FromBoolean(regex.IsMatch(text));
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout("REGEXISMATCH", location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexMatchBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXMATCH", pattern, modes, location);

        try
        {
            var match = regex.Match(text);
            return RuntimeValue.FromString(match.Success ? match.Value : string.Empty);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout("REGEXMATCH", location);
            throw new RuntimeFailureException();
        }
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
            ReportBuiltInArgumentError("REGEXEXTRACT", "group", "РЅРµ РјРѕР¶РµС‚ Р±С‹С‚СЊ РѕС‚СЂРёС†Р°С‚РµР»СЊРЅС‹Рј.", location);
            throw new RuntimeFailureException();
        }

        var regex = CreateRegex("REGEXEXTRACT", pattern, modes, location);

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
            ReportRegexTimeout("REGEXEXTRACT", location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexMatchesBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXMATCHES", pattern, modes, location);

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
            ReportRegexTimeout("REGEXMATCHES", location);
            throw new RuntimeFailureException();
        }
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
            ReportBuiltInArgumentError("REGEXEXTRACTALL", "group", "РЅРµ РјРѕР¶РµС‚ Р±С‹С‚СЊ РѕС‚СЂРёС†Р°С‚РµР»СЊРЅС‹Рј.", location);
            throw new RuntimeFailureException();
        }

        var regex = CreateRegex("REGEXEXTRACTALL", pattern, modes, location);

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
            ReportRegexTimeout("REGEXEXTRACTALL", location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRegexSplitBuiltin(
        string text,
        string pattern,
        string modes,
        TextLocation location)
    {
        var regex = CreateRegex("REGEXSPLIT", pattern, modes, location);

        try
        {
            return text.Length == 0
                ? CreateStringArrayValue([])
                : CreateStringArrayValue(regex.Split(text));
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout("REGEXSPLIT", location);
            throw new RuntimeFailureException();
        }
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

            var result = count is null
                ? regex.Replace(text, replacement)
                : ReplaceRegexMatches(regex, text, replacement, count.Value);

            return string.Equals(result, text, StringComparison.Ordinal)
                ? textValue
                : RuntimeValue.FromString(result);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout("REGEXREPLACE", location);
            throw new RuntimeFailureException();
        }
        catch (ArgumentException ex)
        {
            ReportBuiltInArgumentError(
                "REGEXREPLACE",
                "replacement",
                $"СЃРѕРґРµСЂР¶РёС‚ РЅРµРґРѕРїСѓСЃС‚РёРјСѓСЋ СЃСЃС‹Р»РєСѓ РЅР° РіСЂСѓРїРїСѓ: {ex.Message}",
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
                : count is null
                    ? regex.Replace(text, replacement)
                    : ReplaceRegexMatches(regex, text, replacement, count.Value);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout("REGEXREPLACE", location);
            throw new RuntimeFailureException();
        }
        catch (ArgumentException ex)
        {
            ReportBuiltInArgumentError(
                "REGEXREPLACE",
                "replacement",
                $"СЃРѕРґРµСЂР¶РёС‚ РЅРµРґРѕРїСѓСЃС‚РёРјСѓСЋ СЃСЃС‹Р»РєСѓ РЅР° РіСЂСѓРїРїСѓ: {ex.Message}",
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

        try
        {
            return RuntimeValue.FromInteger(regex.Matches(text).Count);
        }
        catch (RegexMatchTimeoutException)
        {
            ReportRegexTimeout("REGEXCOUNT", location);
            throw new RuntimeFailureException();
        }
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
            ReportBuiltInArgumentError("REGEXREPLACE", "count", "РЅРµ РјРѕР¶РµС‚ Р±С‹С‚СЊ РѕС‚СЂРёС†Р°С‚РµР»СЊРЅС‹Рј.", location);
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

        try
        {
            var regex = new Regex(pattern, options, RegexExecutionLimit);
            _regexCache[cacheKey] = regex;
            return regex;
        }
        catch (ArgumentException ex)
        {
            ReportBuiltInArgumentError(
                functionName,
                "pattern",
                $"СЃРѕРґРµСЂР¶РёС‚ РЅРµРґРѕРїСѓСЃС‚РёРјРѕРµ СЂРµРіСѓР»СЏСЂРЅРѕРµ РІС‹СЂР°Р¶РµРЅРёРµ: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
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

        var options = RegexOptions.CultureInvariant;

        foreach (var mode in modes)
        {
            options |= char.ToLowerInvariant(mode) switch
            {
                'i' => RegexOptions.IgnoreCase,
                'm' => RegexOptions.Multiline,
                's' => RegexOptions.Singleline,
                'x' => RegexOptions.IgnorePatternWhitespace,
                _ => ReportInvalidRegexMode(functionName, mode, location)
            };
        }

        _regexModesCache[modes] = options;
        return options;
    }

    private RegexOptions ReportInvalidRegexMode(string functionName, char mode, TextLocation location)
    {
        ReportBuiltInArgumentError(
            functionName,
            "modes",
            $"СЃРѕРґРµСЂР¶РёС‚ РЅРµРёР·РІРµСЃС‚РЅС‹Р№ СЂРµР¶РёРј '{mode}'. Р”РѕРїСѓСЃС‚РёРјС‹ С‚РѕР»СЊРєРѕ i, m, s, x.",
            location);
        throw new RuntimeFailureException();
    }

    private static string ReplaceRegexMatches(Regex regex, string text, string replacement, int count)
    {
        var matches = regex.Matches(text);

        if (matches.Count == 0)
        {
            return text;
        }

        var builder = new System.Text.StringBuilder(text.Length);
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

    private void ReportRegexTimeout(string functionName, TextLocation location)
    {
        ReportBuiltInArgumentError(functionName, "pattern", "РІС‹РїРѕР»РЅСЏРµС‚СЃСЏ СЃР»РёС€РєРѕРј РґРѕР»РіРѕ.", location);
    }
}
