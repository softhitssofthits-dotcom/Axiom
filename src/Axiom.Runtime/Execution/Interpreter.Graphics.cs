using Axiom.Core;
using System.Text;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private ExecutionSignal ExecuteScreen(BoundScreenStatement statement)
    {
        var mode = EvaluateExpression(statement.ModeExpression).AsInteger();
        var activePage = statement.ActivePageExpression is null ? 0 : EvaluateExpression(statement.ActivePageExpression).AsInteger();
        var visualPage = statement.VisualPageExpression is null ? activePage : EvaluateExpression(statement.VisualPageExpression).AsInteger();
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

        if (activePage is < 0 or > 3)
        {
            ReportBuiltInArgumentError("SCREEN", "activePage", "должен быть в диапазоне 0..3.", statement.Location);
            throw new RuntimeFailureException();
        }

        if (visualPage is < 0 or > 3)
        {
            ReportBuiltInArgumentError("SCREEN", "visualPage", "должен быть в диапазоне 0..3.", statement.Location);
            throw new RuntimeFailureException();
        }

        FlushPendingOutput(forceEmptyLine: false);
        _host.ActivateScreen(mode, activePage, visualPage);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecutePCopy(BoundPCopyStatement statement)
    {
        RequireScreenActive("PCOPY", statement.Location);
        var sourcePage = EvaluateExpression(statement.SourcePageExpression).AsInteger();
        var destinationPage = EvaluateExpression(statement.DestinationPageExpression).AsInteger();
        if (!_host.CopyScreenPage(sourcePage, destinationPage))
        {
            ReportBuiltInArgumentError("PCOPY", "page", "должна быть в диапазоне 0..3.", statement.Location);
            throw new RuntimeFailureException();
        }

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

    private ExecutionSignal ExecuteViewPrint(BoundViewPrintStatement statement)
    {
        RequireScreenActive("VIEW PRINT", statement.Location);
        if (statement.IsReset)
        {
            _host.ResetTextViewport();
            return ExecutionSignal.None;
        }

        var topRow = EvaluateExpression(statement.TopExpression!).AsInteger();
        var bottomRow = EvaluateExpression(statement.BottomExpression!).AsInteger();
        if (!_host.SetTextViewport(topRow, bottomRow))
        {
            ReportInvalidTextViewport("VIEW PRINT", statement.Location);
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

    private ExecutionSignal ExecutePalette(BoundPaletteStatement statement)
    {
        RequireScreenActive("PALETTE", statement.Location);
        if (statement.IsReset)
        {
            _host.ResetPalette();
            return ExecutionSignal.None;
        }

        if (statement.IsUsing)
        {
            ApplyPaletteUsing(EvaluateExpression(statement.ValuesExpression!).AsArray(), statement.Location);
            return ExecutionSignal.None;
        }

        var index = EvaluateExpression(statement.IndexExpression!).AsInteger();
        var color = EvaluateExpression(statement.ColorExpression!).AsInteger();
        _host.SetPaletteColor(index, color);
        return ExecutionSignal.None;
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

    private ExecutionSignal ExecuteDrawImage(BoundDrawImageStatement statement)
    {
        RequireScreenActive("DRAWIMAGE", statement.Location);
        var image = RequireImage("DRAWIMAGE", EvaluateExpression(statement.ImageExpression), statement.Location);
        var x = EvaluateExpression(statement.XExpression).AsLong();
        var y = EvaluateExpression(statement.YExpression).AsLong();
        var width = statement.WidthExpression is null ? image.Width : EvaluateExpression(statement.WidthExpression).AsLong();
        var height = statement.HeightExpression is null ? image.Height : EvaluateExpression(statement.HeightExpression).AsLong();
        ValidateDrawImageSize(width, height, statement.Location);
        _host.DrawImage(
            image,
            x,
            y,
            ToInt32Checked(width, statement.Location, "Ширина DRAWIMAGE"),
            ToInt32Checked(height, statement.Location, "Высота DRAWIMAGE"));
        _host.SetGraphicsCurrentPosition(x, y);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteDrawSprite(BoundDrawSpriteStatement statement)
    {
        RequireScreenActive("DRAWSPRITE", statement.Location);
        var sprite = RequireSprite("DRAWSPRITE", EvaluateExpression(statement.SpriteExpression), statement.Location);
        var frame = EvaluateExpression(statement.FrameExpression).AsLong();
        ValidateSpriteFrame(sprite, frame, statement.Location);
        var x = EvaluateExpression(statement.XExpression).AsLong();
        var y = EvaluateExpression(statement.YExpression).AsLong();
        var width = statement.WidthExpression is null ? sprite.FrameWidth : EvaluateExpression(statement.WidthExpression).AsLong();
        var height = statement.HeightExpression is null ? sprite.FrameHeight : EvaluateExpression(statement.HeightExpression).AsLong();
        ValidateDrawSpriteSize(width, height, statement.Location);
        _host.DrawSprite(
            sprite,
            ToInt32Checked(frame, statement.Location, "Номер кадра DRAWSPRITE"),
            x,
            y,
            ToInt32Checked(width, statement.Location, "Ширина DRAWSPRITE"),
            ToInt32Checked(height, statement.Location, "Высота DRAWSPRITE"));
        _host.SetGraphicsCurrentPosition(x, y);
        return ExecutionSignal.None;
    }

    private RuntimeValue EvaluateLoadImage(BoundCallExpression expression, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            ReportBuiltInArgumentError("LOADIMAGE", "path", "не должен быть пустым.", expression.Location);
            throw new RuntimeFailureException();
        }

        var image = _host.LoadImage(path);
        if (image is null)
        {
            ReportInvalidRuntimeState($"LOADIMAGE: не удалось загрузить изображение '{path}'.", expression.Location);
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromImage(image);
    }

    private RuntimeValue EvaluateLoadSprite(BoundCallExpression expression, string path, long frameWidth, long frameHeight)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            ReportBuiltInArgumentError("LOADSPRITE", "path", "не должен быть пустым.", expression.Location);
            throw new RuntimeFailureException();
        }

        ValidateSpriteFrameSize(frameWidth, frameHeight, expression.Location);
        var sprite = _host.LoadSprite(
            path,
            ToInt32Checked(frameWidth, expression.Location, "Ширина кадра LOADSPRITE"),
            ToInt32Checked(frameHeight, expression.Location, "Высота кадра LOADSPRITE"));
        if (sprite is null)
        {
            ReportInvalidRuntimeState($"LOADSPRITE: не удалось загрузить спрайт '{path}'.", expression.Location);
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromSprite(sprite);
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

    private void ValidateSpriteFrame(RuntimeSpriteValue sprite, long frame, TextLocation location)
    {
        if (frame >= 0 && frame < sprite.FrameCount)
        {
            return;
        }

        ReportBuiltInArgumentError("DRAWSPRITE", "frame", "должен указывать существующий кадр спрайта.", location);
        throw new RuntimeFailureException();
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
