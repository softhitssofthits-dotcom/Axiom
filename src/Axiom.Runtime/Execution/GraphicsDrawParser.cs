using System.Globalization;

namespace Axiom.Runtime;

internal static class GraphicsDrawParser
{
    public static IReadOnlyList<GraphicsDrawCommand> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parser = new Parser(text);
        return parser.Parse();
    }

    private sealed class Parser
    {
        private readonly string _text;
        private readonly List<GraphicsDrawCommand> _commands = [];
        private int _position;
        private bool _blindNext;
        private bool _returnNext;

        public Parser(string text)
        {
            _text = text;
        }

        public IReadOnlyList<GraphicsDrawCommand> Parse()
        {
            while (true)
            {
                SkipWhitespace();
                if (_position >= _text.Length)
                {
                    break;
                }

                var commandPosition = _position + 1;
                var upper = char.ToUpperInvariant(_text[_position]);
                switch (upper)
                {
                    case 'B':
                        _position++;
                        _blindNext = true;
                        break;
                    case 'N':
                        _position++;
                        _returnNext = true;
                        break;
                    case 'U':
                        _position++;
                        AddMove(GraphicsDrawDirection.Up, commandPosition, ReadOptionalNonNegativeNumber(defaultValue: 1));
                        break;
                    case 'D':
                        _position++;
                        AddMove(GraphicsDrawDirection.Down, commandPosition, ReadOptionalNonNegativeNumber(defaultValue: 1));
                        break;
                    case 'L':
                        _position++;
                        AddMove(GraphicsDrawDirection.Left, commandPosition, ReadOptionalNonNegativeNumber(defaultValue: 1));
                        break;
                    case 'R':
                        _position++;
                        AddMove(GraphicsDrawDirection.Right, commandPosition, ReadOptionalNonNegativeNumber(defaultValue: 1));
                        break;
                    case 'E':
                        _position++;
                        AddMove(GraphicsDrawDirection.UpRight, commandPosition, ReadOptionalNonNegativeNumber(defaultValue: 1));
                        break;
                    case 'F':
                        _position++;
                        AddMove(GraphicsDrawDirection.DownRight, commandPosition, ReadOptionalNonNegativeNumber(defaultValue: 1));
                        break;
                    case 'G':
                        _position++;
                        AddMove(GraphicsDrawDirection.DownLeft, commandPosition, ReadOptionalNonNegativeNumber(defaultValue: 1));
                        break;
                    case 'H':
                        _position++;
                        AddMove(GraphicsDrawDirection.UpLeft, commandPosition, ReadOptionalNonNegativeNumber(defaultValue: 1));
                        break;
                    case 'C':
                        _position++;
                        _commands.Add(new GraphicsDrawCommand(
                            GraphicsDrawCommandKind.SetColor,
                            commandPosition,
                            NumericValue: ReadRequiredFiniteNumber("У команды C должен быть числовой аргумент цвета."),
                            Blind: ConsumeBlind(),
                            ReturnToOrigin: ConsumeReturn()));
                        break;
                    case 'S':
                        _position++;
                        _commands.Add(new GraphicsDrawCommand(
                            GraphicsDrawCommandKind.SetScale,
                            commandPosition,
                            NumericValue: ReadRequiredFiniteNumber("У команды S должен быть числовой аргумент масштаба."),
                            Blind: ConsumeBlind(),
                            ReturnToOrigin: ConsumeReturn()));
                        break;
                    case 'A':
                        _position++;
                        _commands.Add(new GraphicsDrawCommand(
                            GraphicsDrawCommandKind.SetAngleQuadrant,
                            commandPosition,
                            NumericValue: ReadRequiredFiniteNumber("У команды A должен быть числовой аргумент угла."),
                            Blind: ConsumeBlind(),
                            ReturnToOrigin: ConsumeReturn()));
                        break;
                    case 'T':
                        if (PeekKeyword("TA"))
                        {
                            _position += 2;
                            _commands.Add(new GraphicsDrawCommand(
                                GraphicsDrawCommandKind.SetAngleDegrees,
                                commandPosition,
                                NumericValue: ReadRequiredFiniteNumber("У команды TA должен быть числовой аргумент угла."),
                                Blind: ConsumeBlind(),
                                ReturnToOrigin: ConsumeReturn()));
                            break;
                        }

                        throw new GraphicsDrawException($"Неизвестная команда DRAW '{_text[_position]}'.", commandPosition);
                    case 'M':
                        _position++;
                        ParseMoveTo(commandPosition);
                        break;
                    case 'P':
                        _position++;
                        ParsePaint(commandPosition);
                        break;
                    case 'X':
                        _position++;
                        _commands.Add(new GraphicsDrawCommand(
                            GraphicsDrawCommandKind.UnsupportedHistoricalX,
                            commandPosition,
                            Blind: ConsumeBlind(),
                            ReturnToOrigin: ConsumeReturn()));
                        SkipHistoricalXPayload();
                        break;
                    default:
                        throw new GraphicsDrawException($"Неизвестная команда DRAW '{_text[_position]}'.", commandPosition);
                }
            }

            if (_blindNext || _returnNext)
            {
                throw new GraphicsDrawException("Модификаторы B и N должны предшествовать команде движения или M.", _position + 1);
            }

            return _commands;
        }

        private void AddMove(GraphicsDrawDirection direction, int position, double amount)
        {
            _commands.Add(new GraphicsDrawCommand(
                GraphicsDrawCommandKind.Move,
                position,
                direction,
                amount,
                Blind: ConsumeBlind(),
                ReturnToOrigin: ConsumeReturn()));
        }

        private void ParseMoveTo(int position)
        {
            SkipWhitespace();
            var start = _position;
            var xSigned = CurrentIsSign();
            var x = ReadRequiredLong("Команда M требует координаты в форме x,y или +dx,-dy.");
            SkipWhitespace();
            if (!TryConsume(','))
            {
                throw new GraphicsDrawException("Команда M требует координаты в форме x,y или +dx,-dy.", start + 1);
            }

            SkipWhitespace();
            var ySigned = CurrentIsSign();
            var y = ReadRequiredLong("Команда M требует координаты в форме x,y или +dx,-dy.");
            var isRelative = xSigned || ySigned;
            if (isRelative && !(xSigned && ySigned))
            {
                throw new GraphicsDrawException("Относительная команда M должна задавать знак у обеих координат.", start + 1);
            }

            _commands.Add(new GraphicsDrawCommand(
                GraphicsDrawCommandKind.MoveTo,
                position,
                Blind: ConsumeBlind(),
                ReturnToOrigin: ConsumeReturn(),
                X: x,
                Y: y,
                IsRelativeMove: isRelative));
        }

        private void ParsePaint(int position)
        {
            var fill = ReadRequiredInt("Команда P требует числовой цвет заливки.");
            int? border = null;
            SkipWhitespace();
            if (TryConsume(','))
            {
                border = ReadRequiredInt("Команда P требует числовой цвет границы после запятой.");
            }

            _commands.Add(new GraphicsDrawCommand(
                GraphicsDrawCommandKind.Paint,
                position,
                Blind: ConsumeBlind(),
                ReturnToOrigin: ConsumeReturn(),
                FillColor: fill,
                BorderColor: border));
        }

        private bool ConsumeBlind()
        {
            var value = _blindNext;
            _blindNext = false;
            return value;
        }

        private bool ConsumeReturn()
        {
            var value = _returnNext;
            _returnNext = false;
            return value;
        }

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        private void SkipHistoricalXPayload()
        {
            while (_position < _text.Length && !char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        private bool CurrentIsSign() => _position < _text.Length && (_text[_position] == '+' || _text[_position] == '-');

        private bool TryConsume(char c)
        {
            if (_position < _text.Length && _text[_position] == c)
            {
                _position++;
                return true;
            }

            return false;
        }

        private bool PeekKeyword(string keyword)
        {
            if (_position + keyword.Length > _text.Length)
            {
                return false;
            }

            return string.Compare(_text, _position, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private double ReadOptionalNonNegativeNumber(double defaultValue)
        {
            SkipWhitespace();
            if (_position >= _text.Length || !IsNumberStart(_text[_position]))
            {
                return defaultValue;
            }

            var value = ReadRequiredFiniteNumber("У команды движения должен быть числовой аргумент длины.");
            if (value < 0)
            {
                throw new GraphicsDrawException("Длина команды движения DRAW не может быть отрицательной.", _position + 1);
            }

            return value;
        }

        private double ReadRequiredFiniteNumber(string message)
        {
            SkipWhitespace();
            var start = _position;
            if (_position >= _text.Length || !IsNumberStart(_text[_position]))
            {
                throw new GraphicsDrawException(message, start + 1);
            }

            if (_text[_position] == '+' || _text[_position] == '-')
            {
                _position++;
            }

            var hasDigit = false;
            while (_position < _text.Length && char.IsDigit(_text[_position]))
            {
                hasDigit = true;
                _position++;
            }

            if (_position < _text.Length && _text[_position] == '.')
            {
                _position++;
                while (_position < _text.Length && char.IsDigit(_text[_position]))
                {
                    hasDigit = true;
                    _position++;
                }
            }

            if (!hasDigit)
            {
                throw new GraphicsDrawException(message, start + 1);
            }

            var token = _text[start.._position];
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
            {
                throw new GraphicsDrawException("Команда DRAW содержит неверное число.", start + 1);
            }

            return value;
        }

        private long ReadRequiredLong(string message)
        {
            SkipWhitespace();
            var start = _position;
            if (_position >= _text.Length || !IsNumberStart(_text[_position]))
            {
                throw new GraphicsDrawException(message, start + 1);
            }

            if (_text[_position] == '+' || _text[_position] == '-')
            {
                _position++;
            }

            var digitsStart = _position;
            while (_position < _text.Length && char.IsDigit(_text[_position]))
            {
                _position++;
            }

            if (digitsStart == _position)
            {
                throw new GraphicsDrawException(message, start + 1);
            }

            var token = _text[start.._position];
            if (!long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                throw new GraphicsDrawException("Команда DRAW содержит неверное целое число.", start + 1);
            }

            return value;
        }

        private int ReadRequiredInt(string message)
        {
            var value = ReadRequiredLong(message);
            if (value is < int.MinValue or > int.MaxValue)
            {
                throw new GraphicsDrawException("Числовой аргумент DRAW выходит за диапазон INTEGER.", _position + 1);
            }

            return (int)value;
        }

        private static bool IsNumberStart(char c) => char.IsDigit(c) || c is '+' or '-' or '.';
    }
}
