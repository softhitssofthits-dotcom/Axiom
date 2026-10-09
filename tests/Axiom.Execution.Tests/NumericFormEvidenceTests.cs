using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class NumericFormEvidenceTests
{
    private const string Setup = "";

    public static IEnumerable<object[]> Forms()
    {
        yield return new object[] { "Rnd", "RND()", "random", "DOUBLE" };
        yield return new object[] { "Abs", "ABS(-3)", "3", "INTEGER" };
        yield return new object[] { "Abs", "ABS(CLNG(-3))", "3", "LONG" };
        yield return new object[] { "Abs", "ABS(-3.0)", "3", "DOUBLE" };
        yield return new object[] { "Abs", "ABS(CDEC(-3))", "3", "DECIMAL" };
        yield return new object[] { "Sgn", "SGN(-3)", "-1", "INTEGER" };
        yield return new object[] { "Sgn", "SGN(CLNG(-3))", "-1", "INTEGER" };
        yield return new object[] { "Sgn", "SGN(-3.0)", "-1", "INTEGER" };
        yield return new object[] { "Sgn", "SGN(CDEC(-3))", "-1", "INTEGER" };
        yield return new object[] { "Min", "MIN(3, 2)", "2", "INTEGER" };
        yield return new object[] { "Min", "MIN(CLNG(3), CLNG(2))", "2", "LONG" };
        yield return new object[] { "Min", "MIN(3.0, 2.0)", "2", "DOUBLE" };
        yield return new object[] { "Min", "MIN(CDEC(3), CDEC(2))", "2", "DECIMAL" };
        yield return new object[] { "Max", "MAX(3, 2)", "3", "INTEGER" };
        yield return new object[] { "Max", "MAX(CLNG(3), CLNG(2))", "3", "LONG" };
        yield return new object[] { "Max", "MAX(3.0, 2.0)", "3", "DOUBLE" };
        yield return new object[] { "Max", "MAX(CDEC(3), CDEC(2))", "3", "DECIMAL" };
        yield return new object[] { "Clamp", "CLAMP(9, 2, 4)", "4", "INTEGER" };
        yield return new object[] { "Clamp", "CLAMP(CLNG(9), CLNG(2), CLNG(4))", "4", "LONG" };
        yield return new object[] { "Clamp", "CLAMP(9.0, 2.0, 4.0)", "4", "DOUBLE" };
        yield return new object[] { "Clamp", "CLAMP(CDEC(9), CDEC(2), CDEC(4))", "4", "DECIMAL" };
        yield return new object[] { "Floor", "FLOOR(-3)", "-3", "INTEGER" };
        yield return new object[] { "Floor", "FLOOR(CLNG(-3))", "-3", "LONG" };
        yield return new object[] { "Floor", "FLOOR(-3.0)", "-3", "DOUBLE" };
        yield return new object[] { "Floor", "FLOOR(CDEC(-3))", "-3", "DECIMAL" };
        yield return new object[] { "Ceil", "CEIL(-3)", "-3", "INTEGER" };
        yield return new object[] { "Ceil", "CEIL(CLNG(-3))", "-3", "LONG" };
        yield return new object[] { "Ceil", "CEIL(-3.0)", "-3", "DOUBLE" };
        yield return new object[] { "Ceil", "CEIL(CDEC(-3))", "-3", "DECIMAL" };
        yield return new object[] { "Round", "ROUND(-3)", "-3", "INTEGER" };
        yield return new object[] { "Round", "ROUND(CLNG(-3))", "-3", "LONG" };
        yield return new object[] { "Round", "ROUND(-3.0)", "-3", "DOUBLE" };
        yield return new object[] { "Round", "ROUND(CDEC(-3))", "-3", "DECIMAL" };
        yield return new object[] { "Fix", "FIX(-3)", "-3", "INTEGER" };
        yield return new object[] { "Fix", "FIX(CLNG(-3))", "-3", "LONG" };
        yield return new object[] { "Fix", "FIX(-3.0)", "-3", "DOUBLE" };
        yield return new object[] { "Fix", "FIX(CDEC(-3))", "-3", "DECIMAL" };
        yield return new object[] { "SetSign", "SETSIGN(3, -1)", "-3", "INTEGER" };
        yield return new object[] { "SetSign", "SETSIGN(CLNG(3), CLNG(-1))", "-3", "LONG" };
        yield return new object[] { "SetSign", "SETSIGN(3.0, -1.0)", "-3", "DOUBLE" };
        yield return new object[] { "SetSign", "SETSIGN(CDEC(3), CDEC(-1))", "-3", "DECIMAL" };
        yield return new object[] { "Sin", "SIN(0)", "0", "DOUBLE" };
        yield return new object[] { "Cos", "COS(0)", "1", "DOUBLE" };
        yield return new object[] { "Tan", "TAN(0)", "0", "DOUBLE" };
        yield return new object[] { "Atn", "ATN(0)", "0", "DOUBLE" };
        yield return new object[] { "Atn2", "ATN2(0, 1)", "0", "DOUBLE" };
        yield return new object[] { "Sqr", "SQR(9)", "3", "DOUBLE" };
        yield return new object[] { "Cbrt", "CBRT(-8)", "-2", "DOUBLE" };
        yield return new object[] { "Exp", "EXP(0)", "1", "DOUBLE" };
        yield return new object[] { "Log", "LOG(1)", "0", "DOUBLE" };
        yield return new object[] { "Log10", "LOG10(100)", "2", "DOUBLE" };
        yield return new object[] { "Log2", "LOG2(8)", "3", "DOUBLE" };
        yield return new object[] { "Pow", "POW(2, 3)", "8", "DOUBLE" };
        yield return new object[] { "Asin", "ASIN(0)", "0", "DOUBLE" };
        yield return new object[] { "Acos", "ACOS(1)", "0", "DOUBLE" };
        yield return new object[] { "Asinh", "ASINH(0)", "0", "DOUBLE" };
        yield return new object[] { "Acosh", "ACOSH(1)", "0", "DOUBLE" };
        yield return new object[] { "Atanh", "ATANH(0)", "0", "DOUBLE" };
        yield return new object[] { "Sinh", "SINH(0)", "0", "DOUBLE" };
        yield return new object[] { "Cosh", "COSH(0)", "1", "DOUBLE" };
        yield return new object[] { "Tanh", "TANH(0)", "0", "DOUBLE" };
    }
    public static IEnumerable<object[]> WrongArguments()
    {
        yield return new object[] { "Abs-arg1", "ABS(TRUE)" };
        yield return new object[] { "Sgn-arg1", "SGN(TRUE)" };
        yield return new object[] { "Min-arg1", "MIN(TRUE, 2)" };
        yield return new object[] { "Min-arg2", "MIN(3, TRUE)" };
        yield return new object[] { "Max-arg1", "MAX(TRUE, 2)" };
        yield return new object[] { "Max-arg2", "MAX(3, TRUE)" };
        yield return new object[] { "Clamp-arg1", "CLAMP(TRUE, 2, 4)" };
        yield return new object[] { "Clamp-arg2", "CLAMP(9, TRUE, 4)" };
        yield return new object[] { "Clamp-arg3", "CLAMP(9, 2, TRUE)" };
        yield return new object[] { "Floor-arg1", "FLOOR(TRUE)" };
        yield return new object[] { "Ceil-arg1", "CEIL(TRUE)" };
        yield return new object[] { "Round-arg1", "ROUND(TRUE)" };
        yield return new object[] { "Fix-arg1", "FIX(TRUE)" };
        yield return new object[] { "SetSign-arg1", "SETSIGN(TRUE, -1)" };
        yield return new object[] { "SetSign-arg2", "SETSIGN(3, TRUE)" };
        yield return new object[] { "Sin-arg1", "SIN(TRUE)" };
        yield return new object[] { "Cos-arg1", "COS(TRUE)" };
        yield return new object[] { "Tan-arg1", "TAN(TRUE)" };
        yield return new object[] { "Atn-arg1", "ATN(TRUE)" };
        yield return new object[] { "Atn2-arg1", "ATN2(TRUE, 1)" };
        yield return new object[] { "Atn2-arg2", "ATN2(0, TRUE)" };
        yield return new object[] { "Sqr-arg1", "SQR(TRUE)" };
        yield return new object[] { "Cbrt-arg1", "CBRT(TRUE)" };
        yield return new object[] { "Exp-arg1", "EXP(TRUE)" };
        yield return new object[] { "Log-arg1", "LOG(TRUE)" };
        yield return new object[] { "Log10-arg1", "LOG10(TRUE)" };
        yield return new object[] { "Log2-arg1", "LOG2(TRUE)" };
        yield return new object[] { "Pow-arg1", "POW(TRUE, 3)" };
        yield return new object[] { "Pow-arg2", "POW(2, TRUE)" };
        yield return new object[] { "Asin-arg1", "ASIN(TRUE)" };
        yield return new object[] { "Acos-arg1", "ACOS(TRUE)" };
        yield return new object[] { "Asinh-arg1", "ASINH(TRUE)" };
        yield return new object[] { "Acosh-arg1", "ACOSH(TRUE)" };
        yield return new object[] { "Atanh-arg1", "ATANH(TRUE)" };
        yield return new object[] { "Sinh-arg1", "SINH(TRUE)" };
        yield return new object[] { "Cosh-arg1", "COSH(TRUE)" };
        yield return new object[] { "Tanh-arg1", "TANH(TRUE)" };
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void ExactRegisteredFormAndTypeProduceExpectedValue(string id, string call, string expected, string expectedType)
    {
        var source = Setup + "\nPRINT " + call;
        var syntax = Parser.Parse(source, out var parsingDiagnostics);
        Assert.Empty(parsingDiagnostics);
        var program = Binder.Bind(syntax, out var bindingDiagnostics);
        Assert.Empty(bindingDiagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements.Last());
        var boundCall = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(boundCall.Target).Function.BuiltinId?.ToString());
        Assert.Equal(expectedType, boundCall.Type.Name.ToUpperInvariant());
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
            Assert.False(result.UsedInterpreterFallback);
            if (id == "Rnd")
            {
                var value = double.Parse(Assert.Single(result.Output), System.Globalization.CultureInfo.InvariantCulture);
                Assert.True(value >= 0 && value < 1);
            }
            else
                Assert.Equal(new[] { expected }, result.Output);
        }
    }

    [Theory]
    [MemberData(nameof(WrongArguments))]
    public void WrongTypesHaveMatchingDiagnostics(string id, string call)
    {
        var source = Setup + "\nPRINT " + call;
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.False(first.Succeeded, id);
        Assert.False(second.Succeeded, id);
        Assert.False(second.UsedInterpreterFallback);
        Assert.NotEmpty(first.Diagnostics);
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
    }
}
