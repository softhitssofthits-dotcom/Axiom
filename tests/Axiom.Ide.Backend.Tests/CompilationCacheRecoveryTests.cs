using System.IO;
using System.Runtime.ExceptionServices;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Backend.Tests;

public sealed class CompilationCacheRecoveryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedCompilationClearsPreviouslyUsableCache(bool infrastructureFailure)
    {
        RunOnSta(() =>
        {
            const string source = "PRINT \"Повтор после отказа\"";
            var service = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService());
            Assert.True(service.Compile(source).Succeeded);
            var initial = service.Run(source);
            Assert.Equal(ProgramRunStatus.Success, initial.Status);
            Assert.True(initial.UsedCompiledCache);
            Assert.Equal("Повтор после отказа", initial.OutputText);

            var failed = infrastructureFailure
                ? service.Compile(source, new FailingProgress())
                : service.Compile("PRINT missingValue");
            Assert.False(failed.Succeeded);
            Assert.Equal(infrastructureFailure ? ProgramCompilationStatus.InfrastructureFailure : ProgramCompilationStatus.SemanticFailure,
                failed.Status);
            Assert.Equal(infrastructureFailure ? "IDE0003" : "MB2001", Assert.Single(failed.Diagnostics).Code);

            var recovered = service.Run(source);
            Assert.Equal(ProgramRunStatus.Success, recovered.Status);
            Assert.False(recovered.UsedCompiledCache);
            Assert.Equal("Повтор после отказа", recovered.OutputText);
            Assert.Empty(recovered.Diagnostics);
            Assert.True(service.Run(source).UsedCompiledCache);
        });
    }

    private sealed class FailingProgress : IProgress<int>
    {
        public void Report(int value)
        {
            if (value == 90) throw new IOException("Контролируемый отказ перед выпуском байт-кода");
        }
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception exception) { failure = exception; }
        });
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "Проверка общей службы запуска не завершилась за 20 секунд");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
