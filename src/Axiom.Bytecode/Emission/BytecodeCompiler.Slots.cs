using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BytecodeCompiler
{
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
        // Слитая раскладка слотов кадра (localsplus): параметры занимают первые
        // N слотов по порядку объявления, локальные переменные продолжают
        // нумерацию без отдельного пространства слотов.
        var localOffset = parameterSlots.Count;

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
                slots[variable] = localOffset + slots.Count;
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

    private static VariableSymbol[] CreateRoutineLocalVariables(
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots)
    {
        // Единый список слотов кадра: параметры в позициях 0..N-1, локальные
        // переменные далее. Тип параметра в слоте нужен немедленным записям
        // и созданию значений по умолчанию во время исполнения.
        var variables = new VariableSymbol[parameterSlots.Count + localSlots.Count];
        foreach (var (parameter, slot) in parameterSlots)
        {
            variables[slot] = parameter;
        }

        foreach (var (variable, slot) in localSlots)
        {
            variables[slot] = variable.IsGlobal
                ? new VariableSymbol(variable.Name, variable.Type, variable.IsReadOnly, isGlobal: false, variable.FixedStringLength)
                : variable;
        }

        return variables;
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

}
