using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BytecodeCompiler
{
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
            LoweredInstructionKind.FreshLocal => new BytecodeInstruction(Opcode.FreshLocal, [localSlots[instruction.Variable!]]),
            LoweredInstructionKind.ClearForEachItem => new BytecodeInstruction(Opcode.ClearForEachItem, [localSlots[instruction.Variable!]]),
            LoweredInstructionKind.BindArrayElement => EmitBindArrayElement(instruction, localSlots),
            LoweredInstructionKind.GetArrayStructuralVersion => new BytecodeInstruction(Opcode.GetArrayStructuralVersion),
            LoweredInstructionKind.AppendStringVariable => EmitAppendStringVariable(routine, instruction, parameterSlots, localSlots, captureSlots),
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
                [instruction.HandlerReceivesKeyText ? 1 : 0, instruction.Flags]),
            LoweredInstructionKind.UnregisterMouseHandler => new BytecodeInstruction(Opcode.UnregisterMouseHandler, [instruction.Flags]),
            LoweredInstructionKind.PauseMouseHandler => new BytecodeInstruction(Opcode.PauseMouseHandler, [instruction.Flags]),
            LoweredInstructionKind.ResumeMouseHandler => new BytecodeInstruction(Opcode.ResumeMouseHandler, [instruction.Flags]),
            LoweredInstructionKind.Screen => new BytecodeInstruction(Opcode.Screen),
            LoweredInstructionKind.Smooth => new BytecodeInstruction(Opcode.Smooth, [instruction.Flags]),
            LoweredInstructionKind.Canvas => new BytecodeInstruction(Opcode.Canvas),
            LoweredInstructionKind.BeginFrame => new BytecodeInstruction(Opcode.BeginFrame),
            LoweredInstructionKind.Present => new BytecodeInstruction(Opcode.Present),
            LoweredInstructionKind.Camera => new BytecodeInstruction(Opcode.Camera),
            LoweredInstructionKind.Viewport => new BytecodeInstruction(Opcode.Viewport, [instruction.ValueCount]),
            LoweredInstructionKind.ResetCamera => new BytecodeInstruction(Opcode.ResetCamera),
            LoweredInstructionKind.View => new BytecodeInstruction(Opcode.View, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.ViewPrint => new BytecodeInstruction(Opcode.ViewPrint, [instruction.ValueCount]),
            LoweredInstructionKind.Window => new BytecodeInstruction(Opcode.Window, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.Cls => new BytecodeInstruction(Opcode.Cls, [instruction.ValueCount]),
            LoweredInstructionKind.Color => new BytecodeInstruction(Opcode.Color, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.Locate => new BytecodeInstruction(Opcode.Locate),
            LoweredInstructionKind.Palette => new BytecodeInstruction(Opcode.Palette, [instruction.ValueCount]),
            LoweredInstructionKind.PCopy => new BytecodeInstruction(Opcode.PCopy),
            LoweredInstructionKind.PSet => new BytecodeInstruction(Opcode.PSet, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.Get => new BytecodeInstruction(Opcode.Get, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.Put => new BytecodeInstruction(Opcode.Put, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.Line => new BytecodeInstruction(Opcode.Line, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.Circle => new BytecodeInstruction(Opcode.Circle, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.Paint => new BytecodeInstruction(Opcode.Paint, [instruction.Flags, instruction.ValueCount]),
            LoweredInstructionKind.Draw => new BytecodeInstruction(Opcode.Draw, [instruction.ValueCount]),
            LoweredInstructionKind.DrawLine => new BytecodeInstruction(Opcode.DrawLine),
            LoweredInstructionKind.DrawCircle => new BytecodeInstruction(Opcode.DrawCircle),
            LoweredInstructionKind.DrawRect => new BytecodeInstruction(Opcode.DrawRect),
            LoweredInstructionKind.FillCircle => new BytecodeInstruction(Opcode.FillCircle),
            LoweredInstructionKind.FillRect => new BytecodeInstruction(Opcode.FillRect),
            LoweredInstructionKind.DrawImage => new BytecodeInstruction(Opcode.DrawImage, [instruction.ValueCount]),
            LoweredInstructionKind.DrawSprite => new BytecodeInstruction(Opcode.DrawSprite, [instruction.ValueCount]),
            LoweredInstructionKind.DrawTile => new BytecodeInstruction(Opcode.DrawTile),
            LoweredInstructionKind.DrawTilemap => new BytecodeInstruction(Opcode.DrawTilemap),
            LoweredInstructionKind.DrawTileLayer => new BytecodeInstruction(Opcode.DrawTileLayer),
            LoweredInstructionKind.SetMapTile => new BytecodeInstruction(Opcode.SetMapTile),
            LoweredInstructionKind.DrawText => new BytecodeInstruction(Opcode.DrawText),
            LoweredInstructionKind.PrintValue => new BytecodeInstruction(Opcode.PrintValue),
            LoweredInstructionKind.PrintUsing => EmitPrintUsing(instruction),
            LoweredInstructionKind.PrintSeparator => EmitPrintSeparator(instruction),
            LoweredInstructionKind.PrintLine => new BytecodeInstruction(Opcode.PrintLine),
            LoweredInstructionKind.Input => EmitInput(routine, instruction, parameterSlots, localSlots, captureSlots),
            LoweredInstructionKind.Sleep => new BytecodeInstruction(Opcode.Sleep, [instruction.ValueCount]),
            LoweredInstructionKind.ReseedRandom => new BytecodeInstruction(Opcode.ReseedRandom, [instruction.ValueCount]),
            LoweredInstructionKind.FormatInterpolatedPart => new BytecodeInstruction(Opcode.FormatInterpolatedPart),
            LoweredInstructionKind.PushErrorHandler => EmitPushErrorHandler(routine, instruction, parameterSlots, localSlots, captureSlots, labelTargets),
            LoweredInstructionKind.PopErrorHandler => new BytecodeInstruction(Opcode.PopErrorHandler),
            LoweredInstructionKind.RaiseError => new BytecodeInstruction(Opcode.RaiseError, [instruction.ValueCount]),
            LoweredInstructionKind.EndFinally => new BytecodeInstruction(Opcode.EndFinally),
            LoweredInstructionKind.Jump => EmitJump(routine, instruction, labelTargets, Opcode.Jump),
            LoweredInstructionKind.JumpIfTrue => EmitJump(routine, instruction, labelTargets, Opcode.JumpIfTrue),
            LoweredInstructionKind.JumpIfFalse => EmitJump(routine, instruction, labelTargets, Opcode.JumpIfFalse),
            LoweredInstructionKind.Pop => new BytecodeInstruction(Opcode.Pop),
            LoweredInstructionKind.Return => new BytecodeInstruction(Opcode.Return),
            LoweredInstructionKind.SnapshotValue => new BytecodeInstruction(Opcode.SnapshotValue),
            LoweredInstructionKind.NewArray => EmitNewArray(instruction),
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

        if (type == TypeSymbol.String)
        {
            typeCode = BytecodeScalarTypeCode.String;
            return true;
        }

        if (type == TypeSymbol.Date)
        {
            typeCode = BytecodeScalarTypeCode.Date;
            return true;
        }

        if (type == TypeSymbol.Time)
        {
            typeCode = BytecodeScalarTypeCode.Time;
            return true;
        }

        if (type == TypeSymbol.DateTime)
        {
            typeCode = BytecodeScalarTypeCode.DateTime;
            return true;
        }

        if (type == TypeSymbol.UtcDateTime)
        {
            typeCode = BytecodeScalarTypeCode.UtcDateTime;
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

            if (!TryResolveVariableSource(variable, parameterSlots, localSlots, captureSlots, out var sourceKind, out var sourceSlot))
            {
                Report(
                    "BC5012",
                    $"Эмиттер байткода не смог разрешить источник BYREF-аргумента '{variable.Name}' в процедуре '{routine.Name}'.",
                    instruction.Location);
                return null;
            }

            var address = index < instruction.ByRefAddresses.Count ? instruction.ByRefAddresses[index] : null;
            if (address is null)
            {
                operands.Add(sourceKind);
                operands.Add(sourceSlot);
                continue;
            }

            operands.Add(ByRefArgumentSourceKind.Address);
            operands.Add(sourceKind);
            operands.Add(sourceSlot);
            operands.Add(address.Steps.Count);

            foreach (var step in address.Steps)
            {
                if (step.IsField)
                {
                    operands.Add(0);
                    operands.Add(RegisterConstant(BytecodeConstantKind.String, step.FieldName!));
                    continue;
                }

                operands.Add(1);
                operands.Add(step.IndexVariables.Count);

                foreach (var indexVariable in step.IndexVariables)
                {
                    if (!TryResolveVariableSource(indexVariable, parameterSlots, localSlots, captureSlots, out var indexKind, out var indexSlot))
                    {
                        Report(
                            "BC5013",
                            $"Эмиттер байткода не смог разрешить индекс адреса BYREF '{variable.Name}' в процедуре '{routine.Name}'.",
                            instruction.Location);
                        return null;
                    }

                    operands.Add(indexKind);
                    operands.Add(indexSlot);
                }
            }
        }

        return new BytecodeInstruction(Opcode.CallByRef, operands.ToArray());
    }

    private bool TryResolveVariableSource(
        VariableSymbol variable,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out int sourceKind,
        out int sourceSlot)
    {
        if (variable is ParameterSymbol parameter && parameterSlots.TryGetValue(parameter, out sourceSlot))
        {
            sourceKind = ByRefArgumentSourceKind.Argument;
            return true;
        }

        if (localSlots.TryGetValue(variable, out sourceSlot))
        {
            sourceKind = ByRefArgumentSourceKind.Local;
            return true;
        }

        if (captureSlots.TryGetValue(variable, out sourceSlot))
        {
            sourceKind = ByRefArgumentSourceKind.Capture;
            return true;
        }

        if (variable.IsGlobal)
        {
            sourceKind = ByRefArgumentSourceKind.Global;
            sourceSlot = RegisterGlobal(variable);
            return true;
        }

        sourceKind = -1;
        sourceSlot = -1;
        return false;
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

    private BytecodeInstruction EmitNewArray(LoweredInstruction instruction)
    {
        return new BytecodeInstruction(
            Opcode.NewArray,
            [
                RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName),
                instruction.ValueCount
            ]);
    }

    private static BytecodeInstruction EmitBindArrayElement(
        LoweredInstruction instruction,
        IReadOnlyDictionary<VariableSymbol, int> localSlots)
    {
        var operands = new int[4 + instruction.ValueCount];
        operands[0] = localSlots[instruction.Variables[0]];
        operands[1] = localSlots[instruction.Variables[1]];
        operands[2] = localSlots[instruction.Variables[2]];
        operands[3] = instruction.ValueCount;

        for (var index = 0; index < instruction.ValueCount; index++)
        {
            operands[index + 4] = localSlots[instruction.Variables[index + 3]];
        }

        return new BytecodeInstruction(Opcode.BindArrayElement, operands);
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
        // Идентичность типа, а не короткое имя: закрытые TYPE разных модулей
        // могут называться одинаково, и словарь по имени потерял бы различие.
        var typeIdentity = instruction.ValueType is UserDefinedTypeSymbol userDefinedType
            ? userDefinedType.TypeIdentity
            : instruction.DisplayName;

        return new BytecodeInstruction(Opcode.CreateRecord, [RegisterConstant(BytecodeConstantKind.String, typeIdentity)]);
    }

    private BytecodeInstruction EmitValidateTypeInvariant(LoweredInstruction instruction)
    {
        // Второй операнд — идентификатор процедуры конструктора: VM использует его,
        // чтобы показать кадр конструктора в цепочке вызовов диагностики инварианта.
        if (instruction.Function is not null
            && _routineIds.TryGetValue(instruction.Function, out var constructorRoutineId))
        {
            return new BytecodeInstruction(
                Opcode.ValidateTypeInvariant,
                [RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName), constructorRoutineId]);
        }

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
            instruction.Flags == 1
                ? [mode, RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName), 1]
                : [mode, RegisterConstant(BytecodeConstantKind.String, instruction.DisplayName)]);
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

}
