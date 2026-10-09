using System.Globalization;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class IsNumericContractTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("0", true)]
    [InlineData("-0", true)]
    [InlineData("+12", true)]
    [InlineData(".5", true)]
    [InlineData("1.", true)]
    [InlineData("-12.5e2", true)]
    [InlineData(" 12.5 ", true)]
    [InlineData("1e999", false)]
    [InlineData("-1e999", false)]
    [InlineData("1e-999", true)]
    [InlineData("0e999", true)]
    [InlineData("1.7976931348623157e308", true)]
    [InlineData("1.7976931348623159e308", false)]
    [InlineData("4.9406564584124654e-324", true)]
    [InlineData("NaN", false)]
    [InlineData("Infinity", false)]
    [InlineData("-Infinity", false)]
    [InlineData("12abc", false)]
    [InlineData("1e", false)]
    [InlineData("1e+", false)]
    [InlineData("1,234", false)]
    [InlineData("12,5", false)]
    [InlineData("$12", false)]
    [InlineData("&HFF", false)]
    [InlineData("&O77", false)]
    [InlineData("１２", false)]
    [InlineData("١٢", false)]
    [InlineData("1 2", false)]
    public void FullInvariantFiniteNumberContractMatchesBothEngines(string text, bool expected)
    {
        foreach (var culture in new[] { "en-US", "ru-RU", "tr-TR" })
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
                {
                    var run = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT isnumeric(\"{text}\")", mode: mode));
                    Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
                    Assert.False(run.UsedInterpreterFallback);
                    Assert.Equal(new[] { expected ? "TRUE" : "FALSE" }, run.Output);
                }
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }
    }

    [Theory]
    [InlineData("ISNUMERIC()")]
    [InlineData("ISNUMERIC(1)")]
    [InlineData("ISNUMERIC(TRUE)")]
    [InlineData("ISNUMERIC(\"1\", \"2\")")]
    public void WrongArgumentFormsAreRejectedByBinder(string call)
    {
        var syntax = Parser.Parse("PRINT " + call, out var parsing);
        Assert.Empty(parsing);
        Binder.Bind(syntax, out var binding);
        Assert.NotEmpty(binding);
        Assert.All(binding, d => Assert.StartsWith("MB2", d.Code));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(9, true)]
    [InlineData(13, true)]
    [InlineData(160, false)]
    [InlineData(8192, false)]
    public void HiddenCharactersRespectFullStringContract(int code, bool expected)
    {
        var text = code == 0 ? "\"12\" + CHRW(0)" : $"CHRW({code}) + \"12\"";
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT ISNUMERIC(" + text + ")", mode: mode));
            Assert.True(run.Succeeded);
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(new[] { expected ? "TRUE" : "FALSE" }, run.Output);
        }
    }

    [Fact]
    public void BindsBooleanResultAndPreservesValPrefixContract()
    {
        var syntax = Parser.Parse("PRINT ISNUMERIC(\"12abc\")", out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(Assert.IsType<BoundPrintStatement>(program.GlobalStatements.Last()).Items).Expression);
        Assert.Equal(BuiltinFunctionId.IsNumeric, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId);
        Assert.Equal(TypeSymbol.Boolean, call.Type);
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT ISNUMERIC(\"12abc\")\nPRINT VAL(\"12abc\")\nPRINT ISNUMERIC(\"1e999\")\nPRINT VAL(\"1e999\")", mode: mode));
            Assert.True(run.Succeeded);
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(new[] { "FALSE", "12", "FALSE", "0" }, run.Output);
        }
    }
}
