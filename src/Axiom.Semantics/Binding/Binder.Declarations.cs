using Axiom.Core;
using Axiom.Syntax;
using System.Globalization;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private void DeclareTypeNames(IEnumerable<MemberSyntax> members)
    {
        foreach (var typeDeclaration in members.OfType<TypeDeclarationSyntax>())
        {
            var name = typeDeclaration.Identifier.Text;

            if (_importedTypes.ContainsKey(name))
            {
                Report(DiagnosticCodes.ImportedNameConflict,
                    $"Тип '{name}' конфликтует с импортированным типом.", typeDeclaration.Identifier.Location);
                continue;
            }

            if (TypeSymbol.FromSyntaxKind(typeDeclaration.Identifier.Kind) != TypeSymbol.Error)
            {
                ReportDuplicateDeclaration(name, typeDeclaration.Identifier.Location);
                continue;
            }

            var type = new UserDefinedTypeSymbol(name);
            if (!_declaredTypes.TryAdd(name, type))
            {
                ReportDuplicateDeclaration(name, typeDeclaration.Identifier.Location);
            }
            else _editorReferences?.Add(new(type, typeDeclaration.Identifier.Location, true));
        }
    }

    private void PrepareTypeDeclarations(IEnumerable<MemberSyntax> members)
    {
        foreach (var constStatement in members
                     .Select(static member => member switch
                     {
                         GlobalStatementSyntax global => global.Statement,
                         ModuleStatementSyntax module => module.Statement,
                         _ => null
                     })
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
                                var invalidField = new TypeFieldSymbol(declarator.Identifier.Text, TypeSymbol.Error);
                                fields.Add(invalidField);
                                _editorReferences?.Add(new(invalidField, declarator.Identifier.Location, true));
                                continue;
                            }

                            var field = BindTypeField(fieldDeclaration, declarator, fieldType, fixedStringLength);
                            fields.Add(field);
                            _editorReferences?.Add(new(field, declarator.Identifier.Location, true));
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

    private TypeFieldSymbol BindTypeField(
        TypeFieldSyntax fieldDeclaration,
        VariableDeclaratorSyntax declarator,
        TypeSymbol fieldType,
        int? fixedStringLength)
    {
        ArgumentNullException.ThrowIfNull(fieldDeclaration);
        ArgumentNullException.ThrowIfNull(declarator);

        var (hasDefaultValue, defaultValue) = BindTypeFieldDefaultValue(
            fieldDeclaration,
            declarator,
            fieldType,
            fixedStringLength);

        if (declarator.ArrayBounds is null)
        {
            if (fieldType is FunctionTypeSymbol || fieldType == TypeSymbol.File)
            {
                ReportInvalidTypeMember(declarator.Identifier.Text, declarator.Identifier.Location);
                fieldType = TypeSymbol.Error;
                hasDefaultValue = false;
                defaultValue = null;
            }

            return new TypeFieldSymbol(
                declarator.Identifier.Text,
                fieldType,
                fixedStringLength,
                hasDefaultValue: hasDefaultValue,
                defaultValue: defaultValue);
        }

        if (fieldType is ArrayTypeSymbol or FunctionTypeSymbol || fieldType == TypeSymbol.File)
        {
            ReportInvalidTypeMember(declarator.Identifier.Text, declarator.Identifier.Location);
            return new TypeFieldSymbol(declarator.Identifier.Text, TypeSymbol.Error);
        }

        var arrayType = ArrayTypeSymbol.Create(fieldType, declarator.ArrayBounds.Rank);

        if (declarator.ArrayBounds.IsDynamic)
        {
            return new TypeFieldSymbol(
                declarator.Identifier.Text,
                arrayType,
                fixedStringLength,
                hasDefaultValue: hasDefaultValue,
                defaultValue: defaultValue);
        }

        if (!TryEvaluateFixedTypeArrayBounds(declarator, out var lowerBounds, out var upperBounds))
        {
            return new TypeFieldSymbol(declarator.Identifier.Text, TypeSymbol.Error);
        }

        return new TypeFieldSymbol(
            declarator.Identifier.Text,
            arrayType,
            fixedStringLength,
            lowerBounds,
            upperBounds,
            hasDefaultValue,
            defaultValue);
    }

    private (bool HasDefaultValue, object? DefaultValue) BindTypeFieldDefaultValue(
        TypeFieldSyntax fieldDeclaration,
        VariableDeclaratorSyntax declarator,
        TypeSymbol fieldType,
        int? fixedStringLength)
    {
        ArgumentNullException.ThrowIfNull(fieldDeclaration);
        ArgumentNullException.ThrowIfNull(declarator);

        if (fieldDeclaration.Initializer is null)
        {
            return (false, null);
        }

        if (declarator.ArrayBounds is not null)
        {
            ReportInvalidTypeFieldDefaultValue(
                declarator.Identifier.Text,
                fieldDeclaration.EqualsToken?.Location ?? declarator.Identifier.Location,
                "Значение по умолчанию пока поддерживается только для скалярных и строковых полей TYPE.");
            BindExpression(fieldDeclaration.Initializer);
            return (false, null);
        }

        if (fieldType is UserDefinedTypeSymbol or ArrayTypeSymbol)
        {
            ReportInvalidTypeFieldDefaultValue(
                declarator.Identifier.Text,
                fieldDeclaration.EqualsToken?.Location ?? declarator.Identifier.Location,
                "Значение по умолчанию пока поддерживается только для скалярных и строковых полей TYPE.");
            BindExpression(fieldDeclaration.Initializer);
            return (false, null);
        }

        var initializer = BindExpression(fieldDeclaration.Initializer, fieldType);
        if (initializer.Type == TypeSymbol.Error)
        {
            return (false, null);
        }

        if (TryEvaluateConstantFieldDefault(initializer, fieldType, out var constantValue))
        {
            return (true, constantValue);
        }

        ReportInvalidTypeFieldDefaultValue(
            declarator.Identifier.Text,
            GetLocation(fieldDeclaration.Initializer),
            "Используйте константный литерал без обращения к переменным, вызовам и полям.");
        return (false, null);
    }

    /// <summary>
    /// Значение поля TYPE задаётся только константой. Допустимое преобразование
    /// литерала к типу поля вычисляется здесь: узел преобразования вокруг литерала
    /// остаётся константным, а вызовы, переменные и произвольные выражения запрещены.
    /// </summary>
    private static bool TryEvaluateConstantFieldDefault(
        BoundExpression initializer,
        TypeSymbol fieldType,
        out object? value)
    {
        value = null;

        var expression = initializer;

        while (expression is BoundConversionExpression conversion
            && conversion.Expression is BoundLiteralExpression)
        {
            expression = conversion.Expression;
        }

        if (expression is not BoundLiteralExpression { Value: not null } literal)
        {
            return false;
        }

        if (literal.Type == fieldType)
        {
            value = literal.Value;
            return true;
        }

        if (TryConvertTemporalFieldDefault(literal.Value, literal.Type, fieldType, out value))
        {
            return true;
        }

        return TryConvertLiteralValue(literal.Value, literal.Type, fieldType, out value);
    }

    /// <summary>
    /// Начальное значение временного поля задаётся каноническим текстом и
    /// проверяется при объявлении. Значение хранится типизированным, поэтому
    /// недопустимая дата не попадает в программу, а поле получает готовое DATE,
    /// TIME или DATETIME без повторного разбора.
    /// </summary>
    private static bool TryConvertTemporalFieldDefault(
        object sourceValue,
        TypeSymbol sourceType,
        TypeSymbol fieldType,
        out object? value)
    {
        value = null;

        if (sourceType != TypeSymbol.String || sourceValue is not string text)
        {
            return false;
        }

        if (fieldType == TypeSymbol.Date)
        {
            if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                value = date;
                return true;
            }

            return false;
        }

        if (fieldType == TypeSymbol.Time)
        {
            if (TimeOnly.TryParseExact(text, ["HH:mm:ss", "HH:mm:ss.FFFFFFF"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                value = time;
                return true;
            }

            return false;
        }

        if (fieldType == TypeSymbol.DateTime)
        {
            if (DateTime.TryParseExact(text, ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.FFFFFFF"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                value = dateTime;
                return true;
            }

            return false;
        }

        if (fieldType == TypeSymbol.UtcDateTime)
        {
            // Момент записывается только с признаком Z: без него текст означал бы
            // календарную запись и требовал бы пояса для перевода.
            if (DateTime.TryParseExact(
                    text,
                    ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var instant))
            {
                value = DateTime.SpecifyKind(instant, DateTimeKind.Utc);
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool TryConvertLiteralValue(
        object sourceValue,
        TypeSymbol sourceType,
        TypeSymbol targetType,
        out object? value)
    {
        value = null;

        if (!IsNumeric(sourceType) || !IsNumeric(targetType))
        {
            return false;
        }

        switch (targetType)
        {
            case var type when type == TypeSymbol.Integer:
                return sourceType == TypeSymbol.Integer && TrySetInteger(sourceValue, out value);
            case var type when type == TypeSymbol.Long:
                return TrySetLong(sourceValue, out value);
            case var type when type == TypeSymbol.Decimal:
                return TrySetDecimal(sourceValue, out value);
            case var type when type == TypeSymbol.Double:
                return TrySetDouble(sourceValue, out value);
            default:
                return false;
        }
    }

    private static bool TrySetInteger(object sourceValue, out object? value)
    {
        value = sourceValue;

        return sourceValue is int;
    }

    private static bool TrySetLong(object sourceValue, out object? value)
    {
        value = null;

        switch (sourceValue)
        {
            case int intValue:
                value = (long)intValue;
                return true;
            case long longValue:
                value = longValue;
                return true;
            case decimal decimalValue when decimalValue is >= long.MinValue and <= long.MaxValue:
                value = (long)decimalValue;
                return true;
            default:
                return false;
        }
    }

    private static bool TrySetDecimal(object sourceValue, out object? value)
    {
        value = null;

        switch (sourceValue)
        {
            case int intValue:
                value = (decimal)intValue;
                return true;
            case long longValue:
                value = (decimal)longValue;
                return true;
            case decimal decimalValue:
                value = decimalValue;
                return true;
            default:
                return false;
        }
    }

    private static bool TrySetDouble(object sourceValue, out object? value)
    {
        value = null;

        switch (sourceValue)
        {
            case int intValue:
                value = (double)intValue;
                return true;
            case long longValue:
                value = (double)longValue;
                return true;
            case decimal decimalValue:
                value = (double)decimalValue;
                return true;
            case double doubleValue:
                value = doubleValue;
                return true;
            default:
                return false;
        }
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
                var resolved = TryResolveGlobalIntegralConstant(nameExpression.IdentifierToken.Text, activeConstants, out value);
                if (resolved) _editorGlobalConstantReferences?.Add(nameExpression.IdentifierToken);
                return resolved;

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
                        SyntaxKind.BackslashToken when rightValue != 0 => checked(leftValue / rightValue),
                        SyntaxKind.ModKeyword when rightValue != 0 => checked(leftValue % rightValue),
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

    private void DeclareTypeMethods(IEnumerable<MemberSyntax> members)
    {
        foreach (var typeDeclaration in members.OfType<TypeDeclarationSyntax>())
        {
            if (!_declaredTypes.TryGetValue(typeDeclaration.Identifier.Text, out var type))
            {
                continue;
            }

            if (type.MethodsCompleted)
            {
                continue;
            }

            var methods = new List<FunctionSymbol>(typeDeclaration.Methods.Count);
            var constructors = new List<FunctionSymbol>();
            FunctionSymbol? invariant = null;
            var seenMethodSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenConstructorSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var methodDeclaration in typeDeclaration.Methods)
            {
                var methodName = methodDeclaration switch
                {
                    SubDeclarationSyntax subDeclaration => subDeclaration.Identifier.Text,
                    FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Identifier.Text,
                    _ => throw new ArgumentOutOfRangeException(nameof(methodDeclaration), methodDeclaration, "Unsupported TYPE method declaration.")
                };

                var location = methodDeclaration switch
                {
                    SubDeclarationSyntax subDeclaration => subDeclaration.Identifier.Location,
                    FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Identifier.Location,
                    _ => throw new ArgumentOutOfRangeException(nameof(methodDeclaration), methodDeclaration, "Unsupported TYPE method declaration.")
                };

                var isConstructor = string.Equals(methodName, "New", StringComparison.OrdinalIgnoreCase);
                var isInvariant = string.Equals(methodName, "IsValid", StringComparison.OrdinalIgnoreCase);

                if (type.TryGetField(methodName, out _))
                {
                    ReportDuplicateDeclaration(methodName, location);
                    continue;
                }

                if (isConstructor)
                {
                    if (methodDeclaration is not SubDeclarationSyntax)
                    {
                        ReportInvalidTypeConstructorDeclaration(location);
                        continue;
                    }

                    var constructor = CreateTypeMethodSymbol(type, methodName, GetCallableParameters(methodDeclaration), TypeSymbol.Void);
                    var constructorSignature = GetTypeConstructorSignatureKey(constructor);
                    if (!seenConstructorSignatures.Add(constructorSignature))
                    {
                        ReportDuplicateDeclaration($"{type.Name}.{constructorSignature}", location);
                        continue;
                    }

                    constructors.Add(constructor);
                    _declaredFunctions[methodDeclaration] = constructor;
                    _declaredMethodOwners[methodDeclaration] = type;
                    continue;
                }

                if (isInvariant)
                {
                    if (methodDeclaration is not FunctionDeclarationSyntax functionDeclaration)
                    {
                        ReportInvalidTypeInvariantDeclaration(location);
                        continue;
                    }

                    if (invariant is not null)
                    {
                        ReportDuplicateDeclaration($"{type.Name}.IsValid()", location);
                        continue;
                    }

                    var parameters = GetCallableParameters(methodDeclaration);
                    var invariantReturnType = ResolveFunctionReturnType(functionDeclaration);
                    var invariantSymbol = CreateTypeMethodSymbol(type, methodName, parameters, invariantReturnType);

                    if (parameters.Count != 0 || invariantReturnType != TypeSymbol.Boolean)
                    {
                        ReportInvalidTypeInvariantDeclaration(location);
                        continue;
                    }

                    invariant = invariantSymbol;
                    _declaredFunctions[methodDeclaration] = invariantSymbol;
                    _declaredMethodOwners[methodDeclaration] = type;
                    continue;
                }

                var returnType = methodDeclaration is FunctionDeclarationSyntax function
                    ? ResolveFunctionReturnType(function)
                    : TypeSymbol.Void;
                var symbol = CreateTypeMethodSymbol(type, methodName, GetCallableParameters(methodDeclaration), returnType);
                var methodSignature = GetTypeMethodSignatureKey(symbol);
                if (!seenMethodSignatures.Add(methodSignature))
                {
                    ReportDuplicateDeclaration($"{type.Name}.{methodSignature}", location);
                    continue;
                }

                _declaredFunctions[methodDeclaration] = symbol;
                _declaredMethodOwners[methodDeclaration] = type;
                methods.Add(symbol);
            }

            type.CompleteMethods(methods, constructors, invariant);
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

    private FunctionSymbol CreateTypeMethodSymbol(
        UserDefinedTypeSymbol ownerType,
        string methodName,
        IReadOnlyList<ParameterSyntax> parameters,
        TypeSymbol returnType)
    {
        ArgumentNullException.ThrowIfNull(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(parameters);

        var parameterSymbols = new List<ParameterSymbol>(parameters.Count + 1)
        {
            new("SELF", ownerType, 0, ParameterPassingMode.ByRef)
        };

        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            var parameterType = ReportUnsupportedFixedStringType(parameter.TypeClause, parameter.Identifier.Text)
                ? TypeSymbol.Error
                : ResolveType(parameter.TypeClause);
            parameterSymbols.Add(new ParameterSymbol(
                parameter.Identifier.Text,
                parameterType,
                index + 1,
                ParameterPassingModes.FromKeyword(parameter.PassingKeyword)));
        }

        return new FunctionSymbol($"{ownerType.Name}.{methodName}", methodName, parameterSymbols, returnType);
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
            parameterSymbols.Add(new ParameterSymbol(
                parameter.Identifier.Text,
                parameterType,
                index,
                ParameterPassingModes.FromKeyword(parameter.PassingKeyword)));
        }

        return new FunctionSymbol(name, parameterSymbols, returnType, containingRoutine);
    }

    private static string GetTypeConstructorSignatureKey(FunctionSymbol constructor)
    {
        ArgumentNullException.ThrowIfNull(constructor);

        var signature = constructor.Parameters
            .Skip(1)
            .Select(static parameter => parameter.IsByRef
                ? $"BYREF {parameter.Type.Name}"
                : parameter.Type.Name);

        return $"New({string.Join(", ", signature)})";
    }

    private static string GetTypeMethodSignatureKey(FunctionSymbol method)
    {
        ArgumentNullException.ThrowIfNull(method);

        var signature = method.Parameters
            .Skip(1)
            .Select(static parameter => parameter.IsByRef
                ? $"BYREF {parameter.Type.Name}"
                : parameter.Type.Name);

        return $"{method.ShortName}({string.Join(", ", signature)})";
    }

    private static IReadOnlyList<ParameterSyntax> GetCallableParameters(MemberSyntax declaration)
    {
        return declaration switch
        {
            SubDeclarationSyntax subDeclaration => subDeclaration.Parameters,
            FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Parameters,
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
        };
    }

    private TypeSymbol ResolveFunctionReturnType(FunctionDeclarationSyntax declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        return ReportUnsupportedFixedStringType(declaration.ReturnType, declaration.Identifier.Text)
            ? TypeSymbol.Error
            : ResolveType(declaration.ReturnType);
    }
}
