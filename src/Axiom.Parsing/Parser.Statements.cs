using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private StatementSyntax ParseStatement(bool allowLocalCallableDeclarations = false)
    {
        if (allowLocalCallableDeclarations)
        {
            return Current.Kind switch
            {
                SyntaxKind.SubKeyword => new LocalSubDeclarationStatementSyntax(ParseSubDeclaration()),
                SyntaxKind.FunctionKeyword => new LocalFunctionDeclarationStatementSyntax(ParseFunctionDeclaration()),
                _ => ParseNonDeclarationStatement()
            };
        }

        return ParseNonDeclarationStatement();
    }

    private StatementSyntax ParseNonDeclarationStatement()
    {
        return Current.Kind switch
        {
            SyntaxKind.DimKeyword => ParseDimStatement(),
            SyntaxKind.ConstKeyword => ParseConstStatement(),
            SyntaxKind.PrintKeyword => ParsePrintStatement(),
            SyntaxKind.InputKeyword => ParseInputStatement(),
            SyntaxKind.OnKeyword => ParseOnStatement(),
            SyntaxKind.OpenKeyword => ParseOpenStatement(),
            SyntaxKind.CloseKeyword => ParseCloseStatement(),
            SyntaxKind.ErrorKeyword => ParseErrorStatement(),
            SyntaxKind.EraseKeyword => ParseEraseStatement(),
            SyntaxKind.RedimKeyword => ParseRedimStatement(),
            SyntaxKind.IfKeyword => ParseIfStatement(),
            SyntaxKind.SelectKeyword => ParseSelectCaseStatement(),
            SyntaxKind.ForKeyword => ParseForStatement(),
            SyntaxKind.DoKeyword => ParseDoLoopStatement(),
            SyntaxKind.WhileKeyword => ParseWhileStatement(),
            SyntaxKind.TryKeyword => ParseTryStatement(),
            SyntaxKind.ExitKeyword => ParseExitStatement(),
            SyntaxKind.ReturnKeyword => ParseReturnStatement(),
            SyntaxKind.IdentifierToken or SyntaxKind.SelfKeyword => ParseIdentifierLeadingStatement(),
            _ => new ExpressionStatementSyntax(ParseExpression())
        };
    }

    private DimStatementSyntax ParseDimStatement()
    {
        var dimKeyword = Match(SyntaxKind.DimKeyword, "DIM");
        var declarations = new List<VariableDeclarationSyntax> { ParseVariableDeclaration() };
        var commaTokens = new List<SyntaxToken>();

        while (Current.Kind == SyntaxKind.CommaToken)
        {
            commaTokens.Add(NextToken());
            declarations.Add(ParseVariableDeclaration());
        }

        return new DimStatementSyntax(dimKeyword, declarations, commaTokens);
    }

    private ConstStatementSyntax ParseConstStatement()
    {
        var constKeyword = Match(SyntaxKind.ConstKeyword, "CONST");
        var identifier = MatchIdentifier();
        var typeClause = ParseTypeClause();
        var equalsToken = Match(SyntaxKind.EqualsToken, "=");
        var initializer = ParseExpression();

        return new ConstStatementSyntax(
            constKeyword,
            new ConstantDeclarationSyntax(identifier, typeClause, equalsToken, initializer));
    }

    private StatementSyntax ParsePrintStatement()
    {
        var printKeyword = Match(SyntaxKind.PrintKeyword, "PRINT");

        if (Current.Kind == SyntaxKind.UsingKeyword)
        {
            return ParsePrintUsingStatement(printKeyword);
        }

        var items = new List<PrintItemSyntax>();

        if (IsStatementTerminator(Current.Kind))
        {
            return new PrintStatementSyntax(printKeyword, items);
        }

        while (true)
        {
            var expression = ParseExpression();
            SyntaxToken? separatorToken = null;

            if (Current.Kind is SyntaxKind.SemicolonToken or SyntaxKind.CommaToken)
            {
                separatorToken = NextToken();
            }

            items.Add(new PrintItemSyntax(expression, separatorToken));

            if (separatorToken is null || IsStatementTerminator(Current.Kind))
            {
                break;
            }
        }

        return new PrintStatementSyntax(printKeyword, items);
    }

    private PrintUsingStatementSyntax ParsePrintUsingStatement(SyntaxToken printKeyword)
    {
        var usingKeyword = Match(SyntaxKind.UsingKeyword, "USING");
        var formatExpression = ParseExpression();
        var formatSeparatorToken = Match(SyntaxKind.SemicolonToken, "';' после форматной строки PRINT USING");
        var items = new List<PrintItemSyntax>();

        if (IsStatementTerminator(Current.Kind))
        {
            return new PrintUsingStatementSyntax(
                printKeyword,
                usingKeyword,
                formatExpression,
                formatSeparatorToken,
                items);
        }

        while (true)
        {
            var expression = ParseExpression();
            SyntaxToken? separatorToken = null;

            if (Current.Kind is SyntaxKind.SemicolonToken or SyntaxKind.CommaToken)
            {
                separatorToken = NextToken();
            }

            items.Add(new PrintItemSyntax(expression, separatorToken));

            if (separatorToken is null || IsStatementTerminator(Current.Kind))
            {
                break;
            }
        }

        return new PrintUsingStatementSyntax(
            printKeyword,
            usingKeyword,
            formatExpression,
            formatSeparatorToken,
            items);
    }

    private InputStatementSyntax ParseInputStatement()
    {
        var inputKeyword = Match(SyntaxKind.InputKeyword, "INPUT");
        SyntaxToken? promptToken = null;
        SyntaxToken? promptSeparatorToken = null;

        if (Current.Kind == SyntaxKind.StringLiteralToken &&
            Peek(1).Kind is SyntaxKind.SemicolonToken or SyntaxKind.CommaToken)
        {
            promptToken = NextToken();
            promptSeparatorToken = NextToken();
        }

        var identifiers = new List<SyntaxToken> { MatchIdentifier() };
        var commaTokens = new List<SyntaxToken>();

        while (Current.Kind == SyntaxKind.CommaToken)
        {
            commaTokens.Add(NextToken());
            identifiers.Add(MatchIdentifier());
        }

        return new InputStatementSyntax(inputKeyword, promptToken, promptSeparatorToken, identifiers, commaTokens);
    }

    private StatementSyntax ParseOnStatement()
    {
        return Peek(1).Kind switch
        {
            SyntaxKind.MouseKeyword => ParseOnMouseStatement(),
            _ => ParseOnKeyStatement()
        };
    }

    private OnKeyStatementSyntax ParseOnKeyStatement()
    {
        var onKeyword = Match(SyntaxKind.OnKeyword, "ON");
        var keyKeyword = Match(SyntaxKind.KeyKeyword, "KEY");
        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        var keyExpression = ParseExpression();
        var closeParenToken = Match(SyntaxKind.CloseParenToken, ")");
        var commaToken = Match(SyntaxKind.CommaToken, "',' после ON KEY(...)");

        var actionToken = Current.Kind is SyntaxKind.OnKeyword or SyntaxKind.OffKeyword or SyntaxKind.StopKeyword
            ? NextToken()
            : MatchIdentifier();

        return new OnKeyStatementSyntax(
            onKeyword,
            keyKeyword,
            openParenToken,
            keyExpression,
            closeParenToken,
            commaToken,
            actionToken);
    }

    private OnMouseStatementSyntax ParseOnMouseStatement()
    {
        var onKeyword = Match(SyntaxKind.OnKeyword, "ON");
        var mouseKeyword = Match(SyntaxKind.MouseKeyword, "MOUSE");
        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        var buttonExpression = ParseExpression();
        SyntaxToken? targetCommaToken = null;
        ExpressionSyntax? eventExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            targetCommaToken = NextToken();
            eventExpression = ParseExpression();
        }

        var closeParenToken = Match(SyntaxKind.CloseParenToken, ")");
        var commaToken = Match(SyntaxKind.CommaToken, "',' после ON MOUSE(...)");

        var actionToken = Current.Kind is SyntaxKind.OnKeyword or SyntaxKind.OffKeyword or SyntaxKind.StopKeyword
            ? NextToken()
            : MatchIdentifier();

        return new OnMouseStatementSyntax(
            onKeyword,
            mouseKeyword,
            openParenToken,
            buttonExpression,
            targetCommaToken,
            eventExpression,
            closeParenToken,
            commaToken,
            actionToken);
    }

    private OpenStatementSyntax ParseOpenStatement()
    {
        var openKeyword = Match(SyntaxKind.OpenKeyword, "OPEN");
        var pathExpression = ParseExpression();
        var forKeyword = Match(SyntaxKind.ForKeyword, "FOR");
        var modeKeyword = ParseOpenModeKeyword();
        var asKeyword = Match(SyntaxKind.AsKeyword, "AS");
        var fileIdentifier = MatchIdentifier();

        return new OpenStatementSyntax(openKeyword, pathExpression, forKeyword, modeKeyword, asKeyword, fileIdentifier);
    }

    private CloseStatementSyntax ParseCloseStatement()
    {
        var closeKeyword = Match(SyntaxKind.CloseKeyword, "CLOSE");
        var fileIdentifier = MatchIdentifier();
        return new CloseStatementSyntax(closeKeyword, fileIdentifier);
    }

    private ErrorStatementSyntax ParseErrorStatement()
    {
        var errorKeyword = Match(SyntaxKind.ErrorKeyword, "ERROR");
        var codeExpression = ParseExpression();
        SyntaxToken? commaToken = null;
        ExpressionSyntax? messageExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            commaToken = NextToken();
            messageExpression = ParseExpression();
        }

        return new ErrorStatementSyntax(errorKeyword, codeExpression, commaToken, messageExpression);
    }

    private EraseStatementSyntax ParseEraseStatement()
    {
        var eraseKeyword = Match(SyntaxKind.EraseKeyword, "ERASE");
        var target = ParsePostfixExpression(new NameExpressionSyntax(MatchIdentifier()));
        return new EraseStatementSyntax(eraseKeyword, target);
    }

    private RedimStatementSyntax ParseRedimStatement()
    {
        var redimKeyword = Match(SyntaxKind.RedimKeyword, "REDIM");
        var preserveKeyword = Current.Kind == SyntaxKind.PreserveKeyword
            ? NextToken()
            : null;
        var target = ParseRedimTargetExpression();
        var arrayBounds = ParseExplicitArrayBounds();
        return new RedimStatementSyntax(redimKeyword, preserveKeyword, target, arrayBounds);
    }

    private ExpressionSyntax ParseRedimTargetExpression()
    {
        ExpressionSyntax expression = new NameExpressionSyntax(MatchIdentifier());

        while (true)
        {
            if (Current.Kind == SyntaxKind.DotToken)
            {
                var dotToken = Match(SyntaxKind.DotToken, ".");
                var identifier = MatchIdentifier();
                expression = new FieldAccessExpressionSyntax(expression, dotToken, identifier);
                continue;
            }

            if (Current.Kind == SyntaxKind.OpenParenToken && !LooksLikeRedimBounds())
            {
                var openParenToken = NextToken();
                ParseSeparatedExpressions(
                    out var arguments,
                    out var commaTokens,
                    SyntaxKind.CloseParenToken,
                    ")",
                    out var closeParenToken);

                expression = new CallExpressionSyntax(expression, openParenToken, arguments, commaTokens, closeParenToken);
                continue;
            }

            break;
        }

        return expression;
    }

    private bool LooksLikeRedimBounds()
    {
        if (Current.Kind != SyntaxKind.OpenParenToken)
        {
            return false;
        }

        var depth = 0;

        for (var offset = 0; ; offset++)
        {
            var token = Peek(offset);

            if (token.Kind == SyntaxKind.EndOfFileToken)
            {
                return false;
            }

            switch (token.Kind)
            {
                case SyntaxKind.OpenParenToken:
                    depth++;
                    break;

                case SyntaxKind.CloseParenToken:
                    depth--;

                    if (depth == 0)
                    {
                        return false;
                    }

                    break;

                case SyntaxKind.ToKeyword when depth == 1:
                    return true;
            }
        }
    }

    private ArrayBoundsSyntax ParseExplicitArrayBounds()
    {
        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        var dimensions = new List<ArrayDimensionSyntax>();
        var commaTokens = new List<SyntaxToken>();

        while (true)
        {
            if (Current.Kind == SyntaxKind.CloseParenToken)
            {
                ReportExpectedExpression();
                dimensions.Add(new ArrayDimensionSyntax(
                    new ErrorExpressionSyntax(CreateMissingToken(SyntaxKind.IntegerLiteralToken)),
                    CreateMissingToken(SyntaxKind.ToKeyword),
                    new ErrorExpressionSyntax(CreateMissingToken(SyntaxKind.IntegerLiteralToken))));
                break;
            }

            var lowerBound = ParseExpression();
            var toKeyword = Match(SyntaxKind.ToKeyword, "TO");
            var upperBound = ParseExpression();
            dimensions.Add(new ArrayDimensionSyntax(lowerBound, toKeyword, upperBound));

            if (Current.Kind != SyntaxKind.CommaToken)
            {
                break;
            }

            commaTokens.Add(NextToken());
        }

        var closeParenToken = Match(SyntaxKind.CloseParenToken, ")");
        return new ArrayBoundsSyntax(openParenToken, dimensions, commaTokens, closeParenToken);
    }

    private SyntaxToken ParseOpenModeKeyword()
    {
        if (Current.Kind is SyntaxKind.InputKeyword or SyntaxKind.OutputKeyword or SyntaxKind.AppendKeyword)
        {
            return NextToken();
        }

        ReportExpectedToken("INPUT, OUTPUT или APPEND");
        return CreateMissingToken(SyntaxKind.InputKeyword);
    }

    private IfStatementSyntax ParseIfStatement()
    {
        var ifKeyword = Match(SyntaxKind.IfKeyword, "IF");
        var condition = ParseExpression();
        var thenKeyword = Match(SyntaxKind.ThenKeyword, "THEN");
        SkipNewLines();

        var statements = ParseStatementsUntil(static parser =>
            parser.IsElseIfClauseStart() || parser.IsElseClauseStart() || parser.IsEndIf());
        var elseIfClauses = new List<ElseIfClauseSyntax>();
        ElseClauseSyntax? elseClause = null;

        while (IsElseIfClauseStart())
        {
            var elseIfKeyword = NextToken();
            var elseIfCondition = ParseExpression();
            var elseIfThenKeyword = Match(SyntaxKind.ThenKeyword, "THEN");
            SkipNewLines();

            var elseIfStatements = ParseStatementsUntil(static parser =>
                parser.IsElseIfClauseStart() || parser.IsElseClauseStart() || parser.IsEndIf());
            elseIfClauses.Add(new ElseIfClauseSyntax(elseIfKeyword, elseIfCondition, elseIfThenKeyword, elseIfStatements));
        }

        if (IsElseClauseStart())
        {
            var elseKeyword = NextToken();
            SkipNewLines();
            elseClause = new ElseClauseSyntax(elseKeyword, ParseStatementsUntil(static parser => parser.IsEndIf()));
        }

        var (endKeyword, endIfKeyword) = MatchEndPair(SyntaxKind.IfKeyword, "END IF");
        return new IfStatementSyntax(ifKeyword, condition, thenKeyword, statements, elseIfClauses, elseClause, endKeyword, endIfKeyword);
    }

    private ForStatementSyntax ParseForStatement()
    {
        var forKeyword = Match(SyntaxKind.ForKeyword, "FOR");
        var identifier = MatchIdentifier();
        var equalsToken = Match(SyntaxKind.EqualsToken, "=");
        var startExpression = ParseExpression();
        var toKeyword = Match(SyntaxKind.ToKeyword, "TO");
        var endExpression = ParseExpression();
        StepClauseSyntax? stepClause = null;

        if (Current.Kind == SyntaxKind.StepKeyword)
        {
            var stepKeyword = NextToken();
            var stepExpression = ParseExpression();
            stepClause = new StepClauseSyntax(stepKeyword, stepExpression);
        }

        SkipNewLines();
        var statements = ParseStatementsUntil(static parser => parser.Current.Kind == SyntaxKind.NextKeyword);
        var nextKeyword = Match(SyntaxKind.NextKeyword, "NEXT");

        return new ForStatementSyntax(
            forKeyword,
            identifier,
            equalsToken,
            startExpression,
            toKeyword,
            endExpression,
            stepClause,
            statements,
            nextKeyword);
    }

    private SelectCaseStatementSyntax ParseSelectCaseStatement()
    {
        var selectKeyword = Match(SyntaxKind.SelectKeyword, "SELECT");
        var caseKeyword = Match(SyntaxKind.CaseKeyword, "CASE");
        var testExpression = ParseExpression();
        SkipNewLines();

        var clauses = new List<SelectCaseClauseSyntax>();

        while (IsCaseClauseStart() && !IsCaseElseClauseStart())
        {
            clauses.Add(ParseSelectCaseClause());
        }

        CaseElseClauseSyntax? caseElseClause = null;

        if (IsCaseElseClauseStart())
        {
            caseElseClause = ParseCaseElseClause();
        }

        var (endKeyword, endSelectKeyword) = MatchEndPair(SyntaxKind.SelectKeyword, "END SELECT");
        return new SelectCaseStatementSyntax(selectKeyword, caseKeyword, testExpression, clauses, caseElseClause, endKeyword, endSelectKeyword);
    }

    private SelectCaseClauseSyntax ParseSelectCaseClause()
    {
        var caseKeyword = Match(SyntaxKind.CaseKeyword, "CASE");
        var selectors = new List<SelectCaseSelectorSyntax> { ParseSelectCaseSelector() };
        var commaTokens = new List<SyntaxToken>();

        while (Current.Kind == SyntaxKind.CommaToken)
        {
            commaTokens.Add(NextToken());
            selectors.Add(ParseSelectCaseSelector());
        }

        SkipNewLines();
        var statements = ParseStatementsUntil(static parser => parser.IsCaseClauseStart() || parser.IsEndSelect());
        return new SelectCaseClauseSyntax(caseKeyword, selectors, commaTokens, statements);
    }

    private CaseElseClauseSyntax ParseCaseElseClause()
    {
        var caseKeyword = Match(SyntaxKind.CaseKeyword, "CASE");
        var elseKeyword = Match(SyntaxKind.ElseKeyword, "ELSE");
        SkipNewLines();
        var statements = ParseStatementsUntil(static parser => parser.IsEndSelect());
        return new CaseElseClauseSyntax(caseKeyword, elseKeyword, statements);
    }

    private SelectCaseSelectorSyntax ParseSelectCaseSelector()
    {
        if (Current.Kind == SyntaxKind.IsKeyword)
        {
            var isKeyword = NextToken();
            var operatorToken = ParseCaseComparisonOperator();
            var expression = ParseExpression();
            return new CaseComparisonSelectorSyntax(isKeyword, operatorToken, expression);
        }

        var startExpression = ParseExpression();

        if (Current.Kind == SyntaxKind.ToKeyword)
        {
            var toKeyword = NextToken();
            var endExpression = ParseExpression();
            return new CaseRangeSelectorSyntax(startExpression, toKeyword, endExpression);
        }

        return new CaseValueSelectorSyntax(startExpression);
    }

    private SyntaxToken ParseCaseComparisonOperator()
    {
        if (Current.Kind is SyntaxKind.EqualsToken
            or SyntaxKind.NotEqualsToken
            or SyntaxKind.LessToken
            or SyntaxKind.LessOrEqualsToken
            or SyntaxKind.GreaterToken
            or SyntaxKind.GreaterOrEqualsToken)
        {
            return NextToken();
        }

        ReportExpectedToken("оператор сравнения");
        return CreateMissingToken(SyntaxKind.EqualsToken);
    }

    private WhileStatementSyntax ParseWhileStatement()
    {
        var whileKeyword = Match(SyntaxKind.WhileKeyword, "WHILE");
        var condition = ParseExpression();
        SkipNewLines();

        var statements = ParseStatementsUntil(static parser => parser.IsEndWhile());
        var (endKeyword, endWhileKeyword) = MatchEndPair(SyntaxKind.WhileKeyword, "END WHILE");

        return new WhileStatementSyntax(whileKeyword, condition, statements, endKeyword, endWhileKeyword);
    }

    private DoLoopStatementSyntax ParseDoLoopStatement()
    {
        var doKeyword = Match(SyntaxKind.DoKeyword, "DO");
        var leadingCondition = ParseOptionalDoLoopConditionClause();
        SkipNewLines();

        var statements = ParseStatementsUntil(static parser => parser.IsLoop());
        var loopKeyword = Match(SyntaxKind.LoopKeyword, "LOOP");
        var trailingCondition = ParseOptionalDoLoopConditionClause();

        return new DoLoopStatementSyntax(doKeyword, leadingCondition, statements, loopKeyword, trailingCondition);
    }

    private TryStatementSyntax ParseTryStatement()
    {
        var tryKeyword = Match(SyntaxKind.TryKeyword, "TRY");
        SkipNewLines();

        var statements = ParseStatementsUntil(static parser =>
            parser.IsCatchClauseStart() || parser.IsFinallyClauseStart() || parser.IsEndTry());

        CatchClauseSyntax? catchClause = null;
        FinallyClauseSyntax? finallyClause = null;

        if (IsCatchClauseStart())
        {
            var catchKeyword = NextToken();
            var identifier = MatchIdentifier();
            SkipNewLines();

            var catchStatements = ParseStatementsUntil(static parser =>
                parser.IsFinallyClauseStart() || parser.IsEndTry());

            catchClause = new CatchClauseSyntax(catchKeyword, identifier, catchStatements);
        }

        if (IsFinallyClauseStart())
        {
            var finallyKeyword = NextToken();
            SkipNewLines();

            var finallyStatements = ParseStatementsUntil(static parser => parser.IsEndTry());
            finallyClause = new FinallyClauseSyntax(finallyKeyword, finallyStatements);
        }

        if (catchClause is null && finallyClause is null)
        {
            ReportExpectedToken("CATCH РёР»Рё FINALLY");
        }

        var (endKeyword, endTryKeyword) = MatchEndPair(SyntaxKind.TryKeyword, "END TRY");
        return new TryStatementSyntax(tryKeyword, statements, catchClause, finallyClause, endKeyword, endTryKeyword);
    }

    private DoLoopConditionClauseSyntax? ParseOptionalDoLoopConditionClause()
    {
        if (!IsDoLoopConditionKeyword())
        {
            return null;
        }

        var keyword = NextToken();
        var condition = ParseExpression();
        return new DoLoopConditionClauseSyntax(keyword, condition);
    }

    private StatementSyntax ParseExitStatement()
    {
        var exitKeyword = Match(SyntaxKind.ExitKeyword, "EXIT");

        return Current.Kind switch
        {
            SyntaxKind.ForKeyword => new ExitForStatementSyntax(exitKeyword, Match(SyntaxKind.ForKeyword, "FOR")),
            SyntaxKind.DoKeyword => new ExitDoStatementSyntax(exitKeyword, Match(SyntaxKind.DoKeyword, "DO")),
            _ => new ExitForStatementSyntax(exitKeyword, Match(SyntaxKind.ForKeyword, "FOR или DO"))
        };
    }

    private ReturnStatementSyntax ParseReturnStatement()
    {
        var returnKeyword = Match(SyntaxKind.ReturnKeyword, "RETURN");
        var expression = IsExpressionTerminator(Current.Kind) ? null : ParseExpression();
        return new ReturnStatementSyntax(returnKeyword, expression);
    }

    private StatementSyntax ParseIdentifierLeadingStatement()
    {
        var identifier = Current.Kind == SyntaxKind.SelfKeyword
            ? NextToken()
            : MatchIdentifier();
        var target = ParsePostfixExpression(new NameExpressionSyntax(identifier));

        if (Current.Kind == SyntaxKind.EqualsToken)
        {
            var equalsToken = NextToken();
            var expression = ParseExpression();
            return new AssignmentStatementSyntax(target, equalsToken, expression);
        }

        return new ExpressionStatementSyntax(target);
    }

    private VariableDeclarationSyntax ParseVariableDeclaration()
    {
        var declarators = new List<VariableDeclaratorSyntax> { ParseVariableDeclarator() };
        var commaTokens = new List<SyntaxToken>();

        while (Current.Kind == SyntaxKind.CommaToken)
        {
            commaTokens.Add(NextToken());
            declarators.Add(ParseVariableDeclarator());
        }

        TypeClauseSyntax? typeClause = null;

        if (Current.Kind == SyntaxKind.AsKeyword)
        {
            typeClause = ParseTypeClause();
        }

        SyntaxToken? equalsToken = null;
        ExpressionSyntax? initializer = null;

        if (Current.Kind == SyntaxKind.EqualsToken)
        {
            equalsToken = NextToken();
            initializer = ParseExpression();
        }

        return new VariableDeclarationSyntax(declarators, commaTokens, typeClause, equalsToken, initializer);
    }

    private VariableDeclaratorSyntax ParseVariableDeclarator()
    {
        var identifier = MatchIdentifier();
        var arrayBounds = Current.Kind == SyntaxKind.OpenParenToken ? ParseArrayBounds() : null;
        return new VariableDeclaratorSyntax(identifier, arrayBounds);
    }

    private IReadOnlyList<StatementSyntax> ParseStatementsUntil(
        Func<Parser, bool> terminator,
        bool allowLocalCallableDeclarations = false)
    {
        var statements = new List<StatementSyntax>();
        SkipNewLines();

        while (Current.Kind != SyntaxKind.EndOfFileToken && !terminator(this))
        {
            if (IsUnexpectedBoundaryToken())
            {
                ReportUnexpectedToken(Current, "Неожиданный маркер конца блока");
                NextToken();
                SkipNewLines();
                continue;
            }

            var start = _position;
            statements.Add(ParseStatement(allowLocalCallableDeclarations));

            if (_position == start)
            {
                ReportUnexpectedToken(Current, "Неожиданный токен");
                NextToken();
            }

            SkipNewLines();
        }

        return statements;
    }
}
