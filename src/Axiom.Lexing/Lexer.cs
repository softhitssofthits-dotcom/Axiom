using System.Globalization;
using Axiom.Core;

namespace Axiom.Lexing;

public sealed partial class Lexer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AND"] = TokenKind.AndKeyword,
        ["AS"] = TokenKind.AsKeyword,
        ["APPEND"] = TokenKind.AppendKeyword,
        ["BOOLEAN"] = TokenKind.BooleanKeyword,
        ["BEGINFRAME"] = TokenKind.BeginFrameKeyword,
        ["BYREF"] = TokenKind.ByRefKeyword,
        ["BYVAL"] = TokenKind.ByValKeyword,
        ["CANVAS"] = TokenKind.CanvasKeyword,
        ["CAMERA"] = TokenKind.CameraKeyword,
        ["VIEWPORT"] = TokenKind.ViewportKeyword,
        ["CASE"] = TokenKind.CaseKeyword,
        ["CATCH"] = TokenKind.CatchKeyword,
        ["CIRCLE"] = TokenKind.CircleKeyword,
        ["CLS"] = TokenKind.ClsKeyword,
        ["CLOSE"] = TokenKind.CloseKeyword,
        ["COLOR"] = TokenKind.ColorKeyword,
        ["CONST"] = TokenKind.ConstKeyword,
        ["CONTINUE"] = TokenKind.ContinueKeyword,
        ["DATE"] = TokenKind.DateKeyword,
        ["DATETIME"] = TokenKind.DateTimeKeyword,
        ["UTCDATETIME"] = TokenKind.UtcDateTimeKeyword,
        ["DECIMAL"] = TokenKind.DecimalKeyword,
        ["DICTIONARY"] = TokenKind.DictionaryKeyword,
        ["DIM"] = TokenKind.DimKeyword,
        ["DO"] = TokenKind.DoKeyword,
        ["DOUBLE"] = TokenKind.DoubleKeyword,
        ["DRAW"] = TokenKind.DrawKeyword,
        ["DRAWCIRCLE"] = TokenKind.DrawCircleKeyword,
        ["DRAWIMAGE"] = TokenKind.DrawImageKeyword,
        ["DRAWLINE"] = TokenKind.DrawLineKeyword,
        ["DRAWRECT"] = TokenKind.DrawRectKeyword,
        ["DRAWSPRITE"] = TokenKind.DrawSpriteKeyword,
        ["DRAWTILE"] = TokenKind.DrawTileKeyword,
        ["DRAWTILELAYER"] = TokenKind.DrawTileLayerKeyword,
        ["DRAWTILEMAP"] = TokenKind.DrawTilemapKeyword,
        ["DRAWTEXT"] = TokenKind.DrawTextKeyword,
        ["EACH"] = TokenKind.EachKeyword,
        ["ELSE"] = TokenKind.ElseKeyword,
        ["ELSEIF"] = TokenKind.ElseIfKeyword,
        ["END"] = TokenKind.EndKeyword,
        ["ERROR"] = TokenKind.ErrorKeyword,
        ["ERASE"] = TokenKind.EraseKeyword,
        ["EQV"] = TokenKind.EqvKeyword,
        ["EXIT"] = TokenKind.ExitKeyword,
        ["FILE"] = TokenKind.FileKeyword,
        ["FILLCIRCLE"] = TokenKind.FillCircleKeyword,
        ["FILLRECT"] = TokenKind.FillRectKeyword,
        ["FINALLY"] = TokenKind.FinallyKeyword,
        ["FOR"] = TokenKind.ForKeyword,
        ["FUNCTION"] = TokenKind.FunctionKeyword,
        ["GET"] = TokenKind.GetKeyword,
        ["HASHSET"] = TokenKind.SetKeyword,
        ["IF"] = TokenKind.IfKeyword,
        ["IMAGE"] = TokenKind.ImageKeyword,
        ["IMP"] = TokenKind.ImpKeyword,
        ["IN"] = TokenKind.InKeyword,
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
        ["PALETTE"] = TokenKind.PaletteKeyword,
        ["PAINT"] = TokenKind.PaintKeyword,
        ["PCOPY"] = TokenKind.PCopyKeyword,
        ["PRESENT"] = TokenKind.PresentKeyword,
        ["PRESERVE"] = TokenKind.PreserveKeyword,
        ["PRESET"] = TokenKind.PresetKeyword,
        ["PRINT"] = TokenKind.PrintKeyword,
        ["PRIVATE"] = TokenKind.PrivateKeyword,
        ["PUBLIC"] = TokenKind.PublicKeyword,
        ["IMPORT"] = TokenKind.ImportKeyword,
        ["PUT"] = TokenKind.PutKeyword,
        ["PSET"] = TokenKind.PSetKeyword,
        ["REDIM"] = TokenKind.RedimKeyword,
        ["RESETCAMERA"] = TokenKind.ResetCameraKeyword,
        ["RETURN"] = TokenKind.ReturnKeyword,
        ["SELF"] = TokenKind.SelfKeyword,
        ["SELECT"] = TokenKind.SelectKeyword,
        ["SETMAPTILE"] = TokenKind.SetMapTileKeyword,
        ["SCREEN"] = TokenKind.ScreenKeyword,
        ["SMOOTH"] = TokenKind.SmoothKeyword,
        ["SLEEP"] = TokenKind.SleepKeyword,
        ["ANDALSO"] = TokenKind.AndAlsoKeyword,
        ["ORELSE"] = TokenKind.OrElseKeyword,
        ["RANDOMIZE"] = TokenKind.RandomizeKeyword,
        ["STEP"] = TokenKind.StepKeyword,
        ["STOP"] = TokenKind.StopKeyword,
        ["SPRITE"] = TokenKind.SpriteKeyword,
        ["STRING"] = TokenKind.StringKeyword,
        ["SUB"] = TokenKind.SubKeyword,
        ["TILEMAP"] = TokenKind.TilemapKeyword,
        ["TILESET"] = TokenKind.TilesetKeyword,
        ["TIME"] = TokenKind.TimeKeyword,
        ["THEN"] = TokenKind.ThenKeyword,
        ["TO"] = TokenKind.ToKeyword,
        ["TRANSPARENT"] = TokenKind.TransparentKeyword,
        ["TRY"] = TokenKind.TryKeyword,
        ["TYPE"] = TokenKind.TypeKeyword,
        ["USING"] = TokenKind.UsingKeyword,
        ["UNTIL"] = TokenKind.UntilKeyword,
        ["VIEW"] = TokenKind.ViewKeyword,
        ["WINDOW"] = TokenKind.WindowKeyword,
        ["WHILE"] = TokenKind.WhileKeyword,
        ["XOR"] = TokenKind.XorKeyword
    };

    private readonly string _text;
    private readonly string? _document;
    private int _position;

    public Lexer(string text, string? document = null)
    {
        _text = text ?? string.Empty;
        _document = string.IsNullOrWhiteSpace(document) ? null : document;
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

    public static IReadOnlyList<Token> Lex(string text, out DiagnosticBag diagnostics, string? document = null)
    {
        var lexer = new Lexer(text, document);
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
        if (_interpolatedStringStarts.Count > 0 && !_insideInterpolatedExpression)
        {
            return NextInterpolatedStringToken();
        }

        if (_expectFormatSpecifier)
        {
            _expectFormatSpecifier = false;
            return ReadFormatSpecifier();
        }

        SkipWhitespace();

        if (_position >= _text.Length)
        {
            if (_interpolatedStringStarts.Count > 0)
            {
                ReportDiagnostic(_interpolatedStringStarts.Peek().Start, 0, DiagnosticCodes.UnterminatedString, "Незакрытая интерполированная строка.");
                _interpolatedStringStarts.Pop();
            }
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

        if (Current == '"' )
        {
            return ReadStringLiteral();
        }

        if (Current == '$' && Peek(1) == '"')
        {
            return ReadInterpolatedString();
        }

        if (Current == '}' && _insideInterpolatedExpression && _interpolatedStringStarts.Count > 0)
        {
            _insideInterpolatedExpression = false;
            var start = _position;
            _position++;
            return CreateToken(TokenKind.InterpolatedStringExpressionEndToken, start, 1);
        }

        if (Current == '&' && Peek(1) is 'H' or 'h' or 'B' or 'b')
        {
            return ReadRadixLiteral(Peek(1) is 'B' or 'b');
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
                    DiagnosticCodes.NumericLiteralOutOfRange,
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
            DiagnosticCodes.NumericLiteralOutOfRange,
            $"Целочисленный литерал '{text}' выходит за диапазон LONG.");
        return CreateToken(TokenKind.LongLiteralToken, start, _position - start, 0L);
    }

    private Token ReadRadixLiteral(bool binary)
    {
        var start = _position;
        _position += 2;
        var digitsStart = _position;
        while (binary ? Current is '0' or '1' : IsHexDigit(Current))
            _position++;
        var digitsEnd = _position;
        // Неверная цифра или суффикс относится к этому литералу, а не к следующему выражению.
        while (IsIdentifierPart(Current))
            _position++;
        var text = _text[start.._position];
        var name = binary ? "двоичный" : "шестнадцатеричный";
        var prefix = binary ? "&B" : "&H";
        if (digitsEnd == digitsStart || digitsEnd != _position)
        {
            ReportDiagnostic(start, text.Length, DiagnosticCodes.InvalidNumericLiteral,
                $"Недопустимый {name} литерал '{text}': после {prefix} требуются цифры соответствующей системы счисления без суффикса.");
            return CreateToken(TokenKind.IntegerLiteralToken, start, _position - start, 0);
        }

        var style = binary ? NumberStyles.AllowBinarySpecifier : NumberStyles.AllowHexSpecifier;
        if (!ulong.TryParse(text.AsSpan(2), style, CultureInfo.InvariantCulture, out var unsignedValue)
            || unsignedValue > long.MaxValue)
        {
            ReportDiagnostic(start, text.Length, DiagnosticCodes.NumericLiteralOutOfRange,
                $"Литерал '{text}' выходит за диапазон LONG.");
            return CreateToken(TokenKind.LongLiteralToken, start, _position - start, 0L);
        }
        return unsignedValue <= int.MaxValue
            ? CreateToken(TokenKind.IntegerLiteralToken, start, _position - start, (int)unsignedValue)
            : CreateToken(TokenKind.LongLiteralToken, start, _position - start, (long)unsignedValue);
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
            DiagnosticCodes.InvalidNumericLiteral,
            $"Недопустимый числовой литерал '{text}': ведущие нули не допускаются.");
    }

    private readonly Stack<(int Start, bool InsideExpression, bool ExpectFormat)> _interpolatedStringStarts = new();
    private bool _insideInterpolatedExpression;
    private bool _expectFormatSpecifier;

    private Token ReadOperatorOrBadToken()
    {
        var start = _position;

        switch (Current)
        {
            case '&':
                _position++;
                return CreateToken(TokenKind.AmpersandToken, start, 1);
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

                if (_insideInterpolatedExpression && _interpolatedStringStarts.Count > 0)
                {
                    _position++;
                    _expectFormatSpecifier = true;
                    return CreateToken(TokenKind.ColonToken, start, 1);
                }

                _position++;
                ReportDiagnostic(start, 1, DiagnosticCodes.InvalidCharacter, $"Недопустимый символ: '{_text[start]}'.");
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
            case '[':
                _position++;
                return CreateToken(TokenKind.OpenBracketToken, start, 1);
            case ']':
                _position++;
                return CreateToken(TokenKind.CloseBracketToken, start, 1);
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
                ReportDiagnostic(start, 1, DiagnosticCodes.InvalidCharacter, $"Недопустимый символ: '{_text[start]}'.");
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
        return new TextLocation(new TextSpan(start, length), line, column, _document);
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

    private static bool IsIdentifierStart(char c) => char.IsAsciiLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9')
        || (c >= 'A' && c <= 'F')
        || (c >= 'a' && c <= 'f');

    private static bool IsNewLine(char c) => c is '\r' or '\n';

    private static bool IsCommentContinuation(char c) => c == '\0' || IsNewLine(c) || char.IsWhiteSpace(c);
}
