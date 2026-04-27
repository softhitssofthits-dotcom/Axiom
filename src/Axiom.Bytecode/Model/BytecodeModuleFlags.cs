namespace Axiom.Bytecode;

[Flags]
public enum BytecodeModuleFlags
{
    None = 0,
    HasDebugInfo = 1 << 0,
    UsesBuiltinTable = 1 << 1
}
