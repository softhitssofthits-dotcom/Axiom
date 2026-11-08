namespace Axiom.Runtime.Builtins;

internal static partial class BuiltinPrintUsingHelpers
{
    /// <summary>
    /// Разбирает числовую маску одним проходом по её тексту: ширина поля и число
    /// разрядов до и после точки вычисляются без промежуточных строк, которые
    /// прежний разбор создавал через последовательные Replace и срезы.
    /// </summary>
    public static PrintUsingNumericFormatInfo ParseNumericMask(ReadOnlySpan<char> mask)
    {
        var info = new PrintUsingNumericFormatInfo
        {
            TotalWidth = mask.Length,
            ForceSign = mask.IndexOf('+') >= 0,
            NegativeSignOnly = mask.IndexOf('-') >= 0,
            HasThousands = mask.IndexOf(',') >= 0,
            HasDollar = mask.IndexOf('$') >= 0,
            HasStarFill = mask.IndexOf('*') >= 0,
            HasExponent = mask.IndexOf('^') >= 0
        };

        var seenDot = false;
        var integerDigits = 0;
        var fractionDigits = 0;

        foreach (var character in mask)
        {
            if (character == '.')
            {
                seenDot = true;
                continue;
            }

            if (character is not ('#' or '0'))
            {
                continue;
            }

            if (seenDot)
            {
                fractionDigits++;
            }
            else
            {
                integerDigits++;
            }
        }

        info.IntegerDigits = integerDigits;
        info.FractionDigits = fractionDigits;
        return info;
    }

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

    public static bool IsNumericMaskCharacter(char character)
    {
        return character is '#' or '0' or '+' or '-' or '.' or ',' or '$' or '*' or '^';
    }
}