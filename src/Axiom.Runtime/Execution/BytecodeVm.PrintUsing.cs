using System.Globalization;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void AppendFormattedUsing(
        string format,
        ReadOnlySpan<RuntimeValue> values,
        TextLocation statementLocation)
    {
        var valuesArray = values.ToArray();
        var formatted = BuiltinPrintUsingHelpers.FormatUsing(
            format,
            index => valuesArray[index],
            valuesArray.Length,
            detail =>
            {
                ReportPrintUsingError(detail, statementLocation);
                return new RuntimeFailureException();
            });

        _hasPendingOutput = true;
        _pendingOutput.Append(formatted);
    }

    private List<BuiltinPrintUsingHelpers.PrintUsingFormatToken> GetOrParsePrintUsingFormat(string format)
    {
        if (_printUsingFormatCache.TryGetValue(format, out var cachedTokens))
        {
            return cachedTokens;
        }

        var parsedTokens = BuiltinPrintUsingHelpers.ParsePrintUsingFormat(format);
        _printUsingFormatCache[format] = parsedTokens;
        return parsedTokens;
    }

    private static bool TryFormatAbsoluteNumericValue(
        RuntimeValue value,
        int fractionDigits,
        Span<char> destination,
        out int written)
    {
        Span<char> formatBuffer = stackalloc char[8];
        formatBuffer[0] = 'F';
        if (!fractionDigits.TryFormat(formatBuffer[1..], out var digitsWritten, provider: CultureInfo.InvariantCulture))
        {
            written = 0;
            return false;
        }

        var format = formatBuffer[..(digitsWritten + 1)];

        if (BuiltinPrintUsingHelpers.TryGetDecimal(value, out var decimalValue))
        {
            var absoluteValue = Math.Abs(decimalValue);
            var rounded = Math.Round(absoluteValue, fractionDigits, MidpointRounding.AwayFromZero);
            return rounded.TryFormat(destination, out written, format, CultureInfo.InvariantCulture);
        }

        var absoluteDouble = Math.Abs(value.AsDouble());
        var roundedDouble = Math.Round(absoluteDouble, fractionDigits, MidpointRounding.AwayFromZero);
        return roundedDouble.TryFormat(destination, out written, format, CultureInfo.InvariantCulture);
    }

    private static void SplitNumericParts(ReadOnlySpan<char> text, out ReadOnlySpan<char> integerPart, out ReadOnlySpan<char> fractionPart)
    {
        var dotIndex = text.IndexOf('.');

        if (dotIndex >= 0)
        {
            integerPart = text[..dotIndex];
            fractionPart = text[(dotIndex + 1)..];
            return;
        }

        integerPart = text;
        fractionPart = ReadOnlySpan<char>.Empty;
    }

    private void ReportPrintUsingError(string detail, TextLocation location)
    {
        ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, $"PRINT USING: {detail}", location);
    }
}
