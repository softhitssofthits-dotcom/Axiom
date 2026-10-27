using System.Numerics;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

/// <summary>
/// Реализация агрегатов ARRAYSUM/ARRAYMIN/ARRAYMAX/ARRAYAVERAGE для BytecodeVm.
/// Вызывающий код передаёт два способа сообщения об ошибках:
/// reportArgumentError оборачивает detail в стандартную диагностику аргумента
/// (MB3007 с именем функции), reportResultError получает готовый полный текст
/// для диагностики результата (MB3008).
/// </summary>
internal static class BuiltinArrayAggregateHelpers
{
    public static RuntimeValue Sum(
        RuntimeArrayValue array,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError,
        Action<string, TextLocation> reportResultError)
    {
        var first = FirstValue(array, "ARRAYSUM", location, reportArgumentError);
        RequireNumeric("ARRAYSUM", first, location, reportArgumentError);

        try
        {
            switch (first.Type)
            {
                case var t when t == TypeSymbol.Integer:
                {
                    var acc = 0;
                    BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
                    {
                        acc = checked(acc + array.GetIntegerDirect(indices));
                    });
                    return RuntimeValue.FromInteger(acc);
                }
                case var t when t == TypeSymbol.Long:
                {
                    var acc = first.AsLong();
                    ForEachAfterFirst(array, v =>
                    {
                        RequireNumeric("ARRAYSUM", v, location, reportArgumentError);
                        acc = checked(acc + v.AsLong());
                    });
                    return RuntimeValue.FromLong(acc);
                }
                case var t when t == TypeSymbol.Decimal:
                {
                    var acc = first.AsDecimal();
                    ForEachAfterFirst(array, v =>
                    {
                        RequireNumeric("ARRAYSUM", v, location, reportArgumentError);
                        acc += v.AsDecimal();
                    });
                    return RuntimeValue.FromDecimal(acc);
                }
                default:
                {
                    var acc = first.AsDouble();
                    ForEachAfterFirst(array, v =>
                    {
                        RequireNumeric("ARRAYSUM", v, location, reportArgumentError);
                        acc += v.AsDouble();
                    });
                    RequireFinite("ARRAYSUM", acc, location, reportResultError);
                    return RuntimeValue.FromDouble(acc);
                }
            }
        }
        catch (OverflowException)
        {
            reportResultError($"Результат функции ARRAYSUM выходит за диапазон типа {first.Type.Name}.", location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue Average(
        RuntimeArrayValue array,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError,
        Action<string, TextLocation> reportResultError)
    {
        var count = 0L;
        Int128 lowestUnits = 0;
        Int128[]? exponentSums = null;
        Dictionary<int, Int128>? smallExponentSums = null;
        var useSmallGroups = array.Length < 128;
        var firstShift = 2046;
        var lastShift = 0;
        ForEach(array, v =>
        {
            RequireNumeric("ARRAYAVERAGE", v, location, reportArgumentError);
            var value = v.Type == TypeSymbol.Decimal ? (double)v.AsDecimal() : v.AsDouble();
            RequireFinite("ARRAYAVERAGE", value, location, reportResultError);
            // Каждый конечный DOUBLE точно представим целым числом единиц 2^-1074.
            var bits = BitConverter.DoubleToUInt64Bits(value);
            var exponent = (int)((bits >> 52) & 0x7ff);
            var significand = bits & 0x000fffffffffffffUL;
            if (exponent != 0)
                significand |= 1UL << 52;
            if (significand != 0)
            {
                var signedMantissa = (bits >> 63) == 0 ? (Int128)significand : -(Int128)significand;
                var binaryShift = Math.Max(0, exponent - 1);
                // Мантисса меньше 2^53, число элементов не больше LONG.MaxValue:
                // модуль суммы группы меньше 2^116 и точно помещается в Int128.
                if (binaryShift == 0)
                    lowestUnits += signedMantissa;
                else if (useSmallGroups)
                {
                    smallExponentSums ??= new Dictionary<int, Int128>();
                    smallExponentSums.TryGetValue(binaryShift, out var previous);
                    smallExponentSums[binaryShift] = previous + signedMantissa;
                }
                else
                {
                    exponentSums ??= new Int128[2046];
                    exponentSums[binaryShift] += signedMantissa;
                    firstShift = Math.Min(firstShift, binaryShift);
                    lastShift = Math.Max(lastShift, binaryShift);
                }
            }
            count++;
        });
        if (count == 0)
        {
            reportArgumentError("не может быть пустым выделенным массивом.", location);
            throw new RuntimeFailureException();
        }
        var exactSum = (BigInteger)lowestUnits;
        if (smallExponentSums is not null)
        {
            foreach (var group in smallExponentSums)
            {
                if (group.Value != 0)
                    exactSum += (BigInteger)group.Value << group.Key;
            }
        }
        if (exponentSums is not null)
        {
            for (var binaryShift = firstShift; binaryShift <= lastShift; binaryShift++)
            {
                if (exponentSums[binaryShift] != 0)
                    exactSum += (BigInteger)exponentSums[binaryShift] << binaryShift;
            }
        }
        var magnitude = BigInteger.Abs(exactSum);
        var divisor = new BigInteger(count);
        // Выбираем шаг DOUBLE и округляем частное один раз, к ближайшему чётному.
        var power = (int)(magnitude.GetBitLength() - divisor.GetBitLength());
        if (power >= 0 && magnitude < (divisor << power))
            power--;
        var shift = Math.Max(0, power - 52);
        var denominator = divisor << shift;
        var rounded = BigInteger.DivRem(magnitude, denominator, out var remainder);
        var halfway = (remainder << 1).CompareTo(denominator);
        if (halfway > 0 || (halfway == 0 && !rounded.IsEven))
            rounded++;
        var result = Math.ScaleB((double)rounded, shift - 1074);
        if (exactSum.Sign < 0)
            result = -result;
        RequireFinite("ARRAYAVERAGE", result, location, reportResultError);
        return RuntimeValue.FromDouble(result);
    }

    public static RuntimeValue Min(
        RuntimeArrayValue array,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError) =>
        Extreme(array, "ARRAYMIN", location, reportArgumentError, preferLarger: false);

    public static RuntimeValue Max(
        RuntimeArrayValue array,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError) =>
        Extreme(array, "ARRAYMAX", location, reportArgumentError, preferLarger: true);

    private static RuntimeValue Extreme(
        RuntimeArrayValue array,
        string functionName,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError,
        bool preferLarger)
    {
        var best = FirstValue(array, functionName, location, reportArgumentError);
        var comparisonKind = ResolveExtremeComparisonKind(functionName, best, location, reportArgumentError);

        ForEachAfterFirst(array, v =>
        {
            var comparison = Compare(functionName, best, v, comparisonKind, location, reportArgumentError);
            if ((preferLarger && comparison < 0) || (!preferLarger && comparison > 0))
            {
                best = v;
            }
        });

        return best;
    }

    /// <summary>
    /// Порядковое сравнение уже поддержано для DATE, TIME и DATETIME обычными
    /// операторами, поэтому агрегаты используют тот же порядок и не переводят
    /// значения в строки. Смешение видов внутри массива невозможно: тип элементов
    /// задаётся объявлением.
    /// </summary>
    private static ExtremeComparisonKind ResolveExtremeComparisonKind(
        string functionName,
        RuntimeValue first,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError)
    {
        if (first.Type == TypeSymbol.String)
        {
            return ExtremeComparisonKind.String;
        }

        if (first.Type == TypeSymbol.Date || first.Type == TypeSymbol.Time
            || first.Type == TypeSymbol.DateTime || first.Type == TypeSymbol.UtcDateTime)
        {
            return ExtremeComparisonKind.Temporal;
        }

        RequireNumeric(functionName, first, location, reportArgumentError);
        return ExtremeComparisonKind.Numeric;
    }

    private enum ExtremeComparisonKind
    {
        Numeric,
        String,
        Temporal
    }

    private static int Compare(
        string functionName,
        RuntimeValue left,
        RuntimeValue right,
        ExtremeComparisonKind comparisonKind,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError)
    {
        if (comparisonKind == ExtremeComparisonKind.String)
        {
            if (right.Type != TypeSymbol.String)
            {
                ReportUnsupportedElement(functionName, right, location, reportArgumentError);
            }

            return string.CompareOrdinal(left.AsString(), right.AsString());
        }

        if (comparisonKind == ExtremeComparisonKind.Temporal)
        {
            if (right.Type != left.Type)
            {
                ReportUnsupportedElement(functionName, right, location, reportArgumentError);
            }

            return RuntimeValueHelpers.Compare(left, right);
        }

        if (left.Type == TypeSymbol.String || right.Type == TypeSymbol.String)
        {
            ReportUnsupportedElement(functionName, left.Type == TypeSymbol.String ? left : right, location, reportArgumentError);
        }

        return BuiltinNumericHelpers.CompareNumericValues(left, right, location, (message, errorLocation) =>
        {
            reportArgumentError(message, errorLocation);
            throw new RuntimeFailureException();
        });
    }

    private static RuntimeValue FirstValue(
        RuntimeArrayValue array,
        string functionName,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError)
    {
        if (array.Length == 0)
        {
            reportArgumentError("не может быть пустым выделенным массивом.", location);
            throw new RuntimeFailureException();
        }

        RuntimeValue first = RuntimeValue.Void;
        var found = false;
        ForEachArrayUntil(array, v =>
        {
            first = v;
            found = true;
            return true;
        });

        if (!found)
        {
            reportArgumentError("не может быть пустым выделенным массивом.", location);
            throw new RuntimeFailureException();
        }

        return first;
    }

    private static void ForEach(RuntimeArrayValue array, Action<RuntimeValue> action)
    {
        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices => action(array.GetValue(indices)));
    }

    private static void ForEachArrayUntil(RuntimeArrayValue array, Func<RuntimeValue, bool> action)
    {
        var stop = false;
        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            if (!stop && action(array.GetValue(indices)))
            {
                stop = true;
            }
        });
    }

    private static void ForEachAfterFirst(RuntimeArrayValue array, Action<RuntimeValue> action)
    {
        var skippedFirst = false;
        ForEach(array, v =>
        {
            if (!skippedFirst)
            {
                skippedFirst = true;
                return;
            }

            action(v);
        });
    }

    private static void RequireNumeric(
        string functionName,
        RuntimeValue value,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError)
    {
        var type = value.Type;
        if (type != TypeSymbol.Integer && type != TypeSymbol.Long && type != TypeSymbol.Double && type != TypeSymbol.Decimal)
        {
            ReportUnsupportedElement(functionName, value, location, reportArgumentError);
        }
    }

    private static void RequireFinite(
        string functionName,
        double value,
        TextLocation location,
        Action<string, TextLocation> reportResultError)
    {
        if (!double.IsFinite(value))
        {
            reportResultError($"Результат {functionName} выходит за диапазон DOUBLE.", location);
            throw new RuntimeFailureException();
        }
    }

    private static void ReportUnsupportedElement(
        string functionName,
        RuntimeValue value,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError)
    {
        reportArgumentError(
            $"поддерживает только числовые элементы, получен {value.Type.Name}.",
            location);
        throw new RuntimeFailureException();
    }
}
