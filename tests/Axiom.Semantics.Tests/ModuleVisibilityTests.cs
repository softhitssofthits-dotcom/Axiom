using Axiom.Core;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class ModuleVisibilityTests
{
    [Fact]
    public void PublicFunctionIsVisibleAfterImport()
    {
        var (program, diagnostics) = Bind(
            ("main", """
IMPORT math
PRINT Square(5)
"""),
            ("math", """
PUBLIC FUNCTION Square(x AS INTEGER) AS INTEGER
    RETURN x * x
END FUNCTION
"""));

        Assert.Empty(diagnostics);
        Assert.Single(program.GlobalStatements);
        Assert.Single(program.Routines);
    }

    [Fact]
    public void PrivateFunctionIsNotVisibleOutsideItsModule()
    {
        var (_, diagnostics) = Bind(
            ("main", """
IMPORT math
PRINT Square(5)
"""),
            ("math", """
PRIVATE FUNCTION Square(x AS INTEGER) AS INTEGER
    RETURN x * x
END FUNCTION
"""));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.PrivateModuleMember);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("math", StringComparison.Ordinal));
    }

    [Fact]
    public void PublicTypeIsUsableInImportedSignature()
    {
        var (program, diagnostics) = Bind(
            ("main", """
IMPORT shapes
DIM box AS Box
box.Item = 3
PRINT Area(box)
"""),
            ("shapes", """
PUBLIC TYPE Box
    Item AS INTEGER
END TYPE
PUBLIC FUNCTION Area(box AS Box) AS INTEGER
    RETURN box.Item
END FUNCTION
"""));

        Assert.Empty(diagnostics);
        Assert.Single(program.Routines);
    }

    [Fact]
    public void PrivateTypeIsReportedAsClosed()
    {
        var (_, diagnostics) = Bind(
            ("main", """
IMPORT shapes
DIM box AS Box
"""),
            ("shapes", """
PRIVATE TYPE Box
    Item AS INTEGER
END TYPE
"""));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.PrivateModuleMember);
    }

    [Fact]
    public void MissingModuleIsReported()
    {
        var (_, diagnostics) = Bind(
            ("main", """
IMPORT missing
PRINT 1
"""));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.UnknownModule);
    }

    [Fact]
    public void CyclicImportIsReportedOnce()
    {
        var (_, diagnostics) = Bind(
            ("main", """
IMPORT first
PRINT 1
"""),
            ("first", """
IMPORT second
PUBLIC SUB First()
END SUB
"""),
            ("second", """
IMPORT first
PUBLIC SUB Second()
END SUB
"""));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.CyclicModuleImport);
    }

    [Fact]
    public void DuplicateModuleNamesAreReported()
    {
        var (_, diagnostics) = Bind(
            ("main", "PRINT 1"),
            ("math", "PUBLIC SUB A()\nEND SUB"),
            ("math", "PUBLIC SUB B()\nEND SUB"));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateModule);
    }

    [Fact]
    public void AmbiguousImportedNameIsReported()
    {
        var (_, diagnostics) = Bind(
            ("main", """
IMPORT left
IMPORT right
PRINT Shared()
"""),
            ("left", """
PUBLIC FUNCTION Shared() AS INTEGER
    RETURN 1
END FUNCTION
"""),
            ("right", """
PUBLIC FUNCTION Shared() AS INTEGER
    RETURN 2
END FUNCTION
"""));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousModuleMember);
    }

    [Fact]
    public void SingleFileBindingStillWorks()
    {
        var syntax = Parser.Parse("PRINT 1 + 2", out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);

        Assert.Empty(diagnostics);
        Assert.Single(program.GlobalStatements);
    }

    private static (BoundProgram Program, DiagnosticBag Diagnostics) Bind(
        params (string Name, string Source)[] modules)
    {
        var syntaxes = new List<ModuleSyntax>(modules.Length);

        foreach (var (name, source) in modules)
        {
            var syntax = Parser.Parse(source, out var parserDiagnostics, name);
            Assert.Empty(parserDiagnostics);
            syntaxes.Add(new ModuleSyntax(name, syntax));
        }

        var program = Binder.BindModules(syntaxes, modules[0].Name, out var diagnostics, checkGraphicsModes: false);
        return (program, diagnostics);
    }
}
