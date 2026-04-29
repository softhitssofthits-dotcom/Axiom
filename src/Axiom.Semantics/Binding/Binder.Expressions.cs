using Axiom.Core;
using System.Globalization;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundExpression BindExpression(ExpressionSyntax expression, TypeSymbol? expectedType = null)
    {
        var boundExpression = expression switch
        {
            LiteralExpressionSyntax literalExpression => BindLiteralExpression(literalExpression),
            NameExpressionSyntax nameExpression => BindNameExpression(nameExpression),
            UnaryExpressionSyntax unaryExpression => BindUnaryExpression(unaryExpression),
            BinaryExpressionSyntax binaryExpression => BindBinaryExpression(binaryExpression),
            RangeExpressionSyntax rangeExpression => BindRangeExpression(rangeExpression, reportInvalidStandaloneUse: true),
            ParenthesizedExpressionSyntax parenthesizedExpression => BindExpression(parenthesizedExpression.Expression),
            CallExpressionSyntax callExpression => BindCallExpression(callExpression),
            ArrayAccessExpressionSyntax arrayAccessExpression => BindArrayAccessExpression(arrayAccessExpression),
            FieldAccessExpressionSyntax fieldAccessExpression => BindFieldAccessExpression(fieldAccessExpression),
            ErrorExpressionSyntax errorExpression => new BoundErrorExpression(errorExpression.Token.Location),
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unsupported expression.")
        };

        if (expectedType is null || expectedType == TypeSymbol.Error || boundExpression.Type == TypeSymbol.Error)
        {
            return boundExpression;
        }

        if (boundExpression.Type == expectedType)
        {
            return boundExpression;
        }

        if (TryConvertConstant(expression, boundExpression, expectedType, out var convertedConstant))
        {
            return convertedConstant;
        }

        if (CanConvert(boundExpression.Type, expectedType))
        {
            return new BoundConversionExpression(boundExpression.Location, expectedType, boundExpression);
        }

        ReportTypeMismatch(boundExpression.Type, expectedType, GetLocation(expression));
        return new BoundErrorExpression(GetLocation(expression));
    }

    private BoundExpression BindLiteralExpression(LiteralExpressionSyntax expression)
    {
        var type = expression.LiteralToken.Kind switch
        {
            SyntaxKind.IntegerLiteralToken => TypeSymbol.Integer,
            SyntaxKind.LongLiteralToken => TypeSymbol.Long,
            SyntaxKind.DoubleLiteralToken => TypeSymbol.Double,
            SyntaxKind.StringLiteralToken => TypeSymbol.String,
            SyntaxKind.BooleanLiteralToken => TypeSymbol.Boolean,
            _ => TypeSymbol.Error
        };

        return new BoundLiteralExpression(expression.LiteralToken.Location, expression.LiteralToken.Value, type);
    }

    private BoundExpression BindNameExpression(NameExpressionSyntax expression)
    {
        if (_scope.TryLookupVariable(expression.IdentifierToken.Text, out var variable) && variable is not null)
        {
            return new BoundVariableExpression(expression.IdentifierToken.Location, variable);
        }

        if (TryBindPredefinedConstant(expression.IdentifierToken.Text, expression.IdentifierToken.Location, out var predefinedConstant))
        {
            return predefinedConstant;
        }

        if (_scope.TryLookupFunction(expression.IdentifierToken.Text, out var function) && function is not null)
        {
            if (function.IsSub)
            {
                ReportProcedureCannotBeUsedAsValue(function.Name, expression.IdentifierToken.Location);
                return new BoundErrorExpression(expression.IdentifierToken.Location);
            }

            return new BoundFunctionExpression(expression.IdentifierToken.Location, function);
        }

        ReportUnknownName(expression.IdentifierToken.Text, expression.IdentifierToken.Location);
        return new BoundErrorExpression(expression.IdentifierToken.Location);
    }

    private BoundExpression BindUnaryExpression(UnaryExpressionSyntax expression)
    {
        var operand = BindExpression(expression.Operand);

        if (operand.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(GetLocation(expression));
        }

        if (expression.OperatorToken.Kind == SyntaxKind.MinusToken
            && TryFoldNegatedLiteral(operand, expression.OperatorToken.Location, out var foldedLiteral))
        {
            return foldedLiteral;
        }

        return expression.OperatorToken.Kind switch
            {
            SyntaxKind.MinusToken when IsNumeric(operand.Type) => new BoundUnaryExpression(expression.OperatorToken.Location, expression.OperatorToken.Kind, operand, operand.Type),
            SyntaxKind.NotKeyword when operand.Type == TypeSymbol.Boolean => new BoundUnaryExpression(expression.OperatorToken.Location, expression.OperatorToken.Kind, operand, TypeSymbol.Boolean),
            SyntaxKind.NotKeyword when IsIntegral(operand.Type) => new BoundUnaryExpression(expression.OperatorToken.Location, expression.OperatorToken.Kind, operand, operand.Type),
            _ => ReportInvalidUnaryOperator(expression.OperatorToken.Kind, operand.Type, expression.OperatorToken.Location)
        };
    }

    private BoundExpression BindBinaryExpression(BinaryExpressionSyntax expression)
    {
        var left = BindExpression(expression.Left);
        var right = BindExpression(expression.Right);

        if (left.Type == TypeSymbol.Error || right.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(GetLocation(expression));
        }

        return expression.OperatorToken.Kind switch
        {
            SyntaxKind.PlusToken => BindPlusExpression(left, right, expression),
            SyntaxKind.MinusToken or SyntaxKind.StarToken => BindNumericBinaryExpression(left, right, expression, useFloatingPointResult: false),
            SyntaxKind.SlashToken => BindDivisionExpression(left, right, expression),
            SyntaxKind.CaretToken => BindPowerExpression(left, right, expression),
            SyntaxKind.BackslashToken or SyntaxKind.ModKeyword => BindIntegralBinaryExpression(left, right, expression),
            SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or SyntaxKind.LessToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterToken or SyntaxKind.GreaterOrEqualsToken
                => BindComparisonExpression(left, right, expression),
            SyntaxKind.AndKeyword or SyntaxKind.OrKeyword or SyntaxKind.XorKeyword or SyntaxKind.EqvKeyword or SyntaxKind.ImpKeyword => BindLogicalExpression(left, right, expression),
            SyntaxKind.LessLessToken or SyntaxKind.GreaterGreaterToken => BindShiftExpression(left, right, expression),
            _ => ReportInvalidBinaryOperator(expression.OperatorToken.Kind, left.Type, right.Type, expression.OperatorToken.Location)
        };
    }

    private BoundExpression BindRangeExpression(RangeExpressionSyntax expression, bool reportInvalidStandaloneUse)
    {
        var lowerBound = BindNumericExpression(expression.LowerBound);
        var upperBound = BindNumericExpression(expression.UpperBound);

        if (reportInvalidStandaloneUse)
        {
            ReportInvalidRangeExpression(expression.ToKeyword.Location);
        }

        return new BoundArrayRangeExpression(expression.ToKeyword.Location, lowerBound, upperBound);
    }

    private BoundExpression BindCondition(ExpressionSyntax expression)
    {
        var condition = BindExpression(expression);

        if (condition.Type == TypeSymbol.Error)
        {
            return condition;
        }

        if (condition.Type == TypeSymbol.Boolean)
        {
            return condition;
        }

        ReportInvalidCondition(condition.Type, GetLocation(expression));
        return new BoundErrorExpression(GetLocation(expression));
    }

    private BoundExpression BindNumericExpression(ExpressionSyntax expression)
    {
        var boundExpression = BindExpression(expression);

        if (boundExpression.Type == TypeSymbol.Error)
        {
            return boundExpression;
        }

        if (IsNumeric(boundExpression.Type))
        {
            return boundExpression;
        }

        ReportTypeMismatch(boundExpression.Type, TypeSymbol.Integer, GetLocation(expression));
        return new BoundErrorExpression(GetLocation(expression));
    }

    private BoundExpression BindPlusExpression(BoundExpression left, BoundExpression right, BinaryExpressionSyntax syntax)
    {
        if (left.Type == TypeSymbol.String && right.Type == TypeSymbol.String)
        {
            return new BoundBinaryExpression(syntax.OperatorToken.Location, left, syntax.OperatorToken.Kind, right, TypeSymbol.String);
        }

        return BindNumericBinaryExpression(left, right, syntax, useFloatingPointResult: false);
    }

    private BoundExpression BindNumericBinaryExpression(
        BoundExpression left,
        BoundExpression right,
        BinaryExpressionSyntax syntax,
        bool useFloatingPointResult)
    {
        if (!IsNumeric(left.Type) || !IsNumeric(right.Type))
        {
            return ReportInvalidBinaryOperator(syntax.OperatorToken.Kind, left.Type, right.Type, syntax.OperatorToken.Location);
        }

        var operandType = PromoteNumericType(left.Type, right.Type, useFloatingPointResult);
        var leftOperand = BindExpression(syntax.Left, operandType);
        var rightOperand = BindExpression(syntax.Right, operandType);
        var resultType = useFloatingPointResult ? TypeSymbol.Double : operandType;

        return new BoundBinaryExpression(syntax.OperatorToken.Location, leftOperand, syntax.OperatorToken.Kind, rightOperand, resultType);
    }

    private BoundExpression BindDivisionExpression(BoundExpression left, BoundExpression right, BinaryExpressionSyntax syntax)
    {
        if (!IsNumeric(left.Type) || !IsNumeric(right.Type))
        {
            return ReportInvalidBinaryOperator(syntax.OperatorToken.Kind, left.Type, right.Type, syntax.OperatorToken.Location);
        }

        var operandType = left.Type == TypeSymbol.Double || right.Type == TypeSymbol.Double
            ? TypeSymbol.Double
            : left.Type == TypeSymbol.Decimal || right.Type == TypeSymbol.Decimal
                ? TypeSymbol.Decimal
                : TypeSymbol.Double;

        var leftOperand = BindExpression(syntax.Left, operandType);
        var rightOperand = BindExpression(syntax.Right, operandType);

        if (leftOperand.Type == TypeSymbol.Error || rightOperand.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(syntax.OperatorToken.Location);
        }

        return new BoundBinaryExpression(syntax.OperatorToken.Location, leftOperand, syntax.OperatorToken.Kind, rightOperand, operandType);
    }

    private BoundExpression BindIntegralBinaryExpression(BoundExpression left, BoundExpression right, BinaryExpressionSyntax syntax)
    {
        if (!IsIntegral(left.Type) || !IsIntegral(right.Type))
        {
            return ReportInvalidBinaryOperator(syntax.OperatorToken.Kind, left.Type, right.Type, syntax.OperatorToken.Location);
        }

        var operandType = PromoteNumericType(left.Type, right.Type);
        var leftOperand = BindExpression(syntax.Left, operandType);
        var rightOperand = BindExpression(syntax.Right, operandType);

        if (leftOperand.Type == TypeSymbol.Error || rightOperand.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(syntax.OperatorToken.Location);
        }

        return new BoundBinaryExpression(syntax.OperatorToken.Location, leftOperand, syntax.OperatorToken.Kind, rightOperand, operandType);
    }

    private BoundExpression BindPowerExpression(BoundExpression left, BoundExpression right, BinaryExpressionSyntax syntax)
    {
        if (!IsNumeric(left.Type) || !IsNumeric(right.Type))
        {
            return ReportInvalidBinaryOperator(syntax.OperatorToken.Kind, left.Type, right.Type, syntax.OperatorToken.Location);
        }

        var leftOperand = ConvertNumericExpressionToDouble(syntax.Left, left);
        var rightOperand = ConvertNumericExpressionToDouble(syntax.Right, right);

        if (leftOperand.Type == TypeSymbol.Error || rightOperand.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(syntax.OperatorToken.Location);
        }

        return new BoundBinaryExpression(syntax.OperatorToken.Location, leftOperand, syntax.OperatorToken.Kind, rightOperand, TypeSymbol.Double);
    }

    private BoundExpression BindComparisonExpression(BoundExpression left, BoundExpression right, BinaryExpressionSyntax syntax)
    {
        if (IsNumeric(left.Type) && IsNumeric(right.Type))
        {
            var operandType = PromoteNumericType(left.Type, right.Type);

            var leftOperand = BindExpression(syntax.Left, operandType);
            var rightOperand = BindExpression(syntax.Right, operandType);
            return new BoundBinaryExpression(syntax.OperatorToken.Location, leftOperand, syntax.OperatorToken.Kind, rightOperand, TypeSymbol.Boolean);
        }

        var isEqualityOnly = syntax.OperatorToken.Kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken;

        if (left.Type == right.Type && left.Type == TypeSymbol.String)
        {
            return new BoundBinaryExpression(syntax.OperatorToken.Location, left, syntax.OperatorToken.Kind, right, TypeSymbol.Boolean);
        }

        if (left.Type == right.Type && left.Type == TypeSymbol.Boolean && isEqualityOnly)
        {
            return new BoundBinaryExpression(syntax.OperatorToken.Location, left, syntax.OperatorToken.Kind, right, TypeSymbol.Boolean);
        }

        return ReportInvalidBinaryOperator(syntax.OperatorToken.Kind, left.Type, right.Type, syntax.OperatorToken.Location);
    }

    private BoundExpression BindLogicalExpression(BoundExpression left, BoundExpression right, BinaryExpressionSyntax syntax)
    {
        if (left.Type == TypeSymbol.Boolean && right.Type == TypeSymbol.Boolean)
        {
            return new BoundBinaryExpression(syntax.OperatorToken.Location, left, syntax.OperatorToken.Kind, right, TypeSymbol.Boolean);
        }

        if (IsIntegral(left.Type) && IsIntegral(right.Type))
        {
            var operandType = PromoteNumericType(left.Type, right.Type);
            var leftOperand = BindExpression(syntax.Left, operandType);
            var rightOperand = BindExpression(syntax.Right, operandType);

            if (leftOperand.Type == TypeSymbol.Error || rightOperand.Type == TypeSymbol.Error)
            {
                return new BoundErrorExpression(syntax.OperatorToken.Location);
            }

            return new BoundBinaryExpression(syntax.OperatorToken.Location, leftOperand, syntax.OperatorToken.Kind, rightOperand, operandType);
        }

        return ReportInvalidBinaryOperator(syntax.OperatorToken.Kind, left.Type, right.Type, syntax.OperatorToken.Location);
    }

    private BoundExpression BindShiftExpression(BoundExpression left, BoundExpression right, BinaryExpressionSyntax syntax)
    {
        if (!IsIntegral(left.Type) || !IsIntegral(right.Type))
        {
            return ReportInvalidBinaryOperator(syntax.OperatorToken.Kind, left.Type, right.Type, syntax.OperatorToken.Location);
        }

        return new BoundBinaryExpression(syntax.OperatorToken.Location, left, syntax.OperatorToken.Kind, right, left.Type);
    }

    private static BoundLiteralExpression CreateDefaultStepExpression(TextLocation location, TypeSymbol type)
    {
        if (type == TypeSymbol.Double)
        {
            return new BoundLiteralExpression(location, 1d, TypeSymbol.Double);
        }

        if (type == TypeSymbol.Decimal)
        {
            return new BoundLiteralExpression(location, 1m, TypeSymbol.Decimal);
        }

        if (type == TypeSymbol.Long)
        {
            return new BoundLiteralExpression(location, 1L, TypeSymbol.Long);
        }

        return new BoundLiteralExpression(location, 1, TypeSymbol.Integer);
    }

    private bool TryConvertConstant(ExpressionSyntax syntax, BoundExpression expression, TypeSymbol expectedType, out BoundExpression convertedExpression)
    {
        convertedExpression = expression;

        if (TryParseLiteralTextAsType(syntax, expectedType, out var constantValue))
        {
            convertedExpression = new BoundLiteralExpression(expression.Location, constantValue, expectedType);
            return true;
        }

        if (TryGetNumericLiteralText(syntax, out _)
            && (expectedType == TypeSymbol.Integer || expectedType == TypeSymbol.Long || expectedType == TypeSymbol.Decimal))
        {
            ReportConstantOutOfRange(expectedType, GetLocation(syntax));
            convertedExpression = new BoundErrorExpression(GetLocation(syntax));
            return true;
        }

        return false;
    }

    private static bool TryFoldNegatedLiteral(BoundExpression operand, TextLocation location, out BoundLiteralExpression literal)
    {
        if (operand is BoundLiteralExpression operandLiteral && operandLiteral.Value is not null)
        {
            if (operandLiteral.Type == TypeSymbol.Integer && operandLiteral.Value is int intValue)
            {
                literal = new BoundLiteralExpression(location, checked(-intValue), TypeSymbol.Integer);
                return true;
            }

            if (operandLiteral.Type == TypeSymbol.Long && operandLiteral.Value is long longValue)
            {
                literal = new BoundLiteralExpression(location, checked(-longValue), TypeSymbol.Long);
                return true;
            }

            if (operandLiteral.Type == TypeSymbol.Double && operandLiteral.Value is double doubleValue)
            {
                literal = new BoundLiteralExpression(location, -doubleValue, TypeSymbol.Double);
                return true;
            }

            if (operandLiteral.Type == TypeSymbol.Decimal && operandLiteral.Value is decimal decimalValue)
            {
                literal = new BoundLiteralExpression(location, checked(-decimalValue), TypeSymbol.Decimal);
                return true;
            }
        }

        literal = null!;
        return false;
    }

    private static BoundExpression ConvertNumericExpressionToDouble(ExpressionSyntax syntax, BoundExpression operand)
    {
        return operand.Type switch
        {
            var type when type == TypeSymbol.Double => operand,
            var type when type == TypeSymbol.Integer || type == TypeSymbol.Long || type == TypeSymbol.Decimal
                => new BoundConversionExpression(GetLocation(syntax), TypeSymbol.Double, operand),
            _ => new BoundErrorExpression(GetLocation(syntax))
        };
    }

    private bool TryResolveNumericBuiltinArgumentTypes(
        CallExpressionSyntax expression,
        IReadOnlyList<BoundExpression> rawArguments,
        BuiltinFunctionDescriptor descriptor,
        out IReadOnlyList<TypeSymbol> resolvedArgumentTypes)
    {
        switch (descriptor.OperandBindingPolicy)
        {
            case BuiltinOperandBindingPolicy.PreserveNumericArguments:
                resolvedArgumentTypes = rawArguments.Select(static argument => argument.Type).ToArray();
                return true;

            case BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble:
                resolvedArgumentTypes = Enumerable.Repeat(TypeSymbol.Double, rawArguments.Count).ToArray();
                return true;

            case BuiltinOperandBindingPolicy.PromoteNumericArguments:
            {
                var promotedType = rawArguments[0].Type;

                for (var index = 1; index < rawArguments.Count; index++)
                {
                    promotedType = PromoteNumericType(promotedType, rawArguments[index].Type);
                }

                for (var index = 0; index < rawArguments.Count; index++)
                {
                    if (promotedType == TypeSymbol.Double && rawArguments[index].Type == TypeSymbol.Decimal)
                    {
                        ReportTypeMismatch(TypeSymbol.Decimal, TypeSymbol.Double, GetLocation(expression.Arguments[index]));
                        resolvedArgumentTypes = Array.Empty<TypeSymbol>();
                        return false;
                    }
                }

                resolvedArgumentTypes = Enumerable.Repeat(promotedType, rawArguments.Count).ToArray();
                return true;
            }

            default:
                resolvedArgumentTypes = descriptor.Parameters.Select(static parameter => parameter.Type).ToArray();
                return true;
        }
    }

    private IReadOnlyList<BoundExpression> RebindNumericBuiltinArguments(
        IReadOnlyList<ExpressionSyntax> argumentSyntax,
        IReadOnlyList<BoundExpression> rawArguments,
        IReadOnlyList<TypeSymbol> resolvedArgumentTypes,
        BuiltinFunctionDescriptor descriptor)
    {
        var reboundArguments = new List<BoundExpression>(rawArguments.Count);

        for (var index = 0; index < rawArguments.Count; index++)
        {
            var argument = rawArguments[index];
            var targetType = resolvedArgumentTypes[index];

            if (argument.Type == targetType)
            {
                reboundArguments.Add(argument);
                continue;
            }

            if (descriptor.OperandBindingPolicy == BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble
                && targetType == TypeSymbol.Double)
            {
                reboundArguments.Add(new BoundConversionExpression(argument.Location, TypeSymbol.Double, argument));
                continue;
            }

            reboundArguments.Add(BindExpression(argumentSyntax[index], targetType));
        }

        return reboundArguments;
    }

    private static TypeSymbol ResolveNumericBuiltinReturnType(
        IReadOnlyList<BoundExpression> boundArguments,
        FunctionTypeSymbol functionType,
        BuiltinFunctionDescriptor descriptor)
    {
        return descriptor.ResultTypePolicy switch
        {
            BuiltinResultTypePolicy.SameAsFirstArgument when boundArguments.Count > 0 => boundArguments[0].Type,
            BuiltinResultTypePolicy.PromotedNumeric when boundArguments.Count > 0 => boundArguments[0].Type,
            BuiltinResultTypePolicy.Integer => TypeSymbol.Integer,
            BuiltinResultTypePolicy.Double => TypeSymbol.Double,
            BuiltinResultTypePolicy.String => TypeSymbol.String,
            _ => functionType.ReturnType
        };
    }
}
