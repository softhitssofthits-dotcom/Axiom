using System.Text.Json.Serialization;

namespace Axiom.Bytecode;

public sealed record BytecodeModuleArtifact(
    int? ArtifactSchemaVersion,
    BytecodeHeaderArtifact Header,
    IReadOnlyList<BytecodeConstantArtifact> ConstantPool,
    IReadOnlyList<BytecodeRoutineArtifact> Routines,
    IReadOnlyList<BytecodeGlobalArtifact> Globals,
    IReadOnlyList<BuiltinBindingArtifact> Builtins,
    IReadOnlyList<BytecodeTypeDefinitionArtifact> TypeDefinitions,
    BytecodeDebugInfoArtifact? DebugInfo,
    int EntryPointRoutineId);

public sealed record BytecodeHeaderArtifact(
    string FormatVersion,
    string LanguageVersion,
    int Flags,
    string CompatibilityToken);

public sealed record BytecodeConstantArtifact(
    int Index,
    string Kind,
    object? Value);

public sealed record BytecodeRoutineArtifact(
    int Id,
    string Name,
    TextLocationArtifact Location,
    string ResultType,
    int ParameterCount,
    int LocalSlotCount,
    bool IsEntryPoint,
    BytecodeFunctionSymbolArtifact? Symbol,
    IReadOnlyList<BytecodeVariableSymbolArtifact> ParameterVariables,
    IReadOnlyList<BytecodeVariableSymbolArtifact> LocalVariables,
    IReadOnlyList<BytecodeVariableSymbolArtifact> CapturedVariables,
    IReadOnlyList<BytecodeInstructionArtifact> Instructions);

public sealed record BytecodeInstructionArtifact(
    int Index,
    string Opcode,
    IReadOnlyList<int> Operands);

public sealed record BytecodeGlobalArtifact(
    int Slot,
    string Name,
    string Type,
    bool IsReadOnly,
    string InitializationStrategy,
    int? FixedStringLength);

public sealed record BuiltinBindingArtifact(
    int Id,
    string CanonicalId,
    string Name,
    int ParameterCount,
    bool ReturnsValue,
    BytecodeFunctionSymbolArtifact? Function);

public sealed record BytecodeFunctionSymbolArtifact(
    int? SymbolId,
    string Name,
    string ReturnType,
    bool IsBuiltIn,
    string? BuiltinId,
    int? ContainingRoutineSymbolId,
    string? ContainingRoutineName,
    IReadOnlyList<BytecodeVariableSymbolArtifact> Parameters,
    bool HasImplicitSelfReceiver = false);

public sealed record BytecodeVariableSymbolArtifact(
    int? SymbolId,
    string Name,
    string Type,
    bool IsReadOnly,
    bool IsGlobal,
    int? FixedStringLength,
    string? PassingMode = null);

public sealed record BytecodeTypeDefinitionArtifact(
    string Name,
    IReadOnlyList<BytecodeTypeFieldArtifact> Fields,
    string? Identity = null,
    IReadOnlyList<string?>? TupleElementNames = null);

public sealed record BytecodeTypeFieldArtifact(
    string Name,
    string Type,
    int? FixedStringLength,
    IReadOnlyList<int>? FixedArrayLowerBounds,
    IReadOnlyList<int>? FixedArrayUpperBounds,
    bool HasDefaultValue,
    object? DefaultValue);

public sealed record BytecodeDebugInfoArtifact(
    IReadOnlyList<SourceInstructionMapArtifact> InstructionMap,
    IReadOnlyList<BytecodeRoutineDebugInfoArtifact> Routines);

public sealed record SourceInstructionMapArtifact(
    int RoutineId,
    int InstructionIndex,
    TextLocationArtifact Location);

public sealed record BytecodeRoutineDebugInfoArtifact(
    int RoutineId,
    IReadOnlyDictionary<int, string> ParameterNames,
    IReadOnlyDictionary<int, string> LocalNames,
    IReadOnlyList<BytecodeBlockDebugRangeArtifact> BlockRanges);

public sealed record BytecodeBlockDebugRangeArtifact(
    string Name,
    int StartInstructionIndex,
    int EndInstructionIndexExclusive);

public sealed record TextLocationArtifact(
    int SpanStart,
    int SpanLength,
    int Line,
    int Column,
    string? Document = null)
{
    [JsonIgnore]
    public int SpanEnd => SpanStart + SpanLength;
}
