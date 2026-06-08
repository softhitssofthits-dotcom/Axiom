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
        var valueCount = PopValue(frame, instructionIndex).AsInteger();
        var visualPage = PopValue(frame, instructionIndex).AsInteger();
        var activePage = PopValue(frame, instructionIndex).AsInteger();
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

        if (activePage is < 0 or > 3)
        {
            ReportBuiltInArgumentError("SCREEN", "activePage", "должен быть в диапазоне 0..3.", location);
            throw new RuntimeFailureException();
        }

        if (visualPage is < 0 or > 3)
        {
            ReportBuiltInArgumentError("SCREEN", "visualPage", "должен быть в диапазоне 0..3.", location);
            throw new RuntimeFailureException();
        }

        FlushPendingOutput(forceEmptyLine: false);
        _host.ActivateScreen(mode, activePage, valueCount >= 3 ? visualPage : activePage);
    }

    private void ExecuteCanvasInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var height = GetWholeNumberUnchecked(stack.PopUnchecked());
        var width = GetWholeNumberUnchecked(stack.PopUnchecked());
        if (width <= 0 || height <= 0 || width > int.MaxValue || height > int.MaxValue)
        {
            ValidateCanvasSize(width, height, ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        FlushPendingOutput(forceEmptyLine: false);
        _host.ActivateCanvas(width, height);
    }

    private void ExecuteBeginFrameInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("BEGINFRAME", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.BeginFrame();
    }

    private void ExecutePresentInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("PRESENT", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.PresentFrame();
    }

    private void ExecuteCameraInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("CAMERA", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var stack = frame.EvaluationStack;
        var y = GetWholeNumberUnchecked(stack.PopUnchecked());
        var x = GetWholeNumberUnchecked(stack.PopUnchecked());
        if (!GraphicsCoordinateValidation.IsSupportedCoordinate(x)
            || !GraphicsCoordinateValidation.IsSupportedCoordinate(y))
        {
            ValidateCanvasPoint("CAMERA", x, y, ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.SetCanvasCamera(x, y);
    }

    private void ExecuteViewportInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("VIEWPORT", location);
        }

        var valueCount = instruction.GetOperand(0);
        if (valueCount == 0)
        {
            _host.ResetViewport();
            return;
        }

        var bottomExclusive = GetWholeNumberUnchecked(PopValue(frame, instructionIndex));
        var rightExclusive = GetWholeNumberUnchecked(PopValue(frame, instructionIndex));
        var top = GetWholeNumberUnchecked(PopValue(frame, instructionIndex));
        var left = GetWholeNumberUnchecked(PopValue(frame, instructionIndex));
        if (!TryApplyCanvasViewport(left, top, rightExclusive, bottomExclusive))
        {
            ReportInvalidCanvasViewport("VIEWPORT", location);
            throw new RuntimeFailureException();
        }
    }

    private void ExecuteResetCameraInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("RESETCAMERA", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.ResetCanvasCamera();
    }

    private void ExecutePCopyInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("PCOPY", location);
        var destinationPage = PopValue(frame, instructionIndex).AsInteger();
        var sourcePage = PopValue(frame, instructionIndex).AsInteger();
        if (!_host.CopyScreenPage(sourcePage, destinationPage))
        {
            ReportBuiltInArgumentError("PCOPY", "page", "должна быть в диапазоне 0..3.", location);
            throw new RuntimeFailureException();
        }
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

    private void ExecuteViewPrintInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("VIEW PRINT", location);
        var valueCount = instruction.GetOperand(0);
        if (valueCount == 0)
        {
            _host.ResetTextViewport();
            return;
        }

        var bottomRow = PopValue(frame, instructionIndex).AsInteger();
        var topRow = PopValue(frame, instructionIndex).AsInteger();
        if (!_host.SetTextViewport(topRow, bottomRow))
        {
            ReportInvalidTextViewport("VIEW PRINT", location);
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

    private void ExecuteClsInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        if (instruction.GetOperand(0) == 1)
        {
            if (!_host.IsCanvasActive)
            {
                RequireCanvasActive("CLS", ResolveLocation(frame.Routine.Id, instructionIndex));
            }

            _host.ClearFrame(frame.EvaluationStack.PopUnchecked().AsInteger());
            return;
        }

        if (_host.IsCanvasActive)
        {
            _host.ClearFrame(0);
            return;
        }

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

    private void ExecutePaletteInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireScreenActive("PALETTE", location);
        if (instruction.GetOperand(0) == 0)
        {
            _host.ResetPalette();
            return;
        }

        if (instruction.GetOperand(0) == 1)
        {
            ApplyPaletteUsing(PopValue(frame, instructionIndex).AsArray(), location);
            return;
        }

        var color = PopValue(frame, instructionIndex).AsInteger();
        var index = PopValue(frame, instructionIndex).AsInteger();
        _host.SetPaletteColor(index, color);
    }

    private void ApplyPaletteUsing(RuntimeArrayValue array, TextLocation location)
    {
        if (!array.IsAllocated)
        {
            ReportInvalidRuntimeState("Массив 'PALETTE USING' объявлен, но ещё не выделен.", location);
            return;
        }

        if (array.Rank != 1)
        {
            ReportRuntimeDiagnostic(BuiltinArgumentCode, "PALETTE USING: ожидается одномерный массив.", location);
            return;
        }

        var paletteIndex = 0;
        array.ForEachVisibleIndexSetAscending(indices =>
        {
            if (paletteIndex < 256)
            {
                _host.SetPaletteColor(paletteIndex, ToPaletteColor(array.GetValue(indices), location));
            }

            paletteIndex++;
        });
    }

    private int ToPaletteColor(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => ToInt32Checked(value.AsLong(), location, "Элемент массива PALETTE USING"),
            var type when type == TypeSymbol.Double => ToInt32Checked(Math.Truncate(value.AsDouble()), location, "Элемент массива PALETTE USING"),
            var type when type == TypeSymbol.Decimal => ToInt32Checked(decimal.Truncate(value.AsDecimal()), location, "Элемент массива PALETTE USING"),
            _ => ReportInvalidRuntimeState<int>($"PALETTE USING не поддерживает элемент типа '{value.Type.Name}'.", location)
        };
    }

    private void ExecutePSetInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var flags = instruction.GetOperand(0);
        var reset = (flags & 1) != 0;
        var relative = (flags & 2) != 0;
        var count = instruction.GetOperand(1);
        RequireDrawableSurfaceActive(reset ? "PRESET" : "PSET", location);

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

    private void ExecuteGetInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireDrawableSurfaceActive("GET", location);
        var flags = instruction.GetOperand(0);
        var bufferValue = PopValue(frame, instructionIndex);
        var y2 = PopValue(frame, instructionIndex).AsLong();
        var x2 = PopValue(frame, instructionIndex).AsLong();
        var y1 = PopValue(frame, instructionIndex).AsLong();
        var x1 = PopValue(frame, instructionIndex).AsLong();
        if (!GraphicsBufferTransfer.TryResolveTwoDimensionalNumericBuffer(bufferValue, out var buffer, out var shape)
            || !GraphicsBufferTransfer.TryCreateGetRectangle(
                _host,
                _host.CurrentGraphicsX,
                _host.CurrentGraphicsY,
                x1,
                y1,
                x2,
                y2,
                (flags & 1) != 0,
                (flags & 2) != 0,
                out var rectangle)
            || !GraphicsBufferTransfer.BufferMatchesRectangle(shape, rectangle))
        {
            ReportInvalidGraphicsTransfer("GET", location);
            throw new RuntimeFailureException();
        }

        GraphicsBufferTransfer.CaptureRectangle(_host, buffer, shape, rectangle);
    }

    private void ExecutePutInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        RequireDrawableSurfaceActive("PUT", location);
        var flags = instruction.GetOperand(0);
        var bufferValue = PopValue(frame, instructionIndex);
        var y = PopValue(frame, instructionIndex).AsLong();
        var x = PopValue(frame, instructionIndex).AsLong();
        if (!GraphicsBufferTransfer.TryResolveTwoDimensionalNumericBuffer(bufferValue, out var buffer, out var shape)
            || !GraphicsBufferTransfer.TryCreatePutAnchor(
                _host,
                _host.CurrentGraphicsX,
                _host.CurrentGraphicsY,
                x,
                y,
                (flags & 1) != 0,
                out var anchor)
            || !GraphicsBufferTransfer.TryPutBuffer(
                _host,
                buffer,
                shape,
                anchor,
                (BoundPutActionKind)(flags >> 1)))
        {
            ReportInvalidGraphicsTransfer("PUT", location);
            throw new RuntimeFailureException();
        }
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
        RequireDrawableSurfaceActive("PAINT", location);
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

    private void ExecuteDrawRectInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("DRAWRECT", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var stack = frame.EvaluationStack;
        var color = stack.PopUnchecked().AsInteger();
        var height = GetWholeNumberUnchecked(stack.PopUnchecked());
        var width = GetWholeNumberUnchecked(stack.PopUnchecked());
        var y = GetWholeNumberUnchecked(stack.PopUnchecked());
        var x = GetWholeNumberUnchecked(stack.PopUnchecked());
        if (width <= 0 || height <= 0)
        {
            ValidateCanvasRectangle("DRAWRECT", width, height, ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.DrawRect(x, y, width, height, color);
        _host.SetGraphicsCurrentPosition(x, y);
    }

    private void ExecuteFillRectInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("FILLRECT", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var stack = frame.EvaluationStack;
        var color = stack.PopUnchecked().AsInteger();
        var height = GetWholeNumberUnchecked(stack.PopUnchecked());
        var width = GetWholeNumberUnchecked(stack.PopUnchecked());
        var y = GetWholeNumberUnchecked(stack.PopUnchecked());
        var x = GetWholeNumberUnchecked(stack.PopUnchecked());
        if (width <= 0 || height <= 0)
        {
            ValidateCanvasRectangle("FILLRECT", width, height, ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.FillRect(x, y, width, height, color);
        _host.SetGraphicsCurrentPosition(x, y);
    }

    private void ExecuteDrawLineInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("DRAWLINE", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var stack = frame.EvaluationStack;
        var color = stack.PopUnchecked().AsInteger();
        var y2 = GetWholeNumberUnchecked(stack.PopUnchecked());
        var x2 = GetWholeNumberUnchecked(stack.PopUnchecked());
        var y1 = GetWholeNumberUnchecked(stack.PopUnchecked());
        var x1 = GetWholeNumberUnchecked(stack.PopUnchecked());
        if (!GraphicsCoordinateValidation.AreSupportedLineCoordinates(x1, y1, x2, y2))
        {
            ValidateCanvasLine("DRAWLINE", x1, y1, x2, y2, ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.DrawCanvasLine(x1, y1, x2, y2, color);
        _host.SetGraphicsCurrentPosition(x2, y2);
    }

    private void ExecuteDrawCircleInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("DRAWCIRCLE", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var stack = frame.EvaluationStack;
        var color = stack.PopUnchecked().AsInteger();
        var radius = GetWholeNumberUnchecked(stack.PopUnchecked());
        var y = GetWholeNumberUnchecked(stack.PopUnchecked());
        var x = GetWholeNumberUnchecked(stack.PopUnchecked());
        if (radius < 0 || !GraphicsCoordinateValidation.IsSupportedCircleCenter(x, y))
        {
            ValidateCanvasCircle("DRAWCIRCLE", x, y, radius, ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.DrawCanvasCircle(x, y, radius, color);
        _host.SetGraphicsCurrentPosition(x, y);
    }

    private void ExecuteFillCircleInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("FILLCIRCLE", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var stack = frame.EvaluationStack;
        var color = stack.PopUnchecked().AsInteger();
        var radius = GetWholeNumberUnchecked(stack.PopUnchecked());
        var y = GetWholeNumberUnchecked(stack.PopUnchecked());
        var x = GetWholeNumberUnchecked(stack.PopUnchecked());
        if (radius < 0 || !GraphicsCoordinateValidation.IsSupportedCircleCenter(x, y))
        {
            ValidateCanvasCircle("FILLCIRCLE", x, y, radius, ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.FillCanvasCircle(x, y, radius, color);
        _host.SetGraphicsCurrentPosition(x, y);
    }

    private void ExecuteDrawTextInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        if (!_host.IsCanvasActive)
        {
            RequireCanvasActive("DRAWTEXT", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var stack = frame.EvaluationStack;
        var color = stack.PopUnchecked().AsInteger();
        var y = GetWholeNumberUnchecked(stack.PopUnchecked());
        var x = GetWholeNumberUnchecked(stack.PopUnchecked());
        var text = stack.PopUnchecked().AsString();
        if (!GraphicsCoordinateValidation.IsSupportedCoordinate(x)
            || !GraphicsCoordinateValidation.IsSupportedCoordinate(y))
        {
            ValidateCanvasPoint("DRAWTEXT", x, y, ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        _host.DrawCanvasText(text, x, y, color);
        _host.SetGraphicsCurrentPosition(x, y);
    }

    private void ExecuteDrawImageInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        if (!_host.IsScreenActive && !_host.IsCanvasActive)
        {
            RequireDrawableSurfaceActive("DRAWIMAGE", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var hasScale = instruction.GetOperand(0) != 0;
        var stack = frame.EvaluationStack;
        var heightValue = hasScale ? stack.PopUnchecked() : null;
        var widthValue = hasScale ? stack.PopUnchecked() : null;
        var yValue = stack.PopUnchecked();
        var xValue = stack.PopUnchecked();
        var imageValue = stack.PopUnchecked();
        var height = heightValue is null
            ? (long?)null
            : TryGetFastWholeNumber(heightValue, out var fastHeight) ? fastHeight : heightValue.AsLong();
        var width = widthValue is null
            ? (long?)null
            : TryGetFastWholeNumber(widthValue, out var fastWidth) ? fastWidth : widthValue.AsLong();
        var y = TryGetFastWholeNumber(yValue, out var fastY) ? fastY : yValue.AsLong();
        var x = TryGetFastWholeNumber(xValue, out var fastX) ? fastX : xValue.AsLong();
        if (imageValue.Type != TypeSymbol.Image || imageValue.Value is not RuntimeImageValue image)
        {
            RequireImage("DRAWIMAGE", imageValue, ResolveLocation(frame.Routine.Id, instructionIndex));
            return;
        }

        var targetWidth = width ?? image.Width;
        var targetHeight = height ?? image.Height;
        if (targetWidth <= 0 || targetHeight <= 0 || targetWidth > int.MaxValue || targetHeight > int.MaxValue)
        {
            var location = ResolveLocation(frame.Routine.Id, instructionIndex);
            ValidateDrawImageSize(targetWidth, targetHeight, location);
            return;
        }

        _host.DrawImage(
            image,
            x,
            y,
            (int)targetWidth,
            (int)targetHeight);
        _host.SetGraphicsCurrentPosition(x, y);
    }

    private void ExecuteDrawSpriteInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        if (!_host.IsScreenActive && !_host.IsCanvasActive)
        {
            RequireDrawableSurfaceActive("DRAWSPRITE", ResolveLocation(frame.Routine.Id, instructionIndex));
        }

        var hasScale = instruction.GetOperand(0) != 0;
        var stack = frame.EvaluationStack;
        var heightValue = hasScale ? stack.PopUnchecked() : null;
        var widthValue = hasScale ? stack.PopUnchecked() : null;
        var yValue = stack.PopUnchecked();
        var xValue = stack.PopUnchecked();
        var spriteFrameValue = stack.PopUnchecked();
        var spriteValue = stack.PopUnchecked();
        var height = heightValue is null
            ? (long?)null
            : TryGetFastWholeNumber(heightValue, out var fastHeight) ? fastHeight : heightValue.AsLong();
        var width = widthValue is null
            ? (long?)null
            : TryGetFastWholeNumber(widthValue, out var fastWidth) ? fastWidth : widthValue.AsLong();
        var y = TryGetFastWholeNumber(yValue, out var fastY) ? fastY : yValue.AsLong();
        var x = TryGetFastWholeNumber(xValue, out var fastX) ? fastX : xValue.AsLong();
        var spriteFrame = TryGetFastWholeNumber(spriteFrameValue, out var fastSpriteFrame) ? fastSpriteFrame : spriteFrameValue.AsLong();
        if (spriteValue.Type != TypeSymbol.Sprite
            || spriteValue.Value is not RuntimeSpriteValue sprite
            || !sprite.HasFrames)
        {
            RequireSprite("DRAWSPRITE", spriteValue, ResolveLocation(frame.Routine.Id, instructionIndex));
            return;
        }

        var targetWidth = width ?? sprite.FrameWidth;
        var targetHeight = height ?? sprite.FrameHeight;
        if (targetWidth <= 0
            || targetHeight <= 0
            || targetWidth > int.MaxValue
            || targetHeight > int.MaxValue
            || spriteFrame < 0
            || spriteFrame >= sprite.FrameCount)
        {
            var location = ResolveLocation(frame.Routine.Id, instructionIndex);
            ValidateDrawSpriteSize(targetWidth, targetHeight, location);
            ValidateSpriteFrame(sprite, spriteFrame, location);
            return;
        }

        _host.DrawSprite(
            sprite,
            (int)spriteFrame,
            x,
            y,
            (int)targetWidth,
            (int)targetHeight);
        _host.SetGraphicsCurrentPosition(x, y);
    }

    private RuntimeValue EvaluateLoadImageBuiltin(string path, TextLocation location)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            ReportBuiltInArgumentError("LOADIMAGE", "path", "не должен быть пустым.", location);
            throw new RuntimeFailureException();
        }

        var image = _host.LoadImage(path);
        if (image is null)
        {
            ReportInvalidRuntimeState($"LOADIMAGE: не удалось загрузить изображение '{path}'.", location);
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromImage(image);
    }

    private RuntimeValue EvaluateLoadSpriteBuiltin(string path, long frameWidth, long frameHeight, TextLocation location)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            ReportBuiltInArgumentError("LOADSPRITE", "path", "не должен быть пустым.", location);
            throw new RuntimeFailureException();
        }

        ValidateSpriteFrameSize(frameWidth, frameHeight, location);
        var sprite = _host.LoadSprite(
            path,
            ToInt32Checked(frameWidth, location, "Ширина кадра LOADSPRITE"),
            ToInt32Checked(frameHeight, location, "Высота кадра LOADSPRITE"));
        if (sprite is null)
        {
            ReportInvalidRuntimeState($"LOADSPRITE: не удалось загрузить спрайт '{path}'.", location);
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromSprite(sprite);
    }

    private RuntimeValue EvaluateTextWidthBuiltin(string text, TextLocation location)
    {
        RequireCanvasActive("TEXTWIDTH", location);
        return RuntimeValue.FromInteger(CanvasTextMetrics.MeasureWidth(text));
    }

    private RuntimeValue EvaluateTextHeightBuiltin(string text, TextLocation location)
    {
        RequireCanvasActive("TEXTHEIGHT", location);
        return RuntimeValue.FromInteger(CanvasTextMetrics.MeasureHeight(text));
    }

    private RuntimeImageValue RequireImage(string operationName, RuntimeValue value, TextLocation location)
    {
        var image = value.AsImage();
        if (image is not null)
        {
            return image;
        }

        ReportInvalidRuntimeState($"{operationName}: значение IMAGE пустое.", location);
        throw new RuntimeFailureException();
    }

    private RuntimeSpriteValue RequireSprite(string operationName, RuntimeValue value, TextLocation location)
    {
        var sprite = value.AsSprite();
        if (sprite is not null && sprite.HasFrames)
        {
            return sprite;
        }

        ReportInvalidRuntimeState($"{operationName}: значение SPRITE пустое или не содержит доступных кадров.", location);
        throw new RuntimeFailureException();
    }

    private static int GetImageWidth(RuntimeValue value) => value.AsImage()?.Width ?? 0;

    private static int GetImageHeight(RuntimeValue value) => value.AsImage()?.Height ?? 0;

    private static int GetSpriteWidth(RuntimeValue value) => value.AsSprite()?.FrameWidth ?? 0;

    private static int GetSpriteHeight(RuntimeValue value) => value.AsSprite()?.FrameHeight ?? 0;

    private static int GetSpriteCount(RuntimeValue value) => value.AsSprite()?.FrameCount ?? 0;

    private int EvaluateRgbBuiltin(long red, long green, long blue, TextLocation location)
    {
        if (IsRgbComponent(red) && IsRgbComponent(green) && IsRgbComponent(blue))
        {
            return ((int)red << 16) | ((int)green << 8) | (int)blue;
        }

        ReportBuiltInArgumentError("RGB", "red/green/blue", "должны быть в диапазоне 0..255.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateDrawImageSize(long width, long height, TextLocation location)
    {
        if (width > 0 && height > 0)
        {
            return;
        }

        ReportBuiltInArgumentError("DRAWIMAGE", "width/height", "должны быть положительными целыми значениями.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateSpriteFrameSize(long width, long height, TextLocation location)
    {
        if (width > 0 && height > 0)
        {
            return;
        }

        ReportBuiltInArgumentError("LOADSPRITE", "frameWidth/frameHeight", "должны быть положительными целыми значениями.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateDrawSpriteSize(long width, long height, TextLocation location)
    {
        if (width > 0 && height > 0)
        {
            return;
        }

        ReportBuiltInArgumentError("DRAWSPRITE", "width/height", "должны быть положительными целыми значениями.", location);
        throw new RuntimeFailureException();
    }

    private static bool IsRgbComponent(long value) => value is >= 0 and <= 255;

    private void ValidateCanvasSize(long width, long height, TextLocation location)
    {
        if (width > 0 && height > 0 && width <= int.MaxValue && height <= int.MaxValue)
        {
            return;
        }

        ReportBuiltInArgumentError("CANVAS", "width/height", $"должны быть положительными целыми значениями не больше {int.MaxValue}.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateCanvasRectangle(string operationName, long width, long height, TextLocation location)
    {
        if (width > 0 && height > 0)
        {
            return;
        }

        ReportBuiltInArgumentError(operationName, "width/height", "должны быть положительными целыми значениями.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateCanvasPoint(string operationName, long x, long y, TextLocation location)
    {
        if (GraphicsCoordinateValidation.IsSupportedCoordinate(x)
            && GraphicsCoordinateValidation.IsSupportedCoordinate(y))
        {
            return;
        }

        ReportInvalidGraphicsCoordinates(operationName, location);
        throw new RuntimeFailureException();
    }

    private void ValidateCanvasLine(string operationName, long x1, long y1, long x2, long y2, TextLocation location)
    {
        if (GraphicsCoordinateValidation.AreSupportedLineCoordinates(x1, y1, x2, y2))
        {
            return;
        }

        ReportInvalidGraphicsCoordinates(operationName, location);
        throw new RuntimeFailureException();
    }

    private void ValidateCanvasCircle(string operationName, long x, long y, long radius, TextLocation location)
    {
        if (radius >= 0 && GraphicsCoordinateValidation.IsSupportedCircleCenter(x, y))
        {
            return;
        }

        ReportBuiltInArgumentError(operationName, "x/y/radius", "координаты должны оставаться в допустимом диапазоне, а радиус должен быть неотрицательным.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateSpriteFrame(RuntimeSpriteValue sprite, long frame, TextLocation location)
    {
        if (frame >= 0 && frame < sprite.FrameCount)
        {
            return;
        }

        ReportBuiltInArgumentError("DRAWSPRITE", "frame", "должен указывать существующий кадр спрайта.", location);
        throw new RuntimeFailureException();
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

    private long GetWholeNumberUnchecked(RuntimeValue value)
    {
        return TryGetFastWholeNumber(value, out var result)
            ? result
            : value.AsLong();
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

    private void ReportInvalidTextViewport(string instructionName, TextLocation location)
    {
        ReportBuiltInArgumentError(
            instructionName,
            "row",
            "верхняя и нижняя строки VIEW PRINT должны задавать корректную текстовую область внутри текущего SCREEN.",
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

    private bool TryApplyCanvasViewport(long left, long top, long rightExclusive, long bottomExclusive)
    {
        if (!GraphicsCoordinateValidation.AreSupportedLineCoordinates(left, top, rightExclusive, bottomExclusive))
        {
            return false;
        }

        if (_host.ScreenWidth <= 0 || _host.ScreenHeight <= 0)
        {
            return false;
        }

        if (rightExclusive <= left || bottomExclusive <= top)
        {
            return false;
        }

        var clippedLeft = Math.Max(0L, left);
        var clippedTop = Math.Max(0L, top);
        var clippedRightExclusive = Math.Min((long)_host.ScreenWidth, rightExclusive);
        var clippedBottomExclusive = Math.Min((long)_host.ScreenHeight, bottomExclusive);
        if (clippedRightExclusive <= clippedLeft || clippedBottomExclusive <= clippedTop)
        {
            return false;
        }

        return _host.SetViewport(
            clippedLeft,
            clippedTop,
            clippedRightExclusive - 1,
            clippedBottomExclusive - 1,
            fillColor: null,
            borderColor: null,
            useScreenCoordinates: true);
    }

    private void ReportInvalidCanvasViewport(string instructionName, TextLocation location)
    {
        ReportBuiltInArgumentError(
            instructionName,
            "x/y",
            "координаты VIEWPORT должны задавать непустой прямоугольник CANVAS; после обрезки по холсту он не должен становиться пустым.",
            location);
    }

    private RuntimeValue EvaluatePointBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        return arguments.Count switch
        {
            1 => RuntimeValue.FromInteger(EvaluatePointCoordinateWithScreen(arguments[0].AsInteger(), location)),
            2 => RuntimeValue.FromInteger(EvaluatePointPixelWithDrawableSurface(arguments[0].AsLong(), arguments[1].AsLong(), location)),
            _ => RuntimeValue.FromInteger(0)
        };
    }

    private int EvaluatePointCoordinateWithScreen(int selector, TextLocation location)
    {
        RequireScreenActive("POINT", location);
        return EvaluatePointCoordinate(selector, location);
    }

    private int EvaluatePointPixelWithDrawableSurface(long x, long y, TextLocation location)
    {
        RequireDrawableSurfaceActive("POINT", location);
        return _host.GetPixel(x, y);
    }

    private RuntimeValue EvaluatePMapBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        RequireScreenActive("PMAP", location);
        var coordinate = arguments[0].AsDouble();
        var selector = arguments[1].AsInteger();
        if (!double.IsFinite(coordinate))
        {
            throw CreatePMapCoordinateFailure(location);
        }

        if (selector is < 0 or > 3)
        {
            throw CreatePMapSelectorFailure(location);
        }

        if (!GraphicsCoordinateSpaceMath.TryMapPMapCoordinate(_host, coordinate, selector, out var mapped))
        {
            throw CreatePMapTransformFailure(location);
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

    private void RequireCanvasActive(string operationName, TextLocation location)
    {
        if (_host.IsCanvasActive)
        {
            return;
        }

        ReportRuntimeDiagnostic("MB3010", $"{operationName} требует активного графического режима CANVAS.", location);
        throw new RuntimeFailureException();
    }

    private void RequireDrawableSurfaceActive(string operationName, TextLocation location)
    {
        if (_host.IsScreenActive || _host.IsCanvasActive)
        {
            return;
        }

        ReportRuntimeDiagnostic("MB3010", $"{operationName} требует активного графического режима SCREEN или CANVAS.", location);
        throw new RuntimeFailureException();
    }
}
