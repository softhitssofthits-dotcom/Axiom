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
    private void ExecuteBuiltinCall(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var builtinId = instruction.GetOperand(0);
        var argumentCount = instruction.GetOperand(1);

        if ((uint)builtinId >= (uint)_builtinExecutionInfosById.Length)
        {
            ReportInvalidRuntimeState(
                $"Не найден built-in с идентификатором {builtinId}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        var executionInfo = _builtinExecutionInfosById[builtinId];
        if (executionInfo is null)
        {
            ReportInvalidRuntimeState(
                $"Не найден built-in с идентификатором {builtinId}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

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

        var location = executionInfo.Execution.RequiresLocation
            ? ResolveLocation(frame.Routine.Id, instructionIndex)
            : FallbackRuntimeLocation;
        RuntimeValue result;
        try
        {
            result = ExecuteBuiltin(frame, executionInfo.Execution, argumentCount, instructionIndex, location);
        }
        catch (VmCallbackSuspension suspension)
        {
            suspension.Completion = value =>
            {
                if (executionInfo.ReturnsValue)
                {
                    frame.EvaluationStack.PushUnchecked(value);
                }
            };
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
    {
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

        var builtin = functionValue.BytecodeBuiltinBindingId is { } builtinBindingId && _builtins.TryGetValue(builtinBindingId, out var directBuiltin)
            ? directBuiltin
            : _builtinsByFunction.TryGetValue(function, out var mappedBuiltin)
                ? mappedBuiltin
                : _builtinsByCanonicalId.GetValueOrDefault(builtinId);
        if (builtin is null)
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Для значения встроенной функции '{function.Name}' отсутствует запись в таблице built-in.",
                callSiteLocation);
        }

        return ExecuteBuiltinFromArguments(builtin, arguments, callSiteLocation);
    }

    private RuntimeValue ExecuteBuiltinFromArguments(
        BuiltinBinding builtin,
        ReadOnlySpan<RuntimeValue> arguments,
        TextLocation location)
    {
        if (!_builtinExecutions.TryGetValue(builtin.Id, out var execution))
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Для built-in '{builtin.Name}' отсутствует исполняемый дескриптор Bytecode VM.",
                location);
        }

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

    private static BuiltinExecutionInfo[] CreateBuiltinExecutionInfosById(
        IReadOnlyList<BuiltinBinding> builtins,
        IReadOnlyDictionary<int, BuiltinExecution> executions)
    {
        if (builtins.Count == 0)
        {
            return Array.Empty<BuiltinExecutionInfo>();
        }

        var maxBuiltinId = builtins.Max(static builtin => builtin.Id);
        var infos = new BuiltinExecutionInfo[maxBuiltinId + 1];

        foreach (var builtin in builtins)
        {
            infos[builtin.Id] = new BuiltinExecutionInfo(
                executions[builtin.Id],
                builtin.ReturnsValue,
                IsBuiltinReverseExecutionSafe(builtin.CanonicalId),
                builtin.CanonicalId);
        }

        return infos;
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

    private sealed class BuiltinExecutionInfo
    {
        public BuiltinExecutionInfo(BuiltinExecution execution, bool returnsValue, bool isReverseExecutionSafe, BuiltinFunctionId canonicalId)
        {
            Execution = execution;
            ReturnsValue = returnsValue;
            IsReverseExecutionSafe = isReverseExecutionSafe;
            CanonicalId = canonicalId;
        }

        public BuiltinExecution Execution { get; }

        public bool ReturnsValue { get; }

        public bool IsReverseExecutionSafe { get; }

        public BuiltinFunctionId CanonicalId { get; }
    }

}
