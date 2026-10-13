using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class BytecodeOnlyContractTests
{
    [Fact]
    public void PublicExecutionContractHasOneEngineAndNoModeOrFallback()
    {
        Assert.Equal([ExecutionEngineKind.BytecodeVm], Enum.GetValues<ExecutionEngineKind>());
        Assert.Equal(1, (int)ExecutionEngineKind.BytecodeVm);
        Assert.Null(typeof(ExecutionRequest).Assembly.GetType("Axiom.Execution.ExecutionMode"));
        Assert.Null(typeof(ExecutionRequest).GetProperty("Mode"));
        Assert.Null(typeof(ExecutionResult).GetProperty("RequestedMode"));
        Assert.Null(typeof(ExecutionResult).GetProperty("UsedInterpreterFallback"));
        Assert.Null(typeof(ExecutionResult).GetProperty("InterpreterFallbackReason"));
    }

    [Fact]
    public void CompilerExecutionSampleHasExpectedOutput()
    {
        var source = TestProgramLoader.LoadText("samples/smoke/bytecode-only/compiler_execution.axm");
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest(source));
        Assert.True(compilation.Succeeded);
        Assert.NotNull(compilation.CompiledProgram!.BytecodeModule);
        var result = pipeline.RunCompiled(compilation.CompiledProgram, new ExecutionRequest(source));
        Assert.True(result.Succeeded);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Сумма: 3", "", "Шаг: 1", "Шаг: 2", "MB3007", "Function POW: argument 'value' cannot be used with the specified exponent.", "Завершено"], result.Output);
    }

    [Fact]
    public void ArrayPushOverflowSamplePreservesBoundsAndValue()
    {
        var source = TestProgramLoader.LoadText("samples/functions/arraypush/upper_bound_overflow.axm");
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3007", "2147483647", "42"], result.Output);
    }
}
