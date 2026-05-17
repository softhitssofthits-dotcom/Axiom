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

    private ExecutionSignal ExecuteView(BoundViewStatement statement)
    {
        RequireScreenActive("VIEW", statement.Location);
        if (statement.IsReset)
        {
            _host.ResetViewport();
            return ExecutionSignal.None;
        }

        var left = EvaluateExpression(statement.LeftExpression!).AsLong();
        var top = EvaluateExpression(statement.TopExpression!).AsLong();
        var right = EvaluateExpression(statement.RightExpression!).AsLong();
        var bottom = EvaluateExpression(statement.BottomExpression!).AsLong();
        int? fillColor = statement.FillColorExpression is null ? null : EvaluateExpression(statement.FillColorExpression).AsInteger();
        int? borderColor = statement.BorderColorExpression is null ? null : EvaluateExpression(statement.BorderColorExpression).AsInteger();
        if (!TryApplyViewport(left, top, right, bottom, fillColor, borderColor, statement.UseScreenCoordinates))
        {
            ReportInvalidViewport("VIEW", statement.Location);
            throw new RuntimeFailureException();
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteWindow(BoundWindowStatement statement)
    {
        RequireScreenActive("WINDOW", statement.Location);
        if (statement.IsReset)
        {
            _host.ResetWindow();
            return ExecutionSignal.None;
        }

        var left = EvaluateExpression(statement.LeftExpression!).AsDouble();
        var top = EvaluateExpression(statement.TopExpression!).AsDouble();
        var right = EvaluateExpression(statement.RightExpression!).AsDouble();
        var bottom = EvaluateExpression(statement.BottomExpression!).AsDouble();
        if (!TryApplyWindow(left, top, right, bottom, statement.UseScreenCoordinates))
        {
            ReportInvalidWindow("WINDOW", statement.Location);
            throw new RuntimeFailureException();
        }

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

    private ExecutionSignal ExecuteGet(BoundGetStatement statement)
    {
        RequireScreenActive("GET", statement.Location);
        if (!GraphicsBufferTransfer.TryResolveTwoDimensionalNumericBuffer(
                EvaluateExpression(statement.BufferExpression),
                out var buffer,
                out var shape)
            || !GraphicsBufferTransfer.TryCreateGetRectangle(
                _host,
                _host.CurrentGraphicsX,
                _host.CurrentGraphicsY,
                EvaluateExpression(statement.X1Expression).AsLong(),
                EvaluateExpression(statement.Y1Expression).AsLong(),
                EvaluateExpression(statement.X2Expression).AsLong(),
                EvaluateExpression(statement.Y2Expression).AsLong(),
                statement.IsStartRelative,
                statement.IsEndRelative,
                out var rectangle)
            || !GraphicsBufferTransfer.BufferMatchesRectangle(shape, rectangle))
        {
            ReportInvalidGraphicsTransfer("GET", statement.Location);
            throw new RuntimeFailureException();
        }

        GraphicsBufferTransfer.CaptureRectangle(_host, buffer, shape, rectangle);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecutePut(BoundPutStatement statement)
    {
        RequireScreenActive("PUT", statement.Location);
        if (!GraphicsBufferTransfer.TryResolveTwoDimensionalNumericBuffer(
                EvaluateExpression(statement.BufferExpression),
                out var buffer,
                out var shape)
            || !GraphicsBufferTransfer.TryCreatePutAnchor(
                _host,
                _host.CurrentGraphicsX,
                _host.CurrentGraphicsY,
                EvaluateExpression(statement.XExpression).AsLong(),
                EvaluateExpression(statement.YExpression).AsLong(),
                statement.IsRelative,
                out var anchor)
            || !GraphicsBufferTransfer.TryPutBuffer(_host, buffer, shape, anchor, statement.Action))
        {
            ReportInvalidGraphicsTransfer("PUT", statement.Location);
            throw new RuntimeFailureException();
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteLine(BoundLineStatement statement)
    {
        RequireScreenActive("LINE", statement.Location);
        var color = statement.ColorExpression is null ? _host.CurrentForegroundColor : EvaluateExpression(statement.ColorExpression).AsInteger();
        var flags = new GraphicsLineInstructionFlags(statement.BoxKind, statement.HasExplicitStartPoint, statement.IsStartRelative, statement.IsEndRelative);
        var x2 = EvaluateExpression(statement.X2Expression).AsLong();
        var y2 = EvaluateExpression(statement.Y2Expression).AsLong();
        if (!GraphicsInstructionArguments.TryCreateLine(
                _host.CurrentGraphicsX,
                _host.CurrentGraphicsY,
                x2,
                y2,
                color,
                flags,
                out var line,
                statement.HasExplicitStartPoint ? EvaluateExpression(statement.X1Expression!).AsLong() : null,
                statement.HasExplicitStartPoint ? EvaluateExpression(statement.Y1Expression!).AsLong() : null))
        {
            ReportInvalidGraphicsCoordinates("LINE", statement.Location);
            throw new RuntimeFailureException();
        }

        _host.DrawLine(line.X1, line.Y1, line.X2, line.Y2, line.Color, line.Kind);
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
        ValidateCircleArguments(radius, startAngle, endAngle, aspect, statement.Location);
        if (!GraphicsInstructionArguments.TryCreateCircle(
                _host.CurrentGraphicsX,
                _host.CurrentGraphicsY,
                EvaluateExpression(statement.XExpression).AsLong(),
                EvaluateExpression(statement.YExpression).AsLong(),
                radius,
                color,
                statement.IsRelative,
                startAngle,
                endAngle,
                aspect,
                out var circle))
        {
            ReportInvalidGraphicsCoordinates("CIRCLE", statement.Location);
            throw new RuntimeFailureException();
        }

        _host.DrawCircle(
            circle.CenterX,
            circle.CenterY,
            circle.Radius,
            circle.Color,
            relativeCenter: false,
            circle.StartAngle,
            circle.EndAngle,
            circle.Aspect,
            circle.DrawStartRadius,
            circle.DrawEndRadius);
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

    private ExecutionSignal ExecuteDraw(BoundDrawStatement statement)
    {
        RequireScreenActive("DRAW", statement.Location);
        var commandText = EvaluateExpression(statement.CommandExpression).AsString();
        try
        {
            GraphicsDrawExecutor.Execute(_host, commandText);
            return ExecutionSignal.None;
        }
        catch (GraphicsDrawException exception)
        {
            ReportRuntimeDiagnostic(BuiltinArgumentCode, $"DRAW: {exception.Message}", statement.Location);
            throw new RuntimeFailureException();
        }
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

    private void ValidateCircleArguments(double radius, double? startAngle, double? endAngle, double aspect, TextLocation location)
    {
        if (GraphicsCircleValidation.HasValidArguments(radius, startAngle, endAngle, aspect))
        {
            return;
        }

        ReportBuiltInArgumentError("CIRCLE", "radius/start/end/aspect", "радиус должен быть неотрицательным конечным числом, start и end должны быть конечными числами, а aspect должен быть положительным конечным числом не больше 1000000.", location);
        throw new RuntimeFailureException();
    }

    private void ReportInvalidGraphicsCoordinates(string instructionName, TextLocation location)
    {
        ReportBuiltInArgumentError(
            instructionName,
            "x/y",
            $"координаты должны оставаться в диапазоне {GraphicsCoordinateValidation.MinCoordinate}..{GraphicsCoordinateValidation.MaxCoordinate}.",
            location);
    }

    private void ReportInvalidGraphicsTransfer(string instructionName, TextLocation location)
    {
        ReportBuiltInArgumentError(
            instructionName,
            "buffer",
            "координаты должны успешно преобразовываться через текущее VIEW/WINDOW, а buffer должен быть выделенным двумерным числовым массивом допустимой формы; для GET размер buffer обязан совпадать с прямоугольником.",
            location);
    }

    private void ReportInvalidViewport(string instructionName, TextLocation location)
    {
        ReportBuiltInArgumentError(
            instructionName,
            "x/y",
            "координаты viewport должны задавать прямоугольник внутри текущего SCREEN.",
            location);
    }

    private void ReportInvalidWindow(string instructionName, TextLocation location)
    {
        ReportBuiltInArgumentError(
            instructionName,
            "x/y",
            "координаты WINDOW должны задавать ненулевой логический прямоугольник в допустимом диапазоне, а текущий VIEW должен оставлять минимум два пикселя по каждой оси.",
            location);
    }

    private bool TryApplyViewport(
        long left,
        long top,
        long right,
        long bottom,
        int? fillColor,
        int? borderColor,
        bool useScreenCoordinates)
    {
        if (!GraphicsCoordinateValidation.AreSupportedLineCoordinates(left, top, right, bottom))
        {
            return false;
        }

        if (left > right || top > bottom)
        {
            return false;
        }

        return _host.SetViewport(left, top, right, bottom, fillColor, borderColor, useScreenCoordinates);
    }

    private bool TryApplyWindow(
        double left,
        double top,
        double right,
        double bottom,
        bool useScreenCoordinates)
    {
        if (!GraphicsCoordinateValidation.AreSupportedWindowCoordinates(left, top, right, bottom))
        {
            return false;
        }

        return _host.SetWindow(left, top, right, bottom, useScreenCoordinates);
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

    private RuntimeValue EvaluatePMap(BoundCallExpression expression, IReadOnlyList<RuntimeValue> arguments)
    {
        RequireScreenActive("PMAP", expression.Location);
        var coordinate = arguments[0].AsDouble();
        var selector = arguments[1].AsInteger();
        if (!double.IsFinite(coordinate))
        {
            throw CreatePMapCoordinateFailure(expression.Location);
        }

        if (selector is < 0 or > 3)
        {
            throw CreatePMapSelectorFailure(expression.Location);
        }

        if (!GraphicsCoordinateSpaceMath.TryMapPMapCoordinate(_host, coordinate, selector, out var mapped))
        {
            throw CreatePMapTransformFailure(expression.Location);
        }

        return RuntimeValue.FromDouble(mapped);
    }

    private int EvaluatePointCoordinate(int selector, TextLocation location)
    {
        return selector switch
        {
            0 => checked((int)_host.CurrentViewportX),
            1 => checked((int)_host.CurrentViewportY),
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

    private RuntimeFailureException CreatePMapCoordinateFailure(TextLocation location)
    {
        ReportBuiltInArgumentError("PMAP", "coordinate", "должна быть конечным числом.", location);
        return new RuntimeFailureException();
    }

    private RuntimeFailureException CreatePMapSelectorFailure(TextLocation location)
    {
        ReportBuiltInArgumentError("PMAP", "selector", "должен быть в диапазоне 0..3.", location);
        return new RuntimeFailureException();
    }

    private RuntimeFailureException CreatePMapTransformFailure(TextLocation location)
    {
        ReportBuiltInArgumentError("PMAP", "coordinate", "не может быть преобразована: текущий effective viewport слишком мал для активного WINDOW.", location);
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
