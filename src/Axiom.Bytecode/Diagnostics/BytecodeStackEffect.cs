using Axiom.Semantics;

namespace Axiom.Bytecode;

internal static class BytecodeStackEffect
{
    internal static bool TryGet(
        BytecodeInstruction instruction,
        IReadOnlyDictionary<int, BytecodeRoutine> routinesById,
        IReadOnlyDictionary<int, BuiltinBinding> builtinsById,
        out int popCount,
        out int pushCount)
    {
        popCount = 0;
        pushCount = 0;

        switch (instruction.Opcode)
        {
            case Opcode.PushConst:
            case Opcode.PushIntegerZero:
            case Opcode.PushIntegerOne:
            case Opcode.LoadFunction:
            case Opcode.LoadBuiltinFunction:
            case Opcode.LoadGlobal:
            case Opcode.LoadLocal:
            case Opcode.LoadArgument:
            case Opcode.LoadCapture:
            case Opcode.CreateRecord:
                pushCount = 1;
                return true;

            case Opcode.JumpIfForDoneLocal:
            case Opcode.AdvanceForLoopLocal:
                return true;

            case Opcode.StoreGlobal:
            case Opcode.StoreLocal:
            case Opcode.StoreArgument:
            case Opcode.StoreCapture:
            case Opcode.Pop:
            case Opcode.UnregisterKeyHandler:
            case Opcode.PauseKeyHandler:
            case Opcode.ResumeKeyHandler:
            case Opcode.UnregisterMouseHandler:
            case Opcode.PauseMouseHandler:
            case Opcode.ResumeMouseHandler:
            case Opcode.Negate:
            case Opcode.Not:
            case Opcode.Convert:
            case Opcode.ValidateTypeInvariant:
            case Opcode.ValidateForStep:
            case Opcode.PrintValue:
            case Opcode.EraseArray:
            case Opcode.AppendStringGlobal:
            case Opcode.AppendStringLocal:
            case Opcode.AppendStringArgument:
            case Opcode.AppendStringCapture:
                popCount = 1;
                pushCount = instruction.Opcode is Opcode.Negate or Opcode.Not or Opcode.Convert ? 1 : 0;
                return true;

            case Opcode.FormatInterpolatedPart:
                popCount = 3;
                pushCount = 1;
                return true;

            case Opcode.SnapshotValue:
                popCount = 1;
                pushCount = 1;
                return true;

            case Opcode.OpenFile:
                popCount = instruction.OperandCount == 3 ? 3 : 2;
                pushCount = 1;
                return true;

            case Opcode.AddLongInteger:
            case Opcode.Add:
            case Opcode.Concat:
            case Opcode.Subtract:
            case Opcode.Multiply:
            case Opcode.Power:
            case Opcode.Divide:
            case Opcode.IntegralDivide:
            case Opcode.Modulo:
            case Opcode.And:
            case Opcode.Or:
            case Opcode.Xor:
            case Opcode.Eqv:
            case Opcode.Imp:
            case Opcode.ShiftLeft:
            case Opcode.ShiftRight:
            case Opcode.CompareEqual:
            case Opcode.CompareNotEqual:
            case Opcode.CompareLess:
            case Opcode.CompareLessOrEqual:
            case Opcode.CompareGreater:
            case Opcode.CompareGreaterOrEqual:
            case Opcode.StoreField:
                popCount = 2;
                pushCount = instruction.Opcode is Opcode.StoreField ? 0 : 1;
                return true;

            case Opcode.RegisterKeyHandler:
            case Opcode.RegisterMouseHandler:
                popCount = 2;
                return true;

            case Opcode.LoadElement:
                popCount = instruction.GetOperand(1) + CountRangeBits(instruction) + 1;
                pushCount = 1;
                return true;

            case Opcode.RedimArray:
                popCount = (instruction.GetOperand(2) * 2) + 1;
                return true;

            case Opcode.NewArray:
                // Образец массива заменяется независимым массивом того же типа.
                popCount = 1;
                pushCount = 1;
                return true;

            case Opcode.GetArrayStructuralVersion:
                popCount = 1;
                pushCount = 1;
                return true;

            case Opcode.FreshLocal:
            case Opcode.BindArrayElement:
            case Opcode.ClearForEachItem:
                return true;

            case Opcode.CreateDictionary:
                pushCount = 1;
                return true;

            case Opcode.LoadDictionaryValue:
                // Словарь и ключ заменяются значением записи.
                popCount = 2;
                pushCount = 1;
                return true;

            case Opcode.StoreDictionaryValue:
                // Значение, словарь и ключ уходят со стека.
                popCount = 3;
                return true;

            case Opcode.DictionaryAdd:
                popCount = 3;
                return true;

            case Opcode.DictionaryExists:
            case Opcode.DictionaryRemove:
                popCount = 2;
                pushCount = 1;
                return true;

            case Opcode.DictionaryKeys:
            case Opcode.DictionaryCount:
                popCount = 1;
                pushCount = 1;
                return true;

            case Opcode.DictionaryTryGetValue:
                // Словарь и ключ заменяются признаком наличия; переменная
                // результата записывается самой инструкцией.
                popCount = 2;
                pushCount = 1;
                return true;

            case Opcode.CreateSet:
                pushCount = 1;
                return true;

            case Opcode.SetAdd:
            case Opcode.SetContains:
            case Opcode.SetRemove:
                // Множество и элемент заменяются признаком BOOLEAN.
                popCount = 2;
                pushCount = 1;
                return true;

            case Opcode.SetCount:
                popCount = 1;
                pushCount = 1;
                return true;

            case Opcode.SetToArray:
                // Множество заменяется независимым массивом элементов.
                popCount = 1;
                pushCount = 1;
                return true;

            case Opcode.SetUnionWith:
            case Opcode.SetIntersectWith:
                // Получатель и второе множество уходят со стека: результат
                // записывается в получателя, значение не возвращается.
                popCount = 2;
                return true;
            case Opcode.StoreElement:
                popCount = instruction.GetOperand(1) + 2;
                return true;

            case Opcode.LoadField:
                popCount = 1;
                pushCount = 1;
                return true;

            case Opcode.Call:
            case Opcode.CallByRef:
                if (instruction.OperandCount < 2 || !routinesById.TryGetValue(instruction.GetOperand(0), out var targetRoutine))
                {
                    return false;
                }

                popCount = instruction.Opcode == Opcode.CallByRef ? instruction.GetOperand(2) : instruction.GetOperand(1);
                pushCount = targetRoutine.ResultType == TypeSymbol.Void ? 0 : 1;
                return true;

            case Opcode.CallBuiltin:
                if (instruction.OperandCount < 2 || !builtinsById.TryGetValue(instruction.GetOperand(0), out var builtin))
                {
                    return false;
                }

                popCount = instruction.GetOperand(1);
                pushCount = builtin.ReturnsValue ? 1 : 0;
                return true;

            case Opcode.CallValue:
                if (instruction.OperandCount < 2)
                {
                    return false;
                }

                popCount = instruction.GetOperand(0) == int.MaxValue
                    ? int.MaxValue
                    : instruction.GetOperand(0) + 1;
                pushCount = instruction.GetOperand(1) == 0 ? 0 : 1;
                return true;

            case Opcode.PrintUsing:
                if (instruction.OperandCount < 2)
                {
                    return false;
                }

                popCount = instruction.GetOperand(0) == int.MaxValue
                    ? int.MaxValue
                    : instruction.GetOperand(0) + 1;
                return true;

            case Opcode.ViewPrint:
                popCount = instruction.GetOperand(0);
                return true;

            case Opcode.Cls:
                popCount = instruction.GetOperand(0);
                return true;

            case Opcode.Canvas:
                popCount = 2;
                return true;

            case Opcode.Camera:
                popCount = 2;
                return true;

            case Opcode.Viewport:
                popCount = instruction.GetOperand(0);
                return true;

            case Opcode.Line:
            {
                var flags = instruction.GetOperand(0);
                var count = instruction.GetOperand(1);
                var hasExplicitStartPoint = (flags & 4) != 0;
                var hasColor = hasExplicitStartPoint ? count == 5 : count == 3;
                popCount = 2 + (hasColor ? 1 : 0) + (hasExplicitStartPoint ? 2 : 0);
                return true;
            }

            case Opcode.Screen:
                popCount = 4;
                return true;

            case Opcode.PCopy:
                popCount = 2;
                return true;

            case Opcode.Locate:
                popCount = 2;
                return true;

            case Opcode.View:
            {
                var flags = instruction.GetOperand(0);
                popCount = ((flags & 2) != 0 ? 4 : 0)
                    + ((flags & 4) != 0 ? 1 : 0)
                    + ((flags & 8) != 0 ? 1 : 0);
                return true;
            }

            case Opcode.Window:
                popCount = (instruction.GetOperand(0) & 2) != 0 ? 4 : 0;
                return true;

            case Opcode.Color:
                popCount = instruction.GetOperand(1) == 2 ? 2 : 1;
                return true;

            case Opcode.Palette:
                popCount = instruction.GetOperand(0);
                return true;

            case Opcode.PSet:
                popCount = instruction.GetOperand(1) == 3 ? 3 : 2;
                return true;

            case Opcode.Get:
                popCount = 5;
                return true;

            case Opcode.Put:
                popCount = 3;
                return true;

            case Opcode.Circle:
            {
                var flags = instruction.GetOperand(0);
                popCount = 3
                    + ((flags & 16) != 0 ? 1 : 0)
                    + ((flags & 2) != 0 ? 1 : 0)
                    + ((flags & 4) != 0 ? 1 : 0)
                    + ((flags & 8) != 0 ? 1 : 0);
                return true;
            }

            case Opcode.Paint:
            {
                var flags = instruction.GetOperand(0);
                popCount = 2
                    + ((flags & 2) != 0 ? 1 : 0)
                    + ((flags & 4) != 0 ? 1 : 0)
                    + ((flags & 8) != 0 ? 1 : 0);
                return true;
            }

            case Opcode.Draw:
                popCount = instruction.GetOperand(0);
                return true;

            case Opcode.DrawCircle:
            case Opcode.FillCircle:
            case Opcode.DrawText:
                popCount = 4;
                return true;

            case Opcode.DrawLine:
            case Opcode.DrawRect:
            case Opcode.FillRect:
                popCount = 5;
                return true;

            case Opcode.DrawImage:
                popCount = instruction.GetOperand(0) == 0 ? 3 : 5;
                return true;

            case Opcode.DrawSprite:
                popCount = instruction.GetOperand(0) == 0 ? 4 : 6;
                return true;

            case Opcode.DrawTile:
                popCount = 4;
                return true;

            case Opcode.DrawTilemap:
                popCount = 3;
                return true;

            case Opcode.DrawTileLayer:
                popCount = 4;
                return true;

            case Opcode.SetMapTile:
                popCount = 7;
                return true;

            case Opcode.JumpIfTrue:
            case Opcode.JumpIfFalse:
                popCount = 1;
                return true;

            case Opcode.JumpIfEqual:
            case Opcode.JumpIfNotEqual:
            case Opcode.JumpIfLess:
            case Opcode.JumpIfLessOrEqual:
            case Opcode.JumpIfGreater:
            case Opcode.JumpIfGreaterOrEqual:
                popCount = 2;
                return true;

            case Opcode.Jump:
            case Opcode.StatementBoundary:
            case Opcode.PrintSeparator:
            case Opcode.PrintLine:
            case Opcode.Input:
            case Opcode.BeginFrame:
            case Opcode.Present:
            case Opcode.ResetCamera:
            case Opcode.Smooth:
            case Opcode.ReseedRandom when instruction.GetOperand(0) == 0:
            case Opcode.Sleep when instruction.GetOperand(0) == 0:
            case Opcode.PushErrorHandler:
            case Opcode.PopErrorHandler:
            case Opcode.EndFinally:
                return true;

            case Opcode.ReseedRandom:
            case Opcode.Sleep:
                popCount = instruction.GetOperand(0);
                return true;

            case Opcode.CloseFile:
                popCount = 1;
                pushCount = 1;
                return true;

            case Opcode.RaiseError:
                popCount = instruction.GetOperand(0) == 1 ? 2 : 1;
                return true;

            default:
                return false;
        }
    }

    internal static int CountRangeBits(BytecodeInstruction instruction)
    {
        if (instruction.Opcode != Opcode.LoadElement || instruction.OperandCount < 3)
        {
            return 0;
        }

        var mask = instruction.GetOperand(2);
        var count = 0;

        while (mask > 0)
        {
            count += mask & 1;
            mask >>= 1;
        }

        return count;
    }
}
