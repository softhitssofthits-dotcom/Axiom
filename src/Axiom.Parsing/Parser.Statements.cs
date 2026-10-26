using Axiom.Core;
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
            SyntaxKind.DimKeyword when Peek(1).Kind == SyntaxKind.OpenParenToken => ParseTupleDimStatement(),
            SyntaxKind.OpenParenToken when LooksLikeTupleUnpacking() => ParseTupleLeadingStatement(),
            SyntaxKind.DimKeyword => ParseDimStatement(),
            SyntaxKind.ConstKeyword => ParseConstStatement(),
            SyntaxKind.PrintKeyword => ParsePrintStatement(),
            SyntaxKind.InputKeyword => ParseInputStatement(),
            SyntaxKind.SleepKeyword => ParseSleepStatement(),
            SyntaxKind.RandomizeKeyword => ParseRandomizeStatement(),
            SyntaxKind.ScreenKeyword => ParseScreenStatement(),
            SyntaxKind.SmoothKeyword => ParseSmoothStatement(),
            SyntaxKind.CanvasKeyword => ParseCanvasStatement(),
            SyntaxKind.BeginFrameKeyword => new BeginFrameStatementSyntax(Match(SyntaxKind.BeginFrameKeyword, "BEGINFRAME")),
            SyntaxKind.PresentKeyword => new PresentStatementSyntax(Match(SyntaxKind.PresentKeyword, "PRESENT")),
            SyntaxKind.CameraKeyword => ParseCameraStatement(),
            SyntaxKind.ViewportKeyword => ParseViewportStatement(),
            SyntaxKind.ResetCameraKeyword => ParseResetCameraStatement(),
            SyntaxKind.ViewKeyword => ParseViewStatement(),
            SyntaxKind.WindowKeyword => ParseWindowStatement(),
            SyntaxKind.ClsKeyword => ParseClsStatement(),
            SyntaxKind.ColorKeyword => ParseColorStatement(),
            SyntaxKind.LocateKeyword => ParseLocateStatement(),
            SyntaxKind.PaletteKeyword => ParsePaletteStatement(),
            SyntaxKind.PCopyKeyword => ParsePCopyStatement(),
            SyntaxKind.PSetKeyword => ParsePSetStatement(reset: false),
            SyntaxKind.PresetKeyword => ParsePSetStatement(reset: true),
            SyntaxKind.LineKeyword => Peek(1).Kind is SyntaxKind.OpenParenToken or SyntaxKind.StepKeyword or SyntaxKind.MinusToken ? ParseLineStatement() : ParseIdentifierLeadingStatement(),
            SyntaxKind.CircleKeyword => ParseCircleStatement(),
            SyntaxKind.PaintKeyword => ParsePaintStatement(),
            SyntaxKind.DrawKeyword => ParseDrawStatement(),
            SyntaxKind.DrawLineKeyword => ParseDrawLineStatement(),
            SyntaxKind.DrawCircleKeyword => ParseDrawCircleStatement(),
            SyntaxKind.FillCircleKeyword => ParseFillCircleStatement(),
            SyntaxKind.DrawTextKeyword => ParseDrawTextStatement(),
            SyntaxKind.DrawImageKeyword => ParseDrawImageStatement(),
            SyntaxKind.DrawRectKeyword => ParseDrawRectStatement(),
            SyntaxKind.FillRectKeyword => ParseFillRectStatement(),
            SyntaxKind.DrawSpriteKeyword => ParseDrawSpriteStatement(),
            SyntaxKind.DrawTileKeyword => ParseDrawTileStatement(),
            SyntaxKind.DrawTilemapKeyword => ParseDrawTilemapStatement(),
            SyntaxKind.DrawTileLayerKeyword => ParseDrawTileLayerStatement(),
            SyntaxKind.SetMapTileKeyword => ParseSetMapTileStatement(),
            SyntaxKind.GetKeyword => ParseGetStatement(),
            SyntaxKind.PutKeyword => ParsePutStatement(),
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
            SyntaxKind.ContinueKeyword => ParseContinueStatement(),
            SyntaxKind.ReturnKeyword => ParseReturnStatement(),
            SyntaxKind.IdentifierToken or SyntaxKind.SelfKeyword => ParseIdentifierLeadingStatement(),
            _ => new ExpressionStatementSyntax(ParseExpression())
        };
    }

    private StatementSyntax ParseTupleDimStatement()
    {
        var dim = NextToken();
        return ParseTupleLeadingStatement(dim);
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
        var arrayBounds = Current.Kind == SyntaxKind.OpenBracketToken ? ParseParameterArrayBounds() : null;
        var typeClause = ParseTypeClause();
        var equalsToken = Match(SyntaxKind.EqualsToken, "=");
        var initializer = ParseExpression();

        return new ConstStatementSyntax(
            constKeyword,
            new ConstantDeclarationSyntax(identifier, typeClause, equalsToken, initializer, arrayBounds));
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
        var arrayBounds = Current.Kind switch
        {
            SyntaxKind.OpenBracketToken => ParseBracketArrayBounds(),
            SyntaxKind.OpenParenToken => ParseArrayBounds(),
            _ => null
        };
        return new VariableDeclaratorSyntax(identifier, arrayBounds);
    }

    private IReadOnlyList<StatementSyntax> ParseStatementsUntil(
        Func<Parser, bool> terminator,
        bool allowLocalCallableDeclarations = false,
        Func<Parser, bool>? onDuplicateBoundary = null)
    {
        var statements = new List<StatementSyntax>();
        SkipNewLines();

        while (Current.Kind != SyntaxKind.EndOfFileToken && !terminator(this))
        {
            if (IsUnexpectedBoundaryToken())
            {
                if (onDuplicateBoundary != null && onDuplicateBoundary(this))
                {
                    SkipNewLines();
                    continue;
                }

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
