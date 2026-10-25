using Axiom.Ide.Models;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Execution;

/// <summary>
/// Информация об остановке программы на точке останова.
/// </summary>
public sealed record DebuggerStopInfo(
    int? Line,
    int? Column,
    string? Opcode,
    int CallDepth,
    bool CanStepBack,
    Axiom.Core.TextLocation? SourceLocation = null,
    BytecodeVmForEachItem? ForEachItem = null);

/// <summary>
/// Абстракция состояния отладчика, скрывающая внутренности runtime от IDE.
/// Позволяет выполнять шаги отладки без прямой зависимости от BytecodeVmStopState.
/// </summary>
public interface IProgramDebuggerState
{
    /// <summary>
    /// Информация об остановке.
    /// </summary>
    DebuggerStopInfo StopInfo { get; }

    /// <summary>
    /// Можно ли продолжить выполнение.
    /// </summary>
    bool CanResume { get; }

    /// <summary>
    /// Можно ли сделать шаг назад.
    /// </summary>
    bool CanStepBack { get; }

    /// <summary>
    /// Продолжить выполнение до следующей точки останова.
    /// </summary>
    ProgramRunResult Resume();

    /// <summary>
    /// Шаг с обходом — выполнить до следующей строки в том же или внешнем контексте.
    /// </summary>
    ProgramRunResult StepOver();

    /// <summary>
    /// Шаг с входом — войти в вызов функции.
    /// </summary>
    ProgramRunResult StepInto();

    /// <summary>
    /// Шаг с выходом — выполнить до возврата из текущей функции.
    /// </summary>
    ProgramRunResult StepOut();

    /// <summary>
    /// Шаг назад — обратить выполнение (time-travel debugging).
    /// </summary>
    ProgramRunResult StepBack();
}
