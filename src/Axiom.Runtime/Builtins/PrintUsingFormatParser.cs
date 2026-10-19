using System.Text;

namespace Axiom.Runtime.Builtins;

internal static partial class BuiltinPrintUsingHelpers
{
    public static List<PrintUsingFormatToken> ParsePrintUsingFormat(string format)
    {
        var tokens = new List<PrintUsingFormatToken>();
        var literal = new StringBuilder();
        var index = 0;

        while (index < format.Length)
        {
            var current = format[index];

            if (current == '_')
            {
                if (index + 1 < format.Length)
                {
                    FlushPrintUsingLiteral(tokens, literal);
                    tokens.Add(new PrintUsingLiteralToken(format[index + 1].ToString()));
                    index += 2;
                }
                else
                {
                    literal.Append(current);
                    index++;
                }

                continue;
            }

            if (current == '!')
            {
                FlushPrintUsingLiteral(tokens, literal);
                tokens.Add(new PrintUsingStringFieldToken(PrintUsingStringFieldKind.Bang));
                index++;
                continue;
            }

            if (current == '&')
            {
                FlushPrintUsingLiteral(tokens, literal);
                tokens.Add(new PrintUsingStringFieldToken(PrintUsingStringFieldKind.Ampersand));
                index++;
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
                    FlushPrintUsingLiteral(tokens, literal);
                    tokens.Add(new PrintUsingStringFieldToken(PrintUsingStringFieldKind.Backslash, spaces + 2));
                    continue;
                }

                literal.Append(format[start]);
                index = start + 1;
                continue;
            }

            if (IsNumericMaskCharacter(current))
            {
                FlushPrintUsingLiteral(tokens, literal);
                var start = index;

                while (index < format.Length && IsNumericMaskCharacter(format[index]))
                {
                    index++;
                }

                var mask = format[start..index];
                var info = ParseNumericMask(mask);

                if (info.IntegerDigits + info.FractionDigits == 0)
                {
                    tokens.Add(new PrintUsingLiteralToken(mask));
                }
                else
                {
                    tokens.Add(new PrintUsingNumericFieldToken(mask, info));
                }

                continue;
            }

            literal.Append(current);
            index++;
        }

        FlushPrintUsingLiteral(tokens, literal);
        return tokens;
    }

    public static PrintUsingNumericFormatInfo ParseNumericMask(string mask)
    {
        var info = new PrintUsingNumericFormatInfo
        {
            TotalWidth = mask.Length,
            ForceSign = mask.Contains('+'),
            NegativeSignOnly = mask.Contains('-'),
            HasThousands = mask.Contains(','),
            HasDollar = mask.Contains('$'),
            HasStarFill = mask.Contains('*'),
            HasExponent = mask.Contains('^')
        };

        var core = mask
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("^", string.Empty, StringComparison.Ordinal);

        var dotIndex = core.IndexOf('.');

        if (dotIndex >= 0)
        {
            info.IntegerDigits = core[..dotIndex].Count(static character => character is '#' or '0');
            info.FractionDigits = core[(dotIndex + 1)..].Count(static character => character is '#' or '0');
        }
        else
        {
            info.IntegerDigits = core.Count(static character => character is '#' or '0');
        }

        return info;
    }

    public static bool IsNumericMaskCharacter(char character)
    {
        return character is '#' or '0' or '+' or '-' or '.' or ',' or '$' or '*' or '^';
    }

    public static void FlushPrintUsingLiteral(List<PrintUsingFormatToken> tokens, StringBuilder literal)
    {
        if (literal.Length == 0)
        {
            return;
        }

        tokens.Add(new PrintUsingLiteralToken(literal.ToString()));
        literal.Clear();
    }
}
