using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed class BytecodeCompiler
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
    private readonly List<SourceInstructionMap> _instructionMap = [];
    private readonly List<BytecodeRoutineDebugInfo> _routineDebugInfo = [];

    public BytecodeCompilerResult Compile(LoweredProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        Reset();

        var loweredRoutines = program.AllRoutines.ToArray();
        CollectLiftedEntryGlobals(program, _liftedEntryGlobals);
        RegisterRoutineIds(loweredRoutines);

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

    private static Dictionary<ParameterSymbol, int> CreateParameterSlotMap(LoweredRoutine routine)
    {
        var slots = new Dictionary<ParameterSymbol, int>();
        if (routine.Symbol is null)
        {
            return slots;
        }

        foreach (var parameter in routine.Symbol.Parameters)
        {
            slots[parameter] = parameter.Ordinal;
        }

        return slots;
    }

    private static Dictionary<VariableSymbol, int> CreateLocalSlotMap(
        LoweredRoutine routine,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlySet<VariableSymbol> liftedEntryGlobals)
    {
        var slots = new Dictionary<VariableSymbol, int>();

        foreach (var variable in routine.LocalVariables)
        {
            if (variable is ParameterSymbol)
            {
                continue;
            }

            if (variable.IsGlobal
                && (!routine.IsEntryPoint || !liftedEntryGlobals.Contains(variable)))
            {
                continue;
            }

            if (!slots.ContainsKey(variable))
            {
                slots[variable] = slots.Count;
            }
        }

        return slots;
    }

    private static void CollectLiftedEntryGlobals(LoweredProgram program, HashSet<VariableSymbol> liftedEntryGlobals)
    {
        liftedEntryGlobals.Clear();
        CollectRoutineGlobals(program.EntryPoint, liftedEntryGlobals);

        if (liftedEntryGlobals.Count == 0)
        {
            return;
        }

        var sharedGlobals = new HashSet<VariableSymbol>();
        foreach (var routine in program.Routines)
        {
            CollectRoutineGlobals(routine, sharedGlobals);
        }

        liftedEntryGlobals.ExceptWith(sharedGlobals);
    }

    private static void CollectRoutineGlobals(LoweredRoutine routine, HashSet<VariableSymbol> globals)
    {
        foreach (var instruction in routine.Instructions)
        {
            if (instruction.Variable is { IsGlobal: true } variable)
            {
                globals.Add(variable);
            }

            foreach (var candidate in instruction.Variables)
            {
                if (candidate.IsGlobal)
                {
                    globals.Add(candidate);
                }
            }
        }
    }

    private static VariableSymbol[] CreateRoutineLocalVariables(IReadOnlyDictionary<VariableSymbol, int> localSlots)
    {
        var variables = new VariableSymbol[localSlots.Count];
        foreach (var (variable, slot) in localSlots)
        {
            variables[slot] = variable.IsGlobal
                ? new VariableSymbol(variable.Name, variable.Type, variable.IsReadOnly, isGlobal: false, variable.FixedStringLength)
                : variable;
        }

        return variables;
    }

    private static Dictionary<VariableSymbol, int> CreateCaptureSlotMap(
        LoweredRoutine routine,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots)
    {
        var slots = new Dictionary<VariableSymbol, int>();

        foreach (var instruction in routine.Instructions)
        {
            if (instruction.Variable is { } variable)
            {
                AddCaptureSlot(variable);
            }

            foreach (var inputVariable in instruction.Variables)
            {
                AddCaptureSlot(inputVariable);
            }
        }

        return slots;

        void AddCaptureSlot(VariableSymbol variable)
        {
            if (variable.IsGlobal)
            {
                return;
            }

            if (variable is ParameterSymbol parameter && parameterSlots.ContainsKey(parameter))
            {
                return;
            }

            if (variable is not ParameterSymbol && localSlots.ContainsKey(variable))
            {
                return;
            }

            if (!slots.ContainsKey(variable))
            {
                slots[variable] = slots.Count;
            }
        }
    }

    private Dictionary<string, int> ResolveLabelTargets(
        LoweredRoutine routine,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots)
    {
        var targets = new Dictionary<string, int>(StringComparer.Ordinal);
        var instructionIndex = 0;

        for (var loweredIndex = 0; loweredIndex < routine.Instructions.Count; loweredIndex++)
        {
            var instruction = routine.Instructions[loweredIndex];
            if (instruction.Kind == LoweredInstructionKind.Label)
            {
                if (!targets.TryAdd(instruction.Label!, instructionIndex))
                {
                    Report(
                        "BC5001",
                        $"Эмиттер байткода встретил повторяющуюся метку '{instruction.Label}' в процедуре '{routine.Name}'.",
                        instruction.Location);
                }

                continue;
            }

            if (TryGetCollapsedInstructionWidth(routine.Instructions, loweredIndex, parameterSlots, localSlots, captureSlots, out var width))
            {
                instructionIndex++;
                loweredIndex += width - 1;
                continue;
            }

            if (EmitsBytecodeInstruction(instruction))
            {
                instructionIndex++;
            }
        }

        return targets;
    }

    private static bool EmitsBytecodeInstruction(LoweredInstruction instruction)
    {
        return instruction.Kind switch
        {
            LoweredInstructionKind.Label => false,
            LoweredInstructionKind.UnaryOperation when instruction.OperatorKind == SyntaxKind.PlusToken => false,
            _ => true
        };
    }

    private int GetCollapsedInstructionWidth(
        IReadOnlyList<LoweredInstruction> instructions,
        int index,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots)
    {
        return TryGetCollapsedInstructionWidth(instructions, index, parameterSlots, localSlots, captureSlots, out var width)
            ? width
            : 0;
    }

    private bool TryGetCollapsedInstructionWidth(
        IReadOnlyList<LoweredInstruction> instructions,
        int index,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out int width)
    {
        width = 0;
        if (TryMatchCollapsedLocalForLoopCheck(instructions, index, parameterSlots, localSlots, captureSlots, out _))
        {
            width = 15;
            return true;
        }

        if (TryMatchCollapsedAdvanceForLoopLocal(instructions, index, parameterSlots, localSlots, captureSlots, out _, out _))
        {
            width = 4;
            return true;
        }

        if (index + 1 >= instructions.Count)
        {
            return false;
        }

        var current = instructions[index];
        var next = instructions[index + 1];
        if (TryGetCollapsedComparisonJumpOpcode(current, next, out _))
        {
            width = 2;
            return true;
        }

        if (current.Kind == LoweredInstructionKind.Convert
            && current.ValueType == TypeSymbol.Long
            && next.Kind == LoweredInstructionKind.BinaryOperation
            && next.OperatorKind == SyntaxKind.PlusToken)
        {
            width = 2;
            return true;
        }

        return false;
    }

    private bool TryEmitCollapsedInstruction(
        LoweredRoutine routine,
        int loweredIndex,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        IReadOnlyDictionary<string, int> labelTargets,
        out BytecodeInstruction? instruction,
        out TextLocation sourceLocation)
    {
        instruction = null;
        sourceLocation = routine.Instructions[loweredIndex].Location;
        if (loweredIndex + 1 >= routine.Instructions.Count)
        {
            return false;
        }

        var current = routine.Instructions[loweredIndex];
        var next = routine.Instructions[loweredIndex + 1];

        if (TryGetCollapsedComparisonJumpOpcode(current, next, out var comparisonJumpOpcode))
        {
            if (next.Label is null || !labelTargets.TryGetValue(next.Label, out var targetInstruction))
            {
                Report(
                    "BC5012",
                    $"Эмиттер байткода не нашёл метку '{next.Label ?? "<null>"}' в процедуре '{routine.Name}'.",
                    next.Location);
                return true;
            }

            instruction = new BytecodeInstruction(comparisonJumpOpcode, [targetInstruction]);
            sourceLocation = next.Location;
            return true;
        }

        if (TryMatchCollapsedLocalForLoopCheck(
                routine.Instructions,
                loweredIndex,
                parameterSlots,
                localSlots,
                captureSlots,
                out var collapsedForLoopCheck))
        {
            if (!labelTargets.TryGetValue(collapsedForLoopCheck.EndLabel, out var endTargetInstruction))
            {
                Report(
                    "BC5012",
                    $"Эмиттер байткода не нашёл метку '{collapsedForLoopCheck.EndLabel}' в процедуре '{routine.Name}'.",
                    collapsedForLoopCheck.Location);
                return true;
            }

            instruction = new BytecodeInstruction(
                Opcode.JumpIfForDoneLocal,
                [
                    collapsedForLoopCheck.CurrentSlot,
                    collapsedForLoopCheck.EndSlot,
                    collapsedForLoopCheck.StepSlot,
                    endTargetInstruction
                ]);
            sourceLocation = collapsedForLoopCheck.Location;
            return true;
        }

        if (TryMatchCollapsedAdvanceForLoopLocal(
                routine.Instructions,
                loweredIndex,
                parameterSlots,
                localSlots,
                captureSlots,
                out var currentSlot,
                out var stepSlot))
        {
            instruction = new BytecodeInstruction(Opcode.AdvanceForLoopLocal, [currentSlot, stepSlot]);
            sourceLocation = routine.Instructions[loweredIndex + 3].Location;
            return true;
        }

        if (!TryGetCollapsedInstructionWidth(routine.Instructions, loweredIndex, parameterSlots, localSlots, captureSlots, out _))
        {
            return false;
        }

        instruction = new BytecodeInstruction(Opcode.AddLongInteger);
        sourceLocation = next.Location;
        return true;
    }

    private bool TryMatchCollapsedLocalForLoopCheck(
        IReadOnlyList<LoweredInstruction> instructions,
        int index,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out CollapsedLocalForLoopCheck collapsed)
    {
        collapsed = default;
        if (index + 14 >= instructions.Count)
        {
            return false;
        }

        var stepLoadForSign = instructions[index];
        var zeroLiteral = instructions[index + 1];
        var compareStepToZero = instructions[index + 2];
        var jumpToNegative = instructions[index + 3];
        var currentLoadPositive = instructions[index + 4];
        var endLoadPositive = instructions[index + 5];
        var comparePositive = instructions[index + 6];
        var jumpPositiveEnd = instructions[index + 7];
        var jumpToBody = instructions[index + 8];
        var negativeLabel = instructions[index + 9];
        var currentLoadNegative = instructions[index + 10];
        var endLoadNegative = instructions[index + 11];
        var compareNegative = instructions[index + 12];
        var jumpNegativeEnd = instructions[index + 13];
        var bodyLabel = instructions[index + 14];

        if (!TryResolveVariableSlotForCollapse(stepLoadForSign, parameterSlots, localSlots, captureSlots, out var stepLoadOpcode, out var stepSlot)
            || stepLoadOpcode != Opcode.LoadLocal)
        {
            return false;
        }

        if (zeroLiteral.Kind != LoweredInstructionKind.LoadLiteral
            || zeroLiteral.ValueType != stepLoadForSign.Variable?.Type
            || !Equals(zeroLiteral.LiteralValue, CreateZeroLiteral(stepLoadForSign.Variable!.Type)))
        {
            return false;
        }

        if (compareStepToZero.Kind != LoweredInstructionKind.BinaryOperation
            || compareStepToZero.OperatorKind != SyntaxKind.GreaterToken
            || jumpToNegative.Kind != LoweredInstructionKind.JumpIfFalse
            || string.IsNullOrWhiteSpace(jumpToNegative.Label)
            || negativeLabel.Kind != LoweredInstructionKind.Label
            || negativeLabel.Label != jumpToNegative.Label
            || jumpToBody.Kind != LoweredInstructionKind.Jump
            || string.IsNullOrWhiteSpace(jumpToBody.Label)
            || bodyLabel.Kind != LoweredInstructionKind.Label
            || bodyLabel.Label != jumpToBody.Label)
        {
            return false;
        }

        if (!TryResolveVariableSlotForCollapse(currentLoadPositive, parameterSlots, localSlots, captureSlots, out var currentPositiveOpcode, out var currentSlot)
            || currentPositiveOpcode != Opcode.LoadLocal
            || !TryResolveVariableSlotForCollapse(endLoadPositive, parameterSlots, localSlots, captureSlots, out var endPositiveOpcode, out var endSlot)
            || endPositiveOpcode != Opcode.LoadLocal
            || comparePositive.Kind != LoweredInstructionKind.BinaryOperation
            || comparePositive.OperatorKind != SyntaxKind.GreaterToken
            || jumpPositiveEnd.Kind != LoweredInstructionKind.JumpIfTrue
            || string.IsNullOrWhiteSpace(jumpPositiveEnd.Label))
        {
            return false;
        }

        if (!TryResolveVariableSlotForCollapse(currentLoadNegative, parameterSlots, localSlots, captureSlots, out var currentNegativeOpcode, out var currentNegativeSlot)
            || currentNegativeOpcode != Opcode.LoadLocal
            || currentNegativeSlot != currentSlot
            || !TryResolveVariableSlotForCollapse(endLoadNegative, parameterSlots, localSlots, captureSlots, out var endNegativeOpcode, out var endNegativeSlot)
            || endNegativeOpcode != Opcode.LoadLocal
            || endNegativeSlot != endSlot
            || compareNegative.Kind != LoweredInstructionKind.BinaryOperation
            || compareNegative.OperatorKind != SyntaxKind.LessToken
            || jumpNegativeEnd.Kind != LoweredInstructionKind.JumpIfTrue
            || jumpNegativeEnd.Label != jumpPositiveEnd.Label)
        {
            return false;
        }

        collapsed = new CollapsedLocalForLoopCheck(currentSlot, endSlot, stepSlot, jumpPositiveEnd.Label!, jumpPositiveEnd.Location);
        return true;
    }

    private bool TryMatchCollapsedAdvanceForLoopLocal(
        IReadOnlyList<LoweredInstruction> instructions,
        int index,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out int currentSlot,
        out int stepSlot)
    {
        currentSlot = default;
        stepSlot = default;
        if (index + 3 >= instructions.Count)
        {
            return false;
        }

        var currentLoad = instructions[index];
        var stepLoad = instructions[index + 1];
        var addInstruction = instructions[index + 2];
        var currentStore = instructions[index + 3];

        if (!TryResolveVariableSlotForCollapse(currentLoad, parameterSlots, localSlots, captureSlots, out var currentLoadOpcode, out currentSlot)
            || currentLoadOpcode != Opcode.LoadLocal
            || !TryResolveVariableSlotForCollapse(stepLoad, parameterSlots, localSlots, captureSlots, out var stepLoadOpcode, out stepSlot)
            || stepLoadOpcode != Opcode.LoadLocal
            || addInstruction.Kind != LoweredInstructionKind.BinaryOperation
            || addInstruction.OperatorKind != SyntaxKind.PlusToken
            || !TryResolveVariableSlotForCollapse(currentStore, parameterSlots, localSlots, captureSlots, out var currentStoreOpcode, out var currentStoreSlot)
            || currentStoreOpcode != Opcode.StoreLocal
            || currentStoreSlot != currentSlot)
        {
            return false;
        }

        return true;
    }

    private static bool TryResolveVariableSlotForCollapse(
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out Opcode opcode,
        out int slot)
    {
        opcode = default;
        slot = default;

        if (instruction.Variable is null)
        {
            return false;
        }

        if (instruction.Variable is ParameterSymbol parameter)
        {
            if (parameterSlots.TryGetValue(parameter, out slot))
            {
                opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreArgument : Opcode.LoadArgument;
                return true;
            }

            if (captureSlots.TryGetValue(parameter, out slot))
            {
                opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreCapture : Opcode.LoadCapture;
                return true;
            }

            return false;
        }

        if (instruction.Variable.IsGlobal)
        {
            opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreGlobal : Opcode.LoadGlobal;
            return true;
        }

        if (localSlots.TryGetValue(instruction.Variable, out slot))
        {
            opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreLocal : Opcode.LoadLocal;
            return true;
        }

        if (captureSlots.TryGetValue(instruction.Variable, out slot))
        {
            opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreCapture : Opcode.LoadCapture;
            return true;
        }

        return false;
    }

    private static bool TryGetCollapsedComparisonJumpOpcode(
        LoweredInstruction compareInstruction,
        LoweredInstruction jumpInstruction,
        out Opcode opcode)
    {
        opcode = default;
        if (compareInstruction.Kind != LoweredInstructionKind.BinaryOperation)
        {
            return false;
        }

        if (jumpInstruction.Kind is not (LoweredInstructionKind.JumpIfTrue or LoweredInstructionKind.JumpIfFalse))
        {
            return false;
        }

        var jumpIfTrue = jumpInstruction.Kind == LoweredInstructionKind.JumpIfTrue;
        opcode = compareInstruction.OperatorKind switch
        {
            SyntaxKind.EqualsToken => jumpIfTrue ? Opcode.JumpIfEqual : Opcode.JumpIfNotEqual,
            SyntaxKind.NotEqualsToken => jumpIfTrue ? Opcode.JumpIfNotEqual : Opcode.JumpIfEqual,
            SyntaxKind.LessToken => jumpIfTrue ? Opcode.JumpIfLess : Opcode.JumpIfGreaterOrEqual,
            SyntaxKind.LessOrEqualsToken => jumpIfTrue ? Opcode.JumpIfLessOrEqual : Opcode.JumpIfGreater,
            SyntaxKind.GreaterToken => jumpIfTrue ? Opcode.JumpIfGreater : Opcode.JumpIfLessOrEqual,
            SyntaxKind.GreaterOrEqualsToken => jumpIfTrue ? Opcode.JumpIfGreaterOrEqual : Opcode.JumpIfLess,
            _ => default
        };

        return opcode is Opcode.JumpIfEqual
            or Opcode.JumpIfNotEqual
            or Opcode.JumpIfLess
            or Opcode.JumpIfLessOrEqual
            or Opcode.JumpIfGreater
            or Opcode.JumpIfGreaterOrEqual;
    }

    private BytecodeInstruction? EmitInstruction(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        IReadOnlyDictionary<string, int> labelTargets)
    {
        return instruction.Kind switch
        {
            LoweredInstructionKind.LoadLiteral => EmitLoadLiteral(instruction),
            LoweredInstructionKind.LoadFunction => EmitLoadFunction(routine, instruction),
            LoweredInstructionKind.LoadVariable => EmitLoadVariable(routine, instruction, parameterSlots, localSlots, captureSlots),
            LoweredInstructionKind.StoreVariable => EmitStoreVariable(routine, instruction, parameterSlots, localSlots, captureSlots),
            LoweredInstructionKind.AppendStringVariable => EmitAppendStringVariable(routine, instruction, parameterSlots, localSlots, captureSlots),
            LoweredInstructionKind.Dup => new BytecodeInstruction(Opcode.Dup),
            LoweredInstructionKind.UnaryOperation => EmitUnaryOperation(routine, instruction),
            LoweredInstructionKind.BinaryOperation => EmitBinaryOperation(routine, instruction),
            LoweredInstructionKind.Convert => EmitConversion(routine, instruction),
            LoweredInstructionKind.CallFunction => EmitCall(routine, instruction),
            LoweredInstructionKind.CallFunctionByRef => EmitCallByRef(routine, instruction, parameterSlots, localSlots, captureSlots),
            LoweredInstructionKind.CallValue => EmitCallValue(instruction),
            LoweredInstructionKind.CreateRecord => EmitCreateRecord(instruction),
            LoweredInstructionKind.RedimArray => EmitRedimArray(instruction),
            LoweredInstructionKind.EraseArray => new BytecodeInstruction(Opcode.EraseArray),
            LoweredInstructionKind.LoadElement => EmitArrayElementInstruction(instruction, Opcode.LoadElement),
            LoweredInstructionKind.StoreElement => EmitArrayElementInstruction(instruction, Opcode.StoreElement),
            LoweredInstructionKind.LoadField => EmitFieldInstruction(instruction, Opcode.LoadField),
            LoweredInstructionKind.StoreField => EmitFieldInstruction(instruction, Opcode.StoreField),
            LoweredInstructionKind.OpenFile => EmitOpenFile(instruction),
            LoweredInstructionKind.CloseFile => new BytecodeInstruction(Opcode.CloseFile),
            LoweredInstructionKind.ValidateTypeInvariant => EmitValidateTypeInvariant(instruction),
            LoweredInstructionKind.ValidateForStep => new BytecodeInstruction(Opcode.ValidateForStep),
            LoweredInstructionKind.StatementBoundary => new BytecodeInstruction(Opcode.StatementBoundary),
            LoweredInstructionKind.RegisterKeyHandler => new BytecodeInstruction(
                Opcode.RegisterKeyHandler,
                [instruction.HandlerReceivesKeyText ? 1 : 0]),
            LoweredInstructionKind.UnregisterKeyHandler => new BytecodeInstruction(Opcode.UnregisterKeyHandler),
            LoweredInstructionKind.PauseKeyHandler => new BytecodeInstruction(Opcode.PauseKeyHandler),
            LoweredInstructionKind.ResumeKeyHandler => new BytecodeInstruction(Opcode.ResumeKeyHandler),
            LoweredInstructionKind.RegisterMouseHandler => new BytecodeInstruction(
                Opcode.RegisterMouseHandler,
                [instruction.HandlerReceivesKeyText ? 1 : 0, instruction.MouseEventKind]),
            LoweredInstructionKind.UnregisterMouseHandler => new BytecodeInstruction(Opcode.UnregisterMouseHandler, [instruction.MouseEventKind]),
            LoweredInstructionKind.PauseMouseHandler => new BytecodeInstruction(Opcode.PauseMouseHandler, [instruction.MouseEventKind]),
            LoweredInstructionKind.ResumeMouseHandler => new BytecodeInstruction(Opcode.ResumeMouseHandler, [instruction.MouseEventKind]),
            LoweredInstructionKind.Screen => new BytecodeInstruction(Opcode.Screen),
            LoweredInstructionKind.View => new BytecodeInstruction(Opcode.View, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.ViewPrint => new BytecodeInstruction(Opcode.ViewPrint, [instruction.ValueCount]),
            LoweredInstructionKind.Window => new BytecodeInstruction(Opcode.Window, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.Cls => new BytecodeInstruction(Opcode.Cls),
            LoweredInstructionKind.Color => new BytecodeInstruction(Opcode.Color, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.Locate => new BytecodeInstruction(Opcode.Locate),
            LoweredInstructionKind.Palette => new BytecodeInstruction(Opcode.Palette, [instruction.ValueCount]),
            LoweredInstructionKind.PCopy => new BytecodeInstruction(Opcode.PCopy),
            LoweredInstructionKind.PSet => new BytecodeInstruction(Opcode.PSet, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.Get => new BytecodeInstruction(Opcode.Get, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.Put => new BytecodeInstruction(Opcode.Put, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.Line => new BytecodeInstruction(Opcode.Line, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.Circle => new BytecodeInstruction(Opcode.Circle, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.Paint => new BytecodeInstruction(Opcode.Paint, [instruction.MouseEventKind, instruction.ValueCount]),
            LoweredInstructionKind.Draw => new BytecodeInstruction(Opcode.Draw, [instruction.ValueCount]),
            LoweredInstructionKind.DrawImage => new BytecodeInstruction(Opcode.DrawImage, [instruction.ValueCount]),
            LoweredInstructionKind.DrawSprite => new BytecodeInstruction(Opcode.DrawSprite, [instruction.ValueCount]),
            LoweredInstructionKind.PrintValue => new BytecodeInstruction(Opcode.PrintValue),
            LoweredInstructionKind.PrintUsing => EmitPrintUsing(instruction),
            LoweredInstructionKind.PrintSeparator => EmitPrintSeparator(instruction),
            LoweredInstructionKind.PrintLine => new BytecodeInstruction(Opcode.PrintLine),
            LoweredInstructionKind.Input => EmitInput(routine, instruction, parameterSlots, localSlots, captureSlots),
            LoweredInstructionKind.Sleep => new BytecodeInstruction(Opcode.Sleep, [instruction.ValueCount]),
            LoweredInstructionKind.PushErrorHandler => EmitPushErrorHandler(routine, instruction, parameterSlots, localSlots, captureSlots, labelTargets),
            LoweredInstructionKind.PopErrorHandler => new BytecodeInstruction(Opcode.PopErrorHandler),
            LoweredInstructionKind.RaiseError => new BytecodeInstruction(Opcode.RaiseError, [instruction.ValueCount]),
            LoweredInstructionKind.EndFinally => new BytecodeInstruction(Opcode.EndFinally),
            LoweredInstructionKind.Jump => EmitJump(routine, instruction, labelTargets, Opcode.Jump),
            LoweredInstructionKind.JumpIfTrue => EmitJump(routine, instruction, labelTargets, Opcode.JumpIfTrue),
            LoweredInstructionKind.JumpIfFalse => EmitJump(routine, instruction, labelTargets, Opcode.JumpIfFalse),
            LoweredInstructionKind.Pop => new BytecodeInstruction(Opcode.Pop),
            LoweredInstructionKind.Return => new BytecodeInstruction(Opcode.Return),
            LoweredInstructionKind.Label => null,
            _ => ReportUnsupportedInstruction(routine, instruction)
        };
    }

    private BytecodeInstruction EmitLoadLiteral(LoweredInstruction instruction)
    {
        if (instruction.ValueType == TypeSymbol.Integer && instruction.LiteralValue is int integerLiteral)
        {
            if (integerLiteral == 0)
            {
                return new BytecodeInstruction(Opcode.PushIntegerZero);
            }

            if (integerLiteral == 1)
            {
                return new BytecodeInstruction(Opcode.PushIntegerOne);
            }
        }

        var constantKind = TryGetConstantKind(instruction.ValueType, out var kind)
            ? kind
            : BytecodeConstantKind.Null;

        if (!TryGetConstantKind(instruction.ValueType, out kind))
        {
            Report(
                "BC5002",
                $"Эмиттер байткода пока не поддерживает литерал типа '{instruction.ValueType?.Name ?? "<null>"}'.",
                instruction.Location);
        }

        return new BytecodeInstruction(Opcode.PushConst, [RegisterConstant(constantKind, instruction.LiteralValue)]);
    }

    private BytecodeInstruction? EmitLoadFunction(LoweredRoutine routine, LoweredInstruction instruction)
    {
        if (instruction.Function is null)
        {
            Report(
                "BC5014",
                $"Эмиттер байткода получил загрузку функции без целевой функции в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        if (instruction.Function.IsBuiltIn)
        {
            if (instruction.Function.BuiltinId is not { } builtinId)
            {
                Report(
                    "BC5015",
                    $"Встроенная функция '{instruction.Function.Name}' не содержит канонического идентификатора для загрузки значения.",
                    instruction.Location);
                return null;
            }

            return new BytecodeInstruction(Opcode.LoadBuiltinFunction, [RegisterBuiltin(instruction.Function, builtinId)]);
        }

        if (!_routineIds.TryGetValue(instruction.Function, out var routineId))
        {
            Report(
                "BC5016",
                $"Эмиттер байткода не нашёл процедуру '{instruction.Function.Name}' для загрузки значения функции.",
                instruction.Location);
            return null;
        }

        return new BytecodeInstruction(Opcode.LoadFunction, [routineId]);
    }

    private BytecodeInstruction? EmitLoadVariable(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots)
    {
        return TryResolveVariableSlot(routine, instruction, parameterSlots, localSlots, captureSlots, out var opcode, out var slot)
            ? new BytecodeInstruction(opcode, [slot])
            : null;
    }

    private BytecodeInstruction? EmitStoreVariable(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots)
    {
        return TryResolveVariableSlot(routine, instruction, parameterSlots, localSlots, captureSlots, out var opcode, out var slot, isStore: true)
            ? new BytecodeInstruction(opcode, [slot])
            : null;
    }

    private BytecodeInstruction? EmitAppendStringVariable(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots)
    {
        if (!TryResolveVariableSlot(routine, instruction, parameterSlots, localSlots, captureSlots, out var storeOpcode, out var slot, isStore: true))
        {
            return null;
        }

        var opcode = storeOpcode switch
        {
            Opcode.StoreGlobal => Opcode.AppendStringGlobal,
            Opcode.StoreLocal => Opcode.AppendStringLocal,
            Opcode.StoreArgument => Opcode.AppendStringArgument,
            Opcode.StoreCapture => Opcode.AppendStringCapture,
            _ => (Opcode?)null
        };

        if (opcode is null)
        {
            Report(
                "BC5023",
                $"Эмиттер байткода не смог выбрать инструкцию накопления строки для '{instruction.Variable?.Name ?? "<null>"}' в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        return new BytecodeInstruction(opcode.Value, [slot, instruction.Preserve ? 1 : 0]);
    }

    private bool TryResolveVariableSlot(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out Opcode opcode,
        out int slot,
        bool isStore = false)
    {
        opcode = default;
        slot = default;

        if (instruction.Variable is null)
        {
            Report(
                "BC5003",
                $"Эмиттер байткода получил инструкцию '{instruction.Kind}' без переменной в процедуре '{routine.Name}'.",
                instruction.Location);
            return false;
        }

        if (instruction.Variable is ParameterSymbol parameter)
        {
            if (parameterSlots.TryGetValue(parameter, out slot))
            {
                opcode = isStore ? Opcode.StoreArgument : Opcode.LoadArgument;
                return true;
            }

            if (captureSlots.TryGetValue(parameter, out slot))
            {
                opcode = isStore ? Opcode.StoreCapture : Opcode.LoadCapture;
                return true;
            }

            Report(
                "BC5004",
                $"Эмиттер байткода не смог разрешить захват внешнего параметра '{parameter.Name}' в процедуре '{routine.Name}'.",
                instruction.Location);
            return false;
        }

        if (localSlots.TryGetValue(instruction.Variable, out slot))
        {
            opcode = isStore ? Opcode.StoreLocal : Opcode.LoadLocal;
            return true;
        }

        if (instruction.Variable.IsGlobal)
        {
            opcode = isStore ? Opcode.StoreGlobal : Opcode.LoadGlobal;
            slot = RegisterGlobal(instruction.Variable);
            return true;
        }

        if (captureSlots.TryGetValue(instruction.Variable, out slot))
        {
            opcode = isStore ? Opcode.StoreCapture : Opcode.LoadCapture;
            return true;
        }

        Report(
            "BC5004",
            $"Эмиттер байткода не смог выделить слот для переменной '{instruction.Variable.Name}' в процедуре '{routine.Name}'.",
            instruction.Location);
        return false;
    }

    private BytecodeInstruction? EmitUnaryOperation(LoweredRoutine routine, LoweredInstruction instruction)
    {
        if (instruction.OperatorKind == SyntaxKind.PlusToken)
        {
            return null;
        }

        var opcode = instruction.OperatorKind switch
        {
            SyntaxKind.MinusToken => Opcode.Negate,
            SyntaxKind.NotKeyword => Opcode.Not,
            _ => (Opcode?)null
        };

        if (opcode is null)
        {
            Report(
                "BC5005",
                $"Эмиттер байткода пока не поддерживает унарную операцию '{instruction.OperatorKind}' в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        return new BytecodeInstruction(opcode.Value);
    }

    private BytecodeInstruction? EmitBinaryOperation(LoweredRoutine routine, LoweredInstruction instruction)
    {
        var opcode = instruction.OperatorKind switch
        {
            SyntaxKind.AmpersandToken => Opcode.Concat,
            SyntaxKind.PlusToken => Opcode.Add,
            SyntaxKind.MinusToken => Opcode.Subtract,
            SyntaxKind.StarToken => Opcode.Multiply,
            SyntaxKind.CaretToken => Opcode.Power,
            SyntaxKind.SlashToken => Opcode.Divide,
            SyntaxKind.BackslashToken => Opcode.IntegralDivide,
            SyntaxKind.ModKeyword => Opcode.Modulo,
            SyntaxKind.AndKeyword => Opcode.And,
            SyntaxKind.OrKeyword => Opcode.Or,
            SyntaxKind.XorKeyword => Opcode.Xor,
            SyntaxKind.EqvKeyword => Opcode.Eqv,
            SyntaxKind.ImpKeyword => Opcode.Imp,
            SyntaxKind.EqualsToken => Opcode.CompareEqual,
            SyntaxKind.NotEqualsToken => Opcode.CompareNotEqual,
            SyntaxKind.LessToken => Opcode.CompareLess,
            SyntaxKind.LessOrEqualsToken => Opcode.CompareLessOrEqual,
            SyntaxKind.GreaterToken => Opcode.CompareGreater,
            SyntaxKind.GreaterOrEqualsToken => Opcode.CompareGreaterOrEqual,
            SyntaxKind.LessLessToken => Opcode.ShiftLeft,
            SyntaxKind.GreaterGreaterToken => Opcode.ShiftRight,
            _ => (Opcode?)null
        };

        if (opcode is null)
        {
            Report(
                "BC5006",
                $"Эмиттер байткода пока не поддерживает бинарную операцию '{instruction.OperatorKind}' в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        return new BytecodeInstruction(opcode.Value);
    }

    private BytecodeInstruction? EmitConversion(LoweredRoutine routine, LoweredInstruction instruction)
    {
        if (instruction.ValueType is null)
        {
            Report(
                "BC5007",
                $"Эмиттер байткода получил преобразование без целевого типа в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        if (TryGetScalarTypeCode(instruction.ValueType, out var typeCode))
        {
            return new BytecodeInstruction(Opcode.Convert, [(int)typeCode]);
        }

        Report(
            "BC5008",
            $"Эмиттер байткода пока не поддерживает преобразование к типу '{instruction.ValueType.Name}' в процедуре '{routine.Name}'.",
            instruction.Location);

        return null;
    }

    private static bool TryGetScalarTypeCode(TypeSymbol type, out BytecodeScalarTypeCode typeCode)
    {
        if (type == TypeSymbol.Integer)
        {
            typeCode = BytecodeScalarTypeCode.Integer;
            return true;
        }

        if (type == TypeSymbol.Long)
        {
            typeCode = BytecodeScalarTypeCode.Long;
            return true;
        }

        if (type == TypeSymbol.Double)
        {
            typeCode = BytecodeScalarTypeCode.Double;
            return true;
        }

        if (type == TypeSymbol.Decimal)
        {
            typeCode = BytecodeScalarTypeCode.Decimal;
            return true;
        }

        typeCode = default;
        return false;
    }

    private BytecodeInstruction? EmitCall(LoweredRoutine routine, LoweredInstruction instruction)
    {
        if (instruction.Function is null)
        {
            Report(
                "BC5009",
                $"Эмиттер байткода получил вызов без целевой функции в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        if (instruction.Function.IsBuiltIn)
        {
            if (instruction.Function.BuiltinId is not { } builtinId)
            {
                Report(
                    "BC5010",
                    $"Встроенная функция '{instruction.Function.Name}' не содержит канонического идентификатора.",
                    instruction.Location);
                return null;
            }

            return new BytecodeInstruction(
                Opcode.CallBuiltin,
                [RegisterBuiltin(instruction.Function, builtinId), instruction.Function.Parameters.Count]);
        }

        if (!_routineIds.TryGetValue(instruction.Function, out var routineId))
        {
            Report(
                "BC5011",
                $"Эмиттер байткода не нашёл процедуру '{instruction.Function.Name}' для инструкции вызова.",
                instruction.Location);
            return null;
        }

        return new BytecodeInstruction(Opcode.Call, [routineId, instruction.Function.Parameters.Count]);
    }

    private BytecodeInstruction? EmitCallByRef(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots)
    {
        var baseInstruction = EmitCall(routine, instruction);
        if (baseInstruction is null)
        {
            return null;
        }

        var operands = new List<int>
        {
            baseInstruction.GetOperand(0),
            baseInstruction.GetOperand(1),
            instruction.ValueCount,
            instruction.Variables.Count
        };

        for (var index = 0; index < instruction.Variables.Count; index++)
        {
            var variable = instruction.Variables[index];
            operands.Add(instruction.IntegerOperands[index]);

            if (variable is ParameterSymbol parameter && parameterSlots.TryGetValue(parameter, out var parameterSlot))
            {
                operands.Add(1);
                operands.Add(parameterSlot);
                continue;
            }

            if (localSlots.TryGetValue(variable, out var localSlot))
            {
                operands.Add(2);
                operands.Add(localSlot);
                continue;
            }

            if (captureSlots.TryGetValue(variable, out var captureSlot))
            {
                operands.Add(3);
                operands.Add(captureSlot);
                continue;
            }

            if (variable.IsGlobal)
            {
                operands.Add(0);
                operands.Add(RegisterGlobal(variable));
                continue;
            }

            Report(
                "BC5012",
                $"Эмиттер байткода не смог разрешить источник BYREF-аргумента '{variable.Name}' в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        return new BytecodeInstruction(Opcode.CallByRef, operands.ToArray());
    }

    private static BytecodeInstruction EmitCallValue(LoweredInstruction instruction)
    {
        return new BytecodeInstruction(
            Opcode.CallValue,
            [instruction.ValueCount, instruction.ValueType == TypeSymbol.Void ? 0 : 1]);
    }

    private BytecodeInstruction EmitRedimArray(LoweredInstruction instruction)
    {
        return new BytecodeInstruction(
            Opcode.RedimArray,
            [
                instruction.Preserve ? 1 : 0,
                RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName),
                instruction.ValueCount
            ]);
    }

    private BytecodeInstruction EmitArrayElementInstruction(LoweredInstruction instruction, Opcode opcode)
    {
        return new BytecodeInstruction(
            opcode,
            opcode == Opcode.LoadElement
                ? [RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName), instruction.ValueCount, instruction.IndexRangeMask]
                : [RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName), instruction.ValueCount]);
    }

    private BytecodeInstruction EmitFieldInstruction(LoweredInstruction instruction, Opcode opcode)
    {
        return new BytecodeInstruction(opcode, [RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName)]);
    }

    private BytecodeInstruction EmitCreateRecord(LoweredInstruction instruction)
    {
        return new BytecodeInstruction(Opcode.CreateRecord, [RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName)]);
    }

    private BytecodeInstruction EmitValidateTypeInvariant(LoweredInstruction instruction)
    {
        return new BytecodeInstruction(Opcode.ValidateTypeInvariant, [RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName)]);
    }

    private BytecodeInstruction EmitOpenFile(LoweredInstruction instruction)
    {
        if (instruction.FileOpenMode is null)
        {
            Report(
                "BC5017",
                "Эмиттер байткода получил инструкцию OPEN без режима открытия файла.",
                instruction.Location);
        }

        var mode = instruction.FileOpenMode switch
        {
            BoundFileOpenMode.Input => 0,
            BoundFileOpenMode.Output => 1,
            BoundFileOpenMode.Append => 2,
            _ => 0
        };

        return new BytecodeInstruction(
            Opcode.OpenFile,
            [mode, RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName)]);
    }

    private BytecodeInstruction EmitPrintSeparator(LoweredInstruction instruction)
    {
        var separatorValue = instruction.SeparatorKind switch
        {
            PrintSeparatorKind.Semicolon => 0,
            PrintSeparatorKind.Comma => 1,
            _ => throw new InvalidOperationException("Unknown print separator kind.")
        };

        return new BytecodeInstruction(Opcode.PrintSeparator, [separatorValue]);
    }

    private BytecodeInstruction EmitPrintUsing(LoweredInstruction instruction)
    {
        var separatorValue = instruction.SeparatorKind switch
        {
            null => 0,
            PrintSeparatorKind.Semicolon => 1,
            PrintSeparatorKind.Comma => 2,
            _ => throw new InvalidOperationException("Unknown PRINT USING separator kind.")
        };

        return new BytecodeInstruction(Opcode.PrintUsing, [instruction.ValueCount, separatorValue]);
    }

    private BytecodeInstruction? EmitInput(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots)
    {
        var operands = new List<int>(2 + instruction.Variables.Count * 2)
        {
            instruction.Variables.Count,
            instruction.PromptText is null
                ? 0
                : RegisterConstant(BytecodeConstantKind.String, instruction.PromptText) + 1
        };

        foreach (var variable in instruction.Variables)
        {
            var targetInstruction = LoweredInstruction.StoreVariable(instruction.Location, variable);
            if (!TryResolveVariableSlot(routine, targetInstruction, parameterSlots, localSlots, captureSlots, out var storeOpcode, out var slot, isStore: true))
            {
                return null;
            }

            if (!TryMapInputTargetKind(storeOpcode, out var targetKind))
            {
                Report(
                    "BC5018",
                    $"Эмиттер байткода не смог представить цель INPUT '{variable.Name}' в процедуре '{routine.Name}'.",
                    instruction.Location);
                return null;
            }

            operands.Add(targetKind);
            operands.Add(slot);
        }

        return new BytecodeInstruction(Opcode.Input, operands);
    }

    private BytecodeInstruction? EmitPushErrorHandler(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        IReadOnlyDictionary<string, int> labelTargets)
    {
        if (!TryResolveOptionalLabel(routine, instruction, labelTargets, instruction.CatchLabel, out var catchTarget)
            || !TryResolveOptionalLabel(routine, instruction, labelTargets, instruction.FinallyLabel, out var finallyTarget)
            || instruction.EndLabel is null
            || !labelTargets.TryGetValue(instruction.EndLabel, out var endTarget))
        {
            Report(
                "BC5012",
                $"Эмиттер байткода не нашёл метки обработчика TRY в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        var targetKind = -1;
        var targetSlot = -1;
        if (instruction.Variable is not null)
        {
            var targetInstruction = LoweredInstruction.StoreVariable(instruction.Location, instruction.Variable);
            if (!TryResolveVariableSlot(routine, targetInstruction, parameterSlots, localSlots, captureSlots, out var storeOpcode, out targetSlot, isStore: true))
            {
                return null;
            }

            if (!TryMapErrorTargetKind(storeOpcode, out targetKind))
            {
                Report(
                    "BC5019",
                    $"Эмиттер байткода не смог представить переменную CATCH '{instruction.Variable.Name}' в процедуре '{routine.Name}'.",
                    instruction.Location);
                return null;
            }
        }

        return new BytecodeInstruction(
            Opcode.PushErrorHandler,
            [catchTarget, finallyTarget, endTarget, targetKind, targetSlot]);
    }

    private bool TryResolveOptionalLabel(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<string, int> labelTargets,
        string? label,
        out int target)
    {
        target = -1;
        if (label is null)
        {
            return true;
        }

        if (labelTargets.TryGetValue(label, out target))
        {
            return true;
        }

        Report(
            "BC5012",
            $"Эмиттер байткода не нашёл метку '{label}' в процедуре '{routine.Name}'.",
            instruction.Location);
        return false;
    }

    private static bool TryMapErrorTargetKind(Opcode storeOpcode, out int targetKind)
    {
        switch (storeOpcode)
        {
            case Opcode.StoreGlobal:
                targetKind = 0;
                return true;

            case Opcode.StoreLocal:
                targetKind = 1;
                return true;

            case Opcode.StoreArgument:
                targetKind = 2;
                return true;

            case Opcode.StoreCapture:
                targetKind = 3;
                return true;

            default:
                targetKind = default;
                return false;
        }
    }

    private static bool TryMapInputTargetKind(Opcode storeOpcode, out int targetKind)
    {
        switch (storeOpcode)
        {
            case Opcode.StoreGlobal:
                targetKind = 0;
                return true;

            case Opcode.StoreLocal:
                targetKind = 1;
                return true;

            case Opcode.StoreArgument:
                targetKind = 2;
                return true;

            case Opcode.StoreCapture:
                targetKind = 3;
                return true;

            default:
                targetKind = default;
                return false;
        }
    }

    private BytecodeInstruction? EmitJump(
        LoweredRoutine routine,
        LoweredInstruction instruction,
        IReadOnlyDictionary<string, int> labelTargets,
        Opcode opcode)
    {
        if (instruction.Label is null || !labelTargets.TryGetValue(instruction.Label, out var targetInstruction))
        {
            Report(
                "BC5012",
                $"Эмиттер байткода не нашёл метку '{instruction.Label ?? "<null>"}' в процедуре '{routine.Name}'.",
                instruction.Location);
            return null;
        }

        return new BytecodeInstruction(opcode, [targetInstruction]);
    }

    private BytecodeInstruction? ReportUnsupportedInstruction(LoweredRoutine routine, LoweredInstruction instruction)
    {
        Report(
            "BC5013",
            $"Эмиттер байткода пока не поддерживает lowered-инструкцию '{instruction.Kind}' в процедуре '{routine.Name}'.",
            instruction.Location);
        return null;
    }

    private int RegisterConstant(BytecodeConstantKind kind, object? value)
    {
        var key = new ConstantKey(kind, value);
        if (_constantIndices.TryGetValue(key, out var index))
        {
            return index;
        }

        index = _constants.Count;
        _constants.Add(new BytecodeConstant(kind, value));
        _constantIndices[key] = index;
        return index;
    }

    private int RegisterGlobal(VariableSymbol variable)
    {
        if (_globalSlots.TryGetValue(variable, out var slot))
        {
            return slot;
        }

        slot = _globals.Count;
        _globals.Add(new BytecodeGlobal(
            slot,
            variable.Name,
            variable.Type,
            variable.IsReadOnly,
            GlobalInitializationStrategy.Deferred,
            variable.FixedStringLength));
        _globalSlots[variable] = slot;
        return slot;
    }

    private int RegisterBuiltin(FunctionSymbol function, BuiltinFunctionId builtinId)
    {
        RegisterConstant(BytecodeConstantKind.BuiltinName, function.Name);

        var key = BuiltinBindingKey.Create(function, builtinId);
        if (_builtinIndices.TryGetValue(key, out var index))
        {
            return index;
        }

        index = _builtins.Count;
        _builtins.Add(new BuiltinBinding(
            index,
            builtinId,
            function.Name,
            function.Parameters.Count,
            returnsValue: function.ReturnType != TypeSymbol.Void,
            function));
        _builtinIndices[key] = index;
        return index;
    }

    private static bool TryGetConstantKind(TypeSymbol? type, out BytecodeConstantKind kind)
    {
        if (type == TypeSymbol.Integer)
        {
            kind = BytecodeConstantKind.Integer;
            return true;
        }

        if (type == TypeSymbol.Long)
        {
            kind = BytecodeConstantKind.Long;
            return true;
        }

        if (type == TypeSymbol.Double)
        {
            kind = BytecodeConstantKind.Double;
            return true;
        }

        if (type == TypeSymbol.Decimal)
        {
            kind = BytecodeConstantKind.Decimal;
            return true;
        }

        if (type == TypeSymbol.String)
        {
            kind = BytecodeConstantKind.String;
            return true;
        }

        if (type == TypeSymbol.Boolean)
        {
            kind = BytecodeConstantKind.Boolean;
            return true;
        }

        kind = BytecodeConstantKind.Null;
        return false;
    }

    private static object CreateZeroLiteral(TypeSymbol type)
    {
        if (type == TypeSymbol.Double)
        {
            return 0d;
        }

        if (type == TypeSymbol.Long)
        {
            return 0L;
        }

        if (type == TypeSymbol.Decimal)
        {
            return 0m;
        }

        return 0;
    }

    private void Report(string code, string message, TextLocation location)
    {
        _diagnostics.Add(new BytecodeDiagnostic(code, message, location));
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
