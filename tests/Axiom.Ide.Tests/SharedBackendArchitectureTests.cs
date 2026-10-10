using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
using Axiom.Ide.Services.Graphics;

namespace Axiom.Ide.Tests;

public sealed class SharedBackendArchitectureTests
{
    [Fact]
    public void ExecutionInputAndGraphicsShareAnAssemblyWithoutTheEditor()
    {
        var assembly = typeof(ProgramRunService).Assembly;
        Assert.Equal("Axiom.Ide.Backend", assembly.GetName().Name);
        Assert.Same(assembly, typeof(ProgramConsoleService).Assembly);
        Assert.Same(assembly, typeof(GraphicsHostService).Assembly);
        Assert.Null(assembly.GetType("Axiom.Ide.MainWindow"));
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x => x.Name == "ICSharpCode.AvalonEdit");
    }
}
