using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private ArrayAccessExpressionSyntax ParseArrayAccessExpression(ExpressionSyntax target)
    {
        var openBracketToken = Match(SyntaxKind.OpenBracketToken, "[");
        var indices = new List<ExpressionSyntax>();
        var commaTokens = new List<SyntaxToken>();

        if (Current.Kind != SyntaxKind.CloseBracketToken)
        {
            while (true)
            {
                indices.Add(ParseArgumentValueExpression());
                if (Current.Kind != SyntaxKind.CommaToken)
                {
                    break;
                }

                commaTokens.Add(NextToken());
            }
        }

        var closeBracketToken = Match(SyntaxKind.CloseBracketToken, "]");
        return new ArrayAccessExpressionSyntax(target, openBracketToken, indices, commaTokens, closeBracketToken);
    }

    private ArrayBoundsSyntax ParseBracketArrayBounds(bool requireExplicitBounds = false)
    {
        var openBracketToken = Match(SyntaxKind.OpenBracketToken, "[");
        var dimensions = new List<ArrayDimensionSyntax>();
        var commaTokens = new List<SyntaxToken>();

        while (true)
        {
            if (!requireExplicitBounds && Current.Kind is SyntaxKind.CommaToken or SyntaxKind.CloseBracketToken)
            {
                dimensions.Add(new ArrayDimensionSyntax(null, null, null));
            }
            else
            {
                var lowerBound = ParseExpression();
                var toKeyword = Match(SyntaxKind.ToKeyword, "TO");
                var upperBound = ParseExpression();
                dimensions.Add(new ArrayDimensionSyntax(lowerBound, toKeyword, upperBound));
            }

            if (Current.Kind != SyntaxKind.CommaToken)
            {
                break;
            }

            commaTokens.Add(NextToken());
        }

        var closeBracketToken = Match(SyntaxKind.CloseBracketToken, "]");
        return new ArrayBoundsSyntax(openBracketToken, dimensions, commaTokens, closeBracketToken);
    }

    private bool LooksLikeBracketRedimBounds()
    {
        var bracketDepth = 0;
        var parenDepth = 0;
        for (var offset = 0; ; offset++)
        {
            var kind = Peek(offset).Kind;
            if (kind is SyntaxKind.EndOfFileToken or SyntaxKind.NewLineToken)
            {
                return true;
            }

            switch (kind)
            {
                case SyntaxKind.OpenBracketToken:
                    bracketDepth++;
                    break;
                case SyntaxKind.CloseBracketToken:
                    bracketDepth--;
                    if (bracketDepth == 0)
                    {
                        return Peek(offset + 1).Kind is not (SyntaxKind.DotToken or SyntaxKind.OpenBracketToken or SyntaxKind.OpenParenToken);
                    }
                    break;
                case SyntaxKind.OpenParenToken:
                    parenDepth++;
                    break;
                case SyntaxKind.CloseParenToken:
                    parenDepth--;
                    break;
                case SyntaxKind.ToKeyword when bracketDepth == 1 && parenDepth == 0:
                    return true;
            }
        }
    }

    private ArrayBoundsSyntax ParseParameterArrayBounds()
    {
        var bounds = ParseBracketArrayBounds();
        if (!bounds.IsDynamic)
        {
            ReportUnexpectedToken(bounds.OpenDelimiterToken, "В параметре массива указывается только число измерений: [] или [,]");
        }

        return bounds;
    }
}
