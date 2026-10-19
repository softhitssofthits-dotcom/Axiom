using Axiom.Core;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class ByRefReadOnlyContractTests
{
    [Theory]
    [InlineData("""
CONST fixedValue AS INTEGER = 10
SUB Mutate(BYREF a AS INTEGER)
    a = 99
END SUB
Mutate(fixedValue)
""", "fixedValue")]
    [InlineData("""
SUB Mutate(BYREF a AS DOUBLE)
    a = 3.5
END SUB
Mutate(PI)
""", "PI")]
    [InlineData("""
CONST fixedValue AS INTEGER = 10
SUB Outer(BYREF a AS INTEGER)
    Inner(a)
END SUB
SUB Inner(BYREF a AS INTEGER)
    a = 99
END SUB
Outer(fixedValue)
""", "fixedValue")]
    public void ReadOnlyVariableCannotBePassedByReference(string source, string name)
    {
        var syntax = Parser.Parse(source, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);
        _ = Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);

        var diagnostic = Assert.Single(diagnostics, d => d.Code == DiagnosticCodes.AssignToConst);
        Assert.Contains(name, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrdinaryVariableStillBindsAsByRefArgument()
    {
        var syntax = Parser.Parse("""
SUB Mutate(BYREF a AS INTEGER)
    a = 99
END SUB
DIM value AS INTEGER = 10
Mutate(value)
""", out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);
        _ = Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);

        Assert.Empty(diagnostics);
    }
}
