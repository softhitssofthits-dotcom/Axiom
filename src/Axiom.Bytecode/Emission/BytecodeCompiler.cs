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
        var localSlots = CreateLocalSlotMap(routine, parameterSlots);
        var captureSlots = CreateCaptureSlotMap(routine, parameterSlots, localSlots);
        var labelTargets = ResolveLabelTargets(routine);
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
                    labelTargets,
                    out bytecodeInstruction,
                    out sourceLocation))
            {
                loweredIndex++;
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

        _routineDebugInfo.Add(new BytecodeRoutineDebugInfo(
            routine.Id,
            parameterSlots.ToDictionary(static pair => pair.Value, static pair => pair.Key.Name),
            localSlots.ToDictionary(static pair => pair.Value, static pair => pair.Key.Name),
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
            localSlots.OrderBy(static pair => pair.Value).Select(static pair => pair.Key).ToArray(),
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
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots)
    {
        var slots = new Dictionary<VariableSymbol, int>();

        foreach (var variable in routine.LocalVariables)
        {
            if (variable.IsGlobal || variable is ParameterSymbol)
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

    private Dictionary<string, int> ResolveLabelTargets(LoweredRoutine routine)
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

            if (TryGetCollapsedInstructionWidth(routine.Instructions, loweredIndex, out var width))
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

    private static bool TryGetCollapsedInstructionWidth(IReadOnlyList<LoweredInstruction> instructions, int index, out int width)
    {
        width = 0;
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

        if (!TryGetCollapsedInstructionWidth(routine.Instructions, loweredIndex, out _))
        {
            return false;
        }

        instruction = new BytecodeInstruction(Opcode.AddLongInteger);
        sourceLocation = next.Location;
        return true;
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
            SyntaxKind.LessToken => jumpIfTrue ? Opcode.JumpIfLess : Opcode.JumpIfGreaterOrEqual,
            SyntaxKind.LessOrEqualsToken => jumpIfTrue ? Opcode.JumpIfLessOrEqual : Opcode.JumpIfGreater,
            SyntaxKind.GreaterToken => jumpIfTrue ? Opcode.JumpIfGreater : Opcode.JumpIfLessOrEqual,
            SyntaxKind.GreaterOrEqualsToken => jumpIfTrue ? Opcode.JumpIfGreaterOrEqual : Opcode.JumpIfLess,
            _ => default
        };

        return opcode is Opcode.JumpIfLess
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
            LoweredInstructionKind.UnaryOperation => EmitUnaryOperation(routine, instruction),
            LoweredInstructionKind.BinaryOperation => EmitBinaryOperation(routine, instruction),
            LoweredInstructionKind.Convert => EmitConversion(routine, instruction),
            LoweredInstructionKind.CallFunction => EmitCall(routine, instruction),
            LoweredInstructionKind.CallValue => EmitCallValue(instruction),
            LoweredInstructionKind.RedimArray => EmitRedimArray(instruction),
            LoweredInstructionKind.EraseArray => new BytecodeInstruction(Opcode.EraseArray),
            LoweredInstructionKind.LoadElement => EmitArrayElementInstruction(instruction, Opcode.LoadElement),
            LoweredInstructionKind.StoreElement => EmitArrayElementInstruction(instruction, Opcode.StoreElement),
            LoweredInstructionKind.LoadField => EmitFieldInstruction(instruction, Opcode.LoadField),
            LoweredInstructionKind.StoreField => EmitFieldInstruction(instruction, Opcode.StoreField),
            LoweredInstructionKind.OpenFile => EmitOpenFile(instruction),
            LoweredInstructionKind.CloseFile => new BytecodeInstruction(Opcode.CloseFile),
            LoweredInstructionKind.ValidateForStep => new BytecodeInstruction(Opcode.ValidateForStep),
            LoweredInstructionKind.PrintValue => new BytecodeInstruction(Opcode.PrintValue),
            LoweredInstructionKind.PrintUsing => EmitPrintUsing(instruction),
            LoweredInstructionKind.PrintSeparator => EmitPrintSeparator(instruction),
            LoweredInstructionKind.PrintLine => new BytecodeInstruction(Opcode.PrintLine),
            LoweredInstructionKind.Input => EmitInput(routine, instruction, parameterSlots, localSlots, captureSlots),
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

        if (instruction.Variable.IsGlobal)
        {
            opcode = isStore ? Opcode.StoreGlobal : Opcode.LoadGlobal;
            slot = RegisterGlobal(instruction.Variable);
            return true;
        }

        if (localSlots.TryGetValue(instruction.Variable, out slot))
        {
            opcode = isStore ? Opcode.StoreLocal : Opcode.LoadLocal;
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

    private void Report(string code, string message, TextLocation location)
    {
        _diagnostics.Add(new BytecodeDiagnostic(code, message, location));
    }

    private readonly record struct ConstantKey(BytecodeConstantKind Kind, object? Value);

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
