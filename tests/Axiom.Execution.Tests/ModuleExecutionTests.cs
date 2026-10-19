namespace Axiom.Execution.Tests;

public sealed class ModuleExecutionTests
{
    [Fact]
    public void PublicFunctionFromAnotherModuleRuns()
    {
        var result = Run(
            """
IMPORT math
PRINT Square(5)
""",
            ("math.axm", """
PUBLIC FUNCTION Square(x AS INTEGER) AS INTEGER
    RETURN x * x
END FUNCTION
"""));

        Assert.True(result.Succeeded, Describe(result));
        Assert.Equal(["25"], result.Output);
    }

    [Fact]
    public void ImportedModuleInitializesBeforeEntryStatements()
    {
        var result = Run(
            """
IMPORT state
PRINT counter
""",
            ("state.axm", """
PUBLIC DIM counter AS INTEGER = 10
counter = counter + 5
"""));

        Assert.True(result.Succeeded, Describe(result));
        Assert.Equal(["15"], result.Output);
    }

    [Fact]
    public void ImportedPublicTypeWorksAcrossModules()
    {
        var result = Run(
            """
IMPORT shapes
DIM box AS Box
box.Item = 3
PRINT Area(box)
""",
            ("shapes.axm", """
PUBLIC TYPE Box
    Item AS INTEGER
END TYPE
PUBLIC FUNCTION Area(box AS Box) AS INTEGER
    RETURN box.Item * 2
END FUNCTION
"""));

        Assert.True(result.Succeeded, Describe(result));
        Assert.Equal(["6"], result.Output);
    }

    [Fact]
    public void PrivateMemberIsRejectedBeforeExecution()
    {
        var result = Run(
            """
IMPORT math
PRINT Square(5)
""",
            ("math.axm", """
PRIVATE FUNCTION Square(x AS INTEGER) AS INTEGER
    RETURN x * x
END FUNCTION
"""));

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB2053");
    }

    [Fact]
    public void DiagnosticCarriesDocumentIdentity()
    {
        var result = Run(
            """
IMPORT math
PRINT 1
""",
            ("math.axm", """
PUBLIC FUNCTION Broken() AS INTEGER
    RETURN missingName
END FUNCTION
"""));

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("math.axm", diagnostic.Location.Document);
        Assert.Equal(2, diagnostic.Location.Line);
    }

    [Fact]
    public void ModuleCanCallAnotherImportedModule()
    {
        var result = Run(
            """
IMPORT upper
PRINT Twice("ab")
""",
            ("upper.axm", """
IMPORT text
PUBLIC FUNCTION Twice(value AS STRING) AS STRING
    RETURN RepeatText(value, 2)
END FUNCTION
"""),
            ("text.axm", """
PUBLIC FUNCTION RepeatText(value AS STRING, count AS INTEGER) AS STRING
    RETURN REPEAT(value, count)
END FUNCTION
"""));

        Assert.True(result.Succeeded, Describe(result));
        Assert.Equal(["abab"], result.Output);
    }

    private static ExecutionResult Run(string entrySource, params (string Name, string Source)[] modules)
    {
        return new AxiomExecutionPipeline().Run(new ExecutionRequest(
            entrySource,
            sourceName: "main.axm",
            additionalDocuments: modules.Select(module => new SourceDocument(module.Name, module.Source))));
    }

    private static string Describe(ExecutionResult result)
    {
        return string.Join("\n", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Location}: {diagnostic.Message}"));
    }
}
