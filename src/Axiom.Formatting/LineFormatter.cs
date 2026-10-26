using System.Text;
using Axiom.Lexing;

namespace Axiom.Formatting;

internal static class LineFormatter
{
    public static string Format(string lineText, CodeFormattingStyle style)
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

        SplitComment(body, out var codePart, out var commentPart, out var commentGap);

        if (!TryTokenize(codePart, out var rawTokens, style))
        {
            return lineText;
        }

        var normalizedTokens = NormalizeCompoundShiftOperators(rawTokens);
        normalizedTokens = NormalizeContextualForEachKeywords(normalizedTokens, style);
        var formattedCode = AssembleTokens(normalizedTokens, style);

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

        var effectiveGap = commentGap.Length > 0 ? commentGap : " ";
        return indentation + formattedCode + effectiveGap + normalizedCommentPart;
    }

    public static bool IsSyntaxValid(string lineText)
    {
        if (lineText.Length == 0)
        {
            return true;
        }

        var indentLength = 0;
        while (indentLength < lineText.Length && char.IsWhiteSpace(lineText[indentLength]))
        {
            indentLength++;
        }

        var body = lineText[indentLength..];
        if (body.Length == 0)
        {
            return true;
        }

        SplitComment(body, out var codePart, out _, out _);
        return codePart.Length == 0 || TryTokenize(codePart, out _, CodeFormattingStyle.Classic);
    }

    private static string? AssembleTokens(IReadOnlyList<RawToken> rawTokens, CodeFormattingStyle style)
    {
        if (rawTokens.Count == 0)
        {
            return string.Empty;
        }

        var tokens = TokenClassifier.Classify(rawTokens, style);
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

    private static bool NeedsSpace(FormattedToken previous, FormattedToken current)
    {
        if (current.Kind is FormattedTokenKind.CloseParen
            or FormattedTokenKind.CloseBracket
            or FormattedTokenKind.Comma
            or FormattedTokenKind.Semicolon
            or FormattedTokenKind.PlainText)
        {
            return false;
        }

        if (previous.Kind is FormattedTokenKind.PlainText)
        {
            return false;
        }

        if (current.Kind == FormattedTokenKind.Dot || previous.Kind == FormattedTokenKind.Dot)
        {
            return false;
        }

        if (previous.Kind is FormattedTokenKind.OpenParen or FormattedTokenKind.OpenBracket)
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

        // Квадратная скобка примыкает к имени, типу, результату вызова и
        // закрывающей скобке — так записывается суффикс массива и обращение.
        // В начале выражения она открывает литерал и отделяется как операнд.
        if (current.Kind == FormattedTokenKind.OpenBracket)
        {
            return previous.Kind is not FormattedTokenKind.PlainWord
                and not FormattedTokenKind.BuiltInWord
                and not FormattedTokenKind.CloseParen
                and not FormattedTokenKind.CloseBracket
                && !IsTypeName(previous);
        }

        if (current.Kind == FormattedTokenKind.OpenParen)
        {
            // DICTIONARY(ключ, значение) — тип с аргументами: скобка примыкает
            // к имени типа, как и у FUNCTION(...) AS тип.
            return previous.Kind is not FormattedTokenKind.PlainWord
                and not FormattedTokenKind.BuiltInWord
                and not FormattedTokenKind.CloseParen
                and not FormattedTokenKind.CloseBracket
                && !IsTypeName(previous);
        }

        if (current.Kind == FormattedTokenKind.BinaryOperator || previous.Kind == FormattedTokenKind.BinaryOperator)
        {
            return true;
        }

        return IsWordLike(previous.Kind) && (IsWordLike(current.Kind) || current.Kind == FormattedTokenKind.PrefixOperator)
            || previous.Kind is FormattedTokenKind.CloseParen or FormattedTokenKind.CloseBracket
            || current.Kind == FormattedTokenKind.ReservedWord;
    }

    private static bool IsWordLike(FormattedTokenKind kind) =>        kind is FormattedTokenKind.PlainWord
            or FormattedTokenKind.BuiltInWord
            or FormattedTokenKind.ReservedWord
            or FormattedTokenKind.Number
            or FormattedTokenKind.String;

    // Тип перед суффиксом ранга — часть типа результата (STRING[]), а не
    // отдельный операнд, поэтому скобка примыкает к нему.
    private static bool IsTypeName(FormattedToken previous) =>
        previous.Kind == FormattedTokenKind.ReservedWord
        && AxiomFormattingVocabulary.TypeNames.Contains(previous.Text.ToUpperInvariant());

    private static string NormalizeCommentPart(string commentPart, CodeFormattingStyle style)
    {
        var trimmedComment = commentPart.TrimStart();

        if (!trimmedComment.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
        {
            return trimmedComment;
        }

        return TokenClassifier.ApplyStyle("REM", style) + trimmedComment[3..];
    }

    private static void SplitComment(string lineText, out string codePart, out string commentPart, out string gap)
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
                var codeEnd = index;
                while (codeEnd > 0 && char.IsWhiteSpace(lineText[codeEnd - 1]))
                {
                    codeEnd--;
                }

                codePart = lineText[..codeEnd];
                gap = lineText[codeEnd..index];
                commentPart = lineText[index..];
                return;
            }

            if (!StartsRemComment(lineText, index))
            {
                continue;
            }

            var remCodeEnd = index;
            while (remCodeEnd > 0 && char.IsWhiteSpace(lineText[remCodeEnd - 1]))
            {
                remCodeEnd--;
            }

            codePart = lineText[..remCodeEnd];
            gap = lineText[remCodeEnd..index];
            commentPart = lineText[index..];
            return;
        }

        codePart = lineText.TrimEnd();
        commentPart = string.Empty;
        gap = string.Empty;
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

    private static bool IsIdentifierCharacter(char character) =>
        char.IsLetterOrDigit(character) || character == '_';

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

    // EACH и IN — контекстные слова заголовка FOR EACH, а не ключевые слова
    // языка. Форматтер приводит их к стилю только внутри заголовка; вне его
    // они остаются обычными именами и не меняют регистр.
    private static List<RawToken> NormalizeContextualForEachKeywords(
        IReadOnlyList<RawToken> rawTokens,
        CodeFormattingStyle style)
    {
        if (rawTokens.Count < 2
            || rawTokens[0].Kind != RawTokenKind.Word
            || !rawTokens[0].Text.Equals("FOR", StringComparison.OrdinalIgnoreCase))
        {
            return rawTokens.ToList();
        }

        var eachIndex = -1;
        for (var index = 1; index < rawTokens.Count; index++)
        {
            if (rawTokens[index].Kind != RawTokenKind.Word)
            {
                continue;
            }

            if (rawTokens[index].Text.Equals("EACH", StringComparison.OrdinalIgnoreCase))
            {
                eachIndex = index;
            }

            break;
        }

        if (eachIndex < 0)
        {
            return rawTokens.ToList();
        }

        var tokens = rawTokens.ToList();
        tokens[eachIndex] = new RawToken(
            TokenClassifier.ApplyStyle("EACH", style),
            RawTokenKind.Word);

        for (var index = eachIndex + 1; index < tokens.Count; index++)
        {
            if (tokens[index].Kind == RawTokenKind.Word
                && tokens[index].Text.Equals("IN", StringComparison.OrdinalIgnoreCase))
            {
                tokens[index] = new RawToken(
                    TokenClassifier.ApplyStyle("IN", style),
                    RawTokenKind.Word);
                break;
            }
        }

        return tokens;
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

    private static bool TryTokenize(string text, out List<RawToken> tokens, CodeFormattingStyle style)
    {
        tokens = [];

        if (text.Length == 0)
        {
            return true;
        }

        var lexerTokens = Lexer.Lex(text, out var diagnostics);

        if (diagnostics.Count > 0)
        {
            return false;
        }

        var insideInterpolation = false;
        var insideExpression = false;
        var expressionTokens = new List<RawToken>();
        var interpolationText = new System.Text.StringBuilder();
        var lastTextOffset = -1;
        var interpolationFrames = new Stack<(bool InsideExpression, int LastTextOffset,
            List<RawToken> ExpressionTokens, StringBuilder Text)>();
        var delimiters = new Stack<TokenKind>();
        var validateDelimiters = lexerTokens.Any(static token => token.Kind is TokenKind.OpenBracketToken or TokenKind.CloseBracketToken);

        foreach (var token in lexerTokens)
        {
            if (validateDelimiters && token.Kind is (TokenKind.OpenParenToken or TokenKind.OpenBracketToken))
            {
                delimiters.Push(token.Kind);
            }
            else if (validateDelimiters && token.Kind is (TokenKind.CloseParenToken or TokenKind.CloseBracketToken))
            {
                var expectedOpen = token.Kind == TokenKind.CloseParenToken
                    ? TokenKind.OpenParenToken
                    : TokenKind.OpenBracketToken;
                if (delimiters.Count == 0 || delimiters.Pop() != expectedOpen)
                {
                    return false;
                }
            }

            if (token.Kind is TokenKind.EndOfFileToken or TokenKind.NewLineToken or TokenKind.CommentToken)
            {
                continue;
            }

            if (token.Kind == TokenKind.BadToken)
            {
                return false;
            }

            if (token.Kind == TokenKind.InterpolatedStringStartToken)
            {
                if (insideInterpolation)
                {
                    interpolationFrames.Push((insideExpression, lastTextOffset, expressionTokens, interpolationText));
                }
                insideInterpolation = true;
                insideExpression = false;
                lastTextOffset = token.Span.End;
                expressionTokens = [];
                interpolationText = new StringBuilder("$\"");
                continue;
            }

            if (insideInterpolation)
            {
                if (token.Kind == TokenKind.InterpolatedStringExpressionStartToken)
                {
                    var textBefore = text[lastTextOffset..token.Span.Start];
                    interpolationText.Append(textBefore);
                    interpolationText.Append('{');
                    insideExpression = true;
                    expressionTokens.Clear();
                    continue;
                }

                if (insideExpression)
                {
                    if (token.Kind == TokenKind.InterpolatedStringExpressionEndToken)
                    {
                        var formattedExpr = FormatExpressionTokens(expressionTokens, style);
                        interpolationText.Append(formattedExpr);
                        interpolationText.Append('}');
                        insideExpression = false;
                        lastTextOffset = token.Span.End;
                        continue;
                    }

                    if (token.Kind is TokenKind.ColonToken or TokenKind.InterpolatedStringFormatSpecifierToken)
                    {
                        expressionTokens.Add(new RawToken(token.Text, RawTokenKind.PlainText));
                        continue;
                    }

                    var rawKind = MapTokenKind(token.Kind);
                    if (rawKind is not null)
                    {
                        expressionTokens.Add(new RawToken(token.Text, rawKind.Value));
                    }
                    continue;
                }

                if (token.Kind == TokenKind.InterpolatedStringEndToken)
                {
                    var textAfter = text[lastTextOffset..token.Span.Start];
                    interpolationText.Append(textAfter);
                    interpolationText.Append('"');
                    var formattedString = new RawToken(interpolationText.ToString(), RawTokenKind.String);
                    if (interpolationFrames.TryPop(out var frame))
                    {
                        insideExpression = frame.InsideExpression;
                        lastTextOffset = frame.LastTextOffset;
                        expressionTokens = frame.ExpressionTokens;
                        interpolationText = frame.Text;
                        expressionTokens.Add(formattedString);
                    }
                    else
                    {
                        tokens.Add(formattedString);
                        insideInterpolation = false;
                    }
                    continue;
                }

                continue;
            }

            var kind = MapTokenKind(token.Kind);
            if (kind is not null)
            {
                tokens.Add(new RawToken(token.Text, kind.Value));
            }
        }

        return delimiters.Count == 0 && !insideInterpolation && !insideExpression;
    }

    private static string FormatExpressionTokens(List<RawToken> expressionTokens, CodeFormattingStyle style)
    {
        if (expressionTokens.Count == 0)
        {
            return string.Empty;
        }

        var normalized = NormalizeCompoundShiftOperators(expressionTokens);
        var formatted = AssembleTokens(normalized, style);
        return formatted ?? string.Empty;
    }

    private static RawTokenKind? MapTokenKind(TokenKind kind)
    {
        if (kind >= TokenKind.AndKeyword && kind <= TokenKind.XorKeyword)
        {
            return RawTokenKind.Word;
        }

        if (kind is TokenKind.IdentifierToken)
        {
            return RawTokenKind.Word;
        }

        if (kind is TokenKind.IntegerLiteralToken or TokenKind.LongLiteralToken or TokenKind.DoubleLiteralToken)
        {
            return RawTokenKind.Number;
        }

        if (kind is TokenKind.BooleanLiteralToken)
        {
            return RawTokenKind.Word;
        }

        if (kind is TokenKind.StringLiteralToken)
        {
            return RawTokenKind.String;
        }

        if (kind is TokenKind.OpenParenToken)
        {
            return RawTokenKind.OpenParen;
        }

        if (kind is TokenKind.CloseParenToken)
        {
            return RawTokenKind.CloseParen;
        }

        if (kind is TokenKind.OpenBracketToken)
        {
            return RawTokenKind.OpenBracket;
        }

        if (kind is TokenKind.CloseBracketToken)
        {
            return RawTokenKind.CloseBracket;
        }

        if (kind is TokenKind.CommaToken)
        {
            return RawTokenKind.Comma;
        }

        if (kind is TokenKind.SemicolonToken)
        {
            return RawTokenKind.Semicolon;
        }

        if (kind is TokenKind.DotToken)
        {
            return RawTokenKind.Dot;
        }

        if (kind is TokenKind.PlusToken or TokenKind.MinusToken or TokenKind.StarToken or
            TokenKind.SlashToken or TokenKind.BackslashToken or TokenKind.CaretToken or
            TokenKind.EqualsToken or TokenKind.ColonEqualsToken or TokenKind.LessToken or TokenKind.GreaterToken or
            TokenKind.LessOrEqualsToken or TokenKind.GreaterOrEqualsToken or TokenKind.NotEqualsToken or
            TokenKind.AmpersandToken)
        {
            return RawTokenKind.Operator;
        }

        if (kind is TokenKind.LessLessToken or TokenKind.GreaterGreaterToken)
        {
            return RawTokenKind.Operator;
        }

        return null;
    }
}
