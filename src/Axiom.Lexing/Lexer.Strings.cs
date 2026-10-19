using Axiom.Core;

namespace Axiom.Lexing;

public sealed partial class Lexer
{
    private Token ReadStringLiteral()
    {
        var start = _position;
        _position++;

        var content = new System.Text.StringBuilder();
        var terminated = false;

        while (_position < _text.Length)
        {
            if (Current == '"')
            {
                if (Peek(1) == '"')
                {
                    content.Append('"');
                    _position += 2;
                    continue;
                }

                _position++;
                terminated = true;
                break;
            }

            if (IsNewLine(Current))
            {
                break;
            }

            content.Append(Current);
            _position++;
        }

        if (!terminated)
        {
            ReportDiagnostic(start, _position - start, DiagnosticCodes.UnterminatedString, "Незакрытая строковая литерала.");
        }

        return CreateToken(TokenKind.StringLiteralToken, start, _position - start, content.ToString());
    }

    private Token ReadInterpolatedString()
    {
        var start = _position;
        _position += 2;

        _interpolatedStringStarts.Push((start, _insideInterpolatedExpression, _expectFormatSpecifier));
        _insideInterpolatedExpression = false;
        _expectFormatSpecifier = false;
        return CreateToken(TokenKind.InterpolatedStringStartToken, start, _position - start);
    }

    private Token NextInterpolatedStringToken()
    {
        if (_position >= _text.Length)
        {
            ReportDiagnostic(_interpolatedStringStarts.Peek().Start, 0, DiagnosticCodes.UnterminatedString, "Незакрытая интерполированная строка.");
            var previous = _interpolatedStringStarts.Pop();
            _insideInterpolatedExpression = previous.InsideExpression;
            _expectFormatSpecifier = previous.ExpectFormat;
            return CreateToken(TokenKind.EndOfFileToken, _position, 0);
        }

        if (IsNewLine(Current))
        {
            return ReadUnterminatedInterpolatedStringEnd();
        }

        if (Current == '"' && Peek(1) != '"')
        {
            return ReadInterpolatedStringEnd();
        }

        if (Current == '{')
        {
            if (Peek(1) == '{')
            {
                var textToken = ReadInterpolatedStringText();
                if (textToken is not null)
                {
                    return textToken;
                }
            }

            var start = _position;
            _position++;
            _insideInterpolatedExpression = true;
            return CreateToken(TokenKind.InterpolatedStringExpressionStartToken, start, 1);
        }

        if (Current == '}')
        {
            if (Peek(1) == '}')
            {
                var textToken = ReadInterpolatedStringText();
                if (textToken is not null)
                {
                    return textToken;
                }
            }

            return ReadInterpolatedStringExpressionEnd();
        }

        var token = ReadInterpolatedStringText();
        if (token is not null)
        {
            return token;
        }

        ReportDiagnostic(_position, 1, DiagnosticCodes.UnterminatedString, "Неожиданный символ в интерполированной строке.");
        return CreateToken(TokenKind.BadToken, _position++, 1);
    }

    private Token? ReadInterpolatedStringText()
    {
        var start = _position;
        var content = new System.Text.StringBuilder();

        while (_position < _text.Length)
        {
            if (IsNewLine(Current))
            {
                break;
            }

            if (Current == '{')
            {
                if (Peek(1) == '{')
                {
                    content.Append('{');
                    _position += 2;
                    continue;
                }
                break;
            }

            if (Current == '}' && Peek(1) == '}')
            {
                content.Append('}');
                _position += 2;
                continue;
            }

            if (Current == '"')
            {
                if (Peek(1) == '"')
                {
                    content.Append('"');
                    _position += 2;
                    continue;
                }

                break;
            }

            content.Append(Current);
            _position++;
        }

        if (content.Length == 0)
        {
            return null;
        }

        return CreateToken(TokenKind.InterpolatedStringTextToken, start, _position - start, content.ToString());
    }

    private Token ReadInterpolatedStringExpressionEnd()
    {
        var start = _position;
        _position++;
        _insideInterpolatedExpression = false;
        return CreateToken(TokenKind.InterpolatedStringExpressionEndToken, start, 1);
    }

    private Token ReadInterpolatedStringEnd()
    {
        var start = _position;
        _position++;

        if (_interpolatedStringStarts.Count > 0)
        {
            var previous = _interpolatedStringStarts.Pop();
            _insideInterpolatedExpression = previous.InsideExpression;
            _expectFormatSpecifier = previous.ExpectFormat;
        }

        return CreateToken(TokenKind.InterpolatedStringEndToken, start, 1);
    }

    private Token ReadUnterminatedInterpolatedStringEnd()
    {
        var literalStart = _interpolatedStringStarts.Peek().Start;
        ReportDiagnostic(literalStart, 0, DiagnosticCodes.UnterminatedString, "Незакрытая интерполированная строка.");

        var previous = _interpolatedStringStarts.Pop();
        _insideInterpolatedExpression = previous.InsideExpression;
        _expectFormatSpecifier = previous.ExpectFormat;
        return CreateToken(TokenKind.InterpolatedStringEndToken, _position, 0);
    }

    private Token ReadFormatSpecifier()
    {
        var start = _position;
        var content = new System.Text.StringBuilder();

        while (_position < _text.Length)
        {
            if (Current == '}' || Current == '"' || IsNewLine(Current))
            {
                break;
            }

            content.Append(Current);
            _position++;
        }

        return CreateToken(TokenKind.InterpolatedStringFormatSpecifierToken, start, _position - start, content.ToString().Trim());
    }
}
