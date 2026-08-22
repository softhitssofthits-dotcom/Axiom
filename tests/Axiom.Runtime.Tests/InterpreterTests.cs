using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
    [Fact]
    public void Execute_ArithmeticExpression()
    {
        var (_, host, result) = ExecuteProgram("PRINT 1 + 2 * 3");

        Assert.True(result.Succeeded);
        Assert.Equal(["7"], host.Output);
    }

    [Fact]
    public void Execute_StringAppendAccumulatorPreservesSemantics()
    {
        const string text = """
DIM result AS STRING = ""
FOR i = 1 TO 4
    result = result & i
NEXT
PRINT result
DIM aliasText AS STRING = result
result = result & "-x"
PRINT aliasText
PRINT result
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1234", "1234", "1234-x"], host.Output);
    }

    [Fact]
    public void Execute_StringPlusAppendAccumulatorPreservesSemantics()
    {
        const string text = """
DIM result AS STRING = ""
FOR i = 1 TO 3
    result = result + "x"
NEXT
PRINT result
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["xxx"], host.Output);
    }

    [Fact]
    public void Execute_ForReusesPredeclaredControlVariable()
    {
        const string text = """
DIM i AS INTEGER
FOR i = 1 TO 3
    PRINT i
NEXT
PRINT "after=" & i
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "3", "after=4"], host.Output);
    }

    [Fact]
    public void Execute_IntegerDivisionAndRemainder()
    {
        const string text = """
PRINT 7 \ 2
PRINT 7 MOD 2
PRINT -7 \ 2
PRINT -7 MOD 2
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "1", "-3", "-1"], host.Output);
    }

    [Fact]
    public void Execute_IntegerDivisionAndRemainderPromoteToLong()
    {
        const string text = """
DIM value AS LONG = 21474836479
PRINT value \ 2
PRINT value MOD 2
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10737418239", "1"], host.Output);
    }

    [Fact]
    public void Execute_ExponentiationAndHexLiterals()
    {
        const string text = """
PRINT 2 ^ 3
PRINT 2 ^ 3 ^ 2
PRINT &HFF
PRINT &H80000000
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["8", "512", "255", "2147483648"], host.Output);
    }

    [Fact]
    public void Execute_PredefinedConstantsCanBeReadInExpressionsAndArrayDimensions()
    {
        const string text = """
DIM board(10 TO 12, 20 TO 21) AS INTEGER
PRINT ROW
PRINT COLUMN
PRINT LBOUND(board, ROW)
PRINT UBOUND(board, COLUMN)
PRINT PI > 3
PRINT PI < 4
PRINT ROW + COLUMN
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "10", "21", "TRUE", "TRUE", "3"], host.Output);
    }

    [Fact]
    public void Execute_BitwiseOperatorsAndShifts()
    {
        const string text = """
PRINT 6 AND 3
PRINT 6 OR 1
PRINT 6 XOR 3
PRINT 6 EQV 3
PRINT 6 IMP 3
PRINT NOT 0
PRINT TRUE XOR FALSE
PRINT TRUE EQV TRUE
PRINT TRUE IMP FALSE
PRINT 1 + 2 << 1
DIM mask AS LONG = &H100000000
PRINT mask >> 4
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "7", "5", "-6", "-5", "-1", "TRUE", "TRUE", "FALSE", "6", "268435456"], host.Output);
    }

    [Fact]
    public void Execute_NumericComparisonOperators()
    {
        const string text = """
DIM a AS INTEGER = 5
DIM b AS INTEGER = 10
PRINT a = b
PRINT a <> b
PRINT a < b
PRINT a <= b
PRINT a > b
PRINT a >= b
PRINT a = a
PRINT a <= a
DIM x AS LONG = 100
DIM y AS LONG = 200
PRINT x < y
PRINT x > y
DIM d1 AS DOUBLE = 3.14
DIM d2 AS DOUBLE = 2.71
PRINT d1 > d2
PRINT d1 < d2
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE", "TRUE", "TRUE", "TRUE", "FALSE", "TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_StringComparisonOperators()
    {
        const string text = """
DIM a AS STRING = "apple"
DIM b AS STRING = "banana"
PRINT a < b
PRINT a > b
PRINT a = a
PRINT a <> b
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_InKeyReadsQueuedKeysWithoutBlocking()
    {
        const string text = """
PRINT INKEY()
PRINT INKEY()
PRINT INKEY()
""";

        var (_, host, result) = ExecuteProgramWithKeys(text, "A", "LEFT");

        Assert.True(result.Succeeded);
        Assert.Equal(["A", "LEFT", ""], host.Output);
    }

    [Fact]
    public void Execute_SleepZeroContinues()
    {
        const string text = """
PRINT "before"
SLEEP 0
PRINT "after"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["before", "after"], host.Output);
    }

    [Fact]
    public void Execute_SleepWithoutDurationConsumesQueuedKey()
    {
        const string text = """
SLEEP
PRINT INKEY()
""";

        var (_, host, result) = ExecuteProgramWithKeys(text, "A", "B");

        Assert.True(result.Succeeded);
        Assert.Equal(["B"], host.Output);
    }

    [Fact]
    public void Execute_SleepReportsNegativeDuration()
    {
        var (_, _, result) = ExecuteProgram("SLEEP -1");

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SLEEP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SleepWithoutDuration_StopsWhenCancellationIsRequested()
    {
        const string text = """
SLEEP
PRINT "after"
""";

        var host = new TestRuntimeHost([], cancelAfterReadKeyCalls: 3);
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var result = Interpreter.Execute(program, host);

        Assert.True(result.Cancelled);
        Assert.False(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain("after", host.Output, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SleepWithDuration_StopsWhenCancellationIsRequested()
    {
        const string text = """
SLEEP 1000
PRINT "after"
""";

        var host = new TestRuntimeHost([], cancelAfterCancellationPolls: 3);
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var result = Interpreter.Execute(program, host);

        Assert.True(result.Cancelled);
        Assert.False(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain("after", host.Output, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_KeyDownReadsHeldKeyStateWithoutConsumingQueuedKeys()
    {
        const string text = """
PRINT KEYDOWN("LEFT")
PRINT KEYDOWN("CTRL") AND KEYDOWN("LEFT")
PRINT INKEY()
""";

        var (_, host, result) = ExecuteProgramWithPressedKeys(text, "LEFT", "CTRL");

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", ""], host.Output);
    }

    [Fact]
    public void Execute_KeyDownAcceptsCanonicalAliases()
    {
        const string text = """
PRINT KEYDOWN("CONTROL")
PRINT KEYDOWN("PAGE UP")
PRINT KEYDOWN("ESCAPE")
""";

        var (_, host, result) = ExecuteProgramWithPressedKeys(text, "CTRL", "PAGEUP", "ESC");

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_KeyPressConsumesMatchingQueuedKeyOnce()
    {
        const string text = """
PRINT KEYPRESS("LEFT")
PRINT KEYPRESS("LEFT")
PRINT INKEY()
""";

        var (_, host, result) = ExecuteProgramWithKeys(text, "LEFT", "A");

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "A"], host.Output);
    }

    [Fact]
    public void Execute_KeyPressPreservesUnmatchedQueuedKeys()
    {
        const string text = """
PRINT KEYPRESS("LEFT")
PRINT INKEY()
PRINT KEYPRESS("ESCAPE")
""";

        var (_, host, result) = ExecuteProgramWithKeys(text, "RIGHT", "ESC");

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "RIGHT", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_KeyPressAcceptsCanonicalAliases()
    {
        const string text = """
PRINT KEYPRESS("ESCAPE")
PRINT KEYPRESS("PAGE UP")
PRINT KEYPRESS("CONTROL")
""";

        var (_, host, result) = ExecuteProgramWithKeys(text, "ESC", "PAGEUP", "CTRL");

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_KeyPressAcceptsChordSyntax()
    {
        const string text = """
PRINT KEYPRESS("CTRL+ALT+DELETE")
PRINT KEYPRESS("CTRL+ALT+DELETE")
""";

        var (_, host, result) = ExecuteProgramWithKeysAndPressedKeys(text, ["DELETE"], ["CTRL", "ALT"]);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_KeyDownAcceptsChordSyntax()
    {
        const string text = """
PRINT KEYDOWN("CTRL+F+A+X")
PRINT KEYDOWN("CTRL+F+A+Z")
""";

        var (_, host, result) = ExecuteProgramWithPressedKeys(text, "CTRL", "F", "A", "X");

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_MouseFunctionsReadCoordinatesAndButtons()
    {
        const string text = """
PRINT MOUSEX()
PRINT MOUSEY()
PRINT MOUSEBUTTON(1)
PRINT MOUSEBUTTON(2)
PRINT MOUSEBUTTON(3)
PRINT MOUSEBUTTON(99)
""";

        var (_, host, result) = ExecuteProgramWithMouseState(text, mouseX: 320, mouseY: 240, pressedMouseButtons: [1, 3]);

        Assert.True(result.Succeeded);
        Assert.Equal(["320", "240", "TRUE", "FALSE", "TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_MouseFunctionsUseCanvasWorldCoordinates()
    {
        const string text = """
CANVAS 160, 90
VIEWPORT (10, 12)-(30, 28)
CAMERA 100, 50
PRINT MOUSEX()
PRINT MOUSEY()
PRINT MOUSEBUTTON(1)
""";

        var (_, host, result) = ExecuteProgramWithMouseState(text, mouseX: 15, mouseY: 16, pressedMouseButtons: [1]);

        Assert.True(result.Succeeded);
        Assert.Equal(["105", "54", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_MouseFunctionsWithoutScreenReportRuntimeDiagnostic()
    {
        const string text = """
PRINT MOUSEX()
PRINT MOUSEY()
PRINT MOUSEBUTTON(1)
""";

        var (_, host, result) = ExecuteProgramWithMouseState(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_Assignment()
    {
        const string text = """
DIM value AS INTEGER
value = 10
PRINT value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10"], host.Output);
    }

    [Fact]
    public void Execute_GroupedDimDeclarations()
    {
        const string text = """
DIM anotherVar, choice AS BOOLEAN, birthDate AS STRING
anotherVar = TRUE
choice = FALSE
birthDate = "2026-04-13"
PRINT anotherVar
PRINT choice
PRINT birthDate
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "2026-04-13"], host.Output);
    }

    [Fact]
    public void Execute_InferredDimDeclarations()
    {
        const string text = """
DIM count = 10
DIM name = "Alice"
DIM ready = TRUE
DIM price = 1.5
PRINT count
PRINT name
PRINT ready
PRINT price
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "Alice", "TRUE", "1.5"], host.Output);
    }

    [Fact]
    public void Execute_InferredLongDeclarationAndArithmetic()
    {
        const string text = """
DIM value = 21474836479
PRINT value
PRINT value + value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["21474836479", "42949672958"], host.Output);
    }

    [Fact]
    public void Execute_DynamicArrayDeclarationDoesNotFail()
    {
        const string text = """
DIM items() AS STRING
PRINT "ok"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["ok"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRndReturnsDoubleInUnitInterval()
    {
        const string text = """
DIM value AS DOUBLE = RND()
PRINT value >= 0
PRINT value < 1
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
    }

    [Theory]
    [InlineData("PRINT ABS(-5)", "5")]
    [InlineData("PRINT SGN(-5)", "-1")]
    [InlineData("PRINT MIN(10, 3)", "3")]
    [InlineData("PRINT MAX(10, 3)", "10")]
    [InlineData("PRINT CLAMP(15, 1, 10)", "10")]
    [InlineData("PRINT ROUND(SIN(0))", "0")]
    [InlineData("PRINT ROUND(COS(0))", "1")]
    [InlineData("PRINT ROUND(TAN(0))", "0")]
    [InlineData("PRINT ROUND(ATN2(0, 1))", "0")]
    [InlineData("PRINT SQR(9)", "3")]
    [InlineData("PRINT CBRT(27)", "3")]
    [InlineData("PRINT EXP(0)", "1")]
    [InlineData("PRINT LOG(1)", "0")]
    [InlineData("PRINT LOG10(1000)", "3")]
    [InlineData("PRINT LOG2(8)", "3")]
    [InlineData("PRINT POW(2, 3)", "8")]
    [InlineData("PRINT ROUND(ASIN(0.5) * 1000)", "524")]
    [InlineData("PRINT ROUND(ACOS(0.5) * 1000)", "1047")]
    [InlineData("PRINT ROUND(ASINH(1) * 1000)", "881")]
    [InlineData("PRINT ROUND(ACOSH(2) * 1000)", "1317")]
    [InlineData("PRINT ROUND(ATANH(0.5) * 1000)", "549")]
    [InlineData("PRINT ROUND(SINH(1) * 1000)", "1175")]
    [InlineData("PRINT ROUND(COSH(1) * 1000)", "1543")]
    [InlineData("PRINT ROUND(TANH(1) * 1000)", "762")]
    [InlineData("PRINT SETSIGN(5, -1)", "-5")]
    [InlineData("PRINT FLOOR(-3.2)", "-4")]
    [InlineData("PRINT CEIL(-3.2)", "-3")]
    [InlineData("PRINT ROUND(-2.5)", "-3")]
    [InlineData("PRINT FIX(-2.5)", "-2")]
    public void Execute_MathBuiltinsReturnExpectedOutput(string source, string expectedOutput)
    {
        var (_, host, result) = ExecuteProgram(source);

        Assert.True(result.Succeeded);
        Assert.Equal([expectedOutput], host.Output);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("-1")]
    public void Execute_BuiltinLeftReportsNonPositiveCount(string countText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT LEFT(\"Hello\", {countText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("LEFT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(21, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("-1")]
    public void Execute_BuiltinRightReportsNonPositiveCount(string countText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT RIGHT(\"Hello\", {countText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("RIGHT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(22, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_BuiltinMidReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT MID(\"Hello\", 0, 2)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("MID", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(20, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("-1")]
    public void Execute_BuiltinMidReportsNonPositiveCount(string countText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT MID(\"Hello\", 1, {countText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("MID", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(23, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_BuiltinRightReturnsFullStringWhenCountExceedsLength()
    {
        var (_, host, result) = ExecuteProgram("PRINT RIGHT(\"Hello\", 100)");

        Assert.True(result.Succeeded);
        Assert.Equal(["Hello"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRightHandlesExactLengthAndSingleCharacterSuffix()
    {
        const string text = """
PRINT RIGHT("Hello", 5)
PRINT RIGHT("Hello", 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Hello", "o"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinLeftReturnsFullStringWhenCountExceedsLength()
    {
        var (_, host, result) = ExecuteProgram("PRINT LEFT(\"Hello\", 100)");

        Assert.True(result.Succeeded);
        Assert.Equal(["Hello"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinMidReturnsEmptyStringWhenStartExceedsLength()
    {
        var (_, host, result) = ExecuteProgram("PRINT MID(\"Hello\", 10, 2)");

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
    }

    [Fact]
    public void Execute_BuiltinValReturnsZeroForInvalidText()
    {
        var (_, host, result) = ExecuteProgram("PRINT STR(VAL(\"abc\"))");

        Assert.True(result.Succeeded);
        Assert.Equal(["0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinIntReportsOverflowAfterFloor()
    {
        var (_, _, result) = ExecuteProgram("PRINT INT(3000000000.5)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("INT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(11, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("PRINT SQR(-1)", "SQR", "value")]
    [InlineData("PRINT LOG(0)", "LOG", "value")]
    [InlineData("PRINT LOG10(-10)", "LOG10", "value")]
    [InlineData("PRINT CLAMP(5, 10, 1)", "CLAMP", "high")]
    [InlineData("PRINT POW(-1, 0.5)", "POW", "value")]
    [InlineData("PRINT ASIN(2)", "ASIN", "value")]
    [InlineData("PRINT ACOS(-2)", "ACOS", "value")]
    [InlineData("PRINT ACOSH(0.5)", "ACOSH", "value")]
    [InlineData("PRINT ATANH(1)", "ATANH", "value")]
    public void Execute_MathBuiltinsReportDomainErrors(string source, string functionName, string argumentName)
    {
        var (_, _, result) = ExecuteProgram(source);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains(functionName, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(argumentName, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFixDiffersFromIntForNegativeFraction()
    {
        const string text = """
PRINT INT(-1.2)
PRINT FIX(-1.2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["-2", "-1"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinSetSignPreservesDecimalType()
    {
        const string text = """
DIM value AS DECIMAL = 10.5
PRINT SETSIGN(value, 1)
PRINT SETSIGN(value, -1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10.5", "-10.5"], host.Output);
    }

    [Fact]
    public void Execute_MathBuiltinsReportDoubleOverflow()
    {
        var (_, _, result) = ExecuteProgram("PRINT EXP(10000)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_HyperbolicMathBuiltinsReportDoubleOverflow()
    {
        var (_, _, result) = ExecuteProgram("PRINT COSH(10000)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_InputSingleVariable()
    {
        const string text = """
DIM name AS STRING
INPUT name
PRINT name
""";

        var (_, host, result) = ExecuteProgram(text, "Ada");

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada"], host.Output);
        var request = Assert.Single(host.InputRequests);
        Assert.Null(request.PromptText);
        Assert.Equal(1, request.ValueCount);
    }

    [Fact]
    public void Execute_InputMultipleVariables()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT firstName, age
PRINT firstName; age
""";

        var (_, host, result) = ExecuteProgram(text, "Ada, 42");

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada42"], host.Output);
        Assert.Equal(2, Assert.Single(host.InputRequests).ValueCount);
    }

    [Fact]
    public void Execute_InputLongVariable()
    {
        const string text = """
DIM value AS LONG
INPUT value
PRINT value
""";

        var (_, host, result) = ExecuteProgram(text, "21474836479");

        Assert.True(result.Succeeded);
        Assert.Equal(["21474836479"], host.Output);
    }

    [Fact]
    public void Execute_InputDecimalVariable()
    {
        const string text = """
DIM value AS DECIMAL
INPUT value
PRINT value
""";

        var (_, host, result) = ExecuteProgram(text, "125.75");

        Assert.True(result.Succeeded);
        Assert.Equal(["125.75"], host.Output);
    }

    [Fact]
    public void Execute_InputWithPromptCapturesRequestMetadata()
    {
        const string text = """
DIM name AS STRING
INPUT "Name"; name
PRINT name
""";

        var (_, host, result) = ExecuteProgram(text, "Ada");

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada"], host.Output);
        var request = Assert.Single(host.InputRequests);
        Assert.Equal("Name", request.PromptText);
        Assert.Equal(1, request.ValueCount);
    }

    [Fact]
    public void Execute_InputDoubleVariable()
    {
        const string text = """
DIM value AS DOUBLE
INPUT value
PRINT value
""";

        var (_, host, result) = ExecuteProgram(text, "3.14");

        Assert.True(result.Succeeded);
        Assert.Equal(["3.14"], host.Output);
    }

    [Fact]
    public void Execute_InputBooleanVariable()
    {
        const string text = """
DIM flag AS BOOLEAN
INPUT flag
PRINT flag
""";

        var (_, host, result) = ExecuteProgram(text, "TRUE");

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE"], host.Output);
    }

    [Fact]
    public void Execute_InputBooleanCaseInsensitive()
    {
        const string text = """
DIM flag AS BOOLEAN
INPUT flag
PRINT flag
""";

        var (_, host, result) = ExecuteProgram(text, "false");

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE"], host.Output);
    }

    [Fact]
    public void Execute_InputMixedTypes()
    {
        const string text = """
DIM name AS STRING
DIM age AS INTEGER
DIM score AS DOUBLE
INPUT name, age, score
PRINT name; " "; age; " "; score
""";

        var (_, host, result) = ExecuteProgram(text, "Ada, 25, 98.5");

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada 25 98.5"], host.Output);
    }

    [Fact]
    public void Execute_InputWithCommaPrompt()
    {
        const string text = """
DIM name AS STRING
INPUT "Name,", name
PRINT name
""";

        var (_, host, result) = ExecuteProgram(text, "Ada");

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada"], host.Output);
        var request = Assert.Single(host.InputRequests);
        Assert.Equal("Name,", request.PromptText);
    }

    [Fact]
    public void Execute_InputStringPreservesFullLineForSingleVariable()
    {
        const string text = """
DIM msg AS STRING
INPUT msg
PRINT msg
""";

        var (_, host, result) = ExecuteProgram(text, "hello, world");

        Assert.True(result.Succeeded);
        Assert.Equal(["hello, world"], host.Output);
    }

    [Fact]
    public void Execute_InputIntegerConversion()
    {
        const string text = """
DIM n AS INTEGER
INPUT n
PRINT n + 1
""";

        var (_, host, result) = ExecuteProgram(text, "41");

        Assert.True(result.Succeeded);
        Assert.Equal(["42"], host.Output);
    }

    [Fact]
    public void Execute_InputReportsConversionError()
    {
        const string text = """
DIM n AS INTEGER
INPUT n
""";

        var (_, host, result) = ExecuteProgram(text, "abc");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Execute_InputReportsFieldCountMismatch()
    {
        const string text = """
DIM a AS INTEGER
DIM b AS INTEGER
INPUT a, b
""";

        var (_, host, result) = ExecuteProgram(text, "1");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Execute_InputReportsConstantTarget()
    {
        const string text = """
CONST x AS INTEGER = 5
INPUT x
""";

        var program = Parser.Parse(text, out var parserDiagnostics);
        var bound = Binder.Bind(program, out var binderDiagnostics);

        Assert.NotEmpty(binderDiagnostics);
    }

    [Fact]
    public void Execute_InterpolatedStringBasic()
    {
        const string text = """
DIM name AS STRING = "Алиса"
PRINT $"Привет, {name}!"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded, $"Failed: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}");
        Assert.Equal(["Привет, Алиса!"], host.Output);
    }

    [Fact]
    public void Execute_InterpolatedStringMultipleExpressions()
    {
        const string text = """
DIM name AS STRING = "Алиса"
DIM age AS INTEGER = 25
PRINT $"{name} {age}"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded, $"Failed: {string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}"))}");
        Assert.Equal(["Алиса 25"], host.Output);
    }

    [Fact]
    public void Execute_InterpolatedStringWithExpression()
    {
        const string text = """
DIM a AS INTEGER = 3
DIM b AS INTEGER = 4
PRINT $"Сумма: {a + b}"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded, $"Failed: {string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}"))}");
        Assert.Equal(["Сумма: 7"], host.Output);
    }

    [Fact]
    public void Execute_InterpolatedStringEmpty()
    {
        const string text = """
PRINT $""
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
    }

    [Fact]
    public void Execute_InterpolatedStringNoExpressions()
    {
        const string text = """
PRINT $"Просто текст"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Просто текст"], host.Output);
    }

    [Fact]
    public void Execute_InterpolatedStringWithFunctionCall()
    {
        const string text = """
DIM s AS STRING = "hello"
PRINT $"Длина: {LEN(s)}"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Длина: 5"], host.Output);
    }

    [Fact]
    public void Execute_InterpolatedStringWithBoolean()
    {
        const string text = """
DIM flag AS BOOLEAN = TRUE
PRINT $"Флаг: {flag}"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Флаг: TRUE"], host.Output);
    }

    [Fact]
    public void Execute_InterpolatedStringNested()
    {
        const string text = """
DIM name AS STRING = "Алиса"
DIM age AS INTEGER = 25
DIM city AS STRING = "Москва"
PRINT $"{name}, {age} лет, {city}"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded, $"Failed: {string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}"))}");
        Assert.Equal(["Алиса, 25 лет, Москва"], host.Output);
    }

}
