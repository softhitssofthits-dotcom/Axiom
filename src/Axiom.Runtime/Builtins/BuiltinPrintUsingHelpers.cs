using System.Globalization;
using System.Text;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static partial class BuiltinPrintUsingHelpers
{
    public static string FormatUsing(
        string format,
        Func<int, RuntimeValue> getValue,
        int valueCount,
        Func<string, Exception> createError)
    {
        if (format.Length > 2048)
        {
            throw createError("длина форматной строки не должна превышать 2048 символов.");
        }

        var tokens = ParsePrintUsingFormat(format);
        var builder = new StringBuilder();
        var argumentIndex = 0;

        foreach (var token in tokens)
        {
            switch (token)
            {
                case PrintUsingLiteralToken literalToken:
                    RuntimeStringLimits.Append(builder, literalToken.Text);
                    break;

                case PrintUsingStringFieldToken stringFieldToken:
                {
                    var valueText = GetNextStringFormatValue(stringFieldToken, getValue, valueCount, ref argumentIndex, createError);
                    RuntimeStringLimits.Append(builder, FormatStringField(stringFieldToken, valueText));
                    break;
                }

                case PrintUsingNumericFieldToken numericFieldToken:
                {
                    var value = GetNextNumericFormatValue(getValue, valueCount, ref argumentIndex, createError);
                    RuntimeStringLimits.Append(builder, FormatNumericField(numericFieldToken, value));
                    break;
                }
            }
        }

        return builder.ToString();
    }

    public static string FormatStringField(PrintUsingStringFieldToken field, string value)
    {
        return field.Kind switch
        {
            PrintUsingStringFieldKind.Bang => value.Length == 0 ? string.Empty : value[..1],
            PrintUsingStringFieldKind.Ampersand => value,
            PrintUsingStringFieldKind.Backslash when value.Length == field.Width => value,
            PrintUsingStringFieldKind.Backslash when value.Length < field.Width => value.PadRight(field.Width, ' '),
            PrintUsingStringFieldKind.Backslash => value[..field.Width],
            _ => value
        };
    }

    private static string GetNextStringFormatValue(
        PrintUsingStringFieldToken field,
        Func<int, RuntimeValue> getValue,
        int valueCount,
        ref int index,
        Func<string, Exception> createError)
    {
        if (index >= valueCount)
        {
            return field.Kind == PrintUsingStringFieldKind.Backslash
                ? new string(' ', field.Width)
                : string.Empty;
        }

        var value = getValue(index++);

        if (value.Type != TypeSymbol.String)
        {
            throw createError(
                $"строковое поле требует значение типа STRING, но получен {value.Type.Name}.");
        }

        return value.AsString();
    }

    private static RuntimeValue GetNextNumericFormatValue(
        Func<int, RuntimeValue> getValue,
        int valueCount,
        ref int index,
        Func<string, Exception> createError)
    {
        if (index >= valueCount)
        {
            throw createError("для числового поля не хватило значения.");
        }

        var value = getValue(index++);

        if (!IsNumericType(value.Type))
        {
            throw createError(
                $"числовое поле требует числовое значение, но получен {value.Type.Name}.");
        }

        return value;
    }

    public abstract record PrintUsingFormatToken;

    public sealed record PrintUsingLiteralToken(string Text) : PrintUsingFormatToken;

    public sealed record PrintUsingStringFieldToken(PrintUsingStringFieldKind Kind, int Width = 0) : PrintUsingFormatToken;

    public sealed record PrintUsingNumericFieldToken(string Mask, PrintUsingNumericFormatInfo Info) : PrintUsingFormatToken;

    public sealed class PrintUsingNumericFormatInfo
    {
        public int TotalWidth { get; init; }

        public int IntegerDigits { get; set; }

        public int FractionDigits { get; set; }

        public bool ForceSign { get; init; }

        public bool NegativeSignOnly { get; init; }

        public bool HasThousands { get; init; }

        public bool HasDollar { get; init; }

        public bool HasStarFill { get; init; }

        public bool HasExponent { get; init; }
    }

    public enum PrintUsingStringFieldKind
    {
        Bang,
        Backslash,
        Ampersand
    }
}
