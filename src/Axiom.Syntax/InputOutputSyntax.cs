namespace Axiom.Syntax;

public sealed class PrintItemSyntax : SyntaxNode
{
    public PrintItemSyntax(ExpressionSyntax expression, SyntaxToken? separatorToken)
    {
        ArgumentNullException.ThrowIfNull(expression);

        Expression = expression;
        SeparatorToken = separatorToken;
    }

    public override SyntaxKind Kind => SyntaxKind.PrintItem;

    public ExpressionSyntax Expression { get; }

    public SyntaxToken? SeparatorToken { get; }
}

public sealed class PrintStatementSyntax : StatementSyntax
{
    public PrintStatementSyntax(SyntaxToken printKeyword, IReadOnlyList<PrintItemSyntax> items)
    {
        ArgumentNullException.ThrowIfNull(printKeyword);
        ArgumentNullException.ThrowIfNull(items);

        PrintKeyword = printKeyword;
        Items = items;
    }

    public override SyntaxKind Kind => SyntaxKind.PrintStatement;

    public SyntaxToken PrintKeyword { get; }

    public IReadOnlyList<PrintItemSyntax> Items { get; }
}

public sealed class PrintUsingStatementSyntax : StatementSyntax
{
    public PrintUsingStatementSyntax(
        SyntaxToken printKeyword,
        SyntaxToken usingKeyword,
        ExpressionSyntax formatExpression,
        SyntaxToken formatSeparatorToken,
        IReadOnlyList<PrintItemSyntax> items)
    {
        ArgumentNullException.ThrowIfNull(printKeyword);
        ArgumentNullException.ThrowIfNull(usingKeyword);
        ArgumentNullException.ThrowIfNull(formatExpression);
        ArgumentNullException.ThrowIfNull(formatSeparatorToken);
        ArgumentNullException.ThrowIfNull(items);

        PrintKeyword = printKeyword;
        UsingKeyword = usingKeyword;
        FormatExpression = formatExpression;
        FormatSeparatorToken = formatSeparatorToken;
        Items = items;
    }

    public override SyntaxKind Kind => SyntaxKind.PrintUsingStatement;

    public SyntaxToken PrintKeyword { get; }

    public SyntaxToken UsingKeyword { get; }

    public ExpressionSyntax FormatExpression { get; }

    public SyntaxToken FormatSeparatorToken { get; }

    public IReadOnlyList<PrintItemSyntax> Items { get; }
}

public sealed class InputStatementSyntax : StatementSyntax
{
    public InputStatementSyntax(
        SyntaxToken inputKeyword,
        SyntaxToken? promptToken,
        SyntaxToken? promptSeparatorToken,
        IReadOnlyList<SyntaxToken> identifiers,
        IReadOnlyList<SyntaxToken> commaTokens)
    {
        ArgumentNullException.ThrowIfNull(inputKeyword);
        ArgumentNullException.ThrowIfNull(identifiers);
        ArgumentNullException.ThrowIfNull(commaTokens);

        InputKeyword = inputKeyword;
        PromptToken = promptToken;
        PromptSeparatorToken = promptSeparatorToken;
        Identifiers = identifiers;
        CommaTokens = commaTokens;
    }

    public override SyntaxKind Kind => SyntaxKind.InputStatement;

    public SyntaxToken InputKeyword { get; }

    public SyntaxToken? PromptToken { get; }

    public SyntaxToken? PromptSeparatorToken { get; }

    public IReadOnlyList<SyntaxToken> Identifiers { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }
}

public sealed class OpenStatementSyntax : StatementSyntax
{
    public OpenStatementSyntax(
        SyntaxToken openKeyword,
        ExpressionSyntax pathExpression,
        SyntaxToken forKeyword,
        SyntaxToken modeKeyword,
        SyntaxToken asKeyword,
        SyntaxToken fileIdentifier)
    {
        ArgumentNullException.ThrowIfNull(openKeyword);
        ArgumentNullException.ThrowIfNull(pathExpression);
        ArgumentNullException.ThrowIfNull(forKeyword);
        ArgumentNullException.ThrowIfNull(modeKeyword);
        ArgumentNullException.ThrowIfNull(asKeyword);
        ArgumentNullException.ThrowIfNull(fileIdentifier);

        OpenKeyword = openKeyword;
        PathExpression = pathExpression;
        ForKeyword = forKeyword;
        ModeKeyword = modeKeyword;
        AsKeyword = asKeyword;
        FileIdentifier = fileIdentifier;
    }

    public override SyntaxKind Kind => SyntaxKind.OpenStatement;

    public SyntaxToken OpenKeyword { get; }

    public ExpressionSyntax PathExpression { get; }

    public SyntaxToken ForKeyword { get; }

    public SyntaxToken ModeKeyword { get; }

    public SyntaxToken AsKeyword { get; }

    public SyntaxToken FileIdentifier { get; }
}

public sealed class CloseStatementSyntax : StatementSyntax
{
    public CloseStatementSyntax(SyntaxToken closeKeyword, SyntaxToken fileIdentifier)
    {
        ArgumentNullException.ThrowIfNull(closeKeyword);
        ArgumentNullException.ThrowIfNull(fileIdentifier);

        CloseKeyword = closeKeyword;
        FileIdentifier = fileIdentifier;
    }

    public override SyntaxKind Kind => SyntaxKind.CloseStatement;

    public SyntaxToken CloseKeyword { get; }

    public SyntaxToken FileIdentifier { get; }
}
