using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryPrecedence = GetUnaryOperatorPrecedence(Current.Kind);

        if (unaryPrecedence > 0 && unaryPrecedence >= parentPrecedence)
        {
            var operatorToken = NextToken();
            var operand = ParseExpression(unaryPrecedence);
            left = new UnaryExpressionSyntax(operatorToken, operand);
        }
        else
        {
            left = ParsePrimaryExpression();
        }

        while (true)
        {
            var precedence = GetBinaryOperatorPrecedence(Current.Kind);

            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            var operatorToken = NextToken();
            var right = ParseExpression(IsRightAssociativeBinaryOperator(operatorToken.Kind) ? precedence - 1 : precedence);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        ExpressionSyntax atom = Current.Kind switch
        {
            SyntaxKind.OpenParenToken => ParseParenthesizedExpression(),
            SyntaxKind.IntegerLiteralToken or SyntaxKind.LongLiteralToken or SyntaxKind.DoubleLiteralToken or SyntaxKind.StringLiteralToken or SyntaxKind.BooleanLiteralToken => new LiteralExpressionSyntax(NextToken()),
            SyntaxKind.InterpolatedStringStartToken => ParseInterpolatedStringExpression(),
            SyntaxKind.IdentifierToken
                or SyntaxKind.SelfKeyword
                or SyntaxKind.LineKeyword
                or SyntaxKind.IntegerKeyword
                or SyntaxKind.LongKeyword
                or SyntaxKind.DoubleKeyword
                or SyntaxKind.DecimalKeyword
                or SyntaxKind.StringKeyword
                or SyntaxKind.BooleanKeyword
                or SyntaxKind.DateKeyword
                or SyntaxKind.TimeKeyword
                or SyntaxKind.DateTimeKeyword
                or SyntaxKind.UtcDateTimeKeyword => new NameExpressionSyntax(NextToken()),
            _ => ParseMissingExpression()
        };

        return ParsePostfixExpression(atom);
    }

    private ParenthesizedExpressionSyntax ParseParenthesizedExpression()
    {
        var openParenToken = Match(SyntaxKind.OpenParenToken, "(");
        var expression = ParseExpression();
        var closeParenToken = Match(SyntaxKind.CloseParenToken, ")");
        return new ParenthesizedExpressionSyntax(openParenToken, expression, closeParenToken);
    }

    private InterpolatedStringExpressionSyntax ParseInterpolatedStringExpression()
    {
        var startToken = Match(SyntaxKind.InterpolatedStringStartToken, "$\"");
        var parts = new List<InterpolatedStringPartSyntax>();

        while (Current.Kind is not (SyntaxKind.InterpolatedStringEndToken or SyntaxKind.EndOfFileToken))
        {
            if (Current.Kind == SyntaxKind.InterpolatedStringTextToken)
            {
                var textToken = NextToken();
                parts.Add(new InterpolatedStringTextPartSyntax(textToken));
            }
            else if (Current.Kind == SyntaxKind.InterpolatedStringExpressionStartToken)
            {
                var openBrace = Match(SyntaxKind.InterpolatedStringExpressionStartToken, "{");
                var expression = ParseExpression();

                SyntaxToken? commaToken = null;
                SyntaxToken? alignmentToken = null;
                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    commaToken = NextToken();

                    if (Current.Kind == SyntaxKind.MinusToken)
                    {
                        var minusToken = NextToken();
                        var numberToken = Match(SyntaxKind.IntegerLiteralToken, "выравнивание");
                        var negativeValue = -((int)(numberToken.Value ?? 0));
                        alignmentToken = new SyntaxToken(
                            SyntaxKind.IntegerLiteralToken,
                            $"-{numberToken.Text}",
                            numberToken.Location,
                            negativeValue);
                    }
                    else
                    {
                        alignmentToken = Match(SyntaxKind.IntegerLiteralToken, "выравнивание");
                    }
                }

                SyntaxToken? colonToken = null;
                SyntaxToken? formatSpecifierToken = null;
                if (Current.Kind == SyntaxKind.ColonToken)
                {
                    colonToken = NextToken();
                    formatSpecifierToken = Match(SyntaxKind.InterpolatedStringFormatSpecifierToken, "спецификатор формата");
                }

                var closeBrace = Match(SyntaxKind.InterpolatedStringExpressionEndToken, "}");
                parts.Add(new InterpolatedStringExpressionPartSyntax(openBrace, expression, commaToken, alignmentToken, colonToken, formatSpecifierToken, closeBrace));
            }
            else
            {
                break;
            }
        }

        var endToken = Match(SyntaxKind.InterpolatedStringEndToken, "\"");
        return new InterpolatedStringExpressionSyntax(startToken, parts, endToken);
    }

    private ExpressionSyntax ParsePostfixExpression(ExpressionSyntax target)
    {
        var expression = target;

        while (Current.Kind is SyntaxKind.OpenParenToken or SyntaxKind.OpenBracketToken or SyntaxKind.DotToken)
        {
            if (Current.Kind == SyntaxKind.OpenBracketToken)
            {
                if (_fixedStringLengthDepth > 0 && IsArrayTypeSuffix())
                {
                    break;
                }

                expression = ParseArrayAccessExpression(expression);
                continue;
            }

            if (Current.Kind == SyntaxKind.OpenParenToken)
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

            var dotToken = Match(SyntaxKind.DotToken, ".");
            var identifier = MatchFieldIdentifier();
            expression = new FieldAccessExpressionSyntax(expression, dotToken, identifier);
        }

        return expression;
    }

    private ErrorExpressionSyntax ParseMissingExpression()
    {
        ReportExpectedExpression();

        if (CanConsumeUnexpectedExpressionToken(Current.Kind))
        {
            return new ErrorExpressionSyntax(NextToken());
        }

        return new ErrorExpressionSyntax(CreateMissingToken(SyntaxKind.BadToken));
    }

    private void ParseSeparatedExpressions(
        out IReadOnlyList<ExpressionSyntax> expressions,
        out IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxKind closingKind,
        string closingDisplayName,
        out SyntaxToken closingToken)
    {
        var items = new List<ExpressionSyntax>();
        var commas = new List<SyntaxToken>();

        if (Current.Kind != closingKind)
        {
            while (true)
            {
                var expression = ParseCallArgumentExpression();

                items.Add(expression);

                if (Current.Kind != SyntaxKind.CommaToken)
                {
                    break;
                }

                commas.Add(NextToken());
            }
        }

        closingToken = Match(closingKind, closingDisplayName);
        expressions = items;
        commaTokens = commas;
    }

    private ExpressionSyntax ParseCallArgumentExpression()
    {
        if (IsExpressionNameToken(Current.Kind) && Peek(1).Kind == SyntaxKind.ColonEqualsToken)
        {
            var identifierToken = NextToken();
            var colonEqualsToken = NextToken();
            return new NamedArgumentExpressionSyntax(identifierToken, colonEqualsToken, ParseArgumentValueExpression());
        }

        // STEP в списке аргументов — направление сортировки. Форма разбирается
        // для любого вызова, а разрешает её связыватель только там, где она
        // осмысленна: как и диапазон TO, STEP вне FOR и графики не является
        // частью общего выражения.
        if (Current.Kind == SyntaxKind.StepKeyword)
        {
            var stepKeyword = NextToken();
            return new StepDirectionArgumentExpressionSyntax(stepKeyword, ParseExpression());
        }

        return ParseArgumentValueExpression();
    }

    private ExpressionSyntax ParseArgumentValueExpression()
    {
        var expression = ParseExpression();

        if (Current.Kind == SyntaxKind.ToKeyword)
        {
            var toKeyword = NextToken();
            var upperBound = ParseExpression();
            expression = new RangeExpressionSyntax(expression, toKeyword, upperBound);
        }

        return expression;
    }

    private static int GetUnaryOperatorPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.MinusToken => 10,
            SyntaxKind.NotKeyword => 10,
            _ => 0
        };
    }

    private static int GetBinaryOperatorPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.CaretToken => 11,
            SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.BackslashToken or SyntaxKind.ModKeyword => 9,
            SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.AmpersandToken => 8,
            SyntaxKind.LessLessToken or SyntaxKind.GreaterGreaterToken => 7,
            SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or SyntaxKind.LessToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterToken or SyntaxKind.GreaterOrEqualsToken => 6,
            SyntaxKind.AndKeyword or SyntaxKind.AndAlsoKeyword => 5,
            SyntaxKind.XorKeyword => 4,
            SyntaxKind.OrKeyword or SyntaxKind.OrElseKeyword => 3,
            SyntaxKind.EqvKeyword => 2,
            SyntaxKind.ImpKeyword => 1,
            _ => 0
        };
    }

    private static bool IsRightAssociativeBinaryOperator(SyntaxKind kind)
    {
        return kind == SyntaxKind.CaretToken;
    }
}
