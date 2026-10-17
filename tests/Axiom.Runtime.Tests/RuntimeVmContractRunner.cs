using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

internal static class RuntimeVmContractRunner
{
    internal static (BoundProgram Program, TestRuntimeHost Host, RuntimeResult Result)
        Execute(string source, TestRuntimeHost host, BytecodeBreakpointSet? breakpoints = null)
    {
        var syntax = Parser.Parse(source, out var parseDiagnostics);
        Assert.Empty(parseDiagnostics);

        // Эти проверки вызывают VM напрямую, включая её защиту от неверного
        // графического состояния. Отказ компилятора проверяется отдельно.
        var program = Binder.Bind(syntax, out var bindDiagnostics, checkGraphicsModes: false);
        Assert.DoesNotContain(bindDiagnostics, static d => d.Severity != DiagnosticSeverity.Warning);

        var lowered = new BoundProgramLowerer().Lower(program);
        Assert.True(
            lowered.Succeeded,
            string.Join("\n", lowered.Diagnostics.Select(static d => d.Message)));

        var compiled = new BytecodeCompiler().Compile(lowered.Program!);
        Assert.True(
            compiled.Succeeded,
            string.Join("\n", compiled.Diagnostics.Select(static d => d.Message)));
        Assert.NotNull(compiled.Module);

        var result = BytecodeVm.Execute(compiled.Module!, host, breakpoints: breakpoints);
        return (program, host, result);
    }
}
