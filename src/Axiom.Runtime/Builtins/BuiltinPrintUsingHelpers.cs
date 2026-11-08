using System.Text;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static partial class BuiltinPrintUsingHelpers
{
    /// <summary>
    /// Однопроходное форматирование: маска читается слева направо, литералы и поля
    /// пишутся сразу в общий <see cref="StringBuilder"/>. Список токенов, строки
    /// литералов и промежуточные строковые значения не создаются; тот же подход
    /// применяет _PyUnicodeWriter в CPython, когда форматирование пишет в готовый
    /// буфер вместо последовательности временных объектов. Порядок разбора и
    /// порядок потребления значений, диагностики и предел длины сохранены.
    /// </summary>
    public static string FormatUsing(
        string format,
        RuntimeValue[] values,
        int valueCount,
        Func<string, Exception> createError)
    {
        if (format.Length > 2048)
        {
            throw createError("длина форматной строки не должна превышать 2048 символов.");
        }

        var builder = new StringBuilder(format.Length + 8);
        var argumentIndex = 0;
        var literalStart = 0;
        var index = 0;

        while (index < format.Length)
        {
            var current = format[index];

            if (current == '_')
            {
                if (index + 1 < format.Length)
                {
                    AppendLiteral(builder, format, literalStart, index - literalStart);
                    RuntimeStringLimits.Append(builder, format.AsSpan(index + 1, 1));
                    index += 2;
                    literalStart = index;
                }
                else
                {
                    index++;
                }

                continue;
            }

            if (current is '!' or '&')
            {
                AppendLiteral(builder, format, literalStart, index - literalStart);
                var kind = current == '!'
                    ? PrintUsingStringFieldKind.Bang
                    : PrintUsingStringFieldKind.Ampersand;
                AppendStringField(builder, kind, 0, values, valueCount, ref argumentIndex, createError);
                index++;
                literalStart = index;
                continue;
            }

            if (current == '\\')
            {
                var start = index++;
                var spaces = 0;

                while (index < format.Length && format[index] == ' ')
                {
                    spaces++;
                    index++;
                }

                if (index < format.Length && format[index] == '\\')
                {
                    index++;
                    AppendLiteral(builder, format, literalStart, start - literalStart);
                    AppendStringField(
                        builder,
                        PrintUsingStringFieldKind.Backslash,
                        spaces + 2,
                        values,
                        valueCount,
                        ref argumentIndex,
                        createError);
                    literalStart = index;
                    continue;
                }

                // Одиночная обратная косая черта остаётся литералом; пробелы после
                // неё снова читаются обычным путём, как в прежнем разборе.
                index = start + 1;
                continue;
            }

            if (IsNumericMaskCharacter(current))
            {
                var start = index;

                while (index < format.Length && IsNumericMaskCharacter(format[index]))
                {
                    index++;
                }

                var info = ParseNumericMask(format.AsSpan(start, index - start));
                AppendLiteral(builder, format, literalStart, start - literalStart);

                if (info.IntegerDigits + info.FractionDigits == 0)
                {
                    RuntimeStringLimits.Append(builder, format.AsSpan(start, index - start));
                }
                else
                {
                    var value = GetNextNumericFormatValue(values, valueCount, ref argumentIndex, createError);
                    RuntimeStringLimits.Append(builder, FormatNumericField(info, value));
                }

                literalStart = index;
                continue;
            }

            index++;
        }

        AppendLiteral(builder, format, literalStart, format.Length - literalStart);
        return builder.ToString();
    }

    private static void AppendLiteral(StringBuilder builder, string format, int start, int length)
    {
        if (length > 0)
        {
            RuntimeStringLimits.Append(builder, format.AsSpan(start, length));
        }
    }

    private static void AppendStringField(
        StringBuilder builder,
        PrintUsingStringFieldKind kind,
        int width,
        RuntimeValue[] values,
        int valueCount,
        ref int argumentIndex,
        Func<string, Exception> createError)
    {
        if (argumentIndex >= valueCount)
        {
            // Отсутствующее строковое значение: поле ! и & ничего не выводят,
            // фиксированное поле заполняется пробелами до своей ширины.
            if (kind == PrintUsingStringFieldKind.Backslash && width > 0)
            {
                RuntimeStringLimits.Check((long)builder.Length + width);
                builder.Append(' ', width);
            }

            return;
        }

        var value = values[argumentIndex++];

        if (value.Type != TypeSymbol.String)
        {
            throw createError(
                $"строковое поле требует значение типа STRING, но получен {value.Type.Name}.");
        }

        var text = value.AsString();

        switch (kind)
        {
            case PrintUsingStringFieldKind.Bang:
                if (text.Length > 0)
                {
                    RuntimeStringLimits.Append(builder, text.AsSpan(0, 1));
                }

                break;

            case PrintUsingStringFieldKind.Ampersand:
                RuntimeStringLimits.Append(builder, text);
                break;

            default:
                var length = Math.Min(text.Length, width);
                RuntimeStringLimits.Append(builder, text.AsSpan(0, length));

                if (length < width)
                {
                    RuntimeStringLimits.Check((long)builder.Length + (width - length));
                    builder.Append(' ', width - length);
                }

                break;
        }
    }

    private static RuntimeValue GetNextNumericFormatValue(
        RuntimeValue[] values,
        int valueCount,
        ref int index,
        Func<string, Exception> createError)
    {
        if (index >= valueCount)
        {
            throw createError("для числового поля не хватило значения.");
        }

        var value = values[index++];

        if (!IsNumericType(value.Type))
        {
            throw createError(
                $"числовое поле требует числовое значение, но получен {value.Type.Name}.");
        }

        return value;
    }

    public enum PrintUsingStringFieldKind
    {
        Bang,
        Backslash,
        Ampersand
    }
}
