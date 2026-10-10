using Axiom.Runtime;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class KeyboardUnicodeEvidenceTests
{
    [Theory]
    [InlineData("INKEY()", "InKey", "STRING")]
    [InlineData("KEYDOWN(\"😀\")", "KeyDown", "BOOLEAN")]
    [InlineData("KEYPRESS(\"😀\")", "KeyPress", "BOOLEAN")]
    public void ExactFormsBindToDeclaredTypes(string expression, string id, string type)
    {
        var syntax = Parser.Parse("PRINT " + expression, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        Assert.Equal(type == "STRING" ? TypeSymbol.String : TypeSymbol.Boolean, call.Type);
    }

    [Theory]
    [InlineData("INKEY(1)")]
    [InlineData("KEYDOWN()")]
    [InlineData("KEYPRESS()")]
    [InlineData("KEYDOWN(1)")]
    [InlineData("KEYPRESS(1)")]
    [InlineData("KEYDOWN(\"A\", \"B\")")]
    [InlineData("KEYPRESS(\"A\", \"B\")")]
    public void InvalidArityOrTypeIsRejectedBeforeExecution(string expression)
    {
        var syntax = Parser.Parse("PRINT " + expression, out var parsing);
        Assert.Empty(parsing);
        _ = Binder.Bind(syntax, out var binding);
        Assert.NotEmpty(binding);
    }

    [Fact]
    public void ReadingPreservesWholeUtf16EventsAndQueriesDoNotConsumeThem()
    {
        var input = new LiteralKeyboardProvider(["😀", "\0", "\uD83D", "e\u0301", "é"], ["😀", "\0", "\uD83D"]);
        Check(input, """
            PRINT KEYDOWN("😀")
            PRINT KEYDOWN(CHRW(0))
            PRINT KEYDOWN(LEFT("😀", 1))
            PRINT KEYDOWN("😀" + CHRW(0))
            PRINT KEYDOWN("")
            PRINT INKEY() = "😀"
            PRINT INKEY() = CHRW(0)
            PRINT INKEY() = LEFT("😀", 1)
            PRINT INKEY() = "é"
            PRINT INKEY() = "é"
            PRINT INKEY() = ""
            """, ["TRUE", "TRUE", "TRUE", "FALSE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void PressConsumesOneWholeEventAndPreservesUnmatchedOrder()
    {
        var input = new LiteralKeyboardProvider(["é", "😀", "\0", "\uD83D", "e\u0301", "😀"], []);
        Check(input, """
            PRINT KEYPRESS("😀" + CHRW(0))
            PRINT KEYPRESS("")
            PRINT KEYPRESS("😀")
            PRINT KEYPRESS(CHRW(0))
            PRINT KEYPRESS(LEFT("😀", 1))
            PRINT INKEY() = "é"
            PRINT INKEY() = "é"
            PRINT INKEY() = "😀"
            PRINT INKEY() = ""
            """, ["FALSE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void AbsentProviderReturnsEmptyOrFalseForWholeUnicodeNames()
    {
        Check(null, """
            PRINT INKEY() = ""
            PRINT KEYDOWN("😀")
            PRINT KEYPRESS(CHRW(0))
            PRINT KEYPRESS(LEFT("😀", 1))
            """, ["TRUE", "FALSE", "FALSE", "FALSE"]);
    }

    private static void Check(IInputProvider? input, string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, inputProvider: input));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }

    // Независимый поставщик не использует нормализатор Runtime.
    private sealed class LiteralKeyboardProvider(string[] events, string[] down) : IInputProvider, IKeyboardInputProvider
    {
        private readonly Queue<string> _events = new(events);
        public string ReadInput(InputRequest request) => throw new InvalidOperationException("Блокирующий ввод не ожидается.");
        public string ReadKey() => _events.TryDequeue(out var value) ? value : string.Empty;
        public bool IsKeyDown(string keyText) => down.Contains(keyText, StringComparer.Ordinal);
    }
}
