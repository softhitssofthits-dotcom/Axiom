using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private ImportStatementSyntax ParseImportStatement()
    {
        var importKeyword = Match(SyntaxKind.ImportKeyword, "IMPORT");
        var nameToken = MatchIdentifier();
        return new ImportStatementSyntax(importKeyword, nameToken);
    }
    private MemberSyntax ParseMember()
    {
        return ParseMember(allowVisibility: false);
    }

    private MemberSyntax ParseMember(bool allowVisibility)
    {
        if (allowVisibility && Current.Kind is SyntaxKind.PublicKeyword or SyntaxKind.PrivateKeyword)
        {
            var visibilityKeyword = NextToken();
            SkipNewLines();

            switch (Current.Kind)
            {
                case SyntaxKind.TypeKeyword:
                    return ParseTypeDeclaration(visibilityKeyword);

                case SyntaxKind.SubKeyword:
                    return ParseSubDeclaration(visibilityKeyword);

                case SyntaxKind.FunctionKeyword:
                    return ParseFunctionDeclaration(visibilityKeyword);

                case SyntaxKind.DimKeyword:
                case SyntaxKind.ConstKeyword:
                    return new ModuleStatementSyntax(visibilityKeyword, ParseStatement());

                default:
                    ReportUnexpectedToken(Current, "PUBLIC или PRIVATE допускается только перед объявлением DIM, CONST, SUB, FUNCTION или TYPE");
                    return new GlobalStatementSyntax(ParseStatement());
            }
        }

        return Current.Kind switch
        {
            SyntaxKind.TypeKeyword => ParseTypeDeclaration(),
            SyntaxKind.SubKeyword => ParseSubDeclaration(),
            SyntaxKind.FunctionKeyword => ParseFunctionDeclaration(),
            _ => new GlobalStatementSyntax(ParseStatement())
        };
    }

    private TypeDeclarationSyntax ParseTypeDeclaration(SyntaxToken? visibilityKeyword = null)
    {
        var typeKeyword = Match(SyntaxKind.TypeKeyword, "TYPE");
        var identifier = MatchIdentifier();
        SkipNewLines();

        var fields = new List<TypeFieldSyntax>();
        var methods = new List<MemberSyntax>();

        while (Current.Kind != SyntaxKind.EndOfFileToken && !IsEndType())
        {
            if (IsUnexpectedBoundaryToken())
            {
                ReportUnexpectedToken(Current, "Неожиданный маркер конца блока");
                NextToken();
                SkipNewLines();
                continue;
            }

            var start = _position;
            switch (Current.Kind)
            {
                case SyntaxKind.SubKeyword:
                    methods.Add(ParseSubDeclaration());
                    break;

                case SyntaxKind.FunctionKeyword:
                    methods.Add(ParseFunctionDeclaration());
                    break;

                default:
                    fields.Add(ParseTypeField());
                    break;
            }

            // Ошибочная форма поля может вернуть синтетические токены, не
            // потребив вход. Пропускаем один токен, чтобы TYPE не зациклился.
            if (_position == start)
            {
                ReportUnexpectedToken(Current, "Неожиданный токен в TYPE");
                NextToken();
            }

            SkipNewLines();
        }

        var (endKeyword, endTypeKeyword) = MatchEndPair(SyntaxKind.TypeKeyword, "END TYPE");
        return new TypeDeclarationSyntax(typeKeyword, identifier, fields, methods, endKeyword, endTypeKeyword, visibilityKeyword);
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

    private SubDeclarationSyntax ParseSubDeclaration(SyntaxToken? visibilityKeyword = null)
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
            endSubKeyword,
            visibilityKeyword);
    }

    private FunctionDeclarationSyntax ParseFunctionDeclaration(SyntaxToken? visibilityKeyword = null)
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
            endFunctionKeyword,
            visibilityKeyword);
    }

    private TypeClauseSyntax ParseTypeClause()
    {
        var asKeyword = Match(SyntaxKind.AsKeyword, "AS");
        var type = ParseTypeSyntax();
        return new TypeClauseSyntax(asKeyword, type);
    }

    private int _fixedStringLengthDepth;

    private ExpressionSyntax ParseFixedStringLength()
    {
        _fixedStringLengthDepth++;
        try
        {
            return ParseExpression(GetBinaryOperatorPrecedence(SyntaxKind.EqualsToken));
        }
        finally
        {
            _fixedStringLengthDepth--;
        }
    }

    private TypeSyntax ParseTypeSyntax()
    {
        TypeSyntax typeSyntax;

        if (Current.Kind == SyntaxKind.OpenParenToken)
        {
            typeSyntax = ParseTupleType();
        }
        else if (Current.Kind == SyntaxKind.FunctionKeyword)
        {
            typeSyntax = ParseFunctionTypeSyntax();
        }
        else if (Current.Kind == SyntaxKind.DictionaryKeyword)
        {
            typeSyntax = ParseDictionaryTypeSyntax();
        }
        else if (Current.Kind == SyntaxKind.SetKeyword)
        {
            typeSyntax = ParseSetTypeSyntax();
        }
        else if (Current.Kind == SyntaxKind.StringKeyword && Peek(1).Kind == SyntaxKind.StarToken)
        {
            var stringKeyword = Match(SyntaxKind.StringKeyword, "STRING");
            var starToken = Match(SyntaxKind.StarToken, "*");
            var lengthExpression = ParseFixedStringLength();
            typeSyntax = new FixedLengthStringTypeSyntax(stringKeyword, starToken, lengthExpression);
        }
        else
        {
            typeSyntax = new NamedTypeSyntax(MatchNamedTypeToken());
        }

        if (IsArrayTypeSuffix())
        {
            var openDelimiterToken = NextToken();
            var square = openDelimiterToken.Kind == SyntaxKind.OpenBracketToken;
            var commaTokens = new List<SyntaxToken>();

            while (Current.Kind == SyntaxKind.CommaToken)
            {
                commaTokens.Add(NextToken());
            }

            var closeDelimiterToken = Match(square ? SyntaxKind.CloseBracketToken : SyntaxKind.CloseParenToken, square ? "]" : ")");
            typeSyntax = new ArrayTypeSyntax(typeSyntax, openDelimiterToken, commaTokens, closeDelimiterToken);
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

    private DictionaryTypeSyntax ParseDictionaryTypeSyntax()
    {
        var dictionaryKeyword = Match(SyntaxKind.DictionaryKeyword, "DICTIONARY");

        // DICTIONARY — имя типа с двумя аргументами. Без скобок форма неполна;
        // одно сообщение точнее каскада из ожиданий скобки, типа и запятой.
        if (Current.Kind != SyntaxKind.OpenParenToken)
        {
            Diagnostics.Report(
                Axiom.Core.DiagnosticCodes.ExpectedToken,
                "Тип DICTIONARY записывается с двумя аргументами: DICTIONARY(тип ключа, тип значения).",
                CreateDiagnosticLocation(dictionaryKeyword.Location));
            return new DictionaryTypeSyntax(
                dictionaryKeyword,
                CreateMissingToken(SyntaxKind.OpenParenToken),
                new NamedTypeSyntax(CreateMissingToken(SyntaxKind.IdentifierToken)),
                CreateMissingToken(SyntaxKind.CommaToken),
                new NamedTypeSyntax(CreateMissingToken(SyntaxKind.IdentifierToken)),
                CreateMissingToken(SyntaxKind.CloseParenToken));
        }

        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        var keyType = ParseTypeSyntax();
        var commaToken = Match(SyntaxKind.CommaToken, ",");
        var valueType = ParseTypeSyntax();
        var closeParenToken = Match(SyntaxKind.CloseParenToken, ")");

        return new DictionaryTypeSyntax(
            dictionaryKeyword,
            openParenToken,
            keyType,
            commaToken,
            valueType,
            closeParenToken);
    }

    private SetTypeSyntax ParseSetTypeSyntax()
    {
        var setKeyword = Match(SyntaxKind.SetKeyword, "HASHSET");

        // HASHSET — имя типа с одним аргументом. Без скобок форма неполна, а
        // запятая означала бы второй аргумент: одно сообщение точнее каскада
        // из ожиданий скобки, типа и закрывающей скобки.
        if (Current.Kind != SyntaxKind.OpenParenToken)
        {
            Diagnostics.Report(
                Axiom.Core.DiagnosticCodes.ExpectedToken,
                "Тип HASHSET записывается с одним аргументом: HASHSET(тип элемента).",
                CreateDiagnosticLocation(setKeyword.Location));
            return new SetTypeSyntax(
                setKeyword,
                CreateMissingToken(SyntaxKind.OpenParenToken),
                new NamedTypeSyntax(CreateMissingToken(SyntaxKind.IdentifierToken)),
                CreateMissingToken(SyntaxKind.CloseParenToken));
        }

        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        var elementType = ParseTypeSyntax();

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            Diagnostics.Report(
                Axiom.Core.DiagnosticCodes.ExpectedToken,
                "Тип HASHSET записывается с одним аргументом: HASHSET(тип элемента).",
                CreateDiagnosticLocation(Current.Location));

            // Лишние аргументы пропускаются до закрывающей скобки типа, чтобы
            // одна причина отказа не превращалась в каскад чужих диагностик.
            SkipExtraSetTypeArguments();

            return new SetTypeSyntax(
                setKeyword,
                openParenToken,
                elementType,
                Match(SyntaxKind.CloseParenToken, ")"));
        }

        var closeParenToken = Match(SyntaxKind.CloseParenToken, ")");

        return new SetTypeSyntax(
            setKeyword,
            openParenToken,
            elementType,
            closeParenToken);
    }

    private void SkipExtraSetTypeArguments()
    {
        var depth = 0;

        while (true)
        {
            if (Current.Kind is SyntaxKind.EndOfFileToken or SyntaxKind.NewLineToken)
            {
                return;
            }

            if (Current.Kind == SyntaxKind.OpenParenToken)
            {
                depth++;
            }
            else if (Current.Kind == SyntaxKind.CloseParenToken)
            {
                if (depth == 0)
                {
                    return;
                }

                depth--;
            }

            NextToken();
        }
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
        if (Current.Kind is not (SyntaxKind.OpenParenToken or SyntaxKind.OpenBracketToken))
        {
            return false;
        }

        var closingKind = Current.Kind == SyntaxKind.OpenBracketToken ? SyntaxKind.CloseBracketToken : SyntaxKind.CloseParenToken;
        for (var offset = 1; ; offset++)
        {
            var token = Peek(offset);

            if (token.Kind == closingKind)
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
                parameterList.AddRange(ParseParameterGroup(commas));

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
        var arrayBounds = Current.Kind == SyntaxKind.OpenBracketToken ? ParseParameterArrayBounds() : null;
        var typeClause = ParseTypeClause();
        return new ParameterSyntax(passingKeyword, identifier, typeClause, arrayBounds);
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
