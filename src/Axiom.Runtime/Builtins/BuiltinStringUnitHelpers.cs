using System.Globalization;
using Axiom.Core;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinStringUnitHelpers
{
    internal enum Unit { Utf16, CodePoint, Grapheme }

    public static Unit Parse(string unit, string function, TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (unit.Equals("UTF16", StringComparison.OrdinalIgnoreCase)) return Unit.Utf16;
        if (unit.Equals("CODEPOINT", StringComparison.OrdinalIgnoreCase)) return Unit.CodePoint;
        if (unit.Equals("GRAPHEME", StringComparison.OrdinalIgnoreCase)) return Unit.Grapheme;
        reportError(function, "unit", "должен быть UTF16, CODEPOINT или GRAPHEME.", location);
        throw new RuntimeFailureException();
    }

    public static int Length(string text, Unit unit)
    {
        if (unit == Unit.Utf16) return text.Length;
        if (unit == Unit.Grapheme) return StringInfo.ParseCombiningCharacters(text).Length;
        var count = 0;
        for (var position = 0; position < text.Length; position++)
        {
            count++;
            if (char.IsHighSurrogate(text[position]) && position + 1 < text.Length && char.IsLowSurrogate(text[position + 1])) position++;
        }
        return count;
    }

    public static RuntimeValue Slice(string text, int start, int count, Unit unit, bool fromRight = false)
    {
        if (unit == Unit.Utf16)
            return fromRight ? BuiltinStringHelpers.EvaluateRight(text, count) : BuiltinStringHelpers.EvaluateMid(text, start, count);
        var boundaries = Boundaries(text, unit);
        var first = fromRight ? Math.Max(0, boundaries.Length - count) : start - 1;
        if (first >= boundaries.Length) return RuntimeValue.FromString(string.Empty);
        var last = first + Math.Min(count, boundaries.Length - first);
        var end = last == boundaries.Length ? text.Length : boundaries[last];
        return RuntimeValue.FromString(text.Substring(boundaries[first], end - boundaries[first]));
    }

    private static int[] Boundaries(string text, Unit unit)
    {
        if (unit == Unit.Grapheme) return StringInfo.ParseCombiningCharacters(text);
        var boundaries = new List<int>();
        for (var position = 0; position < text.Length; position++)
        {
            boundaries.Add(position);
            if (char.IsHighSurrogate(text[position]) && position + 1 < text.Length && char.IsLowSurrogate(text[position + 1])) position++;
        }
        return boundaries.ToArray();
    }
}
