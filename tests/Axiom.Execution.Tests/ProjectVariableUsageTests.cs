using Axiom.Core;

namespace Axiom.Execution.Tests;

public sealed class ProjectVariableUsageTests
{
    [Theory]
    [InlineData("PRINT SharedValue", false)]
    [InlineData("SharedValue = 9\nPRINT 0", true)]
    public void AdviceUsesBoundSymbolAndDistinguishesReadsFromWrites(string body, bool unused)
    {
        WithProject("IMPORT helper\n" + body, "PUBLIC DIM SharedValue AS INTEGER = 1\nPUBLIC CONST SpareConstant AS INTEGER = 2\nPRIVATE DIM PrivateUnused AS INTEGER = 3", (root, main, helper) =>
        {
            var result = Compile(main, File.ReadAllText(main));
            Assert.Equal(unused, HasAdvice(result, helper, "SharedValue"));
            if (unused)
            {
                var diagnostic = Assert.Single(result.Diagnostics.Where(d => d.Code == DiagnosticCodes.UnusedVariable && d.Location.Document == helper && d.Message.Contains("'SharedValue'", StringComparison.Ordinal)));
                Assert.Equal("SharedValue", File.ReadAllText(helper).Substring(diagnostic.Location.Span.Start, diagnostic.Location.Span.Length));
            }
            Assert.True(HasAdvice(result, helper, "SpareConstant"));
            Assert.True(HasAdvice(result, helper, "PrivateUnused"));
        });
    }

    [Fact]
    public void UnsavedImporterOverridesClosedDiskModuleAndUnreachableNamesDoNotCount()
    {
        WithProject("IMPORT helper\nPRINT SharedValue", "PUBLIC DIM SharedValue AS INTEGER = 1", (root, main, helper) =>
        {
            var orphan = Path.Combine(root, "orphan.axm");
            File.WriteAllText(orphan, "PUBLIC DIM SharedValue AS INTEGER = 99\nPRINT SharedValue");
            var source = "IMPORT helper\nPRINT 0";
            var result = Compile(main, source, [new SourceDocument(orphan, File.ReadAllText(orphan))]);
            Assert.True(HasAdvice(result, helper, "SharedValue"));
            var used = Compile(main, "IMPORT helper\nPRINT SharedValue", [new SourceDocument(orphan, File.ReadAllText(orphan))]);
            Assert.False(HasAdvice(used, helper, "SharedValue"));
        });
    }

    [Fact]
    public void TransitiveImporterReadsTheOriginalModuleSymbol()
    {
        WithProject("IMPORT bridge\nPRINT ReadShared()", "PUBLIC CONST SharedValue AS INTEGER = 1", (root, main, helper) =>
        {
            File.WriteAllText(Path.Combine(root, "bridge.axm"), "IMPORT helper\nPUBLIC FUNCTION ReadShared() AS INTEGER\nRETURN SharedValue\nEND FUNCTION");
            var result = Compile(main, File.ReadAllText(main));
            Assert.False(HasAdvice(result, helper, "SharedValue"));
        });
    }

    [Fact]
    public void SameNamedLocalInAnotherModuleDoesNotReadExportedSymbol()
    {
        WithProject("IMPORT helper\nIMPORT other\nPRINT ReadLocal()", "PUBLIC DIM SharedValue AS INTEGER = 1", (root, main, helper) =>
        {
            var other = Path.Combine(root, "other.axm");
            File.WriteAllText(other, "PUBLIC FUNCTION ReadLocal() AS INTEGER\nDIM SharedValue AS INTEGER = 99\nRETURN SharedValue\nEND FUNCTION");
            var result = Compile(main, File.ReadAllText(main));
            Assert.True(HasAdvice(result, helper, "SharedValue"));
            Assert.False(HasAdvice(result, other, "SharedValue"));
        });
    }

    [Theory]
    [InlineData("IMPORT helper\nIMPORT absent\nPRINT 0", "PUBLIC DIM SharedValue AS INTEGER = 1")]
    [InlineData("IMPORT helper\nPRINT 0", "PUBLIC DIM SharedValue AS INTEGER = 1\nPRINT MissingName")]
    [InlineData("IMPORT helper\nPRINT 0", "PUBLIC DIM SharedValue AS")]
    public void IncompleteOrInvalidGraphDoesNotClaimUnusedValues(string mainText, string helperText)
    {
        WithProject(mainText, helperText, (root, main, helper) =>
        {
            var dependencies = SourceProject.LoadDependencies(mainText, main);
            var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(mainText, sourceName: main, additionalDocuments: dependencies, includeUsageWarnings: true));
            Assert.False(result.Succeeded);
            Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.UnusedVariable);
        });
    }

    private static bool HasAdvice(CompilationResult result, string file, string name) => result.Diagnostics.Any(d =>
        d.Code == DiagnosticCodes.UnusedVariable && string.Equals(d.Location.Document, file, StringComparison.OrdinalIgnoreCase) && d.Message.Contains("'" + name + "'", StringComparison.Ordinal));

    private static CompilationResult Compile(string main, string text, SourceDocument[]? open = null)
    {
        var dependencies = SourceProject.LoadDependencies(text, main, open);
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(text, sourceName: main, additionalDocuments: dependencies, includeUsageWarnings: true));
        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Message)));
        return result;
    }

    private static void WithProject(string mainText, string helperText, Action<string,string,string> action)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "project-usage-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        try
        {
            var main = Path.Combine(root, "main.axm");var helper = Path.Combine(root, "helper.axm");
            File.WriteAllText(main, mainText);File.WriteAllText(helper, helperText);action(root,main,helper);
        }
        finally { Directory.Delete(root, true); }
    }
}
