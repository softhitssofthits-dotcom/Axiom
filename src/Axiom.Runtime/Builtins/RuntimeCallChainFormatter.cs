using System.Text;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public readonly record struct RuntimeCallChainFrame(
    FunctionSymbol? Function,
    string RoutineName,
    TextLocation CallSite);

/// <summary>
/// Формат цепочки вызовов BytecodeVm в диагностике времени выполнения:
/// одинаковое именование кадров (локальная функция/процедура/функция), одинаковое
/// упоминание содержащей процедуры и одинаковый порядок кадров.
/// </summary>
public static class RuntimeCallChainFormatter
{
    public static string AppendCallChain(string message, IReadOnlyList<RuntimeCallChainFrame> frames)
    {
        if (frames.Count == 0)
        {
            return message;
        }

        var builder = new StringBuilder(message);
        builder.AppendLine();
        builder.AppendLine("Цепочка вызовов:");

        foreach (var frame in frames)
        {
            builder.Append("- ");
            builder.Append(FormatRoutineLabel(frame));
            builder.Append(", вызов в ");
            builder.Append(frame.CallSite.Line);
            builder.Append(':');
            builder.Append(frame.CallSite.Column);

            if (frame.Function?.ContainingRoutine is not null)
            {
                builder.Append(", объявлена внутри ");
                builder.Append(FormatContainingRoutineLabel(frame.Function.ContainingRoutine));
            }

            builder.AppendLine(".");
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatRoutineLabel(RuntimeCallChainFrame frame)
    {
        if (frame.Function is null)
        {
            return $"процедура '{frame.RoutineName}'";
        }

        var builder = new StringBuilder();

        if (frame.Function.IsLocal)
        {
            builder.Append("локальная ");
        }

        builder.Append(frame.Function.IsSub ? "процедура" : "функция");
        builder.Append(" '");
        builder.Append(frame.Function.Name);
        builder.Append('\'');
        return builder.ToString();
    }

    public static string FormatContainingRoutineLabel(FunctionSymbol function)
    {
        var builder = new StringBuilder();

        if (function.IsLocal)
        {
            builder.Append("локальной ");
        }

        builder.Append(function.IsSub ? "процедуры" : "функции");
        builder.Append(" '");
        builder.Append(function.Name);
        builder.Append('\'');
        return builder.ToString();
    }
}
