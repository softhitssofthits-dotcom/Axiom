namespace Axiom.Runtime.Tests;

public sealed class BytecodeOnlyArchitectureTests
{
    [Fact]
    public void RuntimeAssemblyDoesNotExposeTreeInterpreter()
    {
        var assembly = typeof(BytecodeVm).Assembly;
        Assert.Null(assembly.GetType("Axiom.Runtime.Interpreter"));
        Assert.Null(assembly.GetType("Axiom.Runtime.RuntimeScope"));
        Assert.Null(assembly.GetType("Axiom.Runtime.CallFrame"));
    }
}
