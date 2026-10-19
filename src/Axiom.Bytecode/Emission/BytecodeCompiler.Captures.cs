using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BytecodeCompiler
{
    private void ComputeTransitiveCaptures(IReadOnlyList<LoweredRoutine> routines)
    {
        var routinesBySymbol = new Dictionary<FunctionSymbol, LoweredRoutine>();
        foreach (var routine in routines)
        {
            if (routine.Symbol is not null)
            {
                routinesBySymbol[routine.Symbol] = routine;
            }
        }

        var ownVariables = new Dictionary<int, HashSet<VariableSymbol>>();
        var requiredCaptures = new Dictionary<int, List<VariableSymbol>>();
        var dependencies = new Dictionary<int, List<LoweredRoutine>>();

        foreach (var routine in routines)
        {
            var own = new HashSet<VariableSymbol>();
            if (routine.Symbol is not null)
            {
                foreach (var parameter in routine.Symbol.Parameters)
                {
                    own.Add(parameter);
                }
            }

            foreach (var local in routine.LocalVariables)
            {
                own.Add(local);
            }

            ownVariables[routine.Id] = own;

            var direct = new List<VariableSymbol>();
            var callees = new List<LoweredRoutine>();

            foreach (var instruction in routine.Instructions)
            {
                if (instruction.Variable is { } variable)
                {
                    AddDirect(variable);
                }

                foreach (var inputVariable in instruction.Variables)
                {
                    AddDirect(inputVariable);
                }

                if (instruction.Function is { } function
                    && routinesBySymbol.TryGetValue(function, out var callee)
                    && callee.Id != routine.Id)
                {
                    callees.Add(callee);
                }
            }

            requiredCaptures[routine.Id] = direct;
            dependencies[routine.Id] = callees;

            void AddDirect(VariableSymbol variable)
            {
                if (!variable.IsGlobal && !own.Contains(variable) && !direct.Contains(variable))
                {
                    direct.Add(variable);
                }
            }
        }

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var routine in routines)
            {
                var required = requiredCaptures[routine.Id];
                var own = ownVariables[routine.Id];

                foreach (var callee in dependencies[routine.Id])
                {
                    foreach (var variable in requiredCaptures[callee.Id])
                    {
                        if (variable.IsGlobal || own.Contains(variable) || required.Contains(variable))
                        {
                            continue;
                        }

                        required.Add(variable);
                        changed = true;
                    }
                }
            }
        }

        foreach (var (routineId, variables) in requiredCaptures)
        {
            _transitiveCaptures[routineId] = variables.ToArray();
        }
    }


    private Dictionary<VariableSymbol, int> CreateCaptureSlotMap(
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

        if (_transitiveCaptures.TryGetValue(routine.Id, out var transitiveCaptures))
        {
            foreach (var variable in transitiveCaptures)
            {
                AddCaptureSlot(variable);
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

}
