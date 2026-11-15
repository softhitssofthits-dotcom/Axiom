using Axiom.Semantics;

namespace Axiom.Bytecode;

/// <summary>
/// Точный расчёт максимальной высоты стека вычислений процедуры обходом графа
/// переходов по единой таблице стек-эффектов — по образцу <c>stackdepth()</c>
/// CPython (<c>Python/compile.c</c>): глубина распространяется от входа по всем
/// достижимым инструкциям, максимум фиксируется перед снятием операндов каждой
/// инструкции. Результат записывается в <see cref="BytecodeRoutine"/>
/// как <c>co_stacksize</c>: во время исполнения стек кадра не перевыделяется.
/// </summary>
internal static class BytecodeStackDepth
{
    internal static int ComputeMaxEvaluationStackDepth(
        IReadOnlyList<BytecodeInstruction> instructions,
        IReadOnlyDictionary<int, TypeSymbol> resultTypesById,
        IReadOnlyDictionary<int, BuiltinBinding> builtinsById)
    {
        if (instructions.Count == 0)
        {
            return 1;
        }

        var incomingHeights = new Dictionary<int, int> { [0] = 0 };
        var pending = new Queue<int>();
        pending.Enqueue(0);
        var maxDepth = 0;

        while (pending.Count > 0)
        {
            var instructionIndex = pending.Dequeue();
            var incomingHeight = incomingHeights[instructionIndex];
            if (incomingHeight > maxDepth)
            {
                maxDepth = incomingHeight;
            }

            var instruction = instructions[instructionIndex];
            if (instruction.Opcode == Opcode.Return)
            {
                continue;
            }

            if (!BytecodeStackEffect.TryGet(instruction, resultTypesById, builtinsById, out var popCount, out var pushCount)
                || incomingHeight < popCount)
            {
                // Повреждённый или неописанный поток не мешает оценке глубины:
                // несоответствия эффектов сообщает валидатор (BC4053).
                continue;
            }

            var nextHeight = incomingHeight - popCount + pushCount;
            if (nextHeight > maxDepth)
            {
                maxDepth = nextHeight;
            }

            foreach (var successor in GetSuccessorInstructionIndices(instructions.Count, instructionIndex, instruction))
            {
                if (successor < 0 || successor >= instructions.Count || incomingHeights.ContainsKey(successor))
                {
                    continue;
                }

                incomingHeights[successor] = nextHeight;
                pending.Enqueue(successor);
            }
        }

        return Math.Max(1, maxDepth);
    }

    /// <summary>
    /// Преемники инструкции по графу потока управления. Общая для валидатора
    /// стека и расчёта глубины: расхождение между ними делало бы проверку
    /// высот и оценку ёмкости стека непоследовательными.
    /// </summary>
    internal static IEnumerable<int> GetSuccessorInstructionIndices(
        int instructionCount,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        switch (instruction.Opcode)
        {
            case Opcode.Jump:
                if (instruction.OperandCount >= 1)
                {
                    yield return instruction.GetOperand(0);
                }

                break;

            case Opcode.JumpIfLess:
            case Opcode.JumpIfLessOrEqual:
            case Opcode.JumpIfGreater:
            case Opcode.JumpIfGreaterOrEqual:
            case Opcode.JumpIfEqual:
            case Opcode.JumpIfNotEqual:
            case Opcode.JumpIfTrue:
            case Opcode.JumpIfFalse:
                if (instruction.OperandCount >= 1)
                {
                    yield return instruction.GetOperand(0);
                }

                if (instructionIndex + 1 < instructionCount)
                {
                    yield return instructionIndex + 1;
                }

                break;

            case Opcode.JumpIfForDoneLocal:
                // Выход из цикла FOR переходит к цели в операнде 3; прежде этот
                // преемник не посещался обходом высот стека.
                if (instruction.OperandCount >= 4)
                {
                    yield return instruction.GetOperand(3);
                }

                if (instructionIndex + 1 < instructionCount)
                {
                    yield return instructionIndex + 1;
                }

                break;

            default:
                if (instructionIndex + 1 < instructionCount)
                {
                    yield return instructionIndex + 1;
                }

                break;
        }
    }
}
