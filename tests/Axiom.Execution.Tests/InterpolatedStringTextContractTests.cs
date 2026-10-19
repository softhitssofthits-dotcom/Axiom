using Axiom.Execution;

namespace Axiom.Execution.Tests;

public sealed class InterpolatedStringTextContractTests
{
    [Theory]
    [InlineData("PRINT $\"[{n:F2}]\"", "[42.00]")]
    [InlineData("PRINT $\"[{n: F2}]\"", "[42.00]")]
    [InlineData("PRINT $\"[{n:F2 }]\"", "[42.00]")]
    [InlineData("PRINT $\"[{n: F2 }]\"", "[42.00]")]
    [InlineData("PRINT $\"[{n,5}]\"", "[   42]")]
    [InlineData("PRINT $\"[{n,5:F2}]\"", "[42.00]")]
    public void FormatSpecifierIgnoresSurroundingSpaces(string printStatement, string expected)
    {
        var result = Run("DIM n AS INTEGER = 42\n" + printStatement);

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(static diagnostic => diagnostic.Message)));
        Assert.Equal(new[] { expected }, result.Output);
    }

    [Theory]
    [InlineData("PRINT $\"{{x}}\"", "{x}")]
    [InlineData("PRINT $\"a}}b\"", "a}b")]
    [InlineData("PRINT $\"a\"\"b\"", "a\"b")]
    [InlineData("PRINT $\"e{{f\"", "e{f")]
    [InlineData("PRINT $\"c\\\\d\"", "c\\\\d")]
    public void InterpolatedTextUsesDoubledBracesAndQuotesOnly(string printStatement, string expected)
    {
        var result = Run(printStatement);

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(static diagnostic => diagnostic.Message)));
        Assert.Equal(new[] { expected }, result.Output);
    }

    [Fact]
    public void InterpolatedTextDoesNotContinueOnNextLine()
    {
        const string source = "PRINT $\"abc\ndef\"";
        var result = Run(source);

        Assert.False(result.Succeeded);
        var diagnostics = result.Diagnostics.Where(static item => item.Code == "MB0002").ToArray();
        Assert.NotEmpty(diagnostics);
        Assert.Equal(1, diagnostics[0].Location.Line);
        Assert.Equal(7, diagnostics[0].Location.Column);
        Assert.All(diagnostics, static item => Assert.InRange(item.Location.Line, 1, 2));
        Assert.Empty(result.Output);
    }

    [Fact]
    public void InterpolatedTextDoesNotSwallowFollowingStatements()
    {
        const string source = "PRINT $\"oops\nDIM x AS INTEGER = 1\nPRINT x\nPRINT \"tail\"";
        var result = Run(source);

        Assert.False(result.Succeeded);
        var diagnostics = result.Diagnostics.Where(static item => item.Code == "MB0002").ToArray();
        Assert.Single(diagnostics);
        Assert.Equal(1, diagnostics[0].Location.Line);
        Assert.Empty(result.Output);
    }

    private static ExecutionResult Run(string source)
    {
        return new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
    }
}
