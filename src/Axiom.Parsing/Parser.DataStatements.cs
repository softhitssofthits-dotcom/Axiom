using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
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

        SyntaxToken? usingKeyword = null;
        ExpressionSyntax? encodingExpression = null;
        if (Current.Kind == SyntaxKind.UsingKeyword)
        {
            usingKeyword = NextToken();
            encodingExpression = ParseExpression();
        }

        return new OpenStatementSyntax(openKeyword, pathExpression, forKeyword, modeKeyword, asKeyword, fileIdentifier, usingKeyword, encodingExpression);
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
                var identifier = MatchFieldIdentifier();
                expression = new FieldAccessExpressionSyntax(expression, dotToken, identifier);
                continue;
            }

            if (Current.Kind == SyntaxKind.OpenBracketToken && !LooksLikeBracketRedimBounds())
            {
                expression = ParseArrayAccessExpression(expression);
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
        if (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            return ParseBracketArrayBounds(requireExplicitBounds: true);
        }

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

}
