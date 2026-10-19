using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BytecodeCompiler
{
    private readonly List<BytecodeDiagnostic> _diagnostics = [];
    private readonly Dictionary<ConstantKey, int> _constantIndices = [];
    private readonly List<BytecodeConstant> _constants = [];
    private readonly Dictionary<VariableSymbol, int> _globalSlots = [];
    private readonly List<BytecodeGlobal> _globals = [];
    private readonly HashSet<VariableSymbol> _liftedEntryGlobals = [];
    private readonly Dictionary<BuiltinBindingKey, int> _builtinIndices = [];
    private readonly List<BuiltinBinding> _builtins = [];
    private readonly Dictionary<FunctionSymbol, int> _routineIds = [];
    private readonly Dictionary<int, IReadOnlyList<VariableSymbol>> _transitiveCaptures = [];
    private readonly List<SourceInstructionMap> _instructionMap = [];
    private readonly List<BytecodeRoutineDebugInfo> _routineDebugInfo = [];

    public BytecodeCompilerResult Compile(LoweredProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        Reset();

        var loweredRoutines = program.AllRoutines.ToArray();
        CollectLiftedEntryGlobals(program, _liftedEntryGlobals);
        RegisterRoutineIds(loweredRoutines);
        ComputeTransitiveCaptures(loweredRoutines);

        var routines = new List<BytecodeRoutine>(loweredRoutines.Length);
        foreach (var loweredRoutine in loweredRoutines)
        {
            var compiledRoutine = CompileRoutine(loweredRoutine);
            if (compiledRoutine is not null)
            {
                routines.Add(compiledRoutine);
            }
        }

        if (_diagnostics.Count > 0)
        {
            return new BytecodeCompilerResult(null, _diagnostics.ToArray());
        }

        var headerFlags = BytecodeModuleFlags.HasDebugInfo;
        if (_builtins.Count > 0)
        {
            headerFlags |= BytecodeModuleFlags.UsesBuiltinTable;
        }

        var module = new BytecodeModule(
            new BytecodeHeader(
                BytecodeFormatVersion.V1,
                "Axiom language vNext",
                headerFlags,
                "axiom-bytecode-v1"),
            new ConstantPool(_constants.ToArray()),
            routines,
            _globals.ToArray(),
            _builtins.ToArray(),
            new BytecodeDebugInfo(_instructionMap.ToArray(), _routineDebugInfo.ToArray()),
            entryPointRoutineId: program.EntryPoint.Id);

        var validationResult = new BytecodeModuleValidator().Validate(module);
        if (!validationResult.Succeeded)
        {
            return new BytecodeCompilerResult(null, validationResult.Diagnostics);
        }

        return new BytecodeCompilerResult(module, Array.Empty<BytecodeDiagnostic>());
    }

    private void Reset()
    {
        _diagnostics.Clear();
        _constantIndices.Clear();
        _constants.Clear();
        _globalSlots.Clear();
        _globals.Clear();
        _liftedEntryGlobals.Clear();
        _builtinIndices.Clear();
        _builtins.Clear();
        _routineIds.Clear();
        _transitiveCaptures.Clear();
        _instructionMap.Clear();
        _routineDebugInfo.Clear();
    }

    private void RegisterRoutineIds(IEnumerable<LoweredRoutine> routines)
    {
        foreach (var routine in routines)
        {
            RegisterConstant(BytecodeConstantKind.RoutineName, routine.Name);

            if (routine.Symbol is null)
            {
                continue;
            }

            _routineIds[routine.Symbol] = routine.Id;
        }
    }

    private BytecodeRoutine? CompileRoutine(LoweredRoutine routine)
    {
        var parameterSlots = CreateParameterSlotMap(routine);
        var localSlots = CreateLocalSlotMap(routine, parameterSlots, _liftedEntryGlobals);
        var captureSlots = CreateCaptureSlotMap(routine, parameterSlots, localSlots);
        var labelTargets = ResolveLabelTargets(routine, parameterSlots, localSlots, captureSlots);
        var instructions = new List<BytecodeInstruction>(routine.Instructions.Count);

        for (var loweredIndex = 0; loweredIndex < routine.Instructions.Count; loweredIndex++)
        {
            var instruction = routine.Instructions[loweredIndex];
            if (instruction.Kind == LoweredInstructionKind.Label)
            {
                continue;
            }

            BytecodeInstruction? bytecodeInstruction;
            TextLocation sourceLocation;
            if (TryEmitCollapsedInstruction(
                    routine,
                    loweredIndex,
                    parameterSlots,
                    localSlots,
                    captureSlots,
                    labelTargets,
                    out bytecodeInstruction,
                    out sourceLocation))
            {
                loweredIndex += GetCollapsedInstructionWidth(
                    routine.Instructions,
                    loweredIndex,
                    parameterSlots,
                    localSlots,
                    captureSlots) - 1;
            }
            else
            {
                bytecodeInstruction = EmitInstruction(
                    routine,
                    instruction,
                    parameterSlots,
                    localSlots,
                    captureSlots,
                    labelTargets);
                sourceLocation = instruction.Location;
            }

            if (bytecodeInstruction is null)
            {
                continue;
            }

            var instructionIndex = instructions.Count;
            instructions.Add(bytecodeInstruction);
            _instructionMap.Add(new SourceInstructionMap(routine.Id, instructionIndex, sourceLocation));
        }

        if (_diagnostics.Count > 0)
        {
            return null;
        }

        var localVariables = CreateRoutineLocalVariables(localSlots);

        _routineDebugInfo.Add(new BytecodeRoutineDebugInfo(
            routine.Id,
            parameterSlots.ToDictionary(static pair => pair.Value, static pair => pair.Key.Name),
            localVariables.Select((variable, index) => new KeyValuePair<int, string>(index, variable.Name))
                .ToDictionary(static pair => pair.Key, static pair => pair.Value),
            [new BytecodeBlockDebugRange("body", 0, instructions.Count)]));

        return new BytecodeRoutine(
            routine.Id,
            routine.Name,
            routine.Location,
            routine.ResultType,
            parameterCount: parameterSlots.Count,
            localSlotCount: localSlots.Count,
            instructions.ToArray(),
            routine.IsEntryPoint,
            routine.Symbol,
            parameterSlots.OrderBy(static pair => pair.Value).Select(static pair => (VariableSymbol)pair.Key).ToArray(),
            localVariables,
            captureSlots.OrderBy(static pair => pair.Value).Select(static pair => pair.Key).ToArray());
    }

    private readonly record struct ConstantKey(BytecodeConstantKind Kind, object? Value);

    private readonly record struct CollapsedLocalForLoopCheck(
        int CurrentSlot,
        int EndSlot,
        int StepSlot,
        string EndLabel,
        TextLocation Location);

    private readonly record struct BuiltinBindingKey(BuiltinFunctionId Id, string Signature)
    {
        public static BuiltinBindingKey Create(FunctionSymbol function, BuiltinFunctionId id)
        {
            return new BuiltinBindingKey(
                id,
                string.Join(
                    "->",
                    string.Join(",", function.Parameters.Select(static parameter => parameter.Type.Name)),
                    function.ReturnType.Name));
        }
    }
}