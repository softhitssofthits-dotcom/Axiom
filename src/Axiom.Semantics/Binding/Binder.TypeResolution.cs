using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private TypeSymbol ResolveType(TypeClauseSyntax typeClause)
    {
        ArgumentNullException.ThrowIfNull(typeClause);
        return ResolveType(typeClause.Type);
    }

    private TypeSymbol ResolveType(TypeSyntax typeSyntax)
    {
        ArgumentNullException.ThrowIfNull(typeSyntax);

        return typeSyntax switch
        {
            NamedTypeSyntax namedType => ResolveNamedType(namedType),
            FixedLengthStringTypeSyntax => TypeSymbol.String,
            FunctionTypeSyntax functionType => FunctionTypeSymbol.Create(
                functionType.ParameterTypes.Select(ResolveType).ToArray(),
                ResolveType(functionType.ReturnType),
                functionType.ParameterPassingKeywords.Select(ParameterPassingModes.FromKeyword).ToArray()),
            ArrayTypeSyntax arrayType => ArrayTypeSymbol.Create(ResolveType(arrayType.ElementType), arrayType.Rank),
            _ => TypeSymbol.Error
        };
    }

    private bool TryResolveFixedStringLength(
        string declarationName,
        TypeClauseSyntax typeClause,
        bool allowGlobalForwardConstants,
        out int? fixedStringLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(declarationName);
        ArgumentNullException.ThrowIfNull(typeClause);

        fixedStringLength = null;

        if (typeClause.Type is not FixedLengthStringTypeSyntax fixedStringType)
        {
            if (TryFindFixedStringType(typeClause.Type, out var nestedFixedStringType))
            {
                ReportInvalidFixedStringDeclaration(
                    declarationName,
                    GetLocation(nestedFixedStringType.LengthExpression),
                    "Сейчас форма `STRING * N` поддержана только как непосредственный строковый тип в `DIM` и строковых полях `TYPE`.");
                return false;
            }

            return true;
        }

        if (!TryEvaluateFixedStringLength(
                fixedStringType.LengthExpression,
                allowGlobalForwardConstants,
                out var resolvedLength))
        {
            ReportInvalidFixedStringDeclaration(
                fixedStringType.StringKeyword.Text,
                GetLocation(fixedStringType.LengthExpression),
                "Длина после `STRING *` должна быть целочисленной константой в диапазоне от 1 до 65535.");
            return false;
        }

        fixedStringLength = resolvedLength;
        return true;
    }

    private bool ReportUnsupportedFixedStringType(TypeClauseSyntax typeClause, string declarationName)
    {
        ArgumentNullException.ThrowIfNull(typeClause);
        ArgumentException.ThrowIfNullOrWhiteSpace(declarationName);

        if (!TryFindFixedStringType(typeClause.Type, out var fixedStringType))
        {
            return false;
        }

        ReportInvalidFixedStringDeclaration(
            declarationName,
            GetLocation(fixedStringType.LengthExpression),
            "Сейчас форма `STRING * N` поддержана только в `DIM` и строковых полях `TYPE`.");
        return true;
    }

    private static bool TryFindFixedStringType(TypeSyntax typeSyntax, out FixedLengthStringTypeSyntax fixedStringType)
    {
        ArgumentNullException.ThrowIfNull(typeSyntax);

        switch (typeSyntax)
        {
            case FixedLengthStringTypeSyntax directFixedStringType:
                fixedStringType = directFixedStringType;
                return true;

            case FunctionTypeSyntax functionType:
                foreach (var parameterType in functionType.ParameterTypes)
                {
                    if (TryFindFixedStringType(parameterType, out fixedStringType))
                    {
                        return true;
                    }
                }

                return TryFindFixedStringType(functionType.ReturnType, out fixedStringType);

            case ArrayTypeSyntax arrayType:
                return TryFindFixedStringType(arrayType.ElementType, out fixedStringType);

            default:
                fixedStringType = null!;
                return false;
        }
    }

    private bool TryEvaluateFixedStringLength(
        ExpressionSyntax expression,
        bool allowGlobalForwardConstants,
        out int fixedStringLength)
    {
        ArgumentNullException.ThrowIfNull(expression);

        fixedStringLength = 0;
        long rawValue;

        if (allowGlobalForwardConstants)
        {
            if (!TryEvaluateGlobalIntegralConstantCore(expression, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out rawValue))
            {
                return false;
            }
        }
        else
        {
            var boundExpression = BindExpression(expression);

            if (boundExpression.Type == TypeSymbol.Error
                || !TryEvaluateBoundIntegralConstant(boundExpression, out rawValue))
            {
                return false;
            }
        }

        if (rawValue is < 1 or > 65535)
        {
            return false;
        }

        fixedStringLength = (int)rawValue;
        return true;
    }

    private bool TryEvaluateBoundIntegralConstant(BoundExpression expression, out long value)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return TryEvaluateBoundIntegralConstant(expression, new HashSet<VariableSymbol>(), out value);
    }

    private bool TryEvaluateBoundIntegralConstant(
        BoundExpression expression,
        HashSet<VariableSymbol> activeConstants,
        out long value)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(activeConstants);

        switch (expression)
        {
            case BoundLiteralExpression literal when literal.Type == TypeSymbol.Integer:
                value = (int)literal.Value!;
                return true;

            case BoundLiteralExpression literal when literal.Type == TypeSymbol.Long:
                value = (long)literal.Value!;
                return true;

            case BoundVariableExpression variableExpression when variableExpression.Variable.IsReadOnly:
                return _boundIntegralConstants.TryGetValue(variableExpression.Variable, out value);

            case BoundConversionExpression conversion
                when conversion.Type == TypeSymbol.Long && conversion.Expression.Type == TypeSymbol.Integer:
                if (TryEvaluateBoundIntegralConstant(conversion.Expression, activeConstants, out var convertedValue))
                {
                    value = convertedValue;
                    return true;
                }

                break;

            case BoundUnaryExpression unary
                when unary.OperatorKind is SyntaxKind.PlusToken or SyntaxKind.MinusToken:
                if (!TryEvaluateBoundIntegralConstant(unary.Operand, activeConstants, out var operandValue))
                {
                    break;
                }

                if (unary.OperatorKind == SyntaxKind.PlusToken)
                {
                    value = operandValue;
                    return true;
                }

                try
                {
                    value = checked(-operandValue);
                    return true;
                }
                catch (OverflowException)
                {
                    break;
                }

            case BoundBinaryExpression binary
                when binary.OperatorKind is SyntaxKind.PlusToken
                    or SyntaxKind.MinusToken
                    or SyntaxKind.StarToken
                    or SyntaxKind.BackslashToken
                    or SyntaxKind.ModKeyword:
                if (!TryEvaluateBoundIntegralConstant(binary.Left, activeConstants, out var leftValue)
                    || !TryEvaluateBoundIntegralConstant(binary.Right, activeConstants, out var rightValue))
                {
                    break;
                }

                try
                {
                    value = binary.OperatorKind switch
                    {
                        SyntaxKind.PlusToken => checked(leftValue + rightValue),
                        SyntaxKind.MinusToken => checked(leftValue - rightValue),
                        SyntaxKind.StarToken => checked(leftValue * rightValue),
                        SyntaxKind.BackslashToken when rightValue != 0 => checked(leftValue / rightValue),
                        SyntaxKind.ModKeyword when rightValue != 0 => checked(leftValue % rightValue),
                        _ => 0
                    };

                    return binary.OperatorKind is not (SyntaxKind.BackslashToken or SyntaxKind.ModKeyword) || rightValue != 0;
                }
                catch (OverflowException)
                {
                    break;
                }
        }

        value = 0;
        return false;
    }

    private TypeSymbol ResolveNamedType(NamedTypeSyntax typeSyntax)
    {
        var builtInType = TypeSymbol.FromSyntaxKind(typeSyntax.TypeToken.Kind);

        if (builtInType != TypeSymbol.Error)
        {
            return builtInType;
        }

        if (typeSyntax.TypeToken.Kind == SyntaxKind.IdentifierToken
            && _declaredTypes.TryGetValue(typeSyntax.TypeToken.Text, out var userType))
        {
            _editorReferences?.Add(new(userType, typeSyntax.TypeToken.Location, false));
            return userType;
        }

        if (typeSyntax.TypeToken.Kind == SyntaxKind.IdentifierToken
            && _importedTypes.TryGetValue(typeSyntax.TypeToken.Text, out var importedType))
        {
            _editorReferences?.Add(new(importedType, typeSyntax.TypeToken.Location, false));
            return importedType;
        }

        ReportUnknownType(typeSyntax.TypeToken.Text, typeSyntax.TypeToken.Location);
        return TypeSymbol.Error;
    }

    private void ReportCyclicTypeFields()
    {
        var states = new Dictionary<UserDefinedTypeSymbol, int>();
        var cycleReported = false;

        foreach (var type in _declaredTypes.Values)
        {
            Visit(type, type);
        }

        bool Visit(UserDefinedTypeSymbol type, UserDefinedTypeSymbol root)
        {
            if (states.TryGetValue(type, out var state))
            {
                if (state == 1)
                {
                    if (!cycleReported)
                    {
                        ReportCyclicType(root.Name, DefaultLocation);
                        cycleReported = true;
                    }

                    return true;
                }

                return false;
            }

            states[type] = 1;

            foreach (var field in type.Fields)
            {
                if (TryGetNestedTypeForCycleCheck(field) is { } nested && Visit(nested, root))
                {
                    return true;
                }
            }

            states[type] = 2;
            return false;
        }
    }

    private static UserDefinedTypeSymbol? TryGetNestedTypeForCycleCheck(TypeFieldSymbol field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (field.Type is UserDefinedTypeSymbol nestedType)
        {
            return nestedType;
        }

        if (field.Type is ArrayTypeSymbol { ElementType: UserDefinedTypeSymbol arrayElementType })
        {
            return arrayElementType;
        }

        return null;
    }
}
