using Axiom.Ide.Editor.Highlighting;

namespace Axiom.Ide.Editor.Formatting;

internal static class TokenClassifier
{
    public static List<FormattedToken> Classify(IReadOnlyList<RawToken> rawTokens, CodeFormattingStyle style)
    {
        var result = new List<FormattedToken>(rawTokens.Count);

        for (var index = 0; index < rawTokens.Count; index++)
        {
            var rawToken = rawTokens[index];
            var previous = result.Count > 0 ? result[^1] : null;
            var nextRaw = index + 1 < rawTokens.Count ? rawTokens[index + 1] : null;

            result.Add(rawToken.Kind switch
            {
                RawTokenKind.Word => ClassifyWord(rawToken.Text, nextRaw, style),
                RawTokenKind.Number => new FormattedToken(rawToken.Text, FormattedTokenKind.Number),
                RawTokenKind.String => new FormattedToken(rawToken.Text, FormattedTokenKind.String),
                RawTokenKind.OpenParen => new FormattedToken(rawToken.Text, FormattedTokenKind.OpenParen),
                RawTokenKind.CloseParen => new FormattedToken(rawToken.Text, FormattedTokenKind.CloseParen),
                RawTokenKind.Comma => new FormattedToken(rawToken.Text, FormattedTokenKind.Comma),
                RawTokenKind.Semicolon => new FormattedToken(rawToken.Text, FormattedTokenKind.Semicolon),
                RawTokenKind.Dot => new FormattedToken(rawToken.Text, FormattedTokenKind.Dot),
                RawTokenKind.Operator when IsPrefixOperator(rawToken.Text, previous)
                    => new FormattedToken(rawToken.Text, FormattedTokenKind.PrefixOperator),
                RawTokenKind.Operator => new FormattedToken(rawToken.Text, FormattedTokenKind.BinaryOperator),
                _ => new FormattedToken(rawToken.Text, FormattedTokenKind.PlainWord)
            });
        }

        return result;
    }

    private static FormattedToken ClassifyWord(string text, RawToken? nextRaw, CodeFormattingStyle style)
    {
        if (AxiomHighlightingVocabulary.Keywords.Contains(text))
        {
            return new FormattedToken(ApplyStyle(text, style), FormattedTokenKind.ReservedWord);
        }

        if (AxiomHighlightingVocabulary.TypeNames.Contains(text))
        {
            return new FormattedToken(ApplyStyle(text, style), FormattedTokenKind.ReservedWord);
        }

        if (AxiomHighlightingVocabulary.BuiltIns.Contains(text) && nextRaw?.Kind == RawTokenKind.OpenParen)
        {
            return new FormattedToken(ApplyStyle(text, style), FormattedTokenKind.BuiltInWord);
        }

        return new FormattedToken(text, FormattedTokenKind.PlainWord);
    }

    public static bool IsPrefixOperator(string text, FormattedToken? previous)
    {
        if (!text.Equals("-", StringComparison.Ordinal) && !text.Equals("+", StringComparison.Ordinal))
        {
            return false;
        }

        return previous is null || !EndsExpression(previous);
    }

    public static bool EndsExpression(FormattedToken token) =>
        token.Kind is FormattedTokenKind.PlainWord
            or FormattedTokenKind.BuiltInWord
            or FormattedTokenKind.ReservedWord
            or FormattedTokenKind.Number
            or FormattedTokenKind.String
            or FormattedTokenKind.CloseParen;

    public static string ApplyStyle(string text, CodeFormattingStyle style)
    {
        return style switch
        {
            CodeFormattingStyle.Classic => text.ToUpperInvariant(),
            CodeFormattingStyle.Modern => ToModernCase(text),
            _ => text
        };
    }

    private static string ToModernCase(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        if (text.Length == 1)
        {
            return char.ToUpperInvariant(text[0]).ToString();
        }

        return char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();
    }
}
