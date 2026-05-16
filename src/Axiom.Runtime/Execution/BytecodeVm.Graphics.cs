using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteScreenInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var mode = PopValue(frame, instructionIndex).AsInteger();
        if (mode == 0)
        {
            FlushPendingOutput(forceEmptyLine: false);
            _host.ActivateScreen(mode);
            return;
        }

        if (mode is < 1 or > 10)
        {
            ReportBuiltInArgumentError("SCREEN", "mode", "должен быть в диапазоне 0..10.", location);
            throw new RuntimeFailureException();
        }

        FlushPendingOutput(forceEmptyLine: false);
        _host.ActivateScreen(mode);
    }

    private void ExecuteViewInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("VIEW", location);
        var flags = instruction.GetOperand(0);
        var hasCoordinates = (flags & 2) != 0;
        var hasFillColor = (flags & 4) != 0;
        var hasBorderColor = (flags & 8) != 0;
        if (!hasCoordinates)
        {
            _host.ResetViewport();
            return;
        }

        int? borderColor = hasBorderColor ? PopValue(frame, instructionIndex).AsInteger() : null;
        int? fillColor = hasFillColor ? PopValue(frame, instructionIndex).AsInteger() : null;
        var bottom = PopValue(frame, instructionIndex).AsLong();
        var right = PopValue(frame, instructionIndex).AsLong();
        var top = PopValue(frame, instructionIndex).AsLong();
        var left = PopValue(frame, instructionIndex).AsLong();
        if (!TryApplyViewport(left, top, right, bottom, fillColor, borderColor, (flags & 1) != 0))
        {
            ReportInvalidViewport("VIEW", location);
            throw new RuntimeFailureException();
        }
    }

    private void ExecuteWindowInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("WINDOW", location);
        var flags = instruction.GetOperand(0);
        var hasCoordinates = (flags & 2) != 0;
        if (!hasCoordinates)
        {
            _host.ResetWindow();
            return;
        }

        var bottom = PopValue(frame, instructionIndex).AsDouble();
        var right = PopValue(frame, instructionIndex).AsDouble();
        var top = PopValue(frame, instructionIndex).AsDouble();
        var left = PopValue(frame, instructionIndex).AsDouble();
        if (!TryApplyWindow(left, top, right, bottom, (flags & 1) != 0))
        {
            ReportInvalidWindow("WINDOW", location);
            throw new RuntimeFailureException();
        }
    }

    private void ExecuteClsInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (_host.IsScreenOutputActive)
        {
            _host.ClearGraphics();
        }
        else
        {
            _host.WriteOutput(string.Empty);
        }
    }

    private void ExecuteColorInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("COLOR", location);
        var backgroundMode = (TextBackgroundMode)instruction.GetOperand(0);
        var count = instruction.GetOperand(1);
        var background = count == 2 ? PopValue(frame, instructionIndex).AsInteger() : 0;
        var foreground = PopValue(frame, instructionIndex).AsInteger();
        _host.SetTextColor(foreground, backgroundMode, background);
    }

    private void ExecuteLocateInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("LOCATE", location);
        var column = PopValue(frame, instructionIndex).AsInteger();
        var row = PopValue(frame, instructionIndex).AsInteger();
        _host.LocateText(row, column);
    }

    private void ExecutePSetInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var flags = instruction.GetOperand(0);
        var reset = (flags & 1) != 0;
        var relative = (flags & 2) != 0;
        var count = instruction.GetOperand(1);
        RequireScreenActive(reset ? "PRESET" : "PSET", location);
        var color = count == 3
            ? PopValue(frame, instructionIndex).AsInteger()
            : reset ? _host.CurrentBackgroundColor : _host.CurrentForegroundColor;
        var y = PopValue(frame, instructionIndex).AsLong();
        var x = PopValue(frame, instructionIndex).AsLong();
        if (relative)
        {
            x += _host.CurrentGraphicsX;
            y += _host.CurrentGraphicsY;
        }

        _host.SetPixel(x, y, color);
    }

    private void ExecuteLineInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("LINE", location);
        var flags = GraphicsLineInstructionFlags.FromEncoded(instruction.GetOperand(0));
        var count = instruction.GetOperand(1);
        var color = (flags.HasExplicitStartPoint ? count == 5 : count == 3)
            ? PopValue(frame, instructionIndex).AsInteger()
            : _host.CurrentForegroundColor;
        var y2 = PopValue(frame, instructionIndex).AsLong();
        var x2 = PopValue(frame, instructionIndex).AsLong();
        var explicitY1 = flags.HasExplicitStartPoint ? PopValue(frame, instructionIndex).AsLong() : (long?)null;
        var explicitX1 = flags.HasExplicitStartPoint ? PopValue(frame, instructionIndex).AsLong() : (long?)null;
        if (!GraphicsInstructionArguments.TryCreateLine(
                _host.CurrentGraphicsX,
                _host.CurrentGraphicsY,
                x2,
                y2,
                color,
                flags,
                out var line,
                explicitX1,
                explicitY1))
        {
            ReportInvalidGraphicsCoordinates("LINE", location);
            throw new RuntimeFailureException();
        }

        _host.DrawLine(line.X1, line.Y1, line.X2, line.Y2, line.Color, line.Kind);
    }

    private void ExecuteCircleInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("CIRCLE", location);
        var flags = GraphicsCircleInstructionFlags.FromEncoded(instruction.GetOperand(0));
        var count = instruction.GetOperand(1);
        var aspect = flags.HasAspect ? PopValue(frame, instructionIndex).AsDouble() : 1d;
        var endAngle = flags.HasEnd ? PopValue(frame, instructionIndex).AsDouble() : (double?)null;
        var startAngle = flags.HasStart ? PopValue(frame, instructionIndex).AsDouble() : (double?)null;
        var color = flags.HasColor ? PopValue(frame, instructionIndex).AsInteger() : 15;
        var radius = PopValue(frame, instructionIndex).AsDouble();
        var y = PopValue(frame, instructionIndex).AsLong();
        var x = PopValue(frame, instructionIndex).AsLong();
        ValidateCircleArguments(radius, startAngle, endAngle, aspect, location);
        if (!GraphicsInstructionArguments.TryCreateCircle(
                _host.CurrentGraphicsX,
                _host.CurrentGraphicsY,
                x,
                y,
                radius,
                color,
                flags.RelativeCenter,
                startAngle,
                endAngle,
                aspect,
                out var circle))
        {
            ReportInvalidGraphicsCoordinates("CIRCLE", location);
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
    }

    private void ExecutePaintInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("PAINT", location);
        var flags = instruction.GetOperand(0);
        var hasFillColor = HasPaintFill(flags);
        var hasBorderColor = HasPaintBorder(flags);
        var hasBackground = HasPaintBackground(flags);
        var backgroundValue = hasBackground ? PopValue(frame, instructionIndex) : null;
        var borderValue = hasBorderColor ? PopValue(frame, instructionIndex) : null;
        var fillValue = hasFillColor ? PopValue(frame, instructionIndex) : null;
        var y = PopValue(frame, instructionIndex).AsLong();
        var x = PopValue(frame, instructionIndex).AsLong();
        if (HasPaintRelativeStart(flags))
        {
            x += _host.CurrentGraphicsX;
            y += _host.CurrentGraphicsY;
        }

        _host.Paint(CreatePaintRequest(x, y, fillValue, borderValue, backgroundValue, location));
        _host.SetGraphicsCurrentPosition(x, y);
    }

    private void ExecuteDrawInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("DRAW", location);
        var commandText = PopValue(frame, instructionIndex).AsString();
        try
        {
            GraphicsDrawExecutor.Execute(_host, commandText);
        }
        catch (GraphicsDrawException exception)
        {
            ReportRuntimeDiagnostic(BuiltinArgumentCode, $"DRAW: {exception.Message}", location);
            throw new RuntimeFailureException();
        }
    }

    private static bool HasPaintRelativeStart(int flags) => (flags & 1) != 0;

    private static bool HasPaintFill(int flags) => (flags & 2) != 0;

    private static bool HasPaintBorder(int flags) => (flags & 4) != 0;

    private static bool HasPaintBackground(int flags) => (flags & 8) != 0;

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

    private RuntimeValue EvaluatePointBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        RequireScreenActive("POINT", location);
        return arguments.Count switch
        {
            1 => RuntimeValue.FromInteger(EvaluatePointCoordinate(arguments[0].AsInteger(), location)),
            2 => RuntimeValue.FromInteger(_host.GetPixel(arguments[0].AsLong(), arguments[1].AsLong())),
            _ => RuntimeValue.FromInteger(0)
        };
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
