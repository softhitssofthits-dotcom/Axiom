using System.Text;
using Axiom.Ide.Editor.Highlighting;

namespace Axiom.Ide.Editor.Formatting;

public sealed class AxiomCodeFormattingService : ICodeFormattingService
{
    public string FormatDocument(string sourceText, CodeFormattingStyle style)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        if (sourceText.Length == 0)
        {
            return sourceText;
        }

        var builder = new StringBuilder(sourceText.Length);
        var lineStart = 0;

        while (lineStart < sourceText.Length)
        {
            var lineEnd = lineStart;
            while (lineEnd < sourceText.Length
                   && sourceText[lineEnd] != '\r'
                   && sourceText[lineEnd] != '\n')
            {
                lineEnd++;
            }

            builder.Append(FormatLineCore(sourceText[lineStart..lineEnd], style));

            if (lineEnd >= sourceText.Length)
            {
                break;
            }

            if (sourceText[lineEnd] == '\r'
                && lineEnd + 1 < sourceText.Length
                && sourceText[lineEnd + 1] == '\n')
            {
                builder.Append("\r\n");
                lineStart = lineEnd + 2;
                continue;
            }

            builder.Append(sourceText[lineEnd]);
            lineStart = lineEnd + 1;
        }

        return builder.ToString();
    }

    public string FormatLine(string lineText, CodeFormattingStyle style)
    {
        ArgumentNullException.ThrowIfNull(lineText);

        return FormatLineCore(lineText, style);
    }

    private static string FormatLineCore(string lineText, CodeFormattingStyle style)
    {
        if (lineText.Length == 0)
        {
            return lineText;
        }

        var indentLength = 0;
        while (indentLength < lineText.Length && char.IsWhiteSpace(lineText[indentLength]))
        {
            indentLength++;
        }

        var indentation = lineText[..indentLength];
        var body = lineText[indentLength..];

        if (body.Length == 0)
        {
            return lineText;
        }

        SplitComment(body, out var codePart, out var commentPart);

        if (!TryTokenize(codePart, out var rawTokens))
        {
            return lineText;
        }

        var normalizedTokens = NormalizeCompoundShiftOperators(rawTokens);
        var formattedCode = FormatTokens(normalizedTokens, style);

        if (formattedCode is null)
        {
            return lineText;
        }

        var normalizedCommentPart = NormalizeCommentPart(commentPart, style);

        if (normalizedCommentPart.Length == 0)
        {
            return indentation + formattedCode;
        }

        if (formattedCode.Length == 0)
        {
            return indentation + normalizedCommentPart;
        }

        return indentation + formattedCode + " " + normalizedCommentPart;
    }

    private static string? FormatTokens(IReadOnlyList<RawToken> rawTokens, CodeFormattingStyle style)
    {
        if (rawTokens.Count == 0)
        {
            return string.Empty;
        }

        var tokens = ClassifyTokens(rawTokens, style);
        var builder = new StringBuilder();

        for (var index = 0; index < tokens.Count; index++)
        {
            var current = tokens[index];
            var previous = index > 0 ? tokens[index - 1] : null;

            if (previous is not null && NeedsSpace(previous, current))
            {
                builder.Append(' ');
            }

            builder.Append(current.Text);
        }

        return builder.ToString();
    }

    private static List<FormattedToken> ClassifyTokens(IReadOnlyList<RawToken> rawTokens, CodeFormattingStyle style)
    {
        var result = new List<FormattedToken>(rawTokens.Count);

        for (var index = 0; index < rawTokens.Count; index++)
        {
            var rawToken = rawTokens[index];
            var previous = result.Count > 0 ? result[^1] : null;
            var nextRaw = index + 1 < rawTokens.Count ? rawTokens[index + 1] : null;

            result.Add(rawToken.Kind switch
            {
                RawTokenKind.Word => ClassifyWordToken(rawToken.Text, nextRaw, style),
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

    private static FormattedToken ClassifyWordToken(string text, RawToken? nextRaw, CodeFormattingStyle style)
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

    private static bool IsPrefixOperator(string text, FormattedToken? previous)
    {
        if (!text.Equals("-", StringComparison.Ordinal) && !text.Equals("+", StringComparison.Ordinal))
        {
            return false;
        }

        return previous is null || !EndsExpression(previous);
    }

    private static bool NeedsSpace(FormattedToken previous, FormattedToken current)
    {
        if (current.Kind is FormattedTokenKind.CloseParen or FormattedTokenKind.Comma or FormattedTokenKind.Semicolon)
        {
            return false;
        }

        if (current.Kind == FormattedTokenKind.Dot || previous.Kind == FormattedTokenKind.Dot)
        {
            return false;
        }

        if (previous.Kind == FormattedTokenKind.OpenParen)
        {
            return false;
        }

        if (previous.Kind == FormattedTokenKind.PrefixOperator)
        {
            return previous.Text.Equals("NOT", StringComparison.OrdinalIgnoreCase);
        }

        if (previous.Kind is FormattedTokenKind.Comma or FormattedTokenKind.Semicolon)
        {
            return true;
        }

        if (current.Kind == FormattedTokenKind.OpenParen)
        {
            return previous.Kind is not FormattedTokenKind.PlainWord
                and not FormattedTokenKind.BuiltInWord
                and not FormattedTokenKind.CloseParen;
        }

        if (current.Kind == FormattedTokenKind.BinaryOperator || previous.Kind == FormattedTokenKind.BinaryOperator)
        {
            return true;
        }

        return IsWordLike(previous.Kind) && IsWordLike(current.Kind)
            || previous.Kind == FormattedTokenKind.CloseParen
            || current.Kind == FormattedTokenKind.ReservedWord;
    }

    private static bool EndsExpression(FormattedToken token) =>
        token.Kind is FormattedTokenKind.PlainWord
            or FormattedTokenKind.BuiltInWord
            or FormattedTokenKind.ReservedWord
            or FormattedTokenKind.Number
            or FormattedTokenKind.String
            or FormattedTokenKind.CloseParen;

    private static bool IsWordLike(FormattedTokenKind kind) =>
        kind is FormattedTokenKind.PlainWord
            or FormattedTokenKind.BuiltInWord
            or FormattedTokenKind.ReservedWord
            or FormattedTokenKind.Number
            or FormattedTokenKind.String;

    private static string NormalizeCommentPart(string commentPart, CodeFormattingStyle style)
    {
        var trimmedComment = commentPart.TrimStart();

        if (!trimmedComment.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
        {
            return trimmedComment;
        }

        return ApplyStyle("REM", style) + trimmedComment[3..];
    }

    private static string ApplyStyle(string text, CodeFormattingStyle style)
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

    private static void SplitComment(string lineText, out string codePart, out string commentPart)
    {
        var inString = false;

        for (var index = 0; index < lineText.Length; index++)
        {
            var current = lineText[index];

            if (current == '"')
            {
                if (inString && index + 1 < lineText.Length && lineText[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (current == '\'')
            {
                codePart = lineText[..index].TrimEnd();
                commentPart = lineText[index..];
                return;
            }

            if (!StartsRemComment(lineText, index))
            {
                continue;
            }

            codePart = lineText[..index].TrimEnd();
            commentPart = lineText[index..];
            return;
        }

        codePart = lineText.TrimEnd();
        commentPart = string.Empty;
    }

    private static bool StartsRemComment(string lineText, int index)
    {
        if (index + 2 >= lineText.Length)
        {
            return false;
        }

        if (!lineText.AsSpan(index, 3).Equals("REM", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var isWordStart = index == 0 || !IsIdentifierCharacter(lineText[index - 1]);
        var isWordEnd = index + 3 >= lineText.Length || !IsIdentifierCharacter(lineText[index + 3]);
        return isWordStart && isWordEnd;
    }

    private static List<RawToken> NormalizeCompoundShiftOperators(IReadOnlyList<RawToken> rawTokens)
    {
        if (rawTokens.Count < 2)
        {
            return rawTokens.ToList();
        }

        var normalizedTokens = new List<RawToken>(rawTokens.Count);

        for (var index = 0; index < rawTokens.Count; index++)
        {
            if (index + 1 < rawTokens.Count &&
                TryMergeShiftOperator(rawTokens[index], rawTokens[index + 1], out var mergedOperator))
            {
                normalizedTokens.Add(mergedOperator);
                index++;
                continue;
            }

            normalizedTokens.Add(rawTokens[index]);
        }

        return normalizedTokens;
    }

    private static bool TryMergeShiftOperator(RawToken first, RawToken second, out RawToken mergedOperator)
    {
        mergedOperator = new RawToken(string.Empty, RawTokenKind.Operator);

        if (first.Kind != RawTokenKind.Operator ||
            second.Kind != RawTokenKind.Operator ||
            first.Text.Length != 1 ||
            !string.Equals(first.Text, second.Text, StringComparison.Ordinal) ||
            (first.Text != "<" && first.Text != ">"))
        {
            return false;
        }

        mergedOperator = new RawToken(first.Text + second.Text, RawTokenKind.Operator);
        return true;
    }

    private static bool TryTokenize(string text, out List<RawToken> tokens)
    {
        tokens = [];

        for (var index = 0; index < text.Length;)
        {
            var current = text[index];

            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (IsIdentifierStart(current))
            {
                var start = index++;
                while (index < text.Length && IsIdentifierCharacter(text[index]))
                {
                    index++;
                }

                tokens.Add(new RawToken(text[start..index], RawTokenKind.Word));
                continue;
            }

            if (char.IsDigit(current))
            {
                var start = index++;
                while (index < text.Length && char.IsDigit(text[index]))
                {
                    index++;
                }

                if (index < text.Length && text[index] == '.')
                {
                    index++;
                    while (index < text.Length && char.IsDigit(text[index]))
                    {
                        index++;
                    }
                }

                tokens.Add(new RawToken(text[start..index], RawTokenKind.Number));
                continue;
            }

            if (current == '"')
            {
                var start = index++;

                while (index < text.Length)
                {
                    if (text[index] != '"')
                    {
                        index++;
                        continue;
                    }

                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        index += 2;
                        continue;
                    }

                    index++;
                    tokens.Add(new RawToken(text[start..index], RawTokenKind.String));
                    goto ContinueOuterLoop;
                }

                return false;
            }

            if (index + 1 < text.Length)
            {
                var pair = text.Substring(index, 2);
                if (pair is "<>" or "<=" or ">=" or "<<" or ">>")
                {
                    tokens.Add(new RawToken(pair, RawTokenKind.Operator));
                    index += 2;
                    continue;
                }
            }

            if (current is '+' or '-' or '*' or '/' or '\\' or '^' or '=' or '<' or '>')
            {
                tokens.Add(new RawToken(current.ToString(), RawTokenKind.Operator));
                index++;
                continue;
            }

            if (current == '(')
            {
                tokens.Add(new RawToken("(", RawTokenKind.OpenParen));
                index++;
                continue;
            }

            if (current == ')')
            {
                tokens.Add(new RawToken(")", RawTokenKind.CloseParen));
                index++;
                continue;
            }

            if (current == ',')
            {
                tokens.Add(new RawToken(",", RawTokenKind.Comma));
                index++;
                continue;
            }

            if (current == ';')
            {
                tokens.Add(new RawToken(";", RawTokenKind.Semicolon));
                index++;
                continue;
            }

            if (current == '.')
            {
                tokens.Add(new RawToken(".", RawTokenKind.Dot));
                index++;
                continue;
            }

            return false;

        ContinueOuterLoop:
            continue;
        }

        return true;
    }

    private static bool IsIdentifierStart(char character) =>
        char.IsLetter(character) || character == '_';

    private static bool IsIdentifierCharacter(char character) =>
        char.IsLetterOrDigit(character) || character == '_';

    private sealed record RawToken(string Text, RawTokenKind Kind);

    private sealed record FormattedToken(string Text, FormattedTokenKind Kind);

    private enum RawTokenKind
    {
        Word,
        Number,
        String,
        Operator,
        OpenParen,
        CloseParen,
        Comma,
        Semicolon,
        Dot
    }

    private enum FormattedTokenKind
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
        Comma,
        Semicolon,
        Dot
    }
}
