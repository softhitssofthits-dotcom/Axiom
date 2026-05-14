using Axiom.Core;
using System.Text;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private ExecutionSignal ExecuteScreen(BoundScreenStatement statement)
    {
        var mode = EvaluateExpression(statement.ModeExpression).AsInteger();
        if (mode == 0)
        {
            FlushPendingOutput(forceEmptyLine: false);
            _host.ActivateScreen(mode);
            return ExecutionSignal.None;
        }

        if (mode is < 1 or > 10)
        {
            ReportBuiltInArgumentError("SCREEN", "mode", "должен быть в диапазоне 0..10.", statement.Location);
            throw new RuntimeFailureException();
        }

        FlushPendingOutput(forceEmptyLine: false);
        _host.ActivateScreen(mode);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteCls(BoundClsStatement statement)
    {
        if (_host.IsScreenOutputActive)
        {
            _host.ClearGraphics();
        }
        else
        {
            _host.WriteOutput(string.Empty);
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteColor(BoundColorStatement statement)
    {
        RequireScreenActive("COLOR", statement.Location);
        var foreground = EvaluateExpression(statement.ForegroundExpression).AsInteger();
        var background = statement.BackgroundMode == BoundColorBackgroundMode.Color
            ? EvaluateExpression(statement.BackgroundExpression!).AsInteger()
            : 0;
        _host.SetTextColor(foreground, (TextBackgroundMode)statement.BackgroundMode, background);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteLocate(BoundLocateStatement statement)
    {
        RequireScreenActive("LOCATE", statement.Location);
        _host.LocateText(EvaluateExpression(statement.RowExpression).AsInteger(), EvaluateExpression(statement.ColumnExpression).AsInteger());
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecutePSet(BoundPSetStatement statement)
    {
        RequireScreenActive(statement.Reset ? "PRESET" : "PSET", statement.Location);
        var color = statement.ColorExpression is null
            ? statement.Reset ? _host.CurrentBackgroundColor : _host.CurrentForegroundColor
            : EvaluateExpression(statement.ColorExpression).AsInteger();
        var x = EvaluateExpression(statement.XExpression).AsLong();
        var y = EvaluateExpression(statement.YExpression).AsLong();
        if (statement.IsRelative)
        {
            x += _host.CurrentGraphicsX;
            y += _host.CurrentGraphicsY;
        }

        _host.SetPixel(x, y, color);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteLine(BoundLineStatement statement)
    {
        RequireScreenActive("LINE", statement.Location);
        var color = statement.ColorExpression is null ? _host.CurrentForegroundColor : EvaluateExpression(statement.ColorExpression).AsInteger();
        var kind = statement.BoxKind switch
        {
            BoundLineBoxKind.Border => GraphicsLineKind.Box,
            BoundLineBoxKind.Filled => GraphicsLineKind.FilledBox,
            _ => GraphicsLineKind.Normal
        };
        var x1 = statement.HasExplicitStartPoint
            ? EvaluateExpression(statement.X1Expression!).AsLong()
            : _host.CurrentGraphicsX;
        var y1 = statement.HasExplicitStartPoint
            ? EvaluateExpression(statement.Y1Expression!).AsLong()
            : _host.CurrentGraphicsY;
        if (statement.HasExplicitStartPoint && statement.IsStartRelative)
        {
            x1 += _host.CurrentGraphicsX;
            y1 += _host.CurrentGraphicsY;
        }

        var x2 = EvaluateExpression(statement.X2Expression).AsLong();
        var y2 = EvaluateExpression(statement.Y2Expression).AsLong();
        if (statement.IsEndRelative)
        {
            x2 += x1;
            y2 += y1;
        }

        _host.DrawLine(
            x1,
            y1,
            x2,
            y2,
            color,
            kind);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteCircle(BoundCircleStatement statement)
    {
        RequireScreenActive("CIRCLE", statement.Location);
        var color = statement.ColorExpression is null ? 15 : EvaluateExpression(statement.ColorExpression).AsInteger();
        var radius = EvaluateExpression(statement.RadiusExpression).AsDouble();
        var startAngle = statement.StartExpression is null ? (double?)null : EvaluateExpression(statement.StartExpression).AsDouble();
        var endAngle = statement.EndExpression is null ? (double?)null : EvaluateExpression(statement.EndExpression).AsDouble();
        var aspect = statement.AspectExpression is null ? 1d : EvaluateExpression(statement.AspectExpression).AsDouble();
        ValidateCircleArguments(radius, aspect, statement.Location);
        _host.DrawCircle(
            EvaluateExpression(statement.XExpression).AsLong(),
            EvaluateExpression(statement.YExpression).AsLong(),
            radius,
            color,
            statement.IsRelative,
            startAngle is < 0 ? -startAngle : startAngle,
            endAngle is < 0 ? -endAngle : endAngle,
            aspect,
            startAngle is < 0,
            endAngle is < 0);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecutePaint(BoundPaintStatement statement)
    {
        RequireScreenActive("PAINT", statement.Location);
        var x = EvaluateExpression(statement.XExpression).AsLong();
        var y = EvaluateExpression(statement.YExpression).AsLong();
        if (statement.IsRelative)
        {
            x += _host.CurrentGraphicsX;
            y += _host.CurrentGraphicsY;
        }

        var fillValue = statement.FillColorExpression is null ? null : EvaluateExpression(statement.FillColorExpression);
        var borderValue = statement.BorderColorExpression is null ? null : EvaluateExpression(statement.BorderColorExpression);
        var backgroundValue = statement.BackgroundExpression is null ? null : EvaluateExpression(statement.BackgroundExpression);
        _host.Paint(CreatePaintRequest(x, y, fillValue, borderValue, backgroundValue, statement.Location));
        _host.SetGraphicsCurrentPosition(x, y);
        return ExecutionSignal.None;
    }

    private GraphicsPaintRequest CreatePaintRequest(
        long x,
        long y,
        RuntimeValue? fillValue,
        RuntimeValue? borderValue,
        RuntimeValue? backgroundValue,
        TextLocation location)
    {
        if (fillValue is null)
        {
            if (backgroundValue is not null)
            {
                ReportBuiltInArgumentError("PAINT", "фон", "допустим только вместе со строковой формой аргумента 'краска'.", location);
                throw new RuntimeFailureException();
            }

            var fillColor = _host.CurrentBackgroundColor;
            var borderColor = borderValue is null ? fillColor : ToPaintColor(borderValue, "граница", location);
            return GraphicsPaintRequest.CreateSolid(x, y, fillColor, borderColor);
        }

        if (fillValue.Type == TypeSymbol.String)
        {
            var fillPattern = DecodePaintPattern(fillValue.AsString(), "краска", location);
            var borderColor = borderValue is null ? _host.CurrentForegroundColor : ToPaintColor(borderValue, "граница", location);
            var backgroundPattern = backgroundValue is null ? null : DecodePaintPattern(backgroundValue.AsString(), "фон", location);
            return GraphicsPaintRequest.CreatePattern(
                x,
                y,
                fillPattern,
                borderColor,
                _host.CurrentForegroundColor,
                _host.CurrentBackgroundColor,
                backgroundPattern);
        }

        if (backgroundValue is not null)
        {
            ReportBuiltInArgumentError("PAINT", "фон", "допустим только вместе со строковой формой аргумента 'краска'.", location);
            throw new RuntimeFailureException();
        }

        var solidFillColor = ToPaintColor(fillValue, "краска", location);
        var solidBorderColor = borderValue is null ? solidFillColor : ToPaintColor(borderValue, "граница", location);
        return GraphicsPaintRequest.CreateSolid(x, y, solidFillColor, solidBorderColor);
    }

    private byte[] DecodePaintPattern(string text, string argumentName, TextLocation location)
    {
        if (string.IsNullOrEmpty(text))
        {
            ReportBuiltInArgumentError("PAINT", argumentName, "не может быть пустой строкой.", location);
            throw new RuntimeFailureException();
        }

        var bytes = new List<byte>(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            var code = RuntimeCharacterEncoding.Asc(rune);
            if (code > 255)
            {
                ReportBuiltInArgumentError("PAINT", argumentName, "должен содержать только символы с кодами 0..255.", location);
                throw new RuntimeFailureException();
            }

            bytes.Add((byte)code);
            if (bytes.Count > 64)
            {
                ReportBuiltInArgumentError("PAINT", argumentName, "не должен быть длиннее 64 байтов шаблона.", location);
                throw new RuntimeFailureException();
            }
        }

        return bytes.ToArray();
    }

    private int ToPaintColor(RuntimeValue value, string argumentName, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => ToInt32Checked(value.AsLong(), location, $"Аргумент '{argumentName}' у PAINT"),
            var type when type == TypeSymbol.Double => ToPaintColor(value.AsDouble(), argumentName, location),
            var type when type == TypeSymbol.Decimal => ToInt32Checked(decimal.Truncate(value.AsDecimal()), location, $"Аргумент '{argumentName}' у PAINT"),
            _ => ThrowInvalidPaintColor(argumentName, location)
        };
    }

    private int ToPaintColor(double value, string argumentName, TextLocation location)
    {
        if (!double.IsFinite(value))
        {
            ReportBuiltInArgumentError("PAINT", argumentName, "должен быть конечным числом.", location);
            throw new RuntimeFailureException();
        }

        return ToInt32Checked(Math.Truncate(value), location, $"Аргумент '{argumentName}' у PAINT");
    }

    private int ThrowInvalidPaintColor(string argumentName, TextLocation location)
    {
        ReportBuiltInArgumentError("PAINT", argumentName, "должен быть числовым значением.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateCircleArguments(double radius, double aspect, TextLocation location)
    {
        if (double.IsFinite(radius) && radius >= 0 && double.IsFinite(aspect) && aspect > 0)
        {
            return;
        }

        ReportBuiltInArgumentError("CIRCLE", "radius/aspect", "радиус должен быть неотрицательным, а aspect должен быть положительным конечным числом.", location);
        throw new RuntimeFailureException();
    }

    private RuntimeValue EvaluatePoint(BoundCallExpression expression, IReadOnlyList<RuntimeValue> arguments)
    {
        RequireScreenActive("POINT", expression.Location);
        return arguments.Count switch
        {
            1 => RuntimeValue.FromInteger(EvaluatePointCoordinate(arguments[0].AsInteger(), expression.Location)),
            2 => RuntimeValue.FromInteger(_host.GetPixel(arguments[0].AsLong(), arguments[1].AsLong())),
            _ => RuntimeValue.FromInteger(0)
        };
    }

    private int EvaluatePointCoordinate(int selector, TextLocation location)
    {
        return selector switch
        {
            0 => checked((int)_host.CurrentGraphicsX),
            1 => checked((int)_host.CurrentGraphicsY),
            2 => checked((int)_host.CurrentGraphicsX),
            3 => checked((int)_host.CurrentGraphicsY),
            _ =>
            throw CreatePointSelectorFailure(location)
        };
    }

    private RuntimeFailureException CreatePointSelectorFailure(TextLocation location)
    {
        ReportBuiltInArgumentError("POINT", "number", "должен быть в диапазоне 0..3.", location);
        return new RuntimeFailureException();
    }

    private void RequireScreenActive(string operationName, TextLocation location)
    {
        if (_host.IsScreenActive)
        {
            return;
        }

        ReportRuntimeDiagnostic("MB3010", $"{operationName} требует активного графического режима SCREEN.", location);
        throw new RuntimeFailureException();
    }
}
