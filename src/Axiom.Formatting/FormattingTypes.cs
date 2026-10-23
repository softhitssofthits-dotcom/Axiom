namespace Axiom.Formatting;

internal sealed record RawToken(string Text, RawTokenKind Kind);

internal sealed record FormattedToken(string Text, FormattedTokenKind Kind);

internal enum RawTokenKind
{
    Word,
    Number,
    String,
    Operator,
    OpenParen,
    CloseParen,
    OpenBracket,
    CloseBracket,
    Comma,
    Semicolon,
    Dot,
    PlainText
}

internal enum FormattedTokenKind
{
    PlainWord,
    BuiltInWord,
    ReservedWord,
    Number,
    String,
    BinaryOperator,
    PrefixOperator,
    OpenParen,
    CloseParen,
    OpenBracket,
    CloseBracket,
    Comma,
    Semicolon,
    Dot,
    PlainText
}
