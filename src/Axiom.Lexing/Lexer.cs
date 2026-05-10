using System.Globalization;
using Axiom.Core;

namespace Axiom.Lexing;

public sealed class Lexer
{
    private const string InvalidCharacterCode = "MB0001";
    private const string UnterminatedStringCode = "MB0002";
    private const string InvalidNumericLiteralCode = "MB0003";
    private const string NumericLiteralOutOfRangeCode = "MB0004";

    private static readonly Dictionary<string, TokenKind> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AND"] = TokenKind.AndKeyword,
        ["AS"] = TokenKind.AsKeyword,
        ["APPEND"] = TokenKind.AppendKeyword,
        ["BOOLEAN"] = TokenKind.BooleanKeyword,
        ["BYREF"] = TokenKind.ByRefKeyword,
        ["BYVAL"] = TokenKind.ByValKeyword,
        ["CASE"] = TokenKind.CaseKeyword,
        ["CATCH"] = TokenKind.CatchKeyword,
        ["CIRCLE"] = TokenKind.CircleKeyword,
        ["CLS"] = TokenKind.ClsKeyword,
        ["CLOSE"] = TokenKind.CloseKeyword,
        ["COLOR"] = TokenKind.ColorKeyword,
        ["CONST"] = TokenKind.ConstKeyword,
        ["DECIMAL"] = TokenKind.DecimalKeyword,
        ["DIM"] = TokenKind.DimKeyword,
        ["DO"] = TokenKind.DoKeyword,
        ["DOUBLE"] = TokenKind.DoubleKeyword,
        ["ELSE"] = TokenKind.ElseKeyword,
        ["ELSEIF"] = TokenKind.ElseIfKeyword,
        ["END"] = TokenKind.EndKeyword,
        ["ERROR"] = TokenKind.ErrorKeyword,
        ["ERASE"] = TokenKind.EraseKeyword,
        ["EQV"] = TokenKind.EqvKeyword,
        ["EXIT"] = TokenKind.ExitKeyword,
        ["FILE"] = TokenKind.FileKeyword,
        ["FINALLY"] = TokenKind.FinallyKeyword,
        ["FOR"] = TokenKind.ForKeyword,
        ["FUNCTION"] = TokenKind.FunctionKeyword,
        ["IF"] = TokenKind.IfKeyword,
        ["IMP"] = TokenKind.ImpKeyword,
        ["INPUT"] = TokenKind.InputKeyword,
        ["INTEGER"] = TokenKind.IntegerKeyword,
        ["IS"] = TokenKind.IsKeyword,
        ["KEY"] = TokenKind.KeyKeyword,
        ["LINE"] = TokenKind.LineKeyword,
        ["LOCATE"] = TokenKind.LocateKeyword,
        ["LONG"] = TokenKind.LongKeyword,
        ["MOD"] = TokenKind.ModKeyword,
        ["MOUSE"] = TokenKind.MouseKeyword,
        ["NEXT"] = TokenKind.NextKeyword,
        ["NOT"] = TokenKind.NotKeyword,
        ["OFF"] = TokenKind.OffKeyword,
        ["ON"] = TokenKind.OnKeyword,
        ["LOOP"] = TokenKind.LoopKeyword,
        ["OPEN"] = TokenKind.OpenKeyword,
        ["OR"] = TokenKind.OrKeyword,
        ["OUTPUT"] = TokenKind.OutputKeyword,
        ["PRESERVE"] = TokenKind.PreserveKeyword,
        ["PRESET"] = TokenKind.PresetKeyword,
        ["PRINT"] = TokenKind.PrintKeyword,
        ["PSET"] = TokenKind.PSetKeyword,
        ["REDIM"] = TokenKind.RedimKeyword,
        ["RETURN"] = TokenKind.ReturnKeyword,
        ["SELF"] = TokenKind.SelfKeyword,
        ["SELECT"] = TokenKind.SelectKeyword,
        ["SCREEN"] = TokenKind.ScreenKeyword,
        ["STEP"] = TokenKind.StepKeyword,
        ["STOP"] = TokenKind.StopKeyword,
        ["STRING"] = TokenKind.StringKeyword,
        ["SUB"] = TokenKind.SubKeyword,
        ["THEN"] = TokenKind.ThenKeyword,
        ["TO"] = TokenKind.ToKeyword,
        ["TRY"] = TokenKind.TryKeyword,
        ["TYPE"] = TokenKind.TypeKeyword,
        ["USING"] = TokenKind.UsingKeyword,
        ["UNTIL"] = TokenKind.UntilKeyword,
        ["WHILE"] = TokenKind.WhileKeyword,
        ["XOR"] = TokenKind.XorKeyword
    };

    private readonly string _text;
    private int _position;

    public Lexer(string text)
    {
        _text = text ?? string.Empty;
        Diagnostics = new DiagnosticBag();
    }

    public DiagnosticBag Diagnostics { get; }

    public IReadOnlyList<Token> Lex()
    {
        var tokens = new List<Token>();

        while (true)
        {
            var token = NextToken();
            tokens.Add(token);

            if (token.Kind == TokenKind.EndOfFileToken)
            {
                break;
            }
        }

        return tokens;
    }

    public static IReadOnlyList<Token> Lex(string text, out DiagnosticBag diagnostics)
    {
        var lexer = new Lexer(text);
        var tokens = lexer.Lex();
        diagnostics = lexer.Diagnostics;
        return tokens;
    }

    private char Current => Peek(0);

    private char Peek(int offset)
    {
        var index = _position + offset;
        return index >= _text.Length ? '\0' : _text[index];
    }

    private Token NextToken()
    {
        SkipWhitespace();

        if (_position >= _text.Length)
        {
            return CreateToken(TokenKind.EndOfFileToken, _position, 0);
        }

        if (IsNewLine(Current))
        {
            return ReadNewLine();
        }

        if (Current == '\'')
        {
            return ReadQuoteComment();
        }

        if (Current == '"')
        {
            return ReadStringLiteral();
        }

        if (Current == '&' && (Peek(1) == 'H' || Peek(1) == 'h'))
        {
            return ReadHexLiteral();
        }

        if (char.IsDigit(Current))
        {
            return ReadNumberLiteral();
        }

        if (IsIdentifierStart(Current))
        {
            return ReadIdentifierOrKeyword();
        }

        return ReadOperatorOrBadToken();
    }

    private void SkipWhitespace()
    {
        while (_position < _text.Length && char.IsWhiteSpace(Current) && !IsNewLine(Current))
        {
            _position++;
        }
    }

    private Token ReadNewLine()
    {
        var start = _position;

        if (Current == '\r' && Peek(1) == '\n')
        {
            _position += 2;
        }
        else
        {
            _position++;
        }

        return CreateToken(TokenKind.NewLineToken, start, _position - start);
    }

    private Token ReadQuoteComment()
    {
        var start = _position;

        while (_position < _text.Length && !IsNewLine(Current))
        {
            _position++;
        }

        return CreateToken(TokenKind.CommentToken, start, _position - start);
    }

    private Token ReadIdentifierOrKeyword()
    {
        var start = _position;

        while (IsIdentifierPart(Current))
        {
            _position++;
        }

        var identifierText = _text[start.._position];

        if (identifierText.Equals("REM", StringComparison.OrdinalIgnoreCase) && IsCommentContinuation(Current))
        {
            while (_position < _text.Length && !IsNewLine(Current))
            {
                _position++;
            }

            return CreateToken(TokenKind.CommentToken, start, _position - start);
        }

        if (identifierText.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
        {
            return CreateToken(TokenKind.BooleanLiteralToken, start, _position - start, true);
        }

        if (identifierText.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
        {
            return CreateToken(TokenKind.BooleanLiteralToken, start, _position - start, false);
        }

        if (Keywords.TryGetValue(identifierText, out var keywordKind))
        {
            return CreateToken(keywordKind, start, _position - start);
        }

        return CreateToken(TokenKind.IdentifierToken, start, _position - start);
    }

    private Token ReadNumberLiteral()
    {
        var start = _position;

        while (char.IsDigit(Current))
        {
            _position++;
        }

        var integerPartLength = _position - start;
        var isDouble = Current == '.' && char.IsDigit(Peek(1));

        if (isDouble)
        {
            _position++;

            while (char.IsDigit(Current))
            {
                _position++;
            }
        }

        var text = _text[start.._position];
        ValidateNumericLiteral(start, text, integerPartLength);

        if (isDouble)
        {
            if (!double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue)
                || !double.IsFinite(doubleValue))
            {
                ReportDiagnostic(
                    start,
                    text.Length,
                    NumericLiteralOutOfRangeCode,
                    $"Литерал DOUBLE '{text}' выходит за диапазон DOUBLE.");
                return CreateToken(TokenKind.DoubleLiteralToken, start, _position - start, 0d);
            }

            return CreateToken(TokenKind.DoubleLiteralToken, start, _position - start, doubleValue);
        }

        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var integerValue))
        {
            return CreateToken(TokenKind.IntegerLiteralToken, start, _position - start, integerValue);
        }

        if (long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var longValue))
        {
            return CreateToken(TokenKind.LongLiteralToken, start, _position - start, longValue);
        }

        ReportDiagnostic(
            start,
            text.Length,
            NumericLiteralOutOfRangeCode,
            $"Целочисленный литерал '{text}' выходит за диапазон LONG.");
        return CreateToken(TokenKind.LongLiteralToken, start, _position - start, 0L);
    }

    private Token ReadHexLiteral()
    {
        var start = _position;
        _position += 2;
        var digitsStart = _position;

        while (IsHexDigit(Current))
        {
            _position++;
        }

        if (_position == digitsStart)
        {
            var text = _text[start.._position];
            ReportDiagnostic(
                start,
                text.Length,
                InvalidNumericLiteralCode,
                $"Недопустимый шестнадцатеричный литерал '{text}': после &H должна идти хотя бы одна шестнадцатеричная цифра.");
            return CreateToken(TokenKind.IntegerLiteralToken, start, _position - start, 0);
        }

        var literalText = _text[start.._position];
        var digits = literalText[2..];

        if (!ulong.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var unsignedValue))
        {
            ReportDiagnostic(
                start,
                literalText.Length,
                NumericLiteralOutOfRangeCode,
                $"Шестнадцатеричный литерал '{literalText}' выходит за диапазон LONG.");
            return CreateToken(TokenKind.LongLiteralToken, start, _position - start, 0L);
        }

        if (unsignedValue <= int.MaxValue)
        {
            return CreateToken(TokenKind.IntegerLiteralToken, start, _position - start, (int)unsignedValue);
        }

        if (unsignedValue <= long.MaxValue)
        {
            return CreateToken(TokenKind.LongLiteralToken, start, _position - start, (long)unsignedValue);
        }

        ReportDiagnostic(
            start,
            literalText.Length,
            NumericLiteralOutOfRangeCode,
            $"Шестнадцатеричный литерал '{literalText}' выходит за диапазон LONG.");
        return CreateToken(TokenKind.LongLiteralToken, start, _position - start, 0L);
    }

    private void ValidateNumericLiteral(int start, string text, int integerPartLength)
    {
        if (integerPartLength <= 1 || text[0] != '0')
        {
            return;
        }

        ReportDiagnostic(
            start,
            text.Length,
            InvalidNumericLiteralCode,
            $"Недопустимый числовой литерал '{text}': ведущие нули не допускаются.");
    }

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
            ReportDiagnostic(start, _position - start, UnterminatedStringCode, "Незакрытая строковая литерала.");
        }

        return CreateToken(TokenKind.StringLiteralToken, start, _position - start, content.ToString());
    }

    private Token ReadOperatorOrBadToken()
    {
        var start = _position;

        switch (Current)
        {
            case '+':
                _position++;
                return CreateToken(TokenKind.PlusToken, start, 1);
            case '-':
                _position++;
                return CreateToken(TokenKind.MinusToken, start, 1);
            case '*':
                _position++;
                return CreateToken(TokenKind.StarToken, start, 1);
            case '^':
                _position++;
                return CreateToken(TokenKind.CaretToken, start, 1);
            case '/':
                _position++;
                return CreateToken(TokenKind.SlashToken, start, 1);
            case '\\':
                _position++;
                return CreateToken(TokenKind.BackslashToken, start, 1);
            case ':':
                if (Peek(1) == '=')
                {
                    _position += 2;
                    return CreateToken(TokenKind.ColonEqualsToken, start, 2);
                }

                _position++;
                ReportDiagnostic(start, 1, InvalidCharacterCode, $"РќРµРґРѕРїСѓСЃС‚РёРјС‹Р№ СЃРёРјРІРѕР»: '{_text[start]}'.");
                return CreateToken(TokenKind.BadToken, start, 1);
            case '=':
                _position++;
                return CreateToken(TokenKind.EqualsToken, start, 1);
            case '(':
                _position++;
                return CreateToken(TokenKind.OpenParenToken, start, 1);
            case ')':
                _position++;
                return CreateToken(TokenKind.CloseParenToken, start, 1);
            case ',':
                _position++;
                return CreateToken(TokenKind.CommaToken, start, 1);
            case ';':
                _position++;
                return CreateToken(TokenKind.SemicolonToken, start, 1);
            case '.':
                _position++;
                return CreateToken(TokenKind.DotToken, start, 1);
            case '<':
                if (Peek(1) == '>')
                {
                    _position += 2;
                    return CreateToken(TokenKind.NotEqualsToken, start, 2);
                }

                if (Peek(1) == '=')
                {
                    _position += 2;
                    return CreateToken(TokenKind.LessOrEqualsToken, start, 2);
                }

                if (Peek(1) == '<')
                {
                    _position += 2;
                    return CreateToken(TokenKind.LessLessToken, start, 2);
                }

                _position++;
                return CreateToken(TokenKind.LessToken, start, 1);
            case '>':
                if (Peek(1) == '=')
                {
                    _position += 2;
                    return CreateToken(TokenKind.GreaterOrEqualsToken, start, 2);
                }

                if (Peek(1) == '>')
                {
                    _position += 2;
                    return CreateToken(TokenKind.GreaterGreaterToken, start, 2);
                }

                _position++;
                return CreateToken(TokenKind.GreaterToken, start, 1);
            default:
                _position++;
                ReportDiagnostic(start, 1, InvalidCharacterCode, $"Недопустимый символ: '{_text[start]}'.");
                return CreateToken(TokenKind.BadToken, start, 1);
        }
    }

    private Token CreateToken(TokenKind kind, int start, int length, object? value = null)
    {
        var text = length == 0 ? string.Empty : _text.Substring(start, length);
        var location = CreateLocation(start, length);
        return new Token(kind, text, location, value);
    }

    private TextLocation CreateLocation(int start, int length)
    {
        var (line, column) = GetLineAndColumn(start);
        return new TextLocation(new TextSpan(start, length), line, column);
    }

    private (int line, int column) GetLineAndColumn(int targetPosition)
    {
        var line = 1;
        var column = 1;

        for (var index = 0; index < targetPosition; index++)
        {
            var current = _text[index];

            if (current == '\r')
            {
                if (index + 1 < targetPosition && _text[index + 1] == '\n')
                {
                    index++;
                }

                line++;
                column = 1;
                continue;
            }

            if (current == '\n')
            {
                line++;
                column = 1;
                continue;
            }

            column++;
        }

        return (line, column);
    }

    private void ReportDiagnostic(int start, int length, string code, string message)
    {
        Diagnostics.Report(code, message, CreateLocation(start, length));
    }

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9')
        || (c >= 'A' && c <= 'F')
        || (c >= 'a' && c <= 'f');

    private static bool IsNewLine(char c) => c is '\r' or '\n';

    private static bool IsCommentContinuation(char c) => c == '\0' || IsNewLine(c) || char.IsWhiteSpace(c);
}
