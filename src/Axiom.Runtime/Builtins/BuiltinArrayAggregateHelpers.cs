using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

/// <summary>
/// Реализация агрегатов ARRAYSUM/ARRAYMIN/ARRAYMAX/ARRAYAVERAGE: общая для
/// Interpreter и BytecodeVm. Движки передают два способа сообщения об ошибках:
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

        switch (first.Type)
        {
            case var t when t == TypeSymbol.Integer:
            {
                var acc = first.AsInteger();
                ForEachAfterFirst(array, v =>
                {
                    RequireNumeric("ARRAYSUM", v, location, reportArgumentError);
                    acc = checked(acc + v.AsInteger());
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

    public static RuntimeValue Average(
        RuntimeArrayValue array,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError,
        Action<string, TextLocation> reportResultError)
    {
        var count = 0L;
        var sum = 0d;
        ForEach(array, v =>
        {
            RequireNumeric("ARRAYAVERAGE", v, location, reportArgumentError);
            sum += v.AsDouble();
            count++;
        });

        if (count == 0)
        {
            // Пустой массив уже отклонён RequireAllocatedArrayArgument движка,
            // но страховка нужна: деление на ноль здесь недопустимо.
            reportArgumentError("не может быть пустым выделенным массивом.", location);
            throw new RuntimeFailureException();
        }

        var result = sum / count;
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
        var strings = best.Type == TypeSymbol.String;

        ForEachAfterFirst(array, v =>
        {
            var comparison = Compare(functionName, best, v, strings, location, reportArgumentError);
            if ((preferLarger && comparison < 0) || (!preferLarger && comparison > 0))
            {
                best = v;
            }
        });

        return best;
    }

    private static int Compare(
        string functionName,
        RuntimeValue left,
        RuntimeValue right,
        bool strings,
        TextLocation location,
        Action<string, TextLocation> reportArgumentError)
    {
        if (strings)
        {
            if (right.Type != TypeSymbol.String)
            {
                ReportUnsupportedElement(functionName, right, location, reportArgumentError);
            }

            return string.CompareOrdinal(left.AsString(), right.AsString());
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
