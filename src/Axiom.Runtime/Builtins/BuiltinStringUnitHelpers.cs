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

    public static int Length(string text, Unit unit, Action? checkCancellation = null)
    {
        if (unit == Unit.Utf16) return text.Length;
        var count = 0;
        for (var position = 0; position < text.Length;)
        {
            if ((count & 1023) == 0) checkCancellation?.Invoke();
            count++;
            position += ElementLength(text, position, unit);
        }
        return count;
    }

    public static RuntimeValue Slice(string text, int start, int count, Unit unit, bool fromRight = false, Action? checkCancellation = null)
    {
        if (unit == Unit.Utf16)
            return fromRight ? BuiltinStringHelpers.EvaluateRight(text, count) : BuiltinStringHelpers.EvaluateMid(text, start, count);
        if (text.Length == 0 || start > text.Length) return RuntimeValue.FromString(string.Empty);
        if (fromRight)
        {
            if (count >= text.Length) return RuntimeValue.FromString(text);
            if (unit == Unit.CodePoint)
            {
                var position = text.Length;
                for (var seen = 0; seen < count && position > 0; seen++)
                {
                    if ((seen & 1023) == 0) checkCancellation?.Invoke();
                    position--;
                    if (char.IsLowSurrogate(text[position]) && position > 0 && char.IsHighSurrogate(text[position - 1])) position--;
                }
                return RuntimeValue.FromString(text[position..]);
            }
            if (count <= 256) return RightGraphemes(text, count, checkCancellation);
            var length = Length(text, unit, checkCancellation);
            if (count >= length) return RuntimeValue.FromString(text);
            start = length - count + 1;
        }
        var offset = 0;
        var first = -1;
        var index = 1;
        while (offset < text.Length)
        {
            if ((index & 1023) == 1) checkCancellation?.Invoke();
            if (index == start) first = offset;
            if (first >= 0 && index - start >= count) break;
            offset += ElementLength(text, offset, unit);
            index++;
        }
        return RuntimeValue.FromString(first < 0 ? string.Empty : text.Substring(first, offset - first));
    }

    private static RuntimeValue RightGraphemes(string text, int count, Action? checkCancellation)
    {
        Span<int> recent = stackalloc int[256];
        var seen = 0;
        for (var offset = 0; offset < text.Length; seen++)
        {
            if ((seen & 1023) == 0) checkCancellation?.Invoke();
            recent[seen % count] = offset;
            offset += ElementLength(text, offset, Unit.Grapheme);
        }
        return RuntimeValue.FromString(seen <= count ? text : text[recent[seen % count]..]);
    }

    private static int ElementLength(string text, int position, Unit unit) => unit == Unit.Grapheme
        ? StringInfo.GetNextTextElementLength(text.AsSpan(position))
        : char.IsHighSurrogate(text[position]) && position + 1 < text.Length && char.IsLowSurrogate(text[position + 1]) ? 2 : 1;
}
