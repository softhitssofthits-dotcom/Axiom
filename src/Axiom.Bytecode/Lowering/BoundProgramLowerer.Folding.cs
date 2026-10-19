using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    private static bool IsUnconditionalControlTransfer(LoweredInstruction instruction)
    {
        return instruction.Kind is LoweredInstructionKind.Return
            or LoweredInstructionKind.Jump;
    }

    private static bool TryFoldUnary(SyntaxKind operatorKind, object? operandValue, TypeSymbol operandType, out object? result)
    {
        result = null;

        if (operandValue is null)
        {
            return false;
        }

        try
        {
            if (operatorKind == SyntaxKind.MinusToken)
            {
                if (operandType == TypeSymbol.Integer)
                {
                    result = checked(-(int)operandValue);
                    return true;
                }

                if (operandType == TypeSymbol.Long)
                {
                    result = checked(-(long)operandValue);
                    return true;
                }

                if (operandType == TypeSymbol.Double)
                {
                    var value = -(double)operandValue;
                    if (double.IsFinite(value))
                    {
                        result = value;
                        return true;
                    }

                    return false;
                }

                if (operandType == TypeSymbol.Decimal)
                {
                    result = checked(-(decimal)operandValue);
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.NotKeyword)
            {
                if (operandType == TypeSymbol.Boolean)
                {
                    result = !(bool)operandValue;
                    return true;
                }

                if (operandType == TypeSymbol.Integer)
                {
                    result = ~(int)operandValue;
                    return true;
                }

                if (operandType == TypeSymbol.Long)
                {
                    result = ~(long)operandValue;
                    return true;
                }
            }
        }
        catch (OverflowException)
        {
        }

        return false;
    }

    private static bool TryFoldBinary(
        SyntaxKind operatorKind,
        object? leftValue, TypeSymbol leftType,
        object? rightValue, TypeSymbol rightType,
        TypeSymbol resultType,
        out object? result)
    {
        result = null;

        if (leftValue is null || rightValue is null)
        {
            return false;
        }

        try
        {
            if (operatorKind == SyntaxKind.PlusToken)
            {
                if (leftType == TypeSymbol.Integer && rightType == TypeSymbol.Integer)
                {
                    result = checked((int)leftValue + (int)rightValue);
                    return true;
                }

                if (leftType == TypeSymbol.Long && rightType == TypeSymbol.Long)
                {
                    result = checked((long)leftValue + (long)rightValue);
                    return true;
                }

                if (leftType == TypeSymbol.Double && rightType == TypeSymbol.Double)
                {
                    var value = (double)leftValue + (double)rightValue;
                    if (double.IsFinite(value))
                    {
                        result = value;
                        return true;
                    }

                    return false;
                }

                if (leftType == TypeSymbol.Decimal && rightType == TypeSymbol.Decimal)
                {
                    result = checked((decimal)leftValue + (decimal)rightValue);
                    return true;
                }

                if (leftType == TypeSymbol.String && rightType == TypeSymbol.String)
                {
                    result = (string)leftValue + (string)rightValue;
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.MinusToken)
            {
                if (leftType == TypeSymbol.Integer && rightType == TypeSymbol.Integer)
                {
                    result = checked((int)leftValue - (int)rightValue);
                    return true;
                }

                if (leftType == TypeSymbol.Long && rightType == TypeSymbol.Long)
                {
                    result = checked((long)leftValue - (long)rightValue);
                    return true;
                }

                if (leftType == TypeSymbol.Double && rightType == TypeSymbol.Double)
                {
                    var value = (double)leftValue - (double)rightValue;
                    if (double.IsFinite(value))
                    {
                        result = value;
                        return true;
                    }

                    return false;
                }

                if (leftType == TypeSymbol.Decimal && rightType == TypeSymbol.Decimal)
                {
                    result = checked((decimal)leftValue - (decimal)rightValue);
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.StarToken)
            {
                if (leftType == TypeSymbol.Integer && rightType == TypeSymbol.Integer)
                {
                    result = checked((int)leftValue * (int)rightValue);
                    return true;
                }

                if (leftType == TypeSymbol.Long && rightType == TypeSymbol.Long)
                {
                    result = checked((long)leftValue * (long)rightValue);
                    return true;
                }

                if (leftType == TypeSymbol.Double && rightType == TypeSymbol.Double)
                {
                    var value = (double)leftValue * (double)rightValue;
                    if (double.IsFinite(value))
                    {
                        result = value;
                        return true;
                    }

                    return false;
                }

                if (leftType == TypeSymbol.Decimal && rightType == TypeSymbol.Decimal)
                {
                    result = checked((decimal)leftValue * (decimal)rightValue);
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.SlashToken)
            {
                if (leftType == TypeSymbol.Double && rightType == TypeSymbol.Double)
                {
                    var divisor = (double)rightValue;
                    if (divisor == 0d)
                    {
                        return false;
                    }

                    var value = (double)leftValue / divisor;
                    if (double.IsFinite(value))
                    {
                        result = value;
                        return true;
                    }

                    return false;
                }

                if (leftType == TypeSymbol.Decimal && rightType == TypeSymbol.Decimal)
                {
                    var divisor = (decimal)rightValue;
                    if (divisor == 0m)
                    {
                        return false;
                    }

                    result = (decimal)leftValue / divisor;
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.AmpersandToken)
            {
                if (leftType == TypeSymbol.String && rightType == TypeSymbol.String)
                {
                    result = (string)leftValue + (string)rightValue;
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.EqualsToken)
            {
                if (leftType == TypeSymbol.Integer && rightType == TypeSymbol.Integer)
                {
                    result = (int)leftValue == (int)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.Long && rightType == TypeSymbol.Long)
                {
                    result = (long)leftValue == (long)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.Boolean && rightType == TypeSymbol.Boolean)
                {
                    result = (bool)leftValue == (bool)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.String && rightType == TypeSymbol.String)
                {
                    result = string.Equals((string)leftValue, (string)rightValue, StringComparison.Ordinal);
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.NotEqualsToken)
            {
                if (leftType == TypeSymbol.Integer && rightType == TypeSymbol.Integer)
                {
                    result = (int)leftValue != (int)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.Long && rightType == TypeSymbol.Long)
                {
                    result = (long)leftValue != (long)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.Boolean && rightType == TypeSymbol.Boolean)
                {
                    result = (bool)leftValue != (bool)rightValue;
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.AndKeyword)
            {
                if (leftType == TypeSymbol.Boolean && rightType == TypeSymbol.Boolean)
                {
                    result = (bool)leftValue && (bool)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.Integer && rightType == TypeSymbol.Integer)
                {
                    result = (int)leftValue & (int)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.Long && rightType == TypeSymbol.Long)
                {
                    result = (long)leftValue & (long)rightValue;
                    return true;
                }
            }

            if (operatorKind == SyntaxKind.OrKeyword)
            {
                if (leftType == TypeSymbol.Boolean && rightType == TypeSymbol.Boolean)
                {
                    result = (bool)leftValue || (bool)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.Integer && rightType == TypeSymbol.Integer)
                {
                    result = (int)leftValue | (int)rightValue;
                    return true;
                }

                if (leftType == TypeSymbol.Long && rightType == TypeSymbol.Long)
                {
                    result = (long)leftValue | (long)rightValue;
                    return true;
                }
            }
        }
        catch (OverflowException)
        {
        }
        catch (DivideByZeroException)
        {
        }

        return false;
    }

    private static bool TryFoldConversion(object? value, TypeSymbol fromType, TypeSymbol toType, out object? result)
    {
        result = null;

        if (value is null)
        {
            return false;
        }

        try
        {
            if (fromType == TypeSymbol.Integer)
            {
                var intValue = (int)value;

                if (toType == TypeSymbol.Long)
                {
                    result = (long)intValue;
                    return true;
                }

                if (toType == TypeSymbol.Double)
                {
                    result = (double)intValue;
                    return true;
                }

                if (toType == TypeSymbol.Decimal)
                {
                    result = (decimal)intValue;
                    return true;
                }

                if (toType == TypeSymbol.String)
                {
                    result = intValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
            }

            if (fromType == TypeSymbol.Long)
            {
                var longValue = (long)value;

                if (toType == TypeSymbol.Integer)
                {
                    result = checked((int)longValue);
                    return true;
                }

                if (toType == TypeSymbol.Double)
                {
                    result = (double)longValue;
                    return true;
                }

                if (toType == TypeSymbol.Decimal)
                {
                    result = (decimal)longValue;
                    return true;
                }
            }

            if (fromType == TypeSymbol.Double)
            {
                var doubleValue = (double)value;

                if (toType == TypeSymbol.Integer)
                {
                    result = checked((int)doubleValue);
                    return true;
                }

                if (toType == TypeSymbol.Long)
                {
                    result = checked((long)doubleValue);
                    return true;
                }

                if (toType == TypeSymbol.Decimal)
                {
                    result = (decimal)doubleValue;
                    return true;
                }
            }

            if (fromType == TypeSymbol.Decimal)
            {
                var decimalValue = (decimal)value;

                if (toType == TypeSymbol.Integer)
                {
                    result = checked((int)decimalValue);
                    return true;
                }

                if (toType == TypeSymbol.Long)
                {
                    result = checked((long)decimalValue);
                    return true;
                }

                if (toType == TypeSymbol.Double)
                {
                    result = (double)decimalValue;
                    return true;
                }
            }

            if (fromType == TypeSymbol.String && toType == TypeSymbol.Integer)
            {
                if (int.TryParse((string)value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    result = parsed;
                    return true;
                }

                return false;
            }

            if (fromType == TypeSymbol.String && toType == TypeSymbol.Long)
            {
                if (long.TryParse((string)value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    result = parsed;
                    return true;
                }

                return false;
            }

            if (fromType == TypeSymbol.String && toType == TypeSymbol.Double)
            {
                if (double.TryParse((string)value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    result = parsed;
                    return true;
                }

                return false;
            }
        }
        catch (OverflowException)
        {
        }
        catch (FormatException)
        {
        }

        return false;
    }
}
