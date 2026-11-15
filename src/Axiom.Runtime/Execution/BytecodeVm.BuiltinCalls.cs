using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteBuiltinCall(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var builtinId = instruction.GetOperand(0);
        var argumentCount = instruction.GetOperand(1);

        if (!_prepared.TryGetBuiltinExecutionInfo(builtinId, out var executionInfo))
        {
            ReportInvalidRuntimeState(
                $"Не найден built-in с идентификатором {builtinId}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        // Внешняя операция может изменить состояние до отказа. Граница истории
        // устанавливается до вызова, поэтому обработанная ошибка не разрешает
        // повторить уже выполненную часть операции при обратном шаге.
        if (!executionInfo.IsReverseExecutionSafe)
        {
            InvalidateReverseExecutionHistory();
        }

        // Быстрый путь привязан к функции при подготовке модуля: вызов сразу
        // попадает в свой обработчик, а не прогоняет лестницу чужих проверок.
        switch (executionInfo.FastPath)
        {
            case BuiltinFastPathKind.CIntImmediate:
                if (argumentCount == 1 && executionInfo.ReturnsValue && executionInfo.IsReverseExecutionSafe
                    && TryHandleCIntImmediate(frame, executionInfo, instructionIndex))
                    return;
                break;

            case BuiltinFastPathKind.CLngImmediate:
                if (argumentCount == 1 && executionInfo.ReturnsValue && executionInfo.IsReverseExecutionSafe
                    && TryHandleCLngImmediate(frame))
                    return;
                break;

            case BuiltinFastPathKind.AbsImmediate:
                if (argumentCount == 1 && executionInfo.ReturnsValue && executionInfo.IsReverseExecutionSafe
                    && TryHandleAbsImmediate(frame))
                    return;
                break;

            case BuiltinFastPathKind.ArrayPushImmediate:
                if (argumentCount == 2 && !executionInfo.ReturnsValue && executionInfo.IsReverseExecutionSafe
                    && TryHandleArrayPushImmediate(frame))
                    return;
                break;

            case BuiltinFastPathKind.ImmediateDoubleMath:
                if (argumentCount == 1 && executionInfo.ReturnsValue && executionInfo.IsReverseExecutionSafe
                    && frame.EvaluationStack.TryGetImmediateDouble(out var mathOperand)
                    && TryComputeImmediateDoubleMath(executionInfo.CanonicalId, mathOperand, out var mathResult))
                {
                    frame.EvaluationStack.ReplaceTopWithDouble(mathResult);
                    return;
                }

                break;

            case BuiltinFastPathKind.FastGraphics:
                if (TryExecuteFastGraphicsBuiltin(frame, executionInfo, instructionIndex, out var fastResult))
                {
                    if (executionInfo.ReturnsValue)
                    {
                        frame.EvaluationStack.PushUnchecked(fastResult);
                    }

                    if (!executionInfo.IsReverseExecutionSafe)
                    {
                        InvalidateReverseExecutionHistory();
                    }

                    return;
                }

                break;

            case BuiltinFastPathKind.ArrayCallbackDirect:
            {
                var callbackLocation = executionInfo.Execution.RequiresLocation
                    ? ResolveLocation(frame.Routine.Id, instructionIndex)
                    : FallbackRuntimeLocation;
                if (executionInfo.ReturnsValue && executionInfo.IsReverseExecutionSafe
                    && TryExecuteArrayCallbackDirect(frame, executionInfo.CanonicalId, instructionIndex, callbackLocation))
                    return;
                break;
            }

            case BuiltinFastPathKind.None:
            default:
                break;
        }

        var location = executionInfo.Execution.RequiresLocation
            ? ResolveLocation(frame.Routine.Id, instructionIndex)
            : FallbackRuntimeLocation;
        if (executionInfo.ReturnsValue && executionInfo.IsReverseExecutionSafe
            && TryExecuteArrayCallbackDirect(frame, executionInfo.CanonicalId, instructionIndex, location))
            return;

        RuntimeValue result;
        try
        {
            result = ExecuteBuiltin(frame, executionInfo.Execution, argumentCount, instructionIndex, location);
        }
        catch (VmCallbackSuspension suspension)
        {
            // Замыкание создаётся только здесь: если лямбда объявлена прямо в
            // catch, Roslyn поднимает конструкцию display-класса наверх метода
            // (захватываемые переменные объявлены до try), и каждый вызов
            // встроенной функции аллоцирует его зря.
            SetBuiltinCompletion(suspension, frame, executionInfo.ReturnsValue);
            throw;
        }

        if (executionInfo.ReturnsValue)
        {
            frame.EvaluationStack.PushUnchecked(result);
        }

        if (!executionInfo.IsReverseExecutionSafe)
        {
            InvalidateReverseExecutionHistory();
        }
    }

    // CINT над непосредственным INTEGER не меняет значение и не требует упаковки;
    // над LONG и DOUBLE — та же проверка диапазона и та же диагностика, что и в
    // общем пути, без материализации.
    private bool TryHandleCIntImmediate(BytecodeCallFrame frame, BuiltinExecutionInfo executionInfo, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        if (stack.TryGetImmediateInteger(out _))
            return true;

        if (stack.TryGetImmediateLong(out var longForCint))
        {
            stack.ReplaceTopWithInteger(RuntimeArithmeticHelpers.ToInt32Checked(
                longForCint,
                ResolveLocation(frame.Routine.Id, instructionIndex),
                "Результат CINT(value)",
                _reportOverflow));
            return true;
        }

        if (stack.TryGetImmediateDouble(out var doubleForCint))
        {
            stack.ReplaceTopWithInteger(RuntimeArithmeticHelpers.ToInt32Checked(
                doubleForCint,
                ResolveLocation(frame.Routine.Id, instructionIndex),
                "Результат CINT(value)",
                _reportOverflow));
            return true;
        }

        return false;
    }

    // CLNG над непосредственным LONG не меняет значение; над INTEGER расширяет
    // вид без материализации; над DOUBLE и прочими типами — общий путь.
    private bool TryHandleCLngImmediate(BytecodeCallFrame frame)
    {
        var stack = frame.EvaluationStack;
        if (stack.TryGetImmediateLong(out _))
            return true;

        if (stack.TryGetImmediateInteger(out var integerForLong))
        {
            stack.ReplaceTopWithLong(integerForLong);
            return true;
        }

        return false;
    }

    private bool TryHandleAbsImmediate(BytecodeCallFrame frame)
    {
        if (frame.EvaluationStack.TryGetImmediateInteger(out var integerValue) && integerValue != int.MinValue)
        {
            frame.EvaluationStack.ReplaceImmediateInteger(integerValue < 0 ? -integerValue : integerValue);
            return true;
        }

        return false;
    }

    // ARRAYPUSH с непосредственным значением добавляет элемент в плотное
    // типизированное хранилище без материализации аргумента. Несовпадение
    // формы (не одномерный корневой массив, вид, неплотное хранилище) —
    // общий путь с прежними проверками.
    private bool TryHandleArrayPushImmediate(BytecodeCallFrame frame)
    {
        if (frame.EvaluationStack.PeekVmValue(1).Object is not { } peekedPushTarget
            || peekedPushTarget.Type is not ArrayTypeSymbol
            || peekedPushTarget.Value is not RuntimeArrayValue pushArray
            || pushArray.Rank != 1 || pushArray.IsView || !pushArray.IsAllocated
            || pushArray.UpperBound >= int.MaxValue)
        {
            return false;
        }

        var stack = frame.EvaluationStack;
        if (pushArray.ElementType == TypeSymbol.Integer
            && stack.TryGetImmediateInteger(out var pushInteger)
            && pushArray.TryAppendOneDimensionalInteger(pushInteger))
        {
            stack.DiscardTop(2);
            return true;
        }

        if (pushArray.ElementType == TypeSymbol.Long
            && stack.TryGetImmediateLong(out var pushLong)
            && pushArray.TryAppendOneDimensionalLong(pushLong))
        {
            stack.DiscardTop(2);
            return true;
        }

        if (pushArray.ElementType == TypeSymbol.Double
            && stack.TryGetImmediateDouble(out var pushDouble)
            && pushArray.TryAppendOneDimensionalDouble(pushDouble))
        {
            stack.DiscardTop(2);
            return true;
        }

        return false;
    }

    private bool TryExecuteArrayCallbackDirect(BytecodeCallFrame frame, BuiltinFunctionId builtin,
        int instructionIndex, TextLocation location)
    {
        if (builtin is not (BuiltinFunctionId.ArrayEvery or BuiltinFunctionId.ArraySome
            or BuiltinFunctionId.ArrayFilter or BuiltinFunctionId.ArrayMap or BuiltinFunctionId.ArrayReduce))
            return false;

        // Порядок извлечения совпадает с общей диспетчеризацией Binary/Ternary.
        var last = PopValue(frame, instructionIndex);
        var middle = PopValue(frame, instructionIndex);
        var first = builtin == BuiltinFunctionId.ArrayReduce ? PopValue(frame, instructionIndex) : middle;
        var step = builtin switch
        {
            BuiltinFunctionId.ArrayEvery => EvaluateArrayEveryBuiltin(first, last, location),
            BuiltinFunctionId.ArraySome => EvaluateArraySomeBuiltin(first, last, location),
            BuiltinFunctionId.ArrayFilter => EvaluateArrayFilterBuiltin(first, last, location),
            BuiltinFunctionId.ArrayMap => EvaluateArrayMapBuiltin(first, last, location),
            BuiltinFunctionId.ArrayReduce => EvaluateArrayReduceBuiltin(first, middle, last, location),
            _ => throw new InvalidOperationException()
        };
        if (step is VmCallbackComplete complete)
            frame.EvaluationStack.PushUnchecked(complete.Value);
        else
            ResumeCallbackChain(step, CreateCallbackCompletion(frame));
        return true;
    }

    private static Action<RuntimeValue> CreateCallbackCompletion(BytecodeCallFrame frame)
        => value => frame.EvaluationStack.PushUnchecked(value);

    /// <summary>
    /// Назначает завершение приостановленного встроенного вызова. Отдельный
    /// метод удерживает конструкцию замыкания внутри вызова хелпера — только
    /// на пути приостановки, вне горячего пути обычных вызовов.
    /// </summary>
    private static void SetBuiltinCompletion(
        VmCallbackSuspension suspension,
        BytecodeCallFrame frame,
        bool returnsValue)
    {
        suspension.Completion = value =>
        {
            if (returnsValue)
            {
                frame.EvaluationStack.PushUnchecked(value);
            }
        };
    }

    private bool TryExecuteFastGraphicsBuiltin(
        BytecodeCallFrame frame,
        BuiltinExecutionInfo executionInfo,
        int instructionIndex,
        out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        switch (executionInfo.CanonicalId)
        {
            case BuiltinFunctionId.Rgb:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 3)
                {
                    return false;
                }

                var blue = stack.PeekUnchecked();
                var green = stack.PeekUnchecked(1);
                var red = stack.PeekUnchecked(2);
                if (red.Type != TypeSymbol.Integer && red.Type != TypeSymbol.Long)
                {
                    return false;
                }

                if (green.Type != TypeSymbol.Integer && green.Type != TypeSymbol.Long)
                {
                    return false;
                }

                if (blue.Type != TypeSymbol.Integer && blue.Type != TypeSymbol.Long)
                {
                    return false;
                }

                var redValue = red.Type == TypeSymbol.Integer ? GetIntegerUnchecked(red) : GetLongUnchecked(red);
                var greenValue = green.Type == TypeSymbol.Integer ? GetIntegerUnchecked(green) : GetLongUnchecked(green);
                var blueValue = blue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(blue) : GetLongUnchecked(blue);
                if (!IsRgbComponent(redValue) || !IsRgbComponent(greenValue) || !IsRgbComponent(blueValue))
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(((int)redValue << 16) | ((int)greenValue << 8) | (int)blueValue);
                return true;
            }

            case BuiltinFunctionId.LoadImage:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var pathValue = stack.PeekUnchecked();
                if (pathValue.Type != TypeSymbol.String)
                {
                    return false;
                }

                var path = GetStringUnchecked(pathValue);
                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                var image = _host.LoadImage(path);
                if (image is null)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromImage(image);
                return true;
            }

            case BuiltinFunctionId.LoadSprite:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 3)
                {
                    return false;
                }

                var frameHeightValue = stack.PeekUnchecked();
                var frameWidthValue = stack.PeekUnchecked(1);
                var pathValue = stack.PeekUnchecked(2);
                if (pathValue.Type != TypeSymbol.String)
                {
                    return false;
                }

                var path = GetStringUnchecked(pathValue);
                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                if (frameWidthValue.Type != TypeSymbol.Integer && frameWidthValue.Type != TypeSymbol.Long)
                {
                    return false;
                }

                if (frameHeightValue.Type != TypeSymbol.Integer && frameHeightValue.Type != TypeSymbol.Long)
                {
                    return false;
                }

                var frameWidth = frameWidthValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(frameWidthValue) : GetLongUnchecked(frameWidthValue);
                var frameHeight = frameHeightValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(frameHeightValue) : GetLongUnchecked(frameHeightValue);
                if (frameWidth <= 0 || frameHeight <= 0 || frameWidth > int.MaxValue || frameHeight > int.MaxValue)
                {
                    return false;
                }

                var sprite = _host.LoadSprite(path, (int)frameWidth, (int)frameHeight);
                if (sprite is null)
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromSprite(sprite);
                return true;
            }

            case BuiltinFunctionId.LoadTileset:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 3)
                {
                    return false;
                }

                var tileHeightValue = stack.PeekUnchecked();
                var tileWidthValue = stack.PeekUnchecked(1);
                var pathValue = stack.PeekUnchecked(2);
                if (pathValue.Type != TypeSymbol.String)
                {
                    return false;
                }

                var path = GetStringUnchecked(pathValue);
                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                if ((tileWidthValue.Type != TypeSymbol.Integer && tileWidthValue.Type != TypeSymbol.Long)
                    || (tileHeightValue.Type != TypeSymbol.Integer && tileHeightValue.Type != TypeSymbol.Long))
                {
                    return false;
                }

                var tileWidth = tileWidthValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(tileWidthValue) : GetLongUnchecked(tileWidthValue);
                var tileHeight = tileHeightValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(tileHeightValue) : GetLongUnchecked(tileHeightValue);
                if (tileWidth <= 0 || tileHeight <= 0 || tileWidth > int.MaxValue || tileHeight > int.MaxValue)
                {
                    return false;
                }

                var tileset = _host.LoadTileset(path, (int)tileWidth, (int)tileHeight);
                if (tileset is null)
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromTileset(tileset);
                return true;
            }

            case BuiltinFunctionId.LoadTilesetMargin:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 4)
                {
                    return false;
                }

                var marginValue = stack.PeekUnchecked();
                var tileHeightValue = stack.PeekUnchecked(1);
                var tileWidthValue = stack.PeekUnchecked(2);
                var pathValue = stack.PeekUnchecked(3);
                if (pathValue.Type != TypeSymbol.String)
                {
                    return false;
                }

                var path = GetStringUnchecked(pathValue);
                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                if ((tileWidthValue.Type != TypeSymbol.Integer && tileWidthValue.Type != TypeSymbol.Long)
                    || (tileHeightValue.Type != TypeSymbol.Integer && tileHeightValue.Type != TypeSymbol.Long)
                    || (marginValue.Type != TypeSymbol.Integer && marginValue.Type != TypeSymbol.Long))
                {
                    return false;
                }

                var tileWidth = tileWidthValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(tileWidthValue) : GetLongUnchecked(tileWidthValue);
                var tileHeight = tileHeightValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(tileHeightValue) : GetLongUnchecked(tileHeightValue);
                var margin = marginValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(marginValue) : GetLongUnchecked(marginValue);
                if (tileWidth <= 0 || tileHeight <= 0 || margin < 0 || tileWidth > int.MaxValue || tileHeight > int.MaxValue || margin > int.MaxValue)
                {
                    return false;
                }

                var tileset = _host.LoadTileset(path, (int)tileWidth, (int)tileHeight, (int)margin);
                if (tileset is null)
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromTileset(tileset);
                return true;
            }

            case BuiltinFunctionId.LoadTilesetMarginSpacing:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 5)
                {
                    return false;
                }

                var spacingValue = stack.PeekUnchecked();
                var marginValue = stack.PeekUnchecked(1);
                var tileHeightValue = stack.PeekUnchecked(2);
                var tileWidthValue = stack.PeekUnchecked(3);
                var pathValue = stack.PeekUnchecked(4);
                if (pathValue.Type != TypeSymbol.String)
                {
                    return false;
                }

                var path = GetStringUnchecked(pathValue);
                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                if ((tileWidthValue.Type != TypeSymbol.Integer && tileWidthValue.Type != TypeSymbol.Long)
                    || (tileHeightValue.Type != TypeSymbol.Integer && tileHeightValue.Type != TypeSymbol.Long)
                    || (marginValue.Type != TypeSymbol.Integer && marginValue.Type != TypeSymbol.Long)
                    || (spacingValue.Type != TypeSymbol.Integer && spacingValue.Type != TypeSymbol.Long))
                {
                    return false;
                }

                var tileWidth = tileWidthValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(tileWidthValue) : GetLongUnchecked(tileWidthValue);
                var tileHeight = tileHeightValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(tileHeightValue) : GetLongUnchecked(tileHeightValue);
                var margin = marginValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(marginValue) : GetLongUnchecked(marginValue);
                var spacing = spacingValue.Type == TypeSymbol.Integer ? GetIntegerUnchecked(spacingValue) : GetLongUnchecked(spacingValue);
                if (tileWidth <= 0 || tileHeight <= 0 || margin < 0 || spacing < 0
                    || tileWidth > int.MaxValue || tileHeight > int.MaxValue || margin > int.MaxValue || spacing > int.MaxValue)
                {
                    return false;
                }

                var tileset = _host.LoadTileset(path, (int)tileWidth, (int)tileHeight, (int)margin, (int)spacing);
                if (tileset is null)
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                stack.PopUnchecked();
                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromTileset(tileset);
                return true;
            }

            case BuiltinFunctionId.LoadTilemap:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var pathValue = stack.PeekUnchecked();
                if (pathValue.Type != TypeSymbol.String)
                {
                    return false;
                }

                var path = GetStringUnchecked(pathValue);
                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                result = EvaluateLoadTilemapBuiltin(path, ResolveLocation(frame.Routine.Id, instructionIndex));
                stack.PopUnchecked();
                return true;
            }

            case BuiltinFunctionId.ImageWidth:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var imageValue = stack.PeekUnchecked();
                if (imageValue.Type != TypeSymbol.Image)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetImageWidthFast(imageValue));
                return true;
            }

            case BuiltinFunctionId.ImageHeight:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var imageValue = stack.PeekUnchecked();
                if (imageValue.Type != TypeSymbol.Image)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetImageHeightFast(imageValue));
                return true;
            }

            case BuiltinFunctionId.SpriteWidth:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var spriteValue = stack.PeekUnchecked();
                if (spriteValue.Type != TypeSymbol.Sprite)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetSpriteWidthFast(spriteValue));
                return true;
            }

            case BuiltinFunctionId.SpriteHeight:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var spriteValue = stack.PeekUnchecked();
                if (spriteValue.Type != TypeSymbol.Sprite)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetSpriteHeightFast(spriteValue));
                return true;
            }

            case BuiltinFunctionId.SpriteCount:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var spriteValue = stack.PeekUnchecked();
                if (spriteValue.Type != TypeSymbol.Sprite)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetSpriteCountFast(spriteValue));
                return true;
            }

            case BuiltinFunctionId.TileCount:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var tilesetValue = stack.PeekUnchecked();
                if (tilesetValue.Type != TypeSymbol.Tileset)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetTileCountFast(tilesetValue));
                return true;
            }

            case BuiltinFunctionId.TileWidth:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var tilesetValue = stack.PeekUnchecked();
                if (tilesetValue.Type != TypeSymbol.Tileset)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetTileWidthFast(tilesetValue));
                return true;
            }

            case BuiltinFunctionId.TileHeight:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var tilesetValue = stack.PeekUnchecked();
                if (tilesetValue.Type != TypeSymbol.Tileset)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetTileHeightFast(tilesetValue));
                return true;
            }

            case BuiltinFunctionId.MapWidth:
            case BuiltinFunctionId.MapHeight:
            case BuiltinFunctionId.MapPixelWidth:
            case BuiltinFunctionId.MapPixelHeight:
            case BuiltinFunctionId.MapTileWidth:
            case BuiltinFunctionId.MapTileHeight:
            case BuiltinFunctionId.MapLayerCount:
            {
                var stack = frame.EvaluationStack;
                if (stack.Count < 1)
                {
                    return false;
                }

                var tilemapValue = stack.PeekUnchecked();
                if (tilemapValue.Type != TypeSymbol.Tilemap)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(executionInfo.CanonicalId switch
                {
                    BuiltinFunctionId.MapWidth => GetMapWidthFast(tilemapValue),
                    BuiltinFunctionId.MapHeight => GetMapHeightFast(tilemapValue),
                    BuiltinFunctionId.MapPixelWidth => GetMapPixelWidthFast(tilemapValue),
                    BuiltinFunctionId.MapPixelHeight => GetMapPixelHeightFast(tilemapValue),
                    BuiltinFunctionId.MapTileWidth => GetMapTileWidthFast(tilemapValue),
                    BuiltinFunctionId.MapTileHeight => GetMapTileHeightFast(tilemapValue),
                    BuiltinFunctionId.MapLayerCount => GetMapLayerCountFast(tilemapValue),
                    _ => 0
                });
                return true;
            }

            default:
                return false;
        }
    }

    // Домены повторяют условия общего пути: SQR >= 0, LOG* > 0,
    // ASIN/ACOS в [-1; 1], ACOSH >= 1, ATANH в (-1; 1). Не конечный
    // результат и прочие функции дают отказ ветки: общий путь воспроизводит
    // прежние диагностики.
    private static bool TryComputeImmediateDoubleMath(BuiltinFunctionId canonicalId, double operand, out double result)
    {
        switch (canonicalId)
        {
            case BuiltinFunctionId.Sin:
                result = Math.Sin(operand);
                break;
            case BuiltinFunctionId.Cos:
                result = Math.Cos(operand);
                break;
            case BuiltinFunctionId.Tan:
                result = Math.Tan(operand);
                break;
            case BuiltinFunctionId.Atn:
                result = Math.Atan(operand);
                break;
            case BuiltinFunctionId.Cbrt:
                result = Math.Cbrt(operand);
                break;
            case BuiltinFunctionId.Exp:
                result = Math.Exp(operand);
                break;
            case BuiltinFunctionId.Asinh:
                result = Math.Asinh(operand);
                break;
            case BuiltinFunctionId.Sinh:
                result = Math.Sinh(operand);
                break;
            case BuiltinFunctionId.Cosh:
                result = Math.Cosh(operand);
                break;
            case BuiltinFunctionId.Sqr:
                if (operand < 0d) { result = 0d; return false; }
                result = Math.Sqrt(operand);
                break;
            case BuiltinFunctionId.Log:
                if (operand <= 0d) { result = 0d; return false; }
                result = Math.Log(operand);
                break;
            case BuiltinFunctionId.Log10:
                if (operand <= 0d) { result = 0d; return false; }
                result = Math.Log10(operand);
                break;
            case BuiltinFunctionId.Log2:
                if (operand <= 0d) { result = 0d; return false; }
                result = Math.Log2(operand);
                break;
            case BuiltinFunctionId.Asin:
                if (operand < -1d || operand > 1d) { result = 0d; return false; }
                result = Math.Asin(operand);
                break;
            case BuiltinFunctionId.Acos:
                if (operand < -1d || operand > 1d) { result = 0d; return false; }
                result = Math.Acos(operand);
                break;
            case BuiltinFunctionId.Acosh:
                if (operand < 1d) { result = 0d; return false; }
                result = Math.Acosh(operand);
                break;
            case BuiltinFunctionId.Atanh:
                if (operand <= -1d || operand >= 1d) { result = 0d; return false; }
                result = Math.Atanh(operand);
                break;
            default:
                result = 0d;
                return false;
        }

        if (!double.IsFinite(result))
        {
            result = 0d;
            return false;
        }

        return true;
    }

    private RuntimeValue ExecuteBuiltin(
        BytecodeCallFrame frame,
        BuiltinExecution execution,
        int argumentCount,
        int instructionIndex,
        TextLocation location)
    {
        switch (execution.Arity)
        {
            case BuiltinExecutionArity.Nullary:
                return execution.Zero!(this, location);

            case BuiltinExecutionArity.NullaryNoLocation:
                return execution.ZeroNoLocation!(this);

            case BuiltinExecutionArity.Unary:
                return execution.One!(this, PopValue(frame, instructionIndex), location);

            case BuiltinExecutionArity.UnaryNoLocation:
                return execution.OneNoLocation!(this, PopValue(frame, instructionIndex));

            case BuiltinExecutionArity.Binary:
            {
                var right = PopValue(frame, instructionIndex);
                var left = PopValue(frame, instructionIndex);
                return execution.Two!(this, left, right, location);
            }

            case BuiltinExecutionArity.BinaryNoLocation:
            {
                var right = PopValue(frame, instructionIndex);
                var left = PopValue(frame, instructionIndex);
                return execution.TwoNoLocation!(this, left, right);
            }

            case BuiltinExecutionArity.Ternary:
            {
                var third = PopValue(frame, instructionIndex);
                var second = PopValue(frame, instructionIndex);
                var first = PopValue(frame, instructionIndex);
                return execution.Three!(this, first, second, third, location);
            }

            case BuiltinExecutionArity.TernaryNoLocation:
            {
                var third = PopValue(frame, instructionIndex);
                var second = PopValue(frame, instructionIndex);
                var first = PopValue(frame, instructionIndex);
                return execution.ThreeNoLocation!(this, first, second, third);
            }

            default:
                using (var arguments = RentArguments(frame, argumentCount, instructionIndex))
                {
                    return execution.Many!(this, arguments.Span, location);
                }
        }
    }

    private RuntimeValue InvokeBuiltinFunctionValue(
        RuntimeFunctionValue functionValue,
        ReadOnlySpan<RuntimeValue> arguments,
        TextLocation callSiteLocation)
        => InvokeBuiltinFunctionValue(functionValue, arguments, callSiteLocation, false, out _);

    private RuntimeValue InvokeBuiltinFunctionValue(
        RuntimeFunctionValue functionValue,
        ReadOnlySpan<RuntimeValue> arguments,
        TextLocation callSiteLocation,
        bool allowCallbackStep,
        out VmCallbackStep? callbackStep)
    {
        callbackStep = null;
        var function = functionValue.Function;

        if (!function.IsBuiltIn)
        {
            throw new InvalidOperationException(
                $"Функция '{function.Name}' не является встроенной и не может быть вызвана синхронно.");
        }

        if (function.BuiltinId is not { } builtinId)
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Встроенная функция '{function.Name}' не содержит канонического идентификатора.",
                callSiteLocation);
        }

        var builtin = functionValue.BytecodeBuiltinBindingId is { } builtinBindingId && _prepared.TryGetBuiltin(builtinBindingId, out var directBuiltin)
            ? directBuiltin
            : _prepared.BuiltinsByFunction.TryGetValue(function, out var mappedBuiltin)
                ? mappedBuiltin
                : _prepared.BuiltinsByCanonicalId.GetValueOrDefault(builtinId);
        if (builtin is null)
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Для значения встроенной функции '{function.Name}' отсутствует запись в таблице built-in.",
                callSiteLocation);
        }

        if (!IsBuiltinReverseExecutionSafe(builtin.CanonicalId))
        {
            InvalidateReverseExecutionHistory();
        }

        // Сохраняем проверку дескриптора и прежнюю обработку повреждённой
        // арности; для остальных вызовов остаётся общий путь диспетчеризации.
        if (allowCallbackStep && _prepared.TryGetBuiltinExecutionInfo(builtin.Id, out var executionInfo))
        {
            var execution = executionInfo.Execution;
            if (execution.Arity == BuiltinExecutionArity.Binary && arguments.Length >= 2)
            {
                callbackStep = builtin.CanonicalId switch
                {
                    BuiltinFunctionId.ArrayEvery => EvaluateArrayEveryBuiltin(arguments[0], arguments[1], callSiteLocation),
                    BuiltinFunctionId.ArraySome => EvaluateArraySomeBuiltin(arguments[0], arguments[1], callSiteLocation),
                    BuiltinFunctionId.ArrayFilter => EvaluateArrayFilterBuiltin(arguments[0], arguments[1], callSiteLocation),
                    BuiltinFunctionId.ArrayMap => EvaluateArrayMapBuiltin(arguments[0], arguments[1], callSiteLocation),
                    _ => null
                };
            }
            else if (execution.Arity == BuiltinExecutionArity.Ternary && arguments.Length >= 3
                && builtin.CanonicalId == BuiltinFunctionId.ArrayReduce)
            {
                callbackStep = EvaluateArrayReduceBuiltin(arguments[0], arguments[1], arguments[2], callSiteLocation);
            }
            if (callbackStep is not null)
                return RuntimeValue.Void;
        }

        return ExecuteBuiltinFromArguments(builtin, arguments, callSiteLocation);
    }

    private RuntimeValue ExecuteBuiltinFromArguments(
        BuiltinBinding builtin,
        ReadOnlySpan<RuntimeValue> arguments,
        TextLocation location)
    {
        if (!_prepared.TryGetBuiltinExecutionInfo(builtin.Id, out var executionInfo))
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Для built-in '{builtin.Name}' отсутствует исполняемый дескриптор Bytecode VM.",
                location);
        }

        var execution = executionInfo.Execution;

        return execution.Arity switch
        {
            BuiltinExecutionArity.Nullary => execution.Zero!(this, location),
            BuiltinExecutionArity.Unary => execution.One!(this, arguments[0], location),
            BuiltinExecutionArity.Binary => execution.Two!(this, arguments[0], arguments[1], location),
            BuiltinExecutionArity.Ternary => execution.Three!(this, arguments[0], arguments[1], arguments[2], location),
            BuiltinExecutionArity.NullaryNoLocation => execution.ZeroNoLocation!(this),
            BuiltinExecutionArity.UnaryNoLocation => execution.OneNoLocation!(this, arguments[0]),
            BuiltinExecutionArity.BinaryNoLocation => execution.TwoNoLocation!(this, arguments[0], arguments[1]),
            BuiltinExecutionArity.TernaryNoLocation => execution.ThreeNoLocation!(this, arguments[0], arguments[1], arguments[2]),
            _ => execution.Many!(this, arguments, location)
        };
    }

    private static PooledArgumentBuffer RentArguments(BytecodeCallFrame frame, int argumentCount, int instructionIndex)
    {
        if (argumentCount == 0)
        {
            return PooledArgumentBuffer.Empty;
        }

        var buffer = ArrayPool<RuntimeValue>.Shared.Rent(argumentCount);
        var stack = frame.EvaluationStack;

        for (var index = argumentCount - 1; index >= 0; index--)
        {
            if (stack.Count == 0)
            {
                ArrayPool<RuntimeValue>.Shared.Return(buffer, clearArray: true);
                throw new InvalidOperationException(
                    $"Стек вычисления опустел при снятии аргументов на инструкции {instructionIndex} процедуры '{frame.Routine.Name}'.");
            }

            buffer[index] = stack.Pop();
        }

        return new PooledArgumentBuffer(buffer, argumentCount, pooled: true);
    }

    private readonly struct PooledArgumentBuffer : IDisposable
    {
        public static PooledArgumentBuffer Empty { get; } = new(Array.Empty<RuntimeValue>(), 0, pooled: false);

        private readonly RuntimeValue[] _buffer;
        private readonly int _count;
        private readonly bool _pooled;

        public PooledArgumentBuffer(RuntimeValue[] buffer, int count, bool pooled)
        {
            _buffer = buffer;
            _count = count;
            _pooled = pooled;
        }

        public ReadOnlySpan<RuntimeValue> Span => _buffer.AsSpan(0, _count);

        public void Dispose()
        {
            if (_pooled)
            {
                ArrayPool<RuntimeValue>.Shared.Return(_buffer, clearArray: true);
            }
        }
    }

    /// <summary>
    /// Вид быстрого пути встроенной функции, привязанный один раз при подготовке
    /// модуля: вызов больше не прогоняет лестницу чужих проверок CINT, ABS,
    /// ARRAYPUSH и графики, а сразу попадает в свой обработчик или в общий путь.
    /// </summary>
    internal enum BuiltinFastPathKind
    {
        None,
        CIntImmediate,
        CLngImmediate,
        AbsImmediate,
        ArrayPushImmediate,
        ImmediateDoubleMath,
        FastGraphics,
        ArrayCallbackDirect
    }

    private sealed class BuiltinExecutionInfo
    {
        public BuiltinExecutionInfo(
            BuiltinExecution execution,
            bool returnsValue,
            bool isReverseExecutionSafe,
            BuiltinFunctionId canonicalId)
            : this(execution, returnsValue, isReverseExecutionSafe, canonicalId, ClassifyFastPath(canonicalId))
        {
        }

        public BuiltinExecutionInfo(
            BuiltinExecution execution,
            bool returnsValue,
            bool isReverseExecutionSafe,
            BuiltinFunctionId canonicalId,
            BuiltinFastPathKind fastPath)
        {
            Execution = execution;
            ReturnsValue = returnsValue;
            IsReverseExecutionSafe = isReverseExecutionSafe;
            CanonicalId = canonicalId;
            FastPath = fastPath;
        }

        public BuiltinExecution Execution { get; }

        public bool ReturnsValue { get; }

        public bool IsReverseExecutionSafe { get; }

        public BuiltinFunctionId CanonicalId { get; }

        public BuiltinFastPathKind FastPath { get; }

        /// <summary>
        /// Классификация по каноническому идентификатору. Множества совпадают с
        /// обработчиками быстрых путей; прочие идентификаторы дают общий путь.
        /// </summary>
        public static BuiltinFastPathKind ClassifyFastPath(BuiltinFunctionId id)
        {
            switch (id)
            {
                case BuiltinFunctionId.CInt:
                    return BuiltinFastPathKind.CIntImmediate;
                case BuiltinFunctionId.CLng:
                    return BuiltinFastPathKind.CLngImmediate;
                case BuiltinFunctionId.Abs:
                    return BuiltinFastPathKind.AbsImmediate;
                case BuiltinFunctionId.ArrayPush:
                    return BuiltinFastPathKind.ArrayPushImmediate;
                case BuiltinFunctionId.ArrayEvery:
                case BuiltinFunctionId.ArraySome:
                case BuiltinFunctionId.ArrayFilter:
                case BuiltinFunctionId.ArrayMap:
                case BuiltinFunctionId.ArrayReduce:
                    return BuiltinFastPathKind.ArrayCallbackDirect;
                case BuiltinFunctionId.Sin:
                case BuiltinFunctionId.Cos:
                case BuiltinFunctionId.Tan:
                case BuiltinFunctionId.Atn:
                case BuiltinFunctionId.Cbrt:
                case BuiltinFunctionId.Exp:
                case BuiltinFunctionId.Asinh:
                case BuiltinFunctionId.Sinh:
                case BuiltinFunctionId.Cosh:
                case BuiltinFunctionId.Sqr:
                case BuiltinFunctionId.Log:
                case BuiltinFunctionId.Log10:
                case BuiltinFunctionId.Log2:
                case BuiltinFunctionId.Asin:
                case BuiltinFunctionId.Acos:
                case BuiltinFunctionId.Acosh:
                case BuiltinFunctionId.Atanh:
                    return BuiltinFastPathKind.ImmediateDoubleMath;
                case BuiltinFunctionId.Rgb:
                case BuiltinFunctionId.LoadImage:
                case BuiltinFunctionId.LoadSprite:
                case BuiltinFunctionId.LoadTileset:
                case BuiltinFunctionId.LoadTilesetMargin:
                case BuiltinFunctionId.LoadTilesetMarginSpacing:
                case BuiltinFunctionId.LoadTilemap:
                case BuiltinFunctionId.ImageWidth:
                case BuiltinFunctionId.ImageHeight:
                case BuiltinFunctionId.SpriteWidth:
                case BuiltinFunctionId.SpriteHeight:
                case BuiltinFunctionId.SpriteCount:
                case BuiltinFunctionId.TileCount:
                case BuiltinFunctionId.TileWidth:
                case BuiltinFunctionId.TileHeight:
                case BuiltinFunctionId.MapWidth:
                case BuiltinFunctionId.MapHeight:
                case BuiltinFunctionId.MapPixelWidth:
                case BuiltinFunctionId.MapPixelHeight:
                case BuiltinFunctionId.MapTileWidth:
                case BuiltinFunctionId.MapTileHeight:
                case BuiltinFunctionId.MapLayerCount:
                    return BuiltinFastPathKind.FastGraphics;
                default:
                    return BuiltinFastPathKind.None;
            }
        }
    }

}
