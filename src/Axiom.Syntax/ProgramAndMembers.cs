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
        IReadOnlyList<TypeSyntax> parameterTypes,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeParenToken,
        SyntaxToken asKeyword,
        TypeSyntax returnType)
    {
        ArgumentNullException.ThrowIfNull(functionKeyword);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeParenToken);
        ArgumentNullException.ThrowIfNull(asKeyword);
        ArgumentNullException.ThrowIfNull(returnType);

        FunctionKeyword = functionKeyword;
        OpenParenToken = openParenToken;
        ParameterTypes = parameterTypes;
        CommaTokens = commaTokens;
        CloseParenToken = closeParenToken;
        AsKeyword = asKeyword;
        ReturnType = returnType;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionType;

    public SyntaxToken FunctionKeyword { get; }

    public SyntaxToken OpenParenToken { get; }

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
        SyntaxToken openParenToken,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeParenToken)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeParenToken);

        ElementType = elementType;
        OpenParenToken = openParenToken;
        CommaTokens = commaTokens;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayType;

    public TypeSyntax ElementType { get; }

    public SyntaxToken OpenParenToken { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public SyntaxToken CloseParenToken { get; }

    public int Rank => CommaTokens.Count + 1;
}

public sealed class ProgramSyntax : SyntaxNode
{
    public ProgramSyntax(IReadOnlyList<MemberSyntax> members, SyntaxToken endOfFileToken)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(endOfFileToken);

        Members = members;
        EndOfFileToken = endOfFileToken;
    }

    public override SyntaxKind Kind => SyntaxKind.Program;

    public IReadOnlyList<MemberSyntax> Members { get; }

    public SyntaxToken EndOfFileToken { get; }
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
    public ParameterSyntax(SyntaxToken identifier, TypeClauseSyntax typeClause)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(typeClause);

        Identifier = identifier;
        TypeClause = typeClause;
    }

    public override SyntaxKind Kind => SyntaxKind.Parameter;

    public SyntaxToken Identifier { get; }

    public TypeClauseSyntax TypeClause { get; }
}

public sealed class TypeFieldSyntax : SyntaxNode
{
    public TypeFieldSyntax(
        IReadOnlyList<VariableDeclaratorSyntax> declarators,
        IReadOnlyList<SyntaxToken> commaTokens,
        TypeClauseSyntax typeClause)
    {
        ArgumentNullException.ThrowIfNull(declarators);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(typeClause);

        Declarators = declarators;
        CommaTokens = commaTokens;
        TypeClause = typeClause;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeField;

    public IReadOnlyList<VariableDeclaratorSyntax> Declarators { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public TypeClauseSyntax TypeClause { get; }
}

public sealed class TypeDeclarationSyntax : MemberSyntax
{
    public TypeDeclarationSyntax(
        SyntaxToken typeKeyword,
        SyntaxToken identifier,
        IReadOnlyList<TypeFieldSyntax> fields,
        SyntaxToken endKeyword,
        SyntaxToken endTypeKeyword)
    {
        ArgumentNullException.ThrowIfNull(typeKeyword);
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(endKeyword);
        ArgumentNullException.ThrowIfNull(endTypeKeyword);

        TypeKeyword = typeKeyword;
        Identifier = identifier;
        Fields = fields;
        EndKeyword = endKeyword;
        EndTypeKeyword = endTypeKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeDeclaration;

    public SyntaxToken TypeKeyword { get; }

    public SyntaxToken Identifier { get; }

    public IReadOnlyList<TypeFieldSyntax> Fields { get; }

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
        SyntaxToken endSubKeyword)
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
    }

    public override SyntaxKind Kind => SyntaxKind.SubDeclaration;

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
        SyntaxToken endFunctionKeyword)
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
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;

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
