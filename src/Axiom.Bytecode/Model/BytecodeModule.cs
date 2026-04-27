namespace Axiom.Bytecode;

public sealed class BytecodeModule
{
    public BytecodeModule(
        BytecodeHeader header,
        ConstantPool constantPool,
        IReadOnlyList<BytecodeRoutine> routines,
        IReadOnlyList<BytecodeGlobal> globals,
        IReadOnlyList<BuiltinBinding> builtins,
        BytecodeDebugInfo? debugInfo,
        int entryPointRoutineId)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(constantPool);
        ArgumentNullException.ThrowIfNull(routines);
        ArgumentNullException.ThrowIfNull(globals);
        ArgumentNullException.ThrowIfNull(builtins);

        Header = header;
        ConstantPool = constantPool;
        Routines = routines;
        Globals = globals;
        Builtins = builtins;
        DebugInfo = debugInfo;
        EntryPointRoutineId = entryPointRoutineId;
    }

    public BytecodeHeader Header { get; }

    public ConstantPool ConstantPool { get; }

    public IReadOnlyList<BytecodeRoutine> Routines { get; }

    public IReadOnlyList<BytecodeGlobal> Globals { get; }

    public IReadOnlyList<BuiltinBinding> Builtins { get; }

    public BytecodeDebugInfo? DebugInfo { get; }

    public int EntryPointRoutineId { get; }
}
