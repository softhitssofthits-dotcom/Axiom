using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class NumericPromotionContractTests
{
    private static readonly string[] Types = ["INTEGER", "LONG", "DOUBLE", "DECIMAL"];
    public static IEnumerable<object[]> Combinations()
    {
        foreach (var name in new[] { "MIN", "MAX", "CLAMP" })
            foreach (var first in Types)
                foreach (var second in Types)
                    foreach (var third in name == "CLAMP" ? Types : new[] { "" })
                        yield return new object[] { name, first, second, third };
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ExplicitDecimalToDoubleConversionCanRoundAndPreservesOrdinaryValues(ExecutionMode mode)
    {
        const string source = """
PRINT CDBL(CDEC(2))
PRINT CDBL(CDEC(-2.5))
PRINT CDBL(CDEC(0))
DIM exact AS DECIMAL = CDEC(CLNG(9007199254740993))
PRINT CLNG(CDBL(exact))
PRINT exact = CDEC(CLNG(9007199254740993))
DIM large AS DECIMAL = CDEC(9223372036854775807) * CDEC(1000000000)
PRINT CDBL(large) > 0
PRINT MIN(CDBL(CDEC(2)), 3.0)
""";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(new[] { "2", "-2.5", "0", "9007199254740992", "TRUE", "TRUE", "2" }, result.Output);
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void EveryNumericTypeCombinationHasAnExplicitContract(string name, string firstType, string secondType, string thirdType)
    {
        var types = name == "CLAMP" ? new[] { firstType, secondType, thirdType } : new[] { firstType, secondType };
        var values = name == "CLAMP" ? new[] { 9, 2, 4 } : new[] { 3, 2 };
        var arguments = types.Select((type, index) => type switch
        {
            "LONG" => $"CLNG({values[index]})",
            "DOUBLE" => $"{values[index]}.0",
            "DECIMAL" => $"CDEC({values[index]})",
            _ => values[index].ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
        var source = "PRINT " + name + "(" + string.Join(", ", arguments) + ")";
        var syntax = Parser.Parse(source, out var parsingDiagnostics);
        Assert.Empty(parsingDiagnostics);
        var program = Binder.Bind(syntax, out var bindingDiagnostics);
        var rejected = types.Contains("DOUBLE") && types.Contains("DECIMAL");
        if (rejected)
            Assert.NotEmpty(bindingDiagnostics);
        else
        {
            Assert.Empty(bindingDiagnostics);
            var call = Assert.IsType<BoundCallExpression>(Assert.Single(Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements)).Items).Expression);
            var expectedType = types.Contains("DOUBLE") ? "DOUBLE" : types.Contains("DECIMAL") ? "DECIMAL" : types.Contains("LONG") ? "LONG" : "INTEGER";
            Assert.Equal(expectedType, call.Type.Name.ToUpperInvariant());
            Assert.All(call.Arguments, argument => Assert.Equal(expectedType, argument.Type.Name.ToUpperInvariant()));
        }
        var interpreter = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var vm = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.False(vm.UsedInterpreterFallback);
        if (rejected)
        {
            Assert.False(interpreter.Succeeded);
            Assert.False(vm.Succeeded);
            Assert.Equal(interpreter.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)), vm.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
        }
        else
        {
            Assert.True(interpreter.Succeeded);
            Assert.True(vm.Succeeded);
            string[] expected = [name == "MIN" ? "2" : name == "MAX" ? "3" : "4"];
            Assert.Equal(expected, interpreter.Output);
            Assert.Equal(expected, vm.Output);
        }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void LargeLongAndDecimalStayExactWithoutImplicitDouble(ExecutionMode mode)
    {
        const string source = """
DIM smallest AS LONG = -9223372036854775807 - CLNG(1)
DIM largest AS LONG = 9223372036854775807
DIM exact AS DECIMAL = CDEC(largest) * CDEC(1000000000)
PRINT MIN(smallest, 0) = smallest
PRINT MAX(largest, 0) = largest
PRINT CLAMP(largest, 0, largest) = largest
PRINT SETSIGN(smallest, -1) = smallest
PRINT SGN(smallest)
PRINT MIN(CDEC(largest), largest) = CDEC(largest)
PRINT MAX(exact, CDEC(0)) = exact
PRINT CLAMP(exact, CDEC(0), exact) = exact
PRINT ABS(-exact) = exact
PRINT MIN(CDBL(CDEC(2)), 3.0)
TRY
 PRINT ABS(smallest)
CATCH err
 PRINT err.Code
END TRY
TRY
 PRINT SETSIGN(smallest, 1)
CATCH err
 PRINT err.Code
END TRY
""";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(new[] { "TRUE", "TRUE", "TRUE", "TRUE", "-1", "TRUE", "TRUE", "TRUE", "TRUE", "2", "MB3008", "MB3008" }, result.Output);
    }
}
