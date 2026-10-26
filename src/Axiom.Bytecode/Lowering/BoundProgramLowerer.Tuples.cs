using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    // Источник уже на стеке. Преобразование каждого компонента выполняется до
    // присваивания результата, а выражение источника вычисляется ровно один раз.
    private void LowerTupleConversion(TupleTypeSymbol source, TupleTypeSymbol target,
        TextLocation location, List<LoweredInstruction> instructions)
    {
        var sourceTemp = CreateInternalTemporary(source, "tuple_source");
        var resultTemp = CreateInternalTemporary(target, "tuple_result");
        instructions.Add(LoweredInstruction.StoreVariable(location, sourceTemp));
        instructions.Add(LoweredInstruction.CreateRecord(location, target));
        instructions.Add(LoweredInstruction.StoreVariable(location, resultTemp));
        for (var i = 0; i < source.Fields.Count; i++)
        {
            var from = source.Fields[i];
            var to = target.Fields[i];
            instructions.Add(LoweredInstruction.LoadVariable(location, sourceTemp));
            instructions.Add(LoweredInstruction.LoadField(location, from.Name));
            if (from.Type != to.Type)
            {
                if (from.Type is TupleTypeSymbol nestedSource && to.Type is TupleTypeSymbol nestedTarget)
                    LowerTupleConversion(nestedSource, nestedTarget, location, instructions);
                else
                    instructions.Add(LoweredInstruction.Convert(location, to.Type));
            }
            instructions.Add(LoweredInstruction.LoadVariable(location, resultTemp));
            instructions.Add(LoweredInstruction.StoreField(location, to.Name));
        }
        instructions.Add(LoweredInstruction.LoadVariable(location, resultTemp));
    }
}
