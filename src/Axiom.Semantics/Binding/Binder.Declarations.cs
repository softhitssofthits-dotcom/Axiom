using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private void DeclareTypeNames(IEnumerable<MemberSyntax> members)
    {
        foreach (var typeDeclaration in members.OfType<TypeDeclarationSyntax>())
        {
            var name = typeDeclaration.Identifier.Text;

            if (TypeSymbol.FromSyntaxKind(typeDeclaration.Identifier.Kind) != TypeSymbol.Error)
            {
                ReportDuplicateDeclaration(name, typeDeclaration.Identifier.Location);
                continue;
            }

            if (!_declaredTypes.TryAdd(name, new UserDefinedTypeSymbol(name)))
            {
                ReportDuplicateDeclaration(name, typeDeclaration.Identifier.Location);
            }
        }
    }

    private void PrepareTypeDeclarations(IEnumerable<MemberSyntax> members)
    {
        foreach (var constStatement in members
                     .OfType<GlobalStatementSyntax>()
                     .Select(static statement => statement.Statement)
                     .OfType<ConstStatementSyntax>())
        {
            RegisterGlobalIntegralConstantDeclaration(constStatement);
        }

        foreach (var member in members)
        {
            switch (member)
            {
                case TypeDeclarationSyntax typeDeclaration:
                    if (!_declaredTypes.TryGetValue(typeDeclaration.Identifier.Text, out var type) || type.IsCompleted)
                    {
                        break;
                    }

                    var fields = new List<TypeFieldSymbol>();
                    var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var fieldDeclaration in typeDeclaration.Fields)
                    {
                        var fieldType = ResolveType(fieldDeclaration.TypeClause);

                        foreach (var declarator in fieldDeclaration.Declarators)
                        {
                            var hasFixedStringLength = TryResolveFixedStringLength(
                                declarator.Identifier.Text,
                                fieldDeclaration.TypeClause,
                                allowGlobalForwardConstants: true,
                                out var fixedStringLength);

                            if (!fieldNames.Add(declarator.Identifier.Text))
                            {
                                ReportDuplicateDeclaration(declarator.Identifier.Text, declarator.Identifier.Location);
                                continue;
                            }

                            if (!hasFixedStringLength)
                            {
                                fields.Add(new TypeFieldSymbol(declarator.Identifier.Text, TypeSymbol.Error));
                                continue;
                            }

                            fields.Add(BindTypeField(declarator, fieldType, fixedStringLength));
                        }
                    }

                    type.Complete(fields);
                    break;
            }
        }

        ReportCyclicTypeFields();
    }

    private void RegisterGlobalIntegralConstantDeclaration(ConstStatementSyntax statement)
    {
        ArgumentNullException.ThrowIfNull(statement);

        var declaration = statement.Declaration;

        if (declaration.TypeClause.Type is not NamedTypeSyntax namedType)
        {
            return;
        }

        if (namedType.TypeToken.Kind is not (SyntaxKind.IntegerKeyword or SyntaxKind.LongKeyword))
        {
            return;
        }

        _globalIntegralConstantDeclarations.TryAdd(declaration.Identifier.Text, statement);
    }

    private bool TryResolveGlobalIntegralConstant(string name, out long value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return TryResolveGlobalIntegralConstant(name, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out value);
    }

    private bool TryResolveGlobalIntegralConstant(string name, HashSet<string> activeConstants, out long value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(activeConstants);

        if (TryResolvePredefinedIntegralConstant(name, out value))
        {
            return true;
        }

        if (_globalIntegralConstants.TryGetValue(name, out value))
        {
            return true;
        }

        if (!_globalIntegralConstantDeclarations.TryGetValue(name, out var statement))
        {
            value = 0;
            return false;
        }

        if (!activeConstants.Add(name))
        {
            value = 0;
            return false;
        }

        if (!TryEvaluateGlobalIntegralConstantCore(statement.Declaration.Initializer, activeConstants, out value))
        {
            activeConstants.Remove(name);
            value = 0;
            return false;
        }

        activeConstants.Remove(name);
        _globalIntegralConstants.TryAdd(name, value);
        return true;
    }

    private TypeFieldSymbol BindTypeField(VariableDeclaratorSyntax declarator, TypeSymbol fieldType, int? fixedStringLength)
    {
        ArgumentNullException.ThrowIfNull(declarator);

        if (declarator.ArrayBounds is null)
        {
            if (fieldType is FunctionTypeSymbol || fieldType == TypeSymbol.File)
            {
                ReportInvalidTypeMember(declarator.Identifier.Text, declarator.Identifier.Location);
                fieldType = TypeSymbol.Error;
            }

            return new TypeFieldSymbol(declarator.Identifier.Text, fieldType, fixedStringLength);
        }

        if (fieldType is ArrayTypeSymbol or FunctionTypeSymbol || fieldType == TypeSymbol.File)
        {
            ReportInvalidTypeMember(declarator.Identifier.Text, declarator.Identifier.Location);
            return new TypeFieldSymbol(declarator.Identifier.Text, TypeSymbol.Error);
        }

        var arrayType = ArrayTypeSymbol.Create(fieldType, declarator.ArrayBounds.Rank);

        if (declarator.ArrayBounds.IsDynamic)
        {
            return new TypeFieldSymbol(declarator.Identifier.Text, arrayType, fixedStringLength);
        }

        if (!TryEvaluateFixedTypeArrayBounds(declarator, out var lowerBounds, out var upperBounds))
        {
            return new TypeFieldSymbol(declarator.Identifier.Text, TypeSymbol.Error);
        }

        return new TypeFieldSymbol(declarator.Identifier.Text, arrayType, fixedStringLength, lowerBounds, upperBounds);
    }

    private bool TryEvaluateFixedTypeArrayBounds(
        VariableDeclaratorSyntax declarator,
        out IReadOnlyList<int> lowerBounds,
        out IReadOnlyList<int> upperBounds)
    {
        ArgumentNullException.ThrowIfNull(declarator);

        lowerBounds = Array.Empty<int>();
        upperBounds = Array.Empty<int>();

        if (declarator.ArrayBounds is null
            || declarator.ArrayBounds.IsDynamic)
        {
            ReportInvalidFixedTypeArrayBounds(
                declarator.Identifier.Text,
                declarator.Identifier.Location,
                "Фиксированное поле-массив в TYPE требует константные границы по каждому измерению.");
            return false;
        }

        var evaluatedLowerBounds = new int[declarator.ArrayBounds.Dimensions.Count];
        var evaluatedUpperBounds = new int[declarator.ArrayBounds.Dimensions.Count];

        for (var dimensionIndex = 0; dimensionIndex < declarator.ArrayBounds.Dimensions.Count; dimensionIndex++)
        {
            var dimension = declarator.ArrayBounds.Dimensions[dimensionIndex];
            var detailSuffix = declarator.ArrayBounds.Dimensions.Count == 1
                ? string.Empty
                : $" измерения {dimensionIndex + 1}";

            if (dimension.StartExpression is null || dimension.EndExpression is null)
            {
                ReportInvalidFixedTypeArrayBounds(
                    declarator.Identifier.Text,
                    declarator.ArrayBounds.OpenParenToken.Location,
                    $"Фиксированное поле-массив в TYPE требует константные границы{detailSuffix}.");
                return false;
            }

            if (!TryEvaluateGlobalIntegralConstantExpression(dimension.StartExpression, out evaluatedLowerBounds[dimensionIndex]))
            {
                ReportInvalidFixedTypeArrayBounds(
                    declarator.Identifier.Text,
                    GetLocation(dimension.StartExpression),
                    $"Нижняя граница фиксированного поля-массива в TYPE{detailSuffix} должна быть целочисленной константой.");
                return false;
            }

            if (!TryEvaluateGlobalIntegralConstantExpression(dimension.EndExpression, out evaluatedUpperBounds[dimensionIndex]))
            {
                ReportInvalidFixedTypeArrayBounds(
                    declarator.Identifier.Text,
                    GetLocation(dimension.EndExpression),
                    $"Верхняя граница фиксированного поля-массива в TYPE{detailSuffix} должна быть целочисленной константой.");
                return false;
            }

            if (evaluatedLowerBounds[dimensionIndex] == int.MinValue)
            {
                ReportInvalidFixedTypeArrayBounds(
                    declarator.Identifier.Text,
                    GetLocation(dimension.StartExpression),
                    $"Нижняя граница фиксированного поля-массива в TYPE{detailSuffix} не может быть равна {int.MinValue}.");
                return false;
            }

            if (evaluatedUpperBounds[dimensionIndex] < evaluatedLowerBounds[dimensionIndex]
                && !(evaluatedLowerBounds[dimensionIndex] == 1 && evaluatedUpperBounds[dimensionIndex] == 0))
            {
                ReportInvalidFixedTypeArrayBounds(
                    declarator.Identifier.Text,
                    declarator.ArrayBounds.OpenParenToken.Location,
                    $"У фиксированного поля-массива в TYPE верхняя граница не может быть меньше нижней{detailSuffix}, кроме формы 1 TO 0.");
                return false;
            }
        }

        lowerBounds = evaluatedLowerBounds;
        upperBounds = evaluatedUpperBounds;
        return true;
    }

    private bool TryEvaluateGlobalIntegralConstantExpression(ExpressionSyntax expression, out int value)
    {
        ArgumentNullException.ThrowIfNull(expression);

        value = 0;

        if (!TryEvaluateGlobalIntegralConstantCore(expression, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out var rawValue))
        {
            return false;
        }

        if (rawValue < int.MinValue || rawValue > int.MaxValue)
        {
            return false;
        }

        value = (int)rawValue;
        return true;
    }

    private bool TryEvaluateGlobalIntegralConstantCore(
        ExpressionSyntax expression,
        HashSet<string> activeConstants,
        out long value)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(activeConstants);

        switch (expression)
        {
            case LiteralExpressionSyntax literalExpression:
                switch (literalExpression.LiteralToken.Kind)
                {
                    case SyntaxKind.IntegerLiteralToken:
                        value = (int)literalExpression.LiteralToken.Value!;
                        return true;

                    case SyntaxKind.LongLiteralToken:
                        value = (long)literalExpression.LiteralToken.Value!;
                        return true;
                }

                break;

            case NameExpressionSyntax nameExpression:
                return TryResolveGlobalIntegralConstant(nameExpression.IdentifierToken.Text, activeConstants, out value);

            case ParenthesizedExpressionSyntax parenthesizedExpression:
                return TryEvaluateGlobalIntegralConstantCore(parenthesizedExpression.Expression, activeConstants, out value);

            case UnaryExpressionSyntax unaryExpression
                when unaryExpression.OperatorToken.Kind is SyntaxKind.PlusToken or SyntaxKind.MinusToken:
                if (!TryEvaluateGlobalIntegralConstantCore(unaryExpression.Operand, activeConstants, out var operandValue))
                {
                    break;
                }

                if (unaryExpression.OperatorToken.Kind == SyntaxKind.PlusToken)
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
                    value = 0;
                    return false;
                }

            case BinaryExpressionSyntax binaryExpression
                when binaryExpression.OperatorToken.Kind is SyntaxKind.PlusToken
                    or SyntaxKind.MinusToken
                    or SyntaxKind.StarToken
                    or SyntaxKind.BackslashToken
                    or SyntaxKind.ModKeyword:
                if (!TryEvaluateGlobalIntegralConstantCore(binaryExpression.Left, activeConstants, out var leftValue)
                    || !TryEvaluateGlobalIntegralConstantCore(binaryExpression.Right, activeConstants, out var rightValue))
                {
                    break;
                }

                try
                {
                    value = binaryExpression.OperatorToken.Kind switch
                    {
                        SyntaxKind.PlusToken => checked(leftValue + rightValue),
                        SyntaxKind.MinusToken => checked(leftValue - rightValue),
                        SyntaxKind.StarToken => checked(leftValue * rightValue),
                        SyntaxKind.BackslashToken when rightValue != 0 => leftValue / rightValue,
                        SyntaxKind.ModKeyword when rightValue != 0 => leftValue % rightValue,
                        _ => 0
                    };

                    if (binaryExpression.OperatorToken.Kind is SyntaxKind.BackslashToken or SyntaxKind.ModKeyword
                        && rightValue == 0)
                    {
                        break;
                    }

                    return true;
                }
                catch (OverflowException)
                {
                    value = 0;
                    return false;
                }
        }

        value = 0;
        return false;
    }

    private void DeclareFunctions(IEnumerable<MemberSyntax> members)
    {
        foreach (var member in members)
        {
            switch (member)
            {
                case SubDeclarationSyntax subDeclaration:
                    DeclareFunction(subDeclaration, TypeSymbol.Void);
                    break;

                case FunctionDeclarationSyntax functionDeclaration:
                    DeclareFunction(functionDeclaration, ResolveFunctionReturnType(functionDeclaration));
                    break;
            }
        }
    }

    private void DeclareFunction(MemberSyntax declaration, TypeSymbol returnType)
    {
        var symbol = declaration switch
        {
            SubDeclarationSyntax subDeclaration => CreateFunctionSymbol(subDeclaration.Identifier.Text, subDeclaration.Parameters, returnType),
            FunctionDeclarationSyntax functionDeclaration => CreateFunctionSymbol(functionDeclaration.Identifier.Text, functionDeclaration.Parameters, returnType),
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
        };

        if (TryLookupPredefinedConstantVariable(symbol.Name, out _))
        {
            var predefinedLocation = declaration switch
            {
                SubDeclarationSyntax subDeclaration => subDeclaration.Identifier.Location,
                FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Identifier.Location,
                _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
            };

            _declaredFunctions[declaration] = symbol;
            ReportDuplicateDeclaration(symbol.Name, predefinedLocation);
            return;
        }

        _declaredFunctions[declaration] = symbol;

        if (!_globalScope.TryDeclareFunction(symbol))
        {
            var location = declaration switch
            {
                SubDeclarationSyntax subDeclaration => subDeclaration.Identifier.Location,
                FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Identifier.Location,
                _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
            };

            ReportDuplicateDeclaration(symbol.Name, location);
        }
    }

    private FunctionSymbol CreateFunctionSymbol(
        string name,
        IReadOnlyList<ParameterSyntax> parameters,
        TypeSymbol returnType,
        FunctionSymbol? containingRoutine = null)
    {
        var parameterSymbols = new List<ParameterSymbol>(parameters.Count);

        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            var parameterType = ReportUnsupportedFixedStringType(parameter.TypeClause, parameter.Identifier.Text)
                ? TypeSymbol.Error
                : ResolveType(parameter.TypeClause);
            parameterSymbols.Add(new ParameterSymbol(parameter.Identifier.Text, parameterType, index));
        }

        return new FunctionSymbol(name, parameterSymbols, returnType, containingRoutine);
    }

    private TypeSymbol ResolveFunctionReturnType(FunctionDeclarationSyntax declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        return ReportUnsupportedFixedStringType(declaration.ReturnType, declaration.Identifier.Text)
            ? TypeSymbol.Error
            : ResolveType(declaration.ReturnType);
    }
}
