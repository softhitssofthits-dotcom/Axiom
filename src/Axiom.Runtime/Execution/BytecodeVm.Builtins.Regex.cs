using System.Text.RegularExpressions;
using Axiom.Core;
using Axiom.Semantics;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private const int RegexCacheCapacity = 128;
    private readonly Dictionary<(string Pattern, RegexOptions Options),
        (Regex Regex, LinkedListNode<(string Pattern, RegexOptions Options)> Node)> _regexCache = [];
    private readonly LinkedList<(string Pattern, RegexOptions Options)> _regexCacheUsage = new();

    private RuntimeValue EvaluateRegexIsMatchBuiltin(string text, string pattern, string modes, TextLocation location) =>
        BuiltinRegexHelpers.IsMatch(CreateRegex("REGEXISMATCH", pattern, modes, location), text, location,
            ReportBuiltInArgumentError, "REGEXISMATCH", ThrowIfRegexCancellationRequested);

    private RuntimeValue EvaluateRegexMatchBuiltin(string text, string pattern, string modes, TextLocation location) =>
        BuiltinRegexHelpers.Match(CreateRegex("REGEXMATCH", pattern, modes, location), text, location,
            ReportBuiltInArgumentError, "REGEXMATCH", ThrowIfRegexCancellationRequested);

    private RuntimeValue EvaluateRegexExtractBuiltin(string text, string pattern, int group, string modes, TextLocation location)
    {
        ValidateRegexGroup("REGEXEXTRACT", group, location);
        return BuiltinRegexHelpers.Extract(CreateRegex("REGEXEXTRACT", pattern, modes, location), text, group, location,
            ReportBuiltInArgumentError, "REGEXEXTRACT", ThrowIfRegexCancellationRequested);
    }

    private RuntimeValue EvaluateRegexMatchesBuiltin(string text, string pattern, string modes, TextLocation location) =>
        BuiltinRegexHelpers.Matches(CreateRegex("REGEXMATCHES", pattern, modes, location), text, location,
            ReportBuiltInArgumentError, "REGEXMATCHES", CreateRegexArrayValue, ThrowIfRegexCancellationRequested);

    private RuntimeValue EvaluateRegexExtractAllBuiltin(string text, string pattern, int group, string modes, TextLocation location)
    {
        ValidateRegexGroup("REGEXEXTRACTALL", group, location);
        return BuiltinRegexHelpers.ExtractAll(CreateRegex("REGEXEXTRACTALL", pattern, modes, location), text, group, location,
            ReportBuiltInArgumentError, "REGEXEXTRACTALL", CreateRegexArrayValue, ThrowIfRegexCancellationRequested);
    }

    private RuntimeValue EvaluateRegexSplitBuiltin(string text, string pattern, string modes, TextLocation location) =>
        BuiltinRegexHelpers.Split(CreateRegex("REGEXSPLIT", pattern, modes, location), text, location,
            ReportBuiltInArgumentError, "REGEXSPLIT", CreateRegexArrayValue, ThrowIfRegexCancellationRequested);

    private RuntimeValue EvaluateRegexCountBuiltin(string text, string pattern, string modes, TextLocation location) =>
        RuntimeValue.FromInteger(BuiltinRegexHelpers.Count(CreateRegex("REGEXCOUNT", pattern, modes, location), text, location,
            ReportBuiltInArgumentError, "REGEXCOUNT", ThrowIfRegexCancellationRequested));

    private RuntimeValue EvaluateRegexReplaceBuiltin(RuntimeValue textValue, RuntimeValue patternValue,
        RuntimeValue replacementValue, string modes, int? count, TextLocation location)
    {
        ValidateRegexReplaceCount(count, location);
        if (count == 0) return textValue;
        var text = GetStringUnchecked(textValue);
        var regex = CreateRegex("REGEXREPLACE", GetStringUnchecked(patternValue), modes, location);
        var result = BuiltinRegexHelpers.Replace(regex, text, GetStringUnchecked(replacementValue), count, location,
            ReportBuiltInArgumentError, "REGEXREPLACE", ThrowIfRegexCancellationRequested);
        return string.Equals(result, text, StringComparison.Ordinal) ? textValue : RuntimeValue.FromString(result);
    }

    private void ValidateRegexGroup(string name, int group, TextLocation location)
    {
        if (group >= 0) return;
        ReportBuiltInArgumentError(name, "group", "cannot be negative.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateRegexReplaceCount(int? count, TextLocation location)
    {
        if (count is not < 0) return;
        ReportBuiltInArgumentError("REGEXREPLACE", "count", "cannot be negative.", location);
        throw new RuntimeFailureException();
    }

    private Regex CreateRegex(string name, string pattern, string modes, TextLocation location)
    {
        ThrowIfRegexCancellationRequested();
        var options = BuiltinRegexHelpers.ParseRegexModes(modes, location, name, ReportBuiltInArgumentError);
        var key = (pattern, options);
        if (_regexCache.TryGetValue(key, out var cached))
        {
            _regexCacheUsage.Remove(cached.Node);
            _regexCacheUsage.AddLast(cached.Node);
            return cached.Regex;
        }
        var regex = BuiltinRegexHelpers.CreateRegex(pattern, options, location, name, ReportBuiltInArgumentError);
        if (_regexCache.Count == RegexCacheCapacity)
        {
            var oldest = _regexCacheUsage.First!;
            _regexCache.Remove(oldest.Value);
            _regexCacheUsage.RemoveFirst();
        }
        _regexCache.Add(key, (regex, _regexCacheUsage.AddLast(key)));
        return regex;
    }

    private void ThrowIfRegexCancellationRequested()
    {
        // Общая отмена очищает стек: CATCH не должен перехватывать остановку пользователя.
        if (CheckCancellationRequested()) throw new RuntimeFailureException();
    }

    private static RuntimeValue CreateRegexArrayValue(IReadOnlyList<RuntimeValue> values) =>
        CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.String), values);
}
