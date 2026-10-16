using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class BackendFailureRecoveryTests
{
    [Fact]
    public void FailedSnapshotAfterSuccessCannotExecutePartialCompilation()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "backend-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var pipeline = new AxiomExecutionPipeline();
            var successful = pipeline.Compile(new CompilationRequest("PRINT \"Старый результат\""));
            Assert.True(successful.Succeeded);
            Assert.Equal(["Старый результат"], pipeline.RunCompiled(successful.CompiledProgram!,
                new ExecutionRequest(successful.CompiledProgram!.SourceText)).Output);

            var marker = Path.Combine(directory, "must-not-exist.txt");
            var source = $"PRINT WRITEFILE(\"{marker}\", \"Не исполнять\")\nPRINT \"Новый результат\"";
            var failed = pipeline.Compile(new CompilationRequest(source, bytecodeSnapshotPath: directory));
            Assert.Equal(CompilationStatus.BackendFailure, failed.Status);
            Assert.True(failed.FrontendSucceeded);
            Assert.False(failed.Succeeded);
            Assert.False(failed.BackendSucceeded);
            Assert.False(failed.BytecodeAvailable);
            Assert.Equal("MB4003", Assert.Single(failed.BackendDiagnostics).Code);
            Assert.NotNull(failed.CompiledProgram);
            Assert.Null(failed.CompiledProgram.BytecodeModule);

            var rejected = pipeline.RunCompiled(failed.CompiledProgram, new ExecutionRequest(source));
            Assert.Equal(ExecutionStatus.BackendFailure, rejected.Status);
            Assert.Equal("MB4001", Assert.Single(rejected.Diagnostics).Code);
            Assert.Empty(rejected.Output);
            Assert.Equal(string.Empty, rejected.PendingOutput);
            Assert.Null(rejected.ActualEngine);
            Assert.Null(rejected.StopState);
            Assert.False(File.Exists(marker));

            var recovered = pipeline.Run(new ExecutionRequest(source));
            Assert.Equal(ExecutionStatus.Success, recovered.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, recovered.ActualEngine);
            Assert.Equal(["TRUE", "Новый результат"], recovered.Output);
            Assert.Empty(recovered.Diagnostics);
            Assert.Equal("Не исполнять", File.ReadAllText(marker));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void RunWithFailedSnapshotDoesNotExecuteAnyInstructionAfterEarlierSuccess()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "backend-run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var pipeline = new AxiomExecutionPipeline();
            Assert.Equal(["17"], pipeline.Run(new ExecutionRequest("PRINT 17")).Output);
            var marker = Path.Combine(directory, "must-not-exist.txt");
            var source = $"PRINT WRITEFILE(\"{marker}\", \"Записано\")\nPRINT 29";
            var failed = pipeline.Run(new ExecutionRequest(source, bytecodeSnapshotPath: directory));
            Assert.Equal(ExecutionStatus.BackendFailure, failed.Status);
            Assert.Equal("MB4003", Assert.Single(failed.Diagnostics).Code);
            Assert.Empty(failed.Output);
            Assert.Null(failed.ActualEngine);
            Assert.Null(failed.StopState);
            Assert.False(File.Exists(marker));
            var recovered = pipeline.Run(new ExecutionRequest(source));
            Assert.Equal(ExecutionStatus.Success, recovered.Status);
            Assert.Equal(["TRUE", "29"], recovered.Output);
            Assert.Empty(recovered.Diagnostics);
            Assert.Equal("Записано", File.ReadAllText(marker));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
