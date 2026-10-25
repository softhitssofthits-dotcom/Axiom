using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Bytecode.Tests;

public sealed class ArrayLiteralLoweringTests
{
    [Fact]
    public void LiteralRedimsFromOneAndStoresEachElement()
    {
        var instructions = Lower("DIM fruits[] AS STRING = [\"яблоко\", \"банан\"]");

        var redim = Assert.Single(instructions, i => i.Kind == LoweredInstructionKind.RedimArray);
        Assert.Equal(1, redim.ValueCount);
        Assert.False(redim.Preserve);

        var stores = instructions.Where(i => i.Kind == LoweredInstructionKind.StoreElement).ToList();
        Assert.Equal(2, stores.Count);
        Assert.All(stores, store => Assert.Equal(1, store.ValueCount));

        // Объявленная переменная получает готовый литерал ровно один раз.
        var storeToVariable = Assert.Single(
            instructions,
            i => i.Kind == LoweredInstructionKind.StoreVariable && i.Variable!.Name == "fruits");
        Assert.True(storeToVariable.Variable!.IsArray);
    }

    [Fact]
    public void EmptyLiteralStillCreatesCanonicalEmptyArray()
    {
        var instructions = Lower("DIM values[] AS INTEGER = []");

        var redim = Assert.Single(instructions, i => i.Kind == LoweredInstructionKind.RedimArray);
        Assert.Equal(1, redim.ValueCount);
        Assert.DoesNotContain(instructions, i => i.Kind == LoweredInstructionKind.StoreElement);
    }

    [Fact]
    public void ElementsAreLoweredInSourceOrder()
    {
        var instructions = Lower(
            "FUNCTION Bump() AS INTEGER\r\n" +
            "    RETURN 1\r\n" +
            "END FUNCTION\r\n" +
            "DIM values[] AS INTEGER = [Bump(), Bump(), Bump()]");

        var calls = instructions
            .Select((instruction, index) => (instruction, index))
            .Where(item => item.instruction.Kind == LoweredInstructionKind.CallFunction)
            .Select(item => item.index)
            .ToList();
        var stores = instructions
            .Select((instruction, index) => (instruction, index))
            .Where(item => item.instruction.Kind == LoweredInstructionKind.StoreElement)
            .Select(item => item.index)
            .ToList();

        Assert.Equal(3, calls.Count);
        Assert.Equal(3, stores.Count);
        for (var index = 0; index < calls.Count; index++)
        {
            Assert.True(calls[index] < stores[index], "Элемент вычисляется до сохранения.");
            if (index > 0)
            {
                Assert.True(stores[index - 1] < calls[index], "Элементы идут слева направо.");
            }
        }
    }

    [Fact]
    public void LiteralModulePassesValidation()
    {
        var syntax = Parser.Parse("DIM fruits[] AS STRING = [\"яблоко\", \"банан\"]\r\nPRINT fruits[2]", out var parsing);
        Assert.Empty(parsing);
        var bound = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var result = new BoundProgramLowerer().Lower(bound);
        Assert.True(result.Succeeded);

        var compilation = new BytecodeCompiler().Compile(result.Program!);
        Assert.True(compilation.Succeeded);
        Assert.Empty(new BytecodeModuleValidator().Validate(compilation.Module!).Diagnostics);
    }

    private static IReadOnlyList<LoweredInstruction> Lower(string source)
    {
        var syntax = Parser.Parse(source, out var parsing);
        Assert.Empty(parsing);
        var bound = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var result = new BoundProgramLowerer().Lower(bound);
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        return result.Program!.EntryPoint.Instructions;
    }
}
