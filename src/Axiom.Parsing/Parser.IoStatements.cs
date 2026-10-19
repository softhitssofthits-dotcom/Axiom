using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
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

    private SleepStatementSyntax ParseSleepStatement()
    {
        var sleepKeyword = Match(SyntaxKind.SleepKeyword, "SLEEP");
        var durationExpression = IsStatementTerminator(Current.Kind) ? null : ParseExpression();
        return new SleepStatementSyntax(sleepKeyword, durationExpression);
    }

    private RandomizeStatementSyntax ParseRandomizeStatement()
    {
        var randomizeKeyword = Match(SyntaxKind.RandomizeKeyword, "RANDOMIZE");
        var seedExpression = IsStatementTerminator(Current.Kind) ? null : ParseExpression();
        return new RandomizeStatementSyntax(randomizeKeyword, seedExpression);
    }

}
