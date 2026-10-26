namespace Axiom.Syntax;

public sealed class NamedTypeSyntax : TypeSyntax
{
    public NamedTypeSyntax(SyntaxToken typeToken)
    {
        ArgumentNullException.ThrowIfNull(typeToken);
        TypeToken = typeToken;
    }

    public override SyntaxKind Kind => SyntaxKind.NamedType;

    public SyntaxToken TypeToken { get; }
}

public sealed class FixedLengthStringTypeSyntax : TypeSyntax
{
    public FixedLengthStringTypeSyntax(
        SyntaxToken stringKeyword,
        SyntaxToken starToken,
        ExpressionSyntax lengthExpression)
    {
        ArgumentNullException.ThrowIfNull(stringKeyword);
        ArgumentNullException.ThrowIfNull(starToken);
        ArgumentNullException.ThrowIfNull(lengthExpression);

        StringKeyword = stringKeyword;
        StarToken = starToken;
        LengthExpression = lengthExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.FixedLengthStringType;

    public SyntaxToken StringKeyword { get; }

    public SyntaxToken StarToken { get; }

    public ExpressionSyntax LengthExpression { get; }
}

public sealed class FunctionTypeSyntax : TypeSyntax
{
    public FunctionTypeSyntax(
        SyntaxToken functionKeyword,
        SyntaxToken openParenToken,
        IReadOnlyList<SyntaxToken?> parameterPassingKeywords,
        IReadOnlyList<TypeSyntax> parameterTypes,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeParenToken,
        SyntaxToken asKeyword,
        TypeSyntax returnType)
    {
        ArgumentNullException.ThrowIfNull(functionKeyword);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(parameterPassingKeywords);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeParenToken);
        ArgumentNullException.ThrowIfNull(asKeyword);
        ArgumentNullException.ThrowIfNull(returnType);

        FunctionKeyword = functionKeyword;
        OpenParenToken = openParenToken;
        ParameterPassingKeywords = parameterPassingKeywords;
        ParameterTypes = parameterTypes;
        CommaTokens = commaTokens;
        CloseParenToken = closeParenToken;
        AsKeyword = asKeyword;
        ReturnType = returnType;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionType;

    public SyntaxToken FunctionKeyword { get; }

    public SyntaxToken OpenParenToken { get; }

    public IReadOnlyList<SyntaxToken?> ParameterPassingKeywords { get; }

    public IReadOnlyList<TypeSyntax> ParameterTypes { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public SyntaxToken CloseParenToken { get; }

    public SyntaxToken AsKeyword { get; }

    public TypeSyntax ReturnType { get; }
}

public sealed class ArrayTypeSyntax : TypeSyntax
{
    public ArrayTypeSyntax(
        TypeSyntax elementType,
        SyntaxToken openDelimiterToken,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeDelimiterToken)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ArgumentNullException.ThrowIfNull(openDelimiterToken);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeDelimiterToken);

        ElementType = elementType;
        OpenDelimiterToken = openDelimiterToken;
        CommaTokens = commaTokens;
        CloseDelimiterToken = closeDelimiterToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayType;

    public TypeSyntax ElementType { get; }

    public SyntaxToken OpenDelimiterToken { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public SyntaxToken CloseDelimiterToken { get; }

    public int Rank => CommaTokens.Count + 1;
}

// DICTIONARY(<тип ключа>, <тип значения>) — самостоятельный тип коллекции.
// Первым указывается ключ, вторым — значение; типы едины для всех записей.
public sealed class DictionaryTypeSyntax : TypeSyntax
{
    public DictionaryTypeSyntax(
        SyntaxToken dictionaryKeyword,
        SyntaxToken openParenToken,
        TypeSyntax keyType,
        SyntaxToken commaToken,
        TypeSyntax valueType,
        SyntaxToken closeParenToken)
    {
        ArgumentNullException.ThrowIfNull(dictionaryKeyword);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(keyType);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(valueType);
        ArgumentNullException.ThrowIfNull(closeParenToken);

        DictionaryKeyword = dictionaryKeyword;
        OpenParenToken = openParenToken;
        KeyType = keyType;
        CommaToken = commaToken;
        ValueType = valueType;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.DictionaryType;

    public SyntaxToken DictionaryKeyword { get; }

    public SyntaxToken OpenParenToken { get; }

    public TypeSyntax KeyType { get; }

    public SyntaxToken CommaToken { get; }

    public TypeSyntax ValueType { get; }

    public SyntaxToken CloseParenToken { get; }
}

public sealed class ProgramSyntax : SyntaxNode
{
    public ProgramSyntax(
        IReadOnlyList<MemberSyntax> members,
        SyntaxToken endOfFileToken,
        bool usesSquareArraySyntax = false)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(endOfFileToken);

        Members = members;
        EndOfFileToken = endOfFileToken;
        UsesSquareArraySyntax = usesSquareArraySyntax;
    }

    public override SyntaxKind Kind => SyntaxKind.Program;

    public IReadOnlyList<MemberSyntax> Members { get; }

    public SyntaxToken EndOfFileToken { get; }

    public bool UsesSquareArraySyntax { get; }
}

public sealed class ImportStatementSyntax : MemberSyntax
{
    public ImportStatementSyntax(SyntaxToken importKeyword, SyntaxToken nameToken)
    {
        ArgumentNullException.ThrowIfNull(importKeyword);
        ArgumentNullException.ThrowIfNull(nameToken);

        ImportKeyword = importKeyword;
        NameToken = nameToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ImportStatement;

    public SyntaxToken ImportKeyword { get; }

    public SyntaxToken NameToken { get; }

    public string ModuleName => NameToken.Text;
}

/// <summary>
/// Верхнеуровневая инструкция с явным модификатором PUBLIC или PRIVATE.
/// Модификатор действует только на уровне модуля.
/// </summary>
public sealed class ModuleStatementSyntax : MemberSyntax
{
    public ModuleStatementSyntax(SyntaxToken visibilityKeyword, StatementSyntax statement)
    {
        ArgumentNullException.ThrowIfNull(visibilityKeyword);
        ArgumentNullException.ThrowIfNull(statement);

        VisibilityKeyword = visibilityKeyword;
        Statement = statement;
    }

    public override SyntaxKind Kind => SyntaxKind.ModuleStatement;

    public SyntaxToken VisibilityKeyword { get; }

    public StatementSyntax Statement { get; }

    public bool IsPublic => VisibilityKeyword.Kind == SyntaxKind.PublicKeyword;
}

public sealed class GlobalStatementSyntax : MemberSyntax
{
    public GlobalStatementSyntax(StatementSyntax statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        Statement = statement;
    }

    public override SyntaxKind Kind => SyntaxKind.GlobalStatement;

    public StatementSyntax Statement { get; }
}

public sealed class TypeClauseSyntax : SyntaxNode
{
    public TypeClauseSyntax(SyntaxToken asKeyword, TypeSyntax type)
    {
        ArgumentNullException.ThrowIfNull(asKeyword);
        ArgumentNullException.ThrowIfNull(type);

        AsKeyword = asKeyword;
        Type = type;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeClause;

    public SyntaxToken AsKeyword { get; }

    public TypeSyntax Type { get; }
}

public sealed class ParameterSyntax : SyntaxNode
{
    public ParameterSyntax(
        SyntaxToken? passingKeyword,
        SyntaxToken identifier,
        TypeClauseSyntax typeClause,
        ArrayBoundsSyntax? arrayBounds = null)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(typeClause);

        PassingKeyword = passingKeyword;
        Identifier = identifier;
        TypeClause = typeClause;
        ArrayBounds = arrayBounds;
    }

    public override SyntaxKind Kind => SyntaxKind.Parameter;

    public SyntaxToken? PassingKeyword { get; }

    public SyntaxToken Identifier { get; }

    public TypeClauseSyntax TypeClause { get; }

    public ArrayBoundsSyntax? ArrayBounds { get; }
}

public sealed class TypeFieldSyntax : SyntaxNode
{
    public TypeFieldSyntax(
        IReadOnlyList<VariableDeclaratorSyntax> declarators,
        IReadOnlyList<SyntaxToken> commaTokens,
        TypeClauseSyntax typeClause,
        SyntaxToken? equalsToken,
        ExpressionSyntax? initializer)
    {
        ArgumentNullException.ThrowIfNull(declarators);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(typeClause);

        Declarators = declarators;
        CommaTokens = commaTokens;
        TypeClause = typeClause;
        EqualsToken = equalsToken;
        Initializer = initializer;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeField;

    public IReadOnlyList<VariableDeclaratorSyntax> Declarators { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public TypeClauseSyntax TypeClause { get; }

    public SyntaxToken? EqualsToken { get; }

    public ExpressionSyntax? Initializer { get; }
}

public sealed class TypeDeclarationSyntax : MemberSyntax
{
    public TypeDeclarationSyntax(
        SyntaxToken typeKeyword,
        SyntaxToken identifier,
        IReadOnlyList<TypeFieldSyntax> fields,
        IReadOnlyList<MemberSyntax> methods,
        SyntaxToken endKeyword,
        SyntaxToken endTypeKeyword,
        SyntaxToken? visibilityKeyword = null)
    {
        ArgumentNullException.ThrowIfNull(typeKeyword);
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(methods);
        ArgumentNullException.ThrowIfNull(endKeyword);
        ArgumentNullException.ThrowIfNull(endTypeKeyword);

        TypeKeyword = typeKeyword;
        Identifier = identifier;
        Fields = fields;
        Methods = methods;
        EndKeyword = endKeyword;
        EndTypeKeyword = endTypeKeyword;
        VisibilityKeyword = visibilityKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeDeclaration;

    public SyntaxToken? VisibilityKeyword { get; }

    public SyntaxToken TypeKeyword { get; }

    public SyntaxToken Identifier { get; }

    public IReadOnlyList<TypeFieldSyntax> Fields { get; }

    public IReadOnlyList<MemberSyntax> Methods { get; }

    public SyntaxToken EndKeyword { get; }

    public SyntaxToken EndTypeKeyword { get; }
}

public sealed class SubDeclarationSyntax : MemberSyntax
{
    public SubDeclarationSyntax(
        SyntaxToken subKeyword,
        SyntaxToken identifier,
        SyntaxToken openParenToken,
        IReadOnlyList<ParameterSyntax> parameters,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeParenToken,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken endKeyword,
        SyntaxToken endSubKeyword,
        SyntaxToken? visibilityKeyword = null)
    {
        ArgumentNullException.ThrowIfNull(subKeyword);
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeParenToken);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(endKeyword);
        ArgumentNullException.ThrowIfNull(endSubKeyword);

        SubKeyword = subKeyword;
        Identifier = identifier;
        OpenParenToken = openParenToken;
        Parameters = parameters;
        CommaTokens = commaTokens;
        CloseParenToken = closeParenToken;
        Statements = statements;
        EndKeyword = endKeyword;
        EndSubKeyword = endSubKeyword;
        VisibilityKeyword = visibilityKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.SubDeclaration;

    public SyntaxToken? VisibilityKeyword { get; }

    public SyntaxToken SubKeyword { get; }

    public SyntaxToken Identifier { get; }

    public SyntaxToken OpenParenToken { get; }

    public IReadOnlyList<ParameterSyntax> Parameters { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public SyntaxToken CloseParenToken { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }

    public SyntaxToken EndKeyword { get; }

    public SyntaxToken EndSubKeyword { get; }
}

public sealed class FunctionDeclarationSyntax : MemberSyntax
{
    public FunctionDeclarationSyntax(
        SyntaxToken functionKeyword,
        SyntaxToken identifier,
        SyntaxToken openParenToken,
        IReadOnlyList<ParameterSyntax> parameters,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeParenToken,
        TypeClauseSyntax returnType,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken endKeyword,
        SyntaxToken endFunctionKeyword,
        SyntaxToken? visibilityKeyword = null)
    {
        ArgumentNullException.ThrowIfNull(functionKeyword);
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeParenToken);
        ArgumentNullException.ThrowIfNull(returnType);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(endKeyword);
        ArgumentNullException.ThrowIfNull(endFunctionKeyword);

        FunctionKeyword = functionKeyword;
        Identifier = identifier;
        OpenParenToken = openParenToken;
        Parameters = parameters;
        CommaTokens = commaTokens;
        CloseParenToken = closeParenToken;
        ReturnType = returnType;
        Statements = statements;
        EndKeyword = endKeyword;
        EndFunctionKeyword = endFunctionKeyword;
        VisibilityKeyword = visibilityKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;

    public SyntaxToken? VisibilityKeyword { get; }

    public SyntaxToken FunctionKeyword { get; }

    public SyntaxToken Identifier { get; }

    public SyntaxToken OpenParenToken { get; }

    public IReadOnlyList<ParameterSyntax> Parameters { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public SyntaxToken CloseParenToken { get; }

    public TypeClauseSyntax ReturnType { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }

    public SyntaxToken EndKeyword { get; }

    public SyntaxToken EndFunctionKeyword { get; }
}
