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
    private void FinishRun()
    {
        if (_stopState is not null)
        {
            return;
        }

        _debugStepBoundary = null;
        RefreshFastInstructionPathFlag();
        CloseAllOpenFiles();
        FlushPendingOutput(forceEmptyLine: false);
    }

    private TextLocation ResolveLocation(int routineId, int instructionIndex)
    {
        var cacheKey = (routineId, instructionIndex);
        if (_sourceLocationCache.TryGetValue(cacheKey, out var cachedLocation))
        {
            return cachedLocation;
        }

        var resolvedLocation = FallbackRuntimeLocation;
        if (_module.DebugInfo is not null
            && _module.DebugInfo.TryGetLocation(routineId, instructionIndex, out var location))
        {
            resolvedLocation = location;
        }
        else if (_routines.TryGetValue(routineId, out var routine))
        {
            resolvedLocation = routine.Location;
        }

        _sourceLocationCache[cacheKey] = resolvedLocation;
        return resolvedLocation;
    }

    private void AppendPendingOutput(string text)
    {
        _hasPendingOutput = true;
        RuntimeStringLimits.Append(_pendingOutput, text);
    }

    private void AppendPendingOutput(RuntimeValue value)
    {
        _hasPendingOutput = true;

        if (value.Type == TypeSymbol.Integer)
        {
            AppendInvariantValue(GetIntegerUnchecked(value));
            return;
        }

        if (value.Type == TypeSymbol.Long)
        {
            AppendInvariantValue(GetLongUnchecked(value));
            return;
        }

        if (value.Type == TypeSymbol.Double)
        {
            AppendInvariantValue(GetDoubleUnchecked(value), "G");
            return;
        }

        if (value.Type == TypeSymbol.Decimal)
        {
            AppendInvariantValue(GetDecimalUnchecked(value));
            return;
        }

        if (value.Type == TypeSymbol.String)
        {
            RuntimeStringLimits.Append(_pendingOutput, GetStringUnchecked(value));
            return;
        }

        if (value.Type == TypeSymbol.Boolean)
        {
            RuntimeStringLimits.Append(_pendingOutput, GetBooleanUnchecked(value) ? "TRUE" : "FALSE");
            return;
        }

        RuntimeStringLimits.Append(_pendingOutput, value.ToDisplayString());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendInvariantValue<T>(T value)
        where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[64];
        if (value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
        {
            RuntimeStringLimits.Append(_pendingOutput, buffer[..written]);
            return;
        }

        RuntimeStringLimits.Append(_pendingOutput, value.ToString(default, CultureInfo.InvariantCulture));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendInvariantValue<T>(T value, string format)
        where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[64];
        if (value.TryFormat(buffer, out var written, format, CultureInfo.InvariantCulture))
        {
            RuntimeStringLimits.Append(_pendingOutput, buffer[..written]);
            return;
        }

        RuntimeStringLimits.Append(_pendingOutput, value.ToString(format, CultureInfo.InvariantCulture));
    }

    private void AppendPrintSeparator()
    {
        _hasPendingOutput = true;
        RuntimeStringLimits.Append(_pendingOutput, " ");
    }

    private string GetPendingOutputPreview()
    {
        return _hasPendingOutput
            ? _pendingOutput.ToString()
            : string.Empty;
    }

    private void FlushPendingOutput(bool forceEmptyLine)
    {
        if (!_hasPendingOutput)
        {
            if (forceEmptyLine)
            {
                WriteTextLine(string.Empty);
                InvalidateReverseExecutionHistoryIfOutputIsNotReversible();
            }

            return;
        }

        WriteTextLine(_pendingOutput.ToString());
        _pendingOutput.Clear();
        _hasPendingOutput = false;
        InvalidateReverseExecutionHistoryIfOutputIsNotReversible();
    }

    private void WriteTextLine(string text)
    {
        if (_host.IsScreenOutputActive)
        {
            _host.WriteGraphicsText(text, newLine: true);
        }
        else
        {
            _host.WriteOutput(text);
        }
    }

}
