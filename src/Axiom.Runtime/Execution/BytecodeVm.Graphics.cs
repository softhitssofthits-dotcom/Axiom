using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

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
        var flags = instruction.GetOperand(0);
        var boxKind = (BoundLineBoxKind)(flags & 3);
        var hasExplicitStartPoint = (flags & 4) != 0;
        var startRelative = (flags & 8) != 0;
        var endRelative = (flags & 16) != 0;
        var count = instruction.GetOperand(1);
        var color = (hasExplicitStartPoint ? count == 5 : count == 3)
            ? PopValue(frame, instructionIndex).AsInteger()
            : _host.CurrentForegroundColor;
        var y2 = PopValue(frame, instructionIndex).AsLong();
        var x2 = PopValue(frame, instructionIndex).AsLong();
        long x1;
        long y1;
        if (hasExplicitStartPoint)
        {
            y1 = PopValue(frame, instructionIndex).AsLong();
            x1 = PopValue(frame, instructionIndex).AsLong();
            if (startRelative)
            {
                x1 += _host.CurrentGraphicsX;
                y1 += _host.CurrentGraphicsY;
            }
        }
        else
        {
            x1 = _host.CurrentGraphicsX;
            y1 = _host.CurrentGraphicsY;
        }

        if (endRelative)
        {
            x2 += x1;
            y2 += y1;
        }

        var kind = boxKind switch
        {
            BoundLineBoxKind.Border => GraphicsLineKind.Box,
            BoundLineBoxKind.Filled => GraphicsLineKind.FilledBox,
            _ => GraphicsLineKind.Normal
        };
        _host.DrawLine(x1, y1, x2, y2, color, kind);
    }

    private void ExecuteCircleInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("CIRCLE", location);
        var flags = instruction.GetOperand(0);
        var count = instruction.GetOperand(1);
        var aspect = HasCircleAspect(flags) ? PopValue(frame, instructionIndex).AsDouble() : 1d;
        var endAngle = HasCircleEnd(flags) ? PopValue(frame, instructionIndex).AsDouble() : (double?)null;
        var startAngle = HasCircleStart(flags) ? PopValue(frame, instructionIndex).AsDouble() : (double?)null;
        var color = HasCircleColor(flags) ? PopValue(frame, instructionIndex).AsInteger() : 15;
        var radius = PopValue(frame, instructionIndex).AsDouble();
        var y = PopValue(frame, instructionIndex).AsLong();
        var x = PopValue(frame, instructionIndex).AsLong();
        ValidateCircleArguments(radius, aspect, location);
        _host.DrawCircle(
            x,
            y,
            radius,
            color,
            HasCircleRelativeCenter(flags),
            startAngle is < 0 ? -startAngle : startAngle,
            endAngle is < 0 ? -endAngle : endAngle,
            aspect,
            startAngle is < 0,
            endAngle is < 0);
    }

    private void ExecutePaintInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("PAINT", location);
        var flags = instruction.GetOperand(0);
        var hasFillColor = (flags & 1) != 0;
        var hasBorderColor = (flags & 2) != 0;
        var borderColorValue = hasBorderColor
            ? PopValue(frame, instructionIndex).AsInteger()
            : 0;
        var fillColor = hasFillColor
            ? PopValue(frame, instructionIndex).AsInteger()
            : _host.CurrentForegroundColor;
        var y = PopValue(frame, instructionIndex).AsLong();
        var x = PopValue(frame, instructionIndex).AsLong();
        var borderColor = hasBorderColor ? borderColorValue : _host.GetPixel(x, y);
        _host.Paint(x, y, fillColor, borderColor);
    }

    private static bool HasCircleRelativeCenter(int flags) => (flags & 1) != 0;

    private static bool HasCircleStart(int flags) => (flags & 2) != 0;

    private static bool HasCircleEnd(int flags) => (flags & 4) != 0;

    private static bool HasCircleAspect(int flags) => (flags & 8) != 0;

    private static bool HasCircleColor(int flags) => (flags & 16) != 0;

    private void ValidateCircleArguments(double radius, double aspect, TextLocation location)
    {
        if (double.IsFinite(radius) && radius >= 0 && double.IsFinite(aspect) && aspect > 0)
        {
            return;
        }

        ReportBuiltInArgumentError("CIRCLE", "radius/aspect", "радиус должен быть неотрицательным, а aspect должен быть положительным конечным числом.", location);
        throw new RuntimeFailureException();
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
