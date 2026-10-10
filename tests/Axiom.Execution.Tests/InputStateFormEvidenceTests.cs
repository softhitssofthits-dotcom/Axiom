using Axiom.Parsing;
using Axiom.Runtime;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class InputStateFormEvidenceTests
{
    [Theory]
    [InlineData("MOUSEX()", "MouseX", "LONG")]
    [InlineData("MOUSEY()", "MouseY", "LONG")]
    [InlineData("MOUSEBUTTON(1)", "MouseButton", "BOOLEAN")]
    [InlineData("MOUSEPRESS(1)", "MousePress", "BOOLEAN")]
    [InlineData("SCREENISACTIVE()", "ScreenIsActive", "BOOLEAN")]
    [InlineData("SCREENMODE()", "ScreenMode", "INTEGER")]
    [InlineData("SCREENWIDTH()", "ScreenWidth", "INTEGER")]
    [InlineData("SCREENHEIGHT()", "ScreenHeight", "INTEGER")]
    [InlineData("TEXTWIDTH(\"😀\")", "TextWidth", "INTEGER")]
    [InlineData("TEXTHEIGHT(\"😀\")", "TextHeight", "INTEGER")]
    [InlineData("POINT(0)", "PointPosition", "INTEGER")]
    [InlineData("POINT(0, 0)", "Point", "INTEGER")]
    [InlineData("PMAP(0, 0)", "PMap", "DOUBLE")]
    public void ExactFormsBindToDeclaredTypes(string expression, string id, string type)
    {
        var syntax = Parser.Parse("PRINT " + expression, out var parsing);
        Assert.Empty(parsing);
        // Здесь проверяется тип вызова, а не допустимость состояния экрана.
        var program = Binder.Bind(syntax, out var binding, checkGraphicsModes: false);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        Assert.Equal(type switch { "LONG" => TypeSymbol.Long, "BOOLEAN" => TypeSymbol.Boolean, "DOUBLE" => TypeSymbol.Double, _ => TypeSymbol.Integer }, call.Type);
    }

    [Fact]
    public void NoProviderHasInactiveZeroScreenState()
    {
        Check(null, ["FALSE", "0", "0", "0"]);
    }

    [Fact]
    public void QueryReflectsScreenStateWithoutNarrowingIntegerDimensions()
    {
        Check(new StateHost(), ["TRUE", "10", "2147483647", "1"]);
    }

    private static void Check(IInputProvider? host, string[] expected)
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT SCREENISACTIVE()\nPRINT SCREENMODE()\nPRINT SCREENWIDTH()\nPRINT SCREENHEIGHT()", inputProvider: host));
        Assert.True(run.Succeeded);
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(expected, run.Output);
    }

    // Проверка передачи состояния, а не допустимости создания окна такого размера.
    private sealed class StateHost : IInputProvider, IGraphicsHost
    {
        public bool IsScreenActive => true;
        public bool IsScreenOutputActive => false;
        public int ScreenMode => 10;
        public int ScreenWidth => int.MaxValue;
        public int ScreenHeight => 1;
        public string ReadInput(InputRequest request) => throw new InvalidOperationException("Ввод не ожидается.");
    }
}
