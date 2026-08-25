using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateExpression(BoundExpression expression)
    {
        return expression switch
        {
            BoundLiteralExpression literal => EvaluateLiteralExpression(literal),
            BoundFunctionExpression function => EvaluateFunctionExpression(function),
            BoundVariableExpression variable => EvaluateVariableExpression(variable),
            BoundArrayAccessExpression arrayAccess => EvaluateArrayAccessExpression(arrayAccess),
            BoundFieldAccessExpression fieldAccess => EvaluateFieldAccessExpression(fieldAccess),
            BoundRecordLiteralExpression recordLiteral => EvaluateRecordLiteralExpression(recordLiteral),
            BoundTypeConstructorExpression typeConstructor => EvaluateTypeConstructorExpression(typeConstructor),
            BoundUnaryExpression unary => EvaluateUnaryExpression(unary),
            BoundBinaryExpression binary => EvaluateBinaryExpression(binary),
            BoundCallExpression call => EvaluateCallExpression(call),
            BoundConversionExpression conversion => EvaluateConversionExpression(conversion),
            BoundInterpolatedStringExpression interpolated => EvaluateInterpolatedStringExpression(interpolated),
            BoundErrorExpression => ReportInvalidRuntimeState<RuntimeValue>("Невозможно вычислить expression с semantic error.", expression.Location),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Неподдерживаемый bound expression: {expression.Kind}.", expression.Location)
        };
    }

    private RuntimeValue EvaluateFunctionExpression(BoundFunctionExpression expression)
    {
        var capturedScope = CaptureScopeForFunction(expression.Function, expression.Location);
        return RuntimeValue.FromFunction(expression.Function, capturedScope);
    }

    private static RuntimeValue EvaluateLiteralExpression(BoundLiteralExpression expression)
    {
        return expression.Type switch
        {
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger((int)expression.Value!),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong((long)expression.Value!),
            var type when type == TypeSymbol.Double => RuntimeValue.FromDouble((double)expression.Value!),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal((decimal)expression.Value!),
            var type when type == TypeSymbol.String => RuntimeValue.FromString((string)expression.Value!),
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean((bool)expression.Value!),
            var type when type == TypeSymbol.Void => RuntimeValue.Void,
            _ => RuntimeValue.Default(expression.Type)
        };
    }

    private RuntimeValue EvaluateVariableExpression(BoundVariableExpression expression)
    {
        return GetVariableValue(expression.Variable);
    }

    private RuntimeValue EvaluateInterpolatedStringExpression(BoundInterpolatedStringExpression expression)
    {
        if (expression.Parts.Count == 0)
        {
            return RuntimeValue.FromString(string.Empty);
        }

        var sb = new System.Text.StringBuilder();
        foreach (var part in expression.Parts)
        {
            var value = EvaluateExpression(part.Expression);

            string text;
            if (part.FormatSpecifier is not null || part.Alignment is not null)
            {
                text = FormatInterpolatedValue(value, part.Alignment, part.FormatSpecifier);
            }
            else
            {
                text = value.ToDisplayString();
            }

            sb.Append(text);
        }

        return RuntimeValue.FromString(sb.ToString());
    }

    private static string FormatInterpolatedValue(RuntimeValue value, int? alignment, string? format)
    {
        string formatted;

        if (format is not null)
        {
            var upperFormat = format.ToUpperInvariant();
            formatted = value.Type switch
            {
                var t when t == TypeSymbol.Integer => ((int)value.AsInteger()).ToString(upperFormat, System.Globalization.CultureInfo.InvariantCulture),
                var t when t == TypeSymbol.Long => value.AsLong().ToString(upperFormat, System.Globalization.CultureInfo.InvariantCulture),
                var t when t == TypeSymbol.Double => value.AsDouble().ToString(upperFormat, System.Globalization.CultureInfo.InvariantCulture),
                var t when t == TypeSymbol.Decimal => value.AsDecimal().ToString(upperFormat, System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToDisplayString()
            };
        }
        else
        {
            formatted = value.ToDisplayString();
        }

        if (alignment.HasValue)
        {
            formatted = alignment.Value > 0
                ? formatted.PadLeft(alignment.Value)
                : formatted.PadRight(-alignment.Value);
        }

        return formatted;
    }

    private RuntimeValue EvaluateArrayAccessExpression(BoundArrayAccessExpression expression)
    {
        var array = GetAllocatedArray(EvaluateExpression(expression.Target), expression.DisplayName, expression.Location);
        var slices = EvaluateArrayIndexSlices(expression.Indices, "Значение индекса массива");

        if (!TryValidateArraySlices(array, slices))
        {
            ReportArraySlicesOutOfBounds(expression.DisplayName, slices, array, expression.Location);
            throw new RuntimeFailureException();
        }

        if (expression.Type is ArrayTypeSymbol resultArrayType)
        {
            return RuntimeValue.FromArray(resultArrayType, array.CreateView(slices));
        }

        var indices = slices.Select(static slice => slice.LowerBound).ToArray();
        return array.GetValue(indices);
    }

    private RuntimeValue EvaluateFieldAccessExpression(BoundFieldAccessExpression expression)
    {
        var target = EvaluateExpression(expression.Target);

        if (target.Type == TypeSymbol.RuntimeError)
        {
            return target.AsRuntimeError().GetField(expression.Field);
        }

        return target.AsRecord().GetField(expression.Field);
    }

    private RuntimeValue EvaluateRecordLiteralExpression(BoundRecordLiteralExpression expression)
    {
        var record = RuntimeRecordValue.CreateDefault(expression.RecordType);

        foreach (var initializer in expression.Initializers)
        {
            record.SetField(initializer.Field, EvaluateExpression(initializer.Expression));
        }

        return RuntimeValue.FromRecord(expression.RecordType, record);
    }

    private RuntimeValue EvaluateTypeConstructorExpression(BoundTypeConstructorExpression expression)
    {
        var selfValue = RuntimeValue.FromRecord(expression.RecordType, RuntimeRecordValue.CreateDefault(expression.RecordType));
        var arguments = expression.Arguments
            .Select(EvaluateExpression)
            .ToArray();

        return InvokeTypeConstructor(expression, selfValue, arguments);
    }

    private RuntimeValue EvaluateUnaryExpression(BoundUnaryExpression expression)
    {
        var operand = EvaluateExpression(expression.Operand);

        return expression.OperatorKind switch
        {
            SyntaxKind.MinusToken when operand.Type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(-operand.AsInteger()),
                "Результат операции унарного '-'",
                expression.Location),
            SyntaxKind.MinusToken when operand.Type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(-operand.AsLong()),
                "Результат операции унарного '-'",
                expression.Location),
            SyntaxKind.MinusToken when operand.Type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                -operand.AsDouble(),
                "Результат операции унарного '-'",
                expression.Location),
            SyntaxKind.MinusToken when operand.Type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(-operand.AsDecimal()),
                "Результат операции унарного '-'",
                expression.Location),
            SyntaxKind.NotKeyword when operand.Type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(!operand.AsBoolean()),
            SyntaxKind.NotKeyword when operand.Type == TypeSymbol.Integer => RuntimeValue.FromInteger(~operand.AsInteger()),
            SyntaxKind.NotKeyword when operand.Type == TypeSymbol.Long => RuntimeValue.FromLong(~operand.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Неподдерживаемый unary operator: {expression.OperatorKind}.", expression.Location)
        };
    }

    private RuntimeValue EvaluateBinaryExpression(BoundBinaryExpression expression)
    {
        var left = EvaluateExpression(expression.Left);
        var right = EvaluateExpression(expression.Right);

        return expression.OperatorKind switch
        {
            SyntaxKind.AmpersandToken
                => RuntimeValue.FromString(left.ToDisplayString() + right.ToDisplayString()),
            SyntaxKind.PlusToken when expression.Type == TypeSymbol.String
                => RuntimeValue.FromString(left.AsString() + right.AsString()),
            SyntaxKind.PlusToken when expression.Type == TypeSymbol.Integer
                => EvaluateCheckedIntegerOperation(
                    () => checked(left.AsInteger() + right.AsInteger()),
                    "Результат операции '+'",
                    expression.Location),
            SyntaxKind.PlusToken when expression.Type == TypeSymbol.Long
                => EvaluateCheckedLongOperation(
                    () => checked(left.AsLong() + right.AsLong()),
                    "Результат операции '+'",
                    expression.Location),
            SyntaxKind.PlusToken when expression.Type == TypeSymbol.Double
                => EnsureFiniteDoubleResult(left.AsDouble() + right.AsDouble(), "Результат операции '+'", expression.Location),
            SyntaxKind.PlusToken when expression.Type == TypeSymbol.Decimal
                => EvaluateCheckedDecimalOperation(
                    () => checked(left.AsDecimal() + right.AsDecimal()),
                    "Результат операции '+'",
                    expression.Location),
            SyntaxKind.MinusToken when expression.Type == TypeSymbol.Integer
                => EvaluateCheckedIntegerOperation(
                    () => checked(left.AsInteger() - right.AsInteger()),
                    "Результат операции '-'",
                    expression.Location),
            SyntaxKind.MinusToken when expression.Type == TypeSymbol.Long
                => EvaluateCheckedLongOperation(
                    () => checked(left.AsLong() - right.AsLong()),
                    "Результат операции '-'",
                    expression.Location),
            SyntaxKind.MinusToken when expression.Type == TypeSymbol.Double
                => EnsureFiniteDoubleResult(left.AsDouble() - right.AsDouble(), "Результат операции '-'", expression.Location),
            SyntaxKind.MinusToken when expression.Type == TypeSymbol.Decimal
                => EvaluateCheckedDecimalOperation(
                    () => checked(left.AsDecimal() - right.AsDecimal()),
                    "Результат операции '-'",
                    expression.Location),
            SyntaxKind.StarToken when expression.Type == TypeSymbol.Integer
                => EvaluateCheckedIntegerOperation(
                    () => checked(left.AsInteger() * right.AsInteger()),
                    "Результат операции '*'",
                    expression.Location),
            SyntaxKind.StarToken when expression.Type == TypeSymbol.Long
                => EvaluateCheckedLongOperation(
                    () => checked(left.AsLong() * right.AsLong()),
                    "Результат операции '*'",
                    expression.Location),
            SyntaxKind.StarToken when expression.Type == TypeSymbol.Double
                => EnsureFiniteDoubleResult(left.AsDouble() * right.AsDouble(), "Результат операции '*'", expression.Location),
            SyntaxKind.StarToken when expression.Type == TypeSymbol.Decimal
                => EvaluateCheckedDecimalOperation(
                    () => checked(left.AsDecimal() * right.AsDecimal()),
                    "Результат операции '*'",
                    expression.Location),
            SyntaxKind.CaretToken => EvaluatePower(expression, left, right),
            SyntaxKind.SlashToken => EvaluateDivision(expression, left, right),
            SyntaxKind.BackslashToken => EvaluateIntegralDivision(expression, left, right),
            SyntaxKind.ModKeyword => EvaluateRemainder(expression, left, right),
            SyntaxKind.EqualsToken => RuntimeValue.FromBoolean(ValuesEqual(left, right)),
            SyntaxKind.NotEqualsToken => RuntimeValue.FromBoolean(!ValuesEqual(left, right)),
            SyntaxKind.LessToken => RuntimeValue.FromBoolean(CompareWithDiagnostics(left, right, expression.Location) < 0),
            SyntaxKind.LessOrEqualsToken => RuntimeValue.FromBoolean(CompareWithDiagnostics(left, right, expression.Location) <= 0),
            SyntaxKind.GreaterToken => RuntimeValue.FromBoolean(CompareWithDiagnostics(left, right, expression.Location) > 0),
            SyntaxKind.GreaterOrEqualsToken => RuntimeValue.FromBoolean(CompareWithDiagnostics(left, right, expression.Location) >= 0),
            SyntaxKind.AndKeyword when expression.Type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() && right.AsBoolean()),
            SyntaxKind.OrKeyword when expression.Type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() || right.AsBoolean()),
            SyntaxKind.XorKeyword when expression.Type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() ^ right.AsBoolean()),
            SyntaxKind.EqvKeyword when expression.Type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() == right.AsBoolean()),
            SyntaxKind.ImpKeyword when expression.Type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(!left.AsBoolean() || right.AsBoolean()),
            SyntaxKind.AndKeyword when expression.Type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() & right.AsInteger()),
            SyntaxKind.OrKeyword when expression.Type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() | right.AsInteger()),
            SyntaxKind.XorKeyword when expression.Type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() ^ right.AsInteger()),
            SyntaxKind.EqvKeyword when expression.Type == TypeSymbol.Integer => RuntimeValue.FromInteger(~(left.AsInteger() ^ right.AsInteger())),
            SyntaxKind.ImpKeyword when expression.Type == TypeSymbol.Integer => RuntimeValue.FromInteger((~left.AsInteger()) | right.AsInteger()),
            SyntaxKind.AndKeyword when expression.Type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() & right.AsLong()),
            SyntaxKind.OrKeyword when expression.Type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() | right.AsLong()),
            SyntaxKind.XorKeyword when expression.Type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() ^ right.AsLong()),
            SyntaxKind.EqvKeyword when expression.Type == TypeSymbol.Long => RuntimeValue.FromLong(~(left.AsLong() ^ right.AsLong())),
            SyntaxKind.ImpKeyword when expression.Type == TypeSymbol.Long => RuntimeValue.FromLong((~left.AsLong()) | right.AsLong()),
            SyntaxKind.LessLessToken when expression.Type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() << GetShiftCount(right, expression.Location)),
            SyntaxKind.GreaterGreaterToken when expression.Type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() >> GetShiftCount(right, expression.Location)),
            SyntaxKind.LessLessToken when expression.Type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() << GetShiftCount(right, expression.Location)),
            SyntaxKind.GreaterGreaterToken when expression.Type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() >> GetShiftCount(right, expression.Location)),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Неподдерживаемый binary operator: {expression.OperatorKind}.", expression.Location)
        };
    }

    private RuntimeValue EvaluatePower(BoundBinaryExpression expression, RuntimeValue left, RuntimeValue right)
    {
        return EnsureFiniteDoubleResult(
            Math.Pow(left.AsDouble(), right.AsDouble()),
            "Результат операции '^'",
            expression.Location);
    }

    private RuntimeValue EvaluateDivision(BoundBinaryExpression expression, RuntimeValue left, RuntimeValue right)
    {
        if (expression.Type == TypeSymbol.Decimal)
        {
            var divisorDecimal = right.AsDecimal();

            if (divisorDecimal == 0m)
            {
                ReportDivisionByZero(expression.Location);
                throw new RuntimeFailureException();
            }

            return EvaluateCheckedDecimalOperation(
                () => left.AsDecimal() / divisorDecimal,
                "Результат операции '/'",
                expression.Location);
        }

        var divisor = right.AsDouble();

        if (divisor == 0d)
        {
            ReportDivisionByZero(expression.Location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(left.AsDouble() / divisor, "Результат операции '/'", expression.Location);
    }

    private RuntimeValue EvaluateIntegralDivision(BoundBinaryExpression expression, RuntimeValue left, RuntimeValue right)
    {
        if (expression.Type == TypeSymbol.Long)
        {
            var divisor = right.AsLong();

            if (divisor == 0L)
            {
                ReportDivisionByZero(expression.Location);
                throw new RuntimeFailureException();
            }

            return EvaluateCheckedLongOperation(
                () => checked(left.AsLong() / divisor),
                "Результат операции '\\'",
                expression.Location);
        }

        var divisorInteger = right.AsInteger();

        if (divisorInteger == 0)
        {
            ReportDivisionByZero(expression.Location);
            throw new RuntimeFailureException();
        }

        return EvaluateCheckedIntegerOperation(
            () => checked(left.AsInteger() / divisorInteger),
            "Результат операции '\\'",
            expression.Location);
    }

    private RuntimeValue EvaluateRemainder(BoundBinaryExpression expression, RuntimeValue left, RuntimeValue right)
    {
        if (expression.Type == TypeSymbol.Long)
        {
            var divisor = right.AsLong();

            if (divisor == 0L)
            {
                ReportDivisionByZero(expression.Location);
                throw new RuntimeFailureException();
            }

            return EvaluateCheckedLongOperation(
                () => checked(left.AsLong() % divisor),
                "Результат операции 'MOD'",
                expression.Location);
        }

        var divisorInteger = right.AsInteger();

        if (divisorInteger == 0)
        {
            ReportDivisionByZero(expression.Location);
            throw new RuntimeFailureException();
        }

        return EvaluateCheckedIntegerOperation(
            () => checked(left.AsInteger() % divisorInteger),
            "Результат операции 'MOD'",
            expression.Location);
    }

    private int GetShiftCount(RuntimeValue value, TextLocation location)
    {
        var count = value.Type == TypeSymbol.Long
            ? (int)value.AsLong()
            : value.AsInteger();

        if (count < 0 || count > 63)
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Сдвиг на {count} вне допустимого диапазона 0..63.",
                location);
            throw new RuntimeFailureException();
        }

        return count;
    }

    private RuntimeValue EvaluateCallExpression(BoundCallExpression expression)
    {
        var target = EvaluateExpression(expression.Target);
        var arguments = expression.Arguments
            .Select(EvaluateExpression)
            .ToArray();
        var functionValue = target.AsFunction();

        if (functionValue is null)
        {
            ReportEmptyFunctionValue(expression.Target.Location);
            throw new RuntimeFailureException();
        }

        if (functionValue.Function.IsBuiltIn)
        {
            return InvokeBuiltIn(expression, functionValue.Function, arguments);
        }

        return InvokeUserRoutine(functionValue, arguments, expression.Location, expression.Arguments);
    }

    private RuntimeValue EvaluateConversionExpression(BoundConversionExpression expression)
    {
        var value = EvaluateExpression(expression.Expression);

        if (expression.Type == value.Type)
        {
            return value;
        }

        if (expression.Type == TypeSymbol.Double && value.Type == TypeSymbol.Integer)
        {
            return RuntimeValue.FromDouble(value.AsInteger());
        }

        if (expression.Type == TypeSymbol.Long && value.Type == TypeSymbol.Integer)
        {
            return RuntimeValue.FromLong(value.AsInteger());
        }

        if (expression.Type == TypeSymbol.Double && value.Type == TypeSymbol.Long)
        {
            return RuntimeValue.FromDouble(value.AsLong());
        }

        if (expression.Type == TypeSymbol.Double && value.Type == TypeSymbol.Decimal)
        {
            return RuntimeValue.FromDouble((double)value.AsDecimal());
        }

        if (expression.Type == TypeSymbol.Decimal && value.Type == TypeSymbol.Integer)
        {
            return RuntimeValue.FromDecimal(value.AsInteger());
        }

        if (expression.Type == TypeSymbol.Decimal && value.Type == TypeSymbol.Long)
        {
            return RuntimeValue.FromDecimal(value.AsLong());
        }

        if (BuiltinDateTimeHelpers.TryConvertTemporalValue(value, expression.Type, out var temporalValue))
        {
            return temporalValue;
        }

        return ReportInvalidRuntimeState<RuntimeValue>(
            $"Неподдерживаемое преобразование: {value.Type.Name} -> {expression.Type.Name}.",
            expression.Location);
    }

    private static bool ValuesEqual(RuntimeValue left, RuntimeValue right)
    {
        return RuntimeValueHelpers.ValuesEqual(left, right);
    }

    private static int Compare(RuntimeValue left, RuntimeValue right)
    {
        return RuntimeValueHelpers.Compare(left, right);
    }

    private int CompareWithDiagnostics(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        try
        {
            return RuntimeValueHelpers.Compare(left, right);
        }
        catch (InvalidOperationException)
        {
            return ReportInvalidRuntimeState<int>(
                $"Сравнение не поддерживается для типа '{left.Type.Name}'.",
                location);
        }
    }

    private static bool RecordsEqual(RuntimeRecordValue left, RuntimeRecordValue right)
    {
        return RuntimeValueHelpers.RecordsEqual(left, right);
    }
}

