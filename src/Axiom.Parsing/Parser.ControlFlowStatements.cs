using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private IfStatementSyntax ParseIfStatement()
    {
        var ifKeyword = Match(SyntaxKind.IfKeyword, "IF");
        var condition = ParseExpression();
        var thenKeyword = Match(SyntaxKind.ThenKeyword, "THEN");
        if (Current.Kind is not SyntaxKind.NewLineToken and not SyntaxKind.EndOfFileToken)
        {
            return ParseSingleLineIf(ifKeyword, condition, thenKeyword);
        }
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
            elseClause = new ElseClauseSyntax(elseKeyword, ParseStatementsUntil(
                static parser => parser.IsEndIf(),
                onDuplicateBoundary: static parser =>
                {
                    if (parser.IsElseClauseStart() || parser.IsElseIfClauseStart())
                    {
                        parser.ReportDuplicateElse(parser.Current);
                        parser.NextToken();
                        return true;
                    }
                    return false;
                }));
        }

        var (endKeyword, endIfKeyword) = MatchEndPair(SyntaxKind.IfKeyword, "END IF");
        return new IfStatementSyntax(ifKeyword, condition, thenKeyword, statements, elseIfClauses, elseClause, endKeyword, endIfKeyword);
    }

    private IfStatementSyntax ParseSingleLineIf(SyntaxToken ifKeyword, ExpressionSyntax condition, SyntaxToken thenKeyword)
    {
        var statements = ParseSingleLineIfBranch();
        ElseClauseSyntax? elseClause = null;
        if (Current.Kind == SyntaxKind.ElseKeyword)
        {
            var elseKeyword = NextToken();
            elseClause = new ElseClauseSyntax(elseKeyword, ParseSingleLineIfBranch());
        }

        if (Current.Kind is not SyntaxKind.NewLineToken and not SyntaxKind.EndOfFileToken)
        {
            ReportUnexpectedToken(Current, "После ветви однострочного IF ожидается конец строки; допускается один оператор в каждой ветви");
            SkipSingleLineIfRemainder();
        }

        // Короткая форма использует те же ветви и области видимости, но не имеет END IF в исходнике.
        return new IfStatementSyntax(ifKeyword, condition, thenKeyword, statements, [], elseClause,
            CreateMissingToken(SyntaxKind.EndKeyword), CreateMissingToken(SyntaxKind.IfKeyword), isSingleLine: true);
    }

    private IReadOnlyList<StatementSyntax> ParseSingleLineIfBranch()
    {
        if (IsStatementTerminator(Current.Kind))
        {
            ReportUnexpectedToken(Current, "В ветви однострочного IF ожидается оператор");
            return [];
        }

        if (Current.Kind is SyntaxKind.IfKeyword or SyntaxKind.SelectKeyword or SyntaxKind.ForKeyword
            or SyntaxKind.WhileKeyword or SyntaxKind.DoKeyword or SyntaxKind.TryKeyword
            or SyntaxKind.SubKeyword or SyntaxKind.FunctionKeyword or SyntaxKind.TypeKeyword)
        {
            ReportUnexpectedToken(Current, "В ветви однострочного IF нельзя открывать вложенный блок; используйте блочную форму IF");
            SkipSingleLineIfRemainder();
            return [];
        }

        return [ParseStatement()];
    }

    private void SkipSingleLineIfRemainder()
    {
        while (Current.Kind is not SyntaxKind.NewLineToken and not SyntaxKind.EndOfFileToken)
        {
            NextToken();
        }
    }

    private StatementSyntax ParseForStatement()
    {
        var forKeyword = Match(SyntaxKind.ForKeyword, "FOR");

        // EACH — контекстное слово: только сразу после FOR оно открывает обход
        // массива. Вне заголовка EACH остаётся обычным именем.
        if (IsContextualKeyword("EACH"))
        {
            return ParseForEachStatement(forKeyword);
        }

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

        // Необязательное имя сохраняется в синтаксисе; Binder проверяет его
        // совпадение со счётчиком закрываемого FOR без учёта регистра.
        SyntaxToken? nextCounterIdentifier = Current.Kind is SyntaxKind.IdentifierToken or SyntaxKind.LineKeyword ? NextToken() : null;

        if (Current.Kind is not (SyntaxKind.NewLineToken or SyntaxKind.EndOfFileToken))
        {
            Diagnostics.Report(
                DiagnosticCodes.UnexpectedToken,
                "После NEXT и необязательного имени счётчика ожидается конец строки.",
                Current.Location);

            while (Current.Kind is not (SyntaxKind.NewLineToken or SyntaxKind.EndOfFileToken))
            {
                NextToken();
            }
        }

        return new ForStatementSyntax(
            forKeyword,
            identifier,
            equalsToken,
            startExpression,
            toKeyword,
            endExpression,
            stepClause,
            statements,
            nextKeyword,
            nextCounterIdentifier);
    }

    private ForEachStatementSyntax ParseForEachStatement(SyntaxToken forKeyword)
    {
        var eachKeyword = NextToken();

        SyntaxToken? byRefKeyword = null;
        SyntaxToken? byValKeyword = null;

        if (Current.Kind == SyntaxKind.ByRefKeyword)
        {
            byRefKeyword = NextToken();
        }
        else if (Current.Kind == SyntaxKind.ByValKeyword)
        {
            byValKeyword = NextToken();
        }

        var identifier = MatchIdentifier();

        TypeClauseSyntax? typeClause = null;
        if (Current.Kind == SyntaxKind.AsKeyword)
        {
            typeClause = ParseTypeClause();
        }

        // IN — тоже контекстное слово заголовка. Отдельное сообщение точнее
        // общего «ожидался идентификатор»: пропуск IN ломает весь заголовок.
        SyntaxToken inKeyword;
        if (IsContextualKeyword("IN"))
        {
            inKeyword = NextToken();
        }
        else
        {
            Diagnostics.Report(
                DiagnosticCodes.ExpectedIdentifier,
                "Ожидалось слово IN после переменной цикла.",
                CreateDiagnosticLocation(Current.Location));
            inKeyword = CreateMissingToken(SyntaxKind.IdentifierToken);
        }

        var sourceExpression = ParseExpression();

        SkipNewLines();
        var statements = ParseStatementsUntil(static parser => parser.Current.Kind == SyntaxKind.NextKeyword);
        var nextKeyword = Match(SyntaxKind.NextKeyword, "NEXT");

        // Имя после NEXT необязательно; указанное обязано совпадать с именем
        // переменной цикла, как и у числового FOR.
        SyntaxToken? nextCounterIdentifier = Current.Kind is SyntaxKind.IdentifierToken or SyntaxKind.LineKeyword ? NextToken() : null;

        if (Current.Kind is not (SyntaxKind.NewLineToken or SyntaxKind.EndOfFileToken))
        {
            Diagnostics.Report(
                DiagnosticCodes.UnexpectedToken,
                "После NEXT и необязательного имени переменной цикла ожидается конец строки.",
                Current.Location);

            while (Current.Kind is not (SyntaxKind.NewLineToken or SyntaxKind.EndOfFileToken))
            {
                NextToken();
            }
        }

        return new ForEachStatementSyntax(
            forKeyword,
            eachKeyword,
            byRefKeyword,
            byValKeyword,
            identifier,
            typeClause,
            inKeyword,
            sourceExpression,
            statements,
            nextKeyword,
            nextCounterIdentifier);
    }

    private bool IsContextualKeyword(string word)
    {
        return Current.Kind == SyntaxKind.IdentifierToken
            && string.Equals(Current.Text, word, StringComparison.OrdinalIgnoreCase);
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
        var statements = ParseStatementsUntil(
            static parser => parser.IsEndSelect(),
            onDuplicateBoundary: static parser =>
            {
                if (parser.IsCaseElseClauseStart())
                {
                    parser.ReportDuplicateCaseElse(parser.Current);
                    parser.NextToken(); // consume CASE
                    parser.NextToken(); // consume ELSE
                    return true;
                }
                return false;
            });
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
            ReportExpectedToken("CATCH или FINALLY");
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

    private StatementSyntax ParseContinueStatement()
    {
        var continueKeyword = Match(SyntaxKind.ContinueKeyword, "CONTINUE");

        return Current.Kind switch
        {
            SyntaxKind.ForKeyword => new ContinueForStatementSyntax(continueKeyword, Match(SyntaxKind.ForKeyword, "FOR")),
            SyntaxKind.WhileKeyword => new ContinueWhileStatementSyntax(continueKeyword, Match(SyntaxKind.WhileKeyword, "WHILE")),
            SyntaxKind.DoKeyword => new ContinueDoStatementSyntax(continueKeyword, Match(SyntaxKind.DoKeyword, "DO")),
            _ => new ContinueForStatementSyntax(continueKeyword, Match(SyntaxKind.ForKeyword, "FOR, WHILE или DO"))
        };
    }

    private ReturnStatementSyntax ParseReturnStatement()
    {
        var returnKeyword = Match(SyntaxKind.ReturnKeyword, "RETURN");
        var expression = IsExpressionTerminator(Current.Kind) ? null : ParseExpression();
        return new ReturnStatementSyntax(returnKeyword, expression);
    }

}
