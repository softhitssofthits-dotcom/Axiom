using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Bytecode.Tests;

public sealed class ArrayInitializerLoweringTests
{
    [Theory]
    [InlineData("DIM fruits[] = SPLIT(\"яблоко,банан\", \",\")")]
    [InlineData("DIM fruits[] AS STRING = SPLIT(\"яблоко,банан\", \",\")")]
    public void ArrayInitializerCallsExpressionOnceThenStoresWithoutRedim(string source)
    {
        var syntax = Parser.Parse(source, out var parsing);
        Assert.Empty(parsing);
        var bound = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var result = new BoundProgramLowerer().Lower(bound);
        Assert.True(result.Succeeded);
        var instructions = result.Program!.EntryPoint.Instructions;
        var call = Assert.Single(instructions, i => i.Kind == LoweredInstructionKind.CallFunction);
        Assert.Equal(BuiltinFunctionId.SplitSeparator, call.Function!.BuiltinId);
        var store = Assert.Single(instructions, i => i.Kind == LoweredInstructionKind.StoreVariable);
        Assert.Equal("fruits", store.Variable!.Name);
        Assert.True(store.Variable.IsArray);
        Assert.DoesNotContain(instructions, i => i.Kind == LoweredInstructionKind.RedimArray);
        Assert.True(instructions.ToList().IndexOf(call) < instructions.ToList().IndexOf(store));
        var compilation = new BytecodeCompiler().Compile(result.Program!);
        Assert.True(compilation.Succeeded);
        Assert.Empty(new BytecodeModuleValidator().Validate(compilation.Module!).Diagnostics);
    }
}
