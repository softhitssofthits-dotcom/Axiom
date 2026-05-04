using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private MemberSyntax ParseMember()
    {
        return Current.Kind switch
        {
            SyntaxKind.TypeKeyword => ParseTypeDeclaration(),
            SyntaxKind.SubKeyword => ParseSubDeclaration(),
            SyntaxKind.FunctionKeyword => ParseFunctionDeclaration(),
            _ => new GlobalStatementSyntax(ParseStatement())
        };
    }

    private TypeDeclarationSyntax ParseTypeDeclaration()
    {
        var typeKeyword = Match(SyntaxKind.TypeKeyword, "TYPE");
        var identifier = MatchIdentifier();
        SkipNewLines();

        var fields = new List<TypeFieldSyntax>();

        while (Current.Kind != SyntaxKind.EndOfFileToken && !IsEndType())
        {
            if (IsUnexpectedBoundaryToken())
            {
                ReportUnexpectedToken(Current, "Неожиданный маркер конца блока");
                NextToken();
                SkipNewLines();
                continue;
            }

            fields.Add(ParseTypeField());
            SkipNewLines();
        }

        var (endKeyword, endTypeKeyword) = MatchEndPair(SyntaxKind.TypeKeyword, "END TYPE");
        return new TypeDeclarationSyntax(typeKeyword, identifier, fields, endKeyword, endTypeKeyword);
    }

    private TypeFieldSyntax ParseTypeField()
    {
        var declarators = new List<VariableDeclaratorSyntax> { ParseVariableDeclarator() };
        var commaTokens = new List<SyntaxToken>();

        while (Current.Kind == SyntaxKind.CommaToken)
        {
            commaTokens.Add(NextToken());
            declarators.Add(ParseVariableDeclarator());
        }

        var typeClause = ParseTypeClause();
        SyntaxToken? equalsToken = null;
        ExpressionSyntax? initializer = null;

        if (Current.Kind == SyntaxKind.EqualsToken)
        {
            equalsToken = NextToken();
            initializer = ParseExpression();
        }

        return new TypeFieldSyntax(declarators, commaTokens, typeClause, equalsToken, initializer);
    }

    private SubDeclarationSyntax ParseSubDeclaration()
    {
        var subKeyword = Match(SyntaxKind.SubKeyword, "SUB");
        var identifier = MatchIdentifier();
        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        ParseSeparatedParameters(out var parameters, out var commaTokens, out var closeParenToken);
        SkipNewLines();

        var statements = ParseStatementsUntil(static parser => parser.IsEndSub(), allowLocalCallableDeclarations: true);
        var (endKeyword, endSubKeyword) = MatchEndPair(SyntaxKind.SubKeyword, "END SUB");

        return new SubDeclarationSyntax(
            subKeyword,
            identifier,
            openParenToken,
            parameters,
            commaTokens,
            closeParenToken,
            statements,
            endKeyword,
            endSubKeyword);
    }

    private FunctionDeclarationSyntax ParseFunctionDeclaration()
    {
        var functionKeyword = Match(SyntaxKind.FunctionKeyword, "FUNCTION");
        var identifier = MatchIdentifier();
        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        ParseSeparatedParameters(out var parameters, out var commaTokens, out var closeParenToken);
        var returnType = ParseTypeClause();
        SkipNewLines();

        var statements = ParseStatementsUntil(static parser => parser.IsEndFunction(), allowLocalCallableDeclarations: true);
        var (endKeyword, endFunctionKeyword) = MatchEndPair(SyntaxKind.FunctionKeyword, "END FUNCTION");

        return new FunctionDeclarationSyntax(
            functionKeyword,
            identifier,
            openParenToken,
            parameters,
            commaTokens,
            closeParenToken,
            returnType,
            statements,
            endKeyword,
            endFunctionKeyword);
    }

    private TypeClauseSyntax ParseTypeClause()
    {
        var asKeyword = Match(SyntaxKind.AsKeyword, "AS");
        var type = ParseTypeSyntax();
        return new TypeClauseSyntax(asKeyword, type);
    }

    private TypeSyntax ParseTypeSyntax()
    {
        TypeSyntax typeSyntax;

        if (Current.Kind == SyntaxKind.FunctionKeyword)
        {
            typeSyntax = ParseFunctionTypeSyntax();
        }
        else if (Current.Kind == SyntaxKind.StringKeyword && Peek(1).Kind == SyntaxKind.StarToken)
        {
            var stringKeyword = Match(SyntaxKind.StringKeyword, "STRING");
            var starToken = Match(SyntaxKind.StarToken, "*");
            var lengthExpression = ParseExpression(GetBinaryOperatorPrecedence(SyntaxKind.EqualsToken));
            typeSyntax = new FixedLengthStringTypeSyntax(stringKeyword, starToken, lengthExpression);
        }
        else
        {
            typeSyntax = new NamedTypeSyntax(MatchNamedTypeToken());
        }

        if (IsArrayTypeSuffix())
        {
            var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
            var commaTokens = new List<SyntaxToken>();

            while (Current.Kind == SyntaxKind.CommaToken)
            {
                commaTokens.Add(NextToken());
            }

            var closeParenToken = Match(SyntaxKind.CloseParenToken, ")");
            typeSyntax = new ArrayTypeSyntax(typeSyntax, openParenToken, commaTokens, closeParenToken);
        }

        return typeSyntax;
    }

    private FunctionTypeSyntax ParseFunctionTypeSyntax()
    {
        var functionKeyword = Match(SyntaxKind.FunctionKeyword, "FUNCTION");
        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        ParseSeparatedFunctionTypeParameters(out var parameterPassingKeywords, out var parameterTypes, out var commaTokens, out var closeParenToken);
        var asKeyword = Match(SyntaxKind.AsKeyword, "AS");
        var returnType = ParseTypeSyntax();

        return new FunctionTypeSyntax(
            functionKeyword,
            openParenToken,
            parameterPassingKeywords,
            parameterTypes,
            commaTokens,
            closeParenToken,
            asKeyword,
            returnType);
    }

    private ArrayBoundsSyntax ParseArrayBounds()
    {
        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        var dimensions = new List<ArrayDimensionSyntax>();
        var commaTokens = new List<SyntaxToken>();

        while (true)
        {
            if (Current.Kind is SyntaxKind.CommaToken or SyntaxKind.CloseParenToken)
            {
                dimensions.Add(new ArrayDimensionSyntax(startExpression: null, toKeyword: null, endExpression: null));
            }
            else
            {
                var startExpression = ParseExpression();
                var toKeyword = Match(SyntaxKind.ToKeyword, "TO");
                var endExpression = ParseExpression();
                dimensions.Add(new ArrayDimensionSyntax(startExpression, toKeyword, endExpression));
            }

            if (Current.Kind != SyntaxKind.CommaToken)
            {
                break;
            }

            commaTokens.Add(NextToken());
        }

        var closeParenToken = Match(SyntaxKind.CloseParenToken, ")");
        return new ArrayBoundsSyntax(openParenToken, dimensions, commaTokens, closeParenToken);
    }

    private bool IsArrayTypeSuffix()
    {
        if (Current.Kind != SyntaxKind.OpenParenToken)
        {
            return false;
        }

        for (var offset = 1; ; offset++)
        {
            var token = Peek(offset);

            if (token.Kind == SyntaxKind.CloseParenToken)
            {
                return true;
            }

            if (token.Kind != SyntaxKind.CommaToken)
            {
                return false;
            }
        }
    }

    private void ParseSeparatedParameters(
        out IReadOnlyList<ParameterSyntax> parameters,
        out IReadOnlyList<SyntaxToken> commaTokens,
        out SyntaxToken closeParenToken)
    {
        var parameterList = new List<ParameterSyntax>();
        var commas = new List<SyntaxToken>();

        if (Current.Kind != SyntaxKind.CloseParenToken)
        {
            while (true)
            {
                parameterList.Add(ParseParameter());

                if (Current.Kind != SyntaxKind.CommaToken)
                {
                    break;
                }

                commas.Add(NextToken());
            }
        }

        closeParenToken = Match(SyntaxKind.CloseParenToken, ")");
        parameters = parameterList;
        commaTokens = commas;
    }

    private void ParseSeparatedFunctionTypeParameters(
        out IReadOnlyList<SyntaxToken?> parameterPassingKeywords,
        out IReadOnlyList<TypeSyntax> types,
        out IReadOnlyList<SyntaxToken> commaTokens,
        out SyntaxToken closeParenToken)
    {
        var passingKeywords = new List<SyntaxToken?>();
        var typeList = new List<TypeSyntax>();
        var commas = new List<SyntaxToken>();

        if (Current.Kind != SyntaxKind.CloseParenToken)
        {
            while (true)
            {
                passingKeywords.Add(ParseOptionalPassingKeyword());
                typeList.Add(ParseTypeSyntax());

                if (Current.Kind != SyntaxKind.CommaToken)
                {
                    break;
                }

                commas.Add(NextToken());
            }
        }

        closeParenToken = Match(SyntaxKind.CloseParenToken, ")");
        parameterPassingKeywords = passingKeywords;
        types = typeList;
        commaTokens = commas;
    }

    private ParameterSyntax ParseParameter()
    {
        var passingKeyword = ParseOptionalPassingKeyword();
        var identifier = MatchIdentifier();
        var typeClause = ParseTypeClause();
        return new ParameterSyntax(passingKeyword, identifier, typeClause);
    }

    private SyntaxToken? ParseOptionalPassingKeyword()
    {
        return Current.Kind switch
        {
            SyntaxKind.ByRefKeyword => NextToken(),
            SyntaxKind.ByValKeyword => NextToken(),
            _ => null
        };
    }
}
