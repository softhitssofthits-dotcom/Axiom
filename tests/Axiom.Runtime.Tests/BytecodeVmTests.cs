using Axiom.Bytecode;
using Axiom.Parsing;
using Axiom.Semantics;
using System.Linq;

namespace Axiom.Runtime.Tests;

public sealed class BytecodeVmTests
{
    [Fact]
    public void Execute_CompiledAmpersandConcatenatesMixedScalarValues()
    {
        const string text = """
DIM name AS STRING = "Иван"
DIM age AS INTEGER = 7
PRINT "Привет, " & name & " тебе " & age & " лет."
PRINT "flag=" & TRUE
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Привет, Иван тебе 7 лет.", "flag=TRUE"], host.Output);
    }

    [Fact]
    public void Execute_CompiledStringAppendAccumulatorPreservesSemantics()
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

        var (module, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1234", "1234", "1234-x"], host.Output);
        Assert.Contains(
            module.Routines.SelectMany(static routine => routine.Instructions),
            static instruction => instruction.Opcode == Opcode.AppendStringLocal);
    }

    [Fact]
    public void Execute_CompiledStringPlusAppendAccumulatorPreservesSemantics()
    {
        const string text = """
DIM result AS STRING = ""
FOR i = 1 TO 3
    result = result + "x"
NEXT
PRINT result
""";

        var (module, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["xxx"], host.Output);
        Assert.Contains(
            module.Routines.SelectMany(static routine => routine.Instructions),
            static instruction => instruction.Opcode == Opcode.AppendStringLocal);
    }

    [Fact]
    public void Execute_CompiledForReusesPredeclaredControlVariable()
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
    public void Execute_CompiledScalarProgramWithRoutineAndBuiltin()
    {
        const string text = """
FUNCTION Increment(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT Increment(41)
PRINT LEN("abcd")
PRINT "A", "B"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "4", "A B"], host.Output);
    }

    [Fact]
    public void Execute_InKeyReadsQueuedKeysWithoutBlocking()
    {
        const string text = """
PRINT INKEY()
PRINT INKEY()
PRINT INKEY()
""";

        var host = new TestRuntimeHost(keys: ["A", "LEFT"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["A", "LEFT", ""], host.Output);
    }

    [Fact]
    public void Execute_CompiledSleepZeroContinues()
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
    public void Execute_CompiledSleepWithoutDurationConsumesQueuedKey()
    {
        const string text = """
SLEEP
PRINT INKEY()
""";

        var host = new TestRuntimeHost(keys: ["A", "B"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["B"], host.Output);
    }

    [Fact]
    public void Execute_CompiledSleepReportsNegativeDuration()
    {
        var (_, _, result) = ExecuteProgram("SLEEP -1");

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SLEEP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledSleepWithoutDuration_StopsWhenCancellationIsRequested()
    {
        const string text = """
SLEEP
PRINT "after"
""";

        var host = new TestRuntimeHost(cancelAfterReadKeyCalls: 3);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Cancelled);
        Assert.False(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain("after", host.Output, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledSleepWithDuration_StopsWhenCancellationIsRequested()
    {
        const string text = """
SLEEP 1000
PRINT "after"
""";

        var host = new TestRuntimeHost(cancelAfterCancellationPolls: 3);
        var (_, _, result) = ExecuteProgram(text, host);

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

        var host = new TestRuntimeHost(pressedKeys: ["LEFT", "CTRL"]);
        var (_, _, result) = ExecuteProgram(text, host);

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

        var host = new TestRuntimeHost(pressedKeys: ["CTRL", "PAGEUP", "ESC"]);
        var (_, _, result) = ExecuteProgram(text, host);

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

        var host = new TestRuntimeHost(keys: ["LEFT", "A"]);
        var (_, _, result) = ExecuteProgram(text, host);

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

        var host = new TestRuntimeHost(keys: ["RIGHT", "ESC"]);
        var (_, _, result) = ExecuteProgram(text, host);

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

        var host = new TestRuntimeHost(keys: ["ESC", "PAGEUP", "CTRL"]);
        var (_, _, result) = ExecuteProgram(text, host);

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

        var host = new TestRuntimeHost(keys: ["DELETE"], pressedKeys: ["CTRL", "ALT"]);
        var (_, _, result) = ExecuteProgram(text, host);

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

        var host = new TestRuntimeHost(pressedKeys: ["CTRL", "F", "A", "X"]);
        var (_, _, result) = ExecuteProgram(text, host);

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

        var host = new TestRuntimeHost(mouseX: 320, mouseY: 240, pressedMouseButtons: [1, 3]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["320", "240", "TRUE", "FALSE", "TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_MouseFunctionsReturnDefaultValuesWithoutMouseProviderState()
    {
        const string text = """
PRINT MOUSEX()
PRINT MOUSEY()
PRINT MOUSEBUTTON(1)
""";

        var host = new TestRuntimeHost();
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_CompiledCircleSupportsStepArcAndAspect()
    {
        const string text = """
SCREEN 1
PSET (10, 20), 7
CIRCLE STEP (5, 6), 12, , -PI / 2, PI, 0.5
""";

        var (module, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Contains(
            module.Routines.SelectMany(static routine => routine.Instructions),
            static instruction => instruction.Opcode == Opcode.Circle && instruction.GetOperand(0) == 15);
        var circle = Assert.Single(host.Circles);
        Assert.Equal(5, circle.X);
        Assert.Equal(6, circle.Y);
        Assert.Equal(12d, circle.Radius);
        Assert.Equal(15, circle.Color);
        Assert.True(circle.RelativeCenter);
        Assert.True(circle.DrawStartRadius);
        Assert.False(circle.DrawEndRadius);
        Assert.Equal(Math.PI / 2, circle.StartAngle.GetValueOrDefault(), precision: 10);
        Assert.Equal(Math.PI, circle.EndAngle.GetValueOrDefault(), precision: 10);
        Assert.Equal(.5d, circle.Aspect);
    }

    [Fact]
    public void Execute_CompiledPSetAndLineSupportStepAndPointPosition()
    {
        const string text = """
SCREEN 1
COLOR 6, 2
PSET (10, 20)
PRINT POINT(0)
PRINT POINT(1)
PSET STEP (5, 4)
PRINT POINT(0)
PRINT POINT(1)
LINE -(30, 40)
PRINT POINT(0)
PRINT POINT(1)
LINE STEP (5, 5)-STEP (10, 15)
PRINT POINT(0)
PRINT POINT(1)
PRINT POINT(10, 20)
PRINT POINT(15, 24)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "15", "24", "30", "40", "45", "60", "6", "6"], host.Output);
    }

    [Fact]
    public void Execute_CompiledColorSupportsOpaqueTransparentAndUnchangedBackgroundModes()
    {
        const string text = """
SCREEN 1
COLOR 6, 2
PRINT "A";
COLOR 15
PRINT "B";
COLOR 14, TRANSPARENT
PRINT "C";
COLOR 13
PRINT "D"
""";

        var (module, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Contains(
            module.Routines.SelectMany(static routine => routine.Instructions),
            static instruction => instruction.Opcode == Opcode.Color && instruction.GetOperand(0) == (int)BoundColorBackgroundMode.Transparent && instruction.GetOperand(1) == 1);
        Assert.Equal(13, host.CurrentForegroundColor);
        Assert.Equal(2, host.CurrentBackgroundColor);
        Assert.True(host.IsTextBackgroundTransparent);
    }

    [Fact]
    public void Execute_CompiledPaintFillsBoundedRegion()
    {
        const string text = """
SCREEN 1
PSET (2, 2), 9
PSET (3, 2), 9
PSET (4, 2), 9
PSET (2, 3), 9
PSET (4, 3), 9
PSET (2, 4), 9
PSET (3, 4), 9
PSET (4, 4), 9
PAINT (3, 3), 6, 9
PRINT POINT(3, 3)
PRINT POINT(2, 2)
""";

        var (module, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(6, host.GetPixel(3, 3));
        Assert.Equal(9, host.GetPixel(2, 2));
        Assert.Equal(["6", "9"], host.Output);
        Assert.Contains(
            module.Routines.SelectMany(static routine => routine.Instructions),
            static instruction => instruction.Opcode == Opcode.Paint);
    }

    [Fact]
    public void Execute_CompiledPaintStepUsesCurrentPositionAndBackgroundColorByDefault()
    {
        const string text = """
SCREEN 1
COLOR 15, 2
LINE (10, 10)-(12, 12), 9, B
PSET (10, 10), 9
PAINT STEP (1, 1), , 9
PRINT POINT(11, 11)
PRINT POINT(0)
PRINT POINT(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(2, host.GetPixel(11, 11));
        Assert.Equal(["2", "11", "11"], host.Output);
    }

    [Fact]
    public void Execute_CompiledPaintPatternAndBackgroundPatternRepaintRegion()
    {
        const string text = """
SCREEN 1
COLOR 6, 1
DIM oldPattern AS STRING = CHR(170) & CHR(85)
DIM newPattern AS STRING = CHR(85) & CHR(170)
LINE (1, 1)-(4, 4), 9, B
PAINT (2, 2), oldPattern, 9
PAINT (2, 2), newPattern, 9, oldPattern
PRINT POINT(2, 2)
PRINT POINT(3, 2)
PRINT POINT(2, 3)
PRINT POINT(3, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "6", "6", "1"], host.Output);
        Assert.Equal(1, host.GetPixel(2, 2));
        Assert.Equal(6, host.GetPixel(3, 2));
        Assert.Equal(6, host.GetPixel(2, 3));
        Assert.Equal(1, host.GetPixel(3, 3));
    }

    [Fact]
    public void Execute_CompiledDrawWithoutScreenReportsRuntimeDiagnostic()
    {
        var (_, _, result) = ExecuteProgram("DRAW \"R10\"");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_CompiledDrawSupportsMovementModifiersColorScaleAngleMoveAndPaint()
    {
        const string text = """
SCREEN 1
COLOR 6, 2
DRAW ""
DRAW "R10"
DRAW "D10 L5 U5"
DRAW "BR10 D5"
DRAW "NR10"
DRAW "BNR10"
DRAW "C12 R10"
DRAW "S8 R10"
DRAW "A1 U10"
DRAW "TA90 U10"
DRAW "M20,30"
DRAW "BM20,30"
DRAW "M+10,-5"
LINE (40, 40)-(44, 44), 15, B
DRAW "P4,15"
PRINT POINT(0)
PRINT POINT(1)
PRINT POINT(41, 41)
""";

        var (module, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Contains(
            module.Routines.SelectMany(static routine => routine.Instructions),
            static instruction => instruction.Opcode == Opcode.Draw);
        Assert.True(host.Lines.Count >= 9);
        Assert.Contains(host.Lines, line => line is { X1: 0, Y1: 0, X2: 10, Y2: 0, Color: 6 });
        Assert.Contains(host.Lines, line => line is { X1: 15, Y1: 10, X2: 25, Y2: 10, Color: 12 });
        Assert.Contains(host.PaintRequests, request => request.X == 44 && request.Y == 44 && request.SolidFillColor == 4 && request.BorderColor == 15);
        Assert.Equal(["44", "44", "4"], host.Output);
    }

    [Theory]
    [InlineData("DRAW \"Q10\"")]
    [InlineData("DRAW \"R-5\"")]
    [InlineData("DRAW \"M10\"")]
    [InlineData("DRAW \"Xdemo\"")]
    public void Execute_CompiledDrawReportsRuntimeDiagnosticsForInvalidCommands(string statement)
    {
        var (_, _, result) = ExecuteProgram("SCREEN 1\r\n" + statement);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB3007");
    }

    [Theory]
    [InlineData("DRAW \"BC12 R10\"")]
    [InlineData("DRAW \"NC12 R10\"")]
    [InlineData("DRAW \"BP4\"")]
    [InlineData("DRAW \"NP4\"")]
    [InlineData("DRAW \"BA1 R10\"")]
    [InlineData("DRAW \"BTA45 R10\"")]
    [InlineData("DRAW \"BXdemo\"")]
    [InlineData("DRAW \"NXdemo\"")]
    [InlineData("DRAW \"BNXdemo\"")]
    public void Execute_CompiledDrawRejectsMovementModifiersBeforeNonMovementCommands(string statement)
    {
        var (_, host, result) = ExecuteProgram(
            """
SCREEN 1
PSET (10, 10), 15
""" + "\r\n" + statement);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("Модификатор B/N", diagnostic.Message, StringComparison.Ordinal);
        Assert.Empty(host.Lines);
        Assert.Empty(host.PaintRequests);
    }

    [Theory]
    [InlineData("DRAW \"B\"")]
    [InlineData("DRAW \"N\"")]
    [InlineData("DRAW \"BN\"")]
    public void Execute_CompiledDrawRejectsDanglingMovementModifiers(string statement)
    {
        var (_, _, result) = ExecuteProgram("SCREEN 1\r\n" + statement);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("должны предшествовать команде движения или M", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_CompiledDrawReportsUnsupportedHistoricalX()
    {
        var (_, _, result) = ExecuteProgram("SCREEN 1\r\nDRAW \"Xdemo\"");

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("X/VARPTR$", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_CompiledDrawTracksCurrentGraphicsPositionAcrossMovementModes()
    {
        const string text = """
SCREEN 1
PSET (10, 10), 15
DRAW "R10"
PRINT POINT(0)
PRINT POINT(1)
DRAW "BR10"
PRINT POINT(0)
PRINT POINT(1)
DRAW "NR10"
PRINT POINT(0)
PRINT POINT(1)
DRAW "BNR10"
PRINT POINT(0)
PRINT POINT(1)
DRAW "M20,30"
PRINT POINT(0)
PRINT POINT(1)
DRAW "BM20,30"
PRINT POINT(0)
PRINT POINT(1)
DRAW "NM30,40"
PRINT POINT(0)
PRINT POINT(1)
DRAW "M+10,-5"
PRINT POINT(0)
PRINT POINT(1)
LINE -(35, 25), 7
PRINT POINT(0)
PRINT POINT(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["20", "10", "30", "10", "30", "10", "30", "10", "20", "30", "20", "30", "20", "30", "30", "25", "35", "25"],
            host.Output);
        Assert.Equal(6, host.Lines.Count);
        Assert.Contains(host.Lines, line => line is { X1: 10, Y1: 10, X2: 20, Y2: 10, Color: 15 });
        Assert.Contains(host.Lines, line => line is { X1: 30, Y1: 10, X2: 40, Y2: 10, Color: 15 });
        Assert.Contains(host.Lines, line => line is { X1: 30, Y1: 25, X2: 35, Y2: 25, Color: 7 });
    }

    [Fact]
    public void Execute_CompiledDrawUsesPaintDefaultBorderAndNormalizesColors()
    {
        const string text = """
SCREEN 1
PSET (10, 10), 15
DRAW "P4"
PSET (12, 12), 15
DRAW "P4,15"
DRAW "C-1 R1"
PSET (14, 14), 15
DRAW "P-1,-2"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(3, host.PaintRequests.Count);
        Assert.Contains(host.PaintRequests, request => request.X == 10 && request.Y == 10 && request.SolidFillColor == 4 && request.BorderColor == 4);
        Assert.Contains(host.PaintRequests, request => request.X == 12 && request.Y == 12 && request.SolidFillColor == 4 && request.BorderColor == 15);
        Assert.Contains(host.PaintRequests, request => request.X == 14 && request.Y == 14 && request.SolidFillColor == 255 && request.BorderColor == 254);
        Assert.Contains(host.Lines, line => line is { X1: 12, Y1: 12, X2: 13, Y2: 12, Color: 255 });
    }

    [Fact]
    public void Execute_CompiledDrawAppliesAAndTAAnglesInScreenCoordinates()
    {
        const string text = """
SCREEN 1
PSET (50, 50), 15
DRAW "A0 U10 R10"
PRINT POINT(0)
PRINT POINT(1)
PSET (50, 50), 15
DRAW "A1 U10 R10"
PRINT POINT(0)
PRINT POINT(1)
PSET (50, 50), 15
DRAW "A2 U10 R10"
PRINT POINT(0)
PRINT POINT(1)
PSET (50, 50), 15
DRAW "A3 U10 R10"
PRINT POINT(0)
PRINT POINT(1)
PSET (50, 50), 15
DRAW "TA90 U10 R10"
PRINT POINT(0)
PRINT POINT(1)
PSET (50, 50), 15
DRAW "TA-90 U10 R10"
PRINT POINT(0)
PRINT POINT(1)
PSET (50, 50), 15
DRAW "TA90 U10 TA0 U10"
PRINT POINT(0)
PRINT POINT(1)
PSET (50, 50), 15
DRAW "TA90 A0 U10"
PRINT POINT(0)
PRINT POINT(1)
PSET (50, 50), 15
DRAW "A0 TA90 U10"
PRINT POINT(0)
PRINT POINT(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["60", "40", "60", "60", "40", "60", "40", "40", "60", "60", "40", "40", "60", "40", "50", "40", "60", "50"],
            host.Output);
        Assert.Contains(host.Lines, line => line is { X1: 50, Y1: 50, X2: 60, Y2: 50, Color: 15 });
        Assert.Contains(host.Lines, line => line is { X1: 50, Y1: 50, X2: 40, Y2: 50, Color: 15 });
    }

    [Theory]
    [InlineData("DRAW \"M+9223372036854775807,+1\"", "переполнению координат")]
    [InlineData("DRAW \"S255 R9223372036854775807\"", "переполнению координат")]
    [InlineData("DRAW \"R9223372036854775807\"", "переполнению координат")]
    [InlineData("DRAW \"L9223372036854775808\"", "переполнению координат")]
    [InlineData("DRAW \"C9223372036854775808 R1\"", "слишком большой числовой цвет")]
    [InlineData("DRAW \"P2147483648\"", "выходит за диапазон INTEGER")]
    public void Execute_CompiledDrawReportsControlledDiagnosticsForOverflowAndHugeColors(string statement, string messageFragment)
    {
        var (_, _, result) = ExecuteProgram(
            """
SCREEN 1
PSET (1, 1), 15
""" + "\r\n" + statement);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains(messageFragment, diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("OverflowException", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("FormatException", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("IndexOutOfRangeException", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("ArgumentOutOfRangeException", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_CompiledDrawAcceptsWhitespaceLowercaseDefaultLengthsAndFractionalLengths()
    {
        const string text = """
SCREEN 1
DRAW ""
DRAW "   r10   d5   "
PRINT POINT(0)
PRINT POINT(1)
PSET (0, 0), 15
DRAW "R D L U"
PRINT POINT(0)
PRINT POINT(1)
PSET (0, 0), 15
DRAW "R1.5"
PRINT POINT(0)
PRINT POINT(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "5", "0", "0", "2", "0"], host.Output);
        Assert.Contains(host.Lines, line => line is { X1: 0, Y1: 0, X2: 10, Y2: 0 });
        Assert.Contains(host.Lines, line => line is { X1: 0, Y1: 0, X2: 2, Y2: 0 });
    }

    [Theory]
    [InlineData("DRAW \"R.\"", "числовой аргумент длины")]
    [InlineData("DRAW \"R-1\"", "не может быть отрицательной")]
    [InlineData("DRAW \"Q10\"", "Неизвестная команда DRAW")]
    [InlineData("DRAW \"M10\"", "Команда M требует координаты")]
    [InlineData("DRAW \"M+10,20\"", "Относительная команда M должна задавать знак у обеих координат")]
    [InlineData("DRAW \"M10,+20\"", "Относительная команда M должна задавать знак у обеих координат")]
    [InlineData("DRAW \"P\"", "Команда P требует числовой цвет заливки")]
    [InlineData("DRAW \"P4,\"", "Команда P требует числовой цвет границы")]
    [InlineData("DRAW \"P,4\"", "Команда P требует числовой цвет заливки")]
    public void Execute_CompiledDrawReportsDiagnosticsForStringEdgeCases(string statement, string messageFragment)
    {
        var (_, _, result) = ExecuteProgram("SCREEN 1\r\n" + statement);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains(messageFragment, diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_MousePressConsumesMatchingButtonOnce()
    {
        const string text = """
PRINT MOUSEPRESS(1)
PRINT MOUSEPRESS(1)
PRINT MOUSEPRESS(3)
""";

        var host = new TestRuntimeHost(mousePresses: [1, 3]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_MousePressPreservesUnmatchedButtons()
    {
        const string text = """
PRINT MOUSEPRESS(1)
PRINT MOUSEPRESS(3)
PRINT MOUSEPRESS(2)
""";

        var host = new TestRuntimeHost(mousePresses: [2, 3]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_OnMouseConsumesMatchingButtonBeforeMousePress()
    {
        const string text = """
PRINT "ready"
ON MOUSE(1), HandleMouse
DIM marker AS INTEGER = 1
PRINT MOUSEPRESS(1)

SUB HandleMouse()
    PRINT "mouse"
END SUB
""";

        var host = new TestRuntimeHost(mousePresses: [1]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["ready", "mouse", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_OnMousePassesButtonAndCurrentCoordinates()
    {
        const string text = """
ON MOUSE(3), HandleMouse
DIM marker AS INTEGER = 1

SUB HandleMouse(button AS INTEGER, x AS LONG, y AS LONG)
    PRINT button
    PRINT x
    PRINT y
END SUB
""";

        var host = new TestRuntimeHost(mouseX: 320, mouseY: 240, mousePresses: [3]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "320", "240"], host.Output);
    }

    [Fact]
    public void Execute_OnMouseHandlesReleaseAndMoveEvents()
    {
        const string text = """
ON MOUSE(1, "UP"), HandleMouse
ON MOUSE("MOVE"), HandleMouse
PRINT "ready"

SUB HandleMouse(button AS INTEGER, x AS LONG, y AS LONG)
    PRINT button
    PRINT x
    PRINT y
END SUB
""";

        var host = new TestRuntimeHost(
            mouseX: 42,
            mouseY: 24,
            mouseEvents:
            [
                new MouseInputEvent(MouseInputEventKind.Release, 1),
                new MouseInputEvent(MouseInputEventKind.Move, 0)
            ]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "42", "24", "0", "42", "24", "ready"], host.Output);
    }

    [Fact]
    public void Execute_OnMouseHandlesWheelEvents()
    {
        const string text = """
ON MOUSE("WHEEL"), HandleMouse
PRINT "ready"

SUB HandleMouse(delta AS INTEGER, x AS LONG, y AS LONG)
    PRINT delta
    PRINT x
    PRINT y
END SUB
""";

        var host = new TestRuntimeHost(
            mouseX: 50,
            mouseY: 60,
            mouseEvents: [new MouseInputEvent(MouseInputEventKind.Wheel, 120)]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["120", "50", "60", "ready"], host.Output);
    }

    [Fact]
    public void Execute_OnMouseRunsDuringEmptyDoLoop()
    {
        const string text = """
SCREEN 3
DIM handled AS INTEGER = 0
ON MOUSE("MOVE"), HandleMouse
DO WHILE handled = 0
LOOP
PRINT handled

SUB HandleMouse(button AS INTEGER, x AS LONG, y AS LONG)
    handled = 1
END SUB
""";

        var host = new TestRuntimeHost(
            mouseX: 10,
            mouseY: 20,
            mouseEvents: [new MouseInputEvent(MouseInputEventKind.Move, 0)],
            skippedMouseEventReads: 3);
        var instructionCount = 0;
        var (_, _, result) = ExecuteProgram(
            text,
            host,
            shouldStopBeforeInstruction: _ => ++instructionCount > 500);

        Assert.True(result.Succeeded);
        Assert.Equal(["1"], host.Output);
    }

    [Fact]
    public void Execute_OnMouseStopPausesAndOnResumesExistingHandler()
    {
        const string text = """
ON MOUSE(1), HandleMouse
ON MOUSE(1), STOP
DIM paused AS INTEGER = 1
PRINT MOUSEPRESS(1)
ON MOUSE(1), ON
DIM resumed AS INTEGER = 2

SUB HandleMouse()
    PRINT "mouse"
END SUB
""";

        var host = new TestRuntimeHost(mousePresses: [0, 0, 1, 1]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "mouse"], host.Output);
    }

    [Fact]
    public void Execute_OnKeyConsumesMatchingKeyBeforeInKey()
    {
        const string text = """
PRINT "ready"
ON KEY("LEFT"), HandleLeft
DIM marker AS INTEGER = 1
PRINT INKEY()

SUB HandleLeft()
    PRINT "handler"
END SUB
""";

        var host = new TestRuntimeHost(keys: ["LEFT", "A"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["ready", "handler", "A"], host.Output);
    }

    [Fact]
    public void Execute_OnKeyMatchesCanonicalAliases()
    {
        const string text = """
ON KEY("ESCAPE"), HandleEsc
DIM marker AS INTEGER = 1

SUB HandleEsc()
    PRINT "escape"
END SUB
""";

        var host = new TestRuntimeHost(keys: ["ESC"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["escape"], host.Output);
    }

    [Fact]
    public void Execute_OnKeyAnyHandlesFirstUnmatchedKey()
    {
        const string text = """
ON KEY("ANY"), HandleAny
DIM marker AS INTEGER = 1
PRINT INKEY()

SUB HandleAny()
    PRINT "any"
END SUB
""";

        var host = new TestRuntimeHost(keys: ["", "RIGHT", "A"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["any", "any", ""], host.Output);
    }

    [Fact]
    public void Execute_OnKeyExactHandlerTakesPrecedenceOverAnyHandler()
    {
        const string text = """
ON KEY("ANY"), HandleAny
ON KEY("LEFT"), HandleLeft
DIM marker AS INTEGER = 1
PRINT INKEY()

SUB HandleAny()
    PRINT "any"
END SUB

SUB HandleLeft()
    PRINT "left"
END SUB
""";

        var host = new TestRuntimeHost(keys: ["", "", "LEFT", "RIGHT", "A"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["left", "any", "any", ""], host.Output);
    }

    [Fact]
    public void Execute_OnKeyPassesNormalizedKeyTextToStringParameter()
    {
        const string text = """
ON KEY("ANY"), HandleKey
DIM marker AS INTEGER = 1

SUB HandleKey(keyName AS STRING)
    PRINT keyName
END SUB
""";

        var host = new TestRuntimeHost(keys: ["esc", "Page Up"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["ESC", "PAGEUP"], host.Output);
    }

    [Fact]
    public void Execute_OnKeyAcceptsChordSyntax()
    {
        const string text = """
ON KEY("CTRL+LEFT"), HandleKey
ON KEY("LEFT"), HandleLeft
DIM marker AS INTEGER = 1

SUB HandleKey(keyName AS STRING)
    PRINT keyName
END SUB

SUB HandleLeft()
    PRINT "left"
END SUB
""";

        var host = new TestRuntimeHost(keys: ["LEFT"], pressedKeys: ["CTRL", "LEFT"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["CTRL+LEFT"], host.Output);
    }

    [Fact]
    public void Execute_OnKeyStopPausesAndOnResumesExistingHandler()
    {
        const string text = """
ON KEY("LEFT"), HandleLeft
ON KEY("LEFT"), STOP
DIM paused AS INTEGER = 1
PRINT INKEY()
ON KEY("LEFT"), ON
DIM resumed AS INTEGER = 1

SUB HandleLeft()
    PRINT "left"
END SUB
""";

        var host = new TestRuntimeHost(keys: ["", "", "LEFT", "LEFT"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["LEFT", "left"], host.Output);
    }

    [Fact]
    public void Execute_OnKeyLeavesUnmatchedKeyForInKey()
    {
        const string text = """
ON KEY("LEFT"), HandleLeft
DIM marker AS INTEGER = 1
PRINT INKEY()

SUB HandleLeft()
    PRINT "handler"
END SUB
""";

        var host = new TestRuntimeHost(keys: ["RIGHT"]);
        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["RIGHT"], host.Output);
    }

    [Fact]
    public void Execute_CompiledPredefinedConstantsCanBeReadInExpressionsAndArrayDimensions()
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
    public void Execute_CompiledPrintWithTrailingCommaKeepsLoopOutputOnOneLine()
    {
        const string text = """
FOR i = 1 TO 10
    PRINT i,
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1 2 3 4 5 6 7 8 9 10 "], host.Output);
    }

    [Fact]
    public void Execute_StepOverAfterTrailingCommaExposesPendingOutputWithoutFlushing()
    {
        const string text = """
FOR i = 1 TO 2
    PRINT i,
NEXT
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(result.Stopped);
        Assert.Empty(host.Output);
        Assert.Equal(string.Empty, result.PendingOutput);

        var stepped = result.StopState!.StepOver();

        Assert.True(stepped.Stopped);
        Assert.Empty(host.Output);
        Assert.Equal("1 ", stepped.PendingOutput);
    }

    [Fact]
    public void Execute_CompiledPrintWithTrailingSemicolonKeepsLoopOutputOnOneLine()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i;
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["123"], host.Output);
    }

    [Fact]
    public void Execute_CompiledControlFlowProgramUsesGlobalsAndJumps()
    {
        const string text = """
DIM counter AS INTEGER = 1

IF counter < 2 THEN
    counter = counter + 10
ELSE
    counter = 0
END IF

PRINT counter
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["11"], host.Output);
    }

    [Fact]
    public void Execute_CompiledProgramReportsDivisionByZeroWithSourceLocation()
    {
        const string text = "PRINT 1 / 0";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3001", diagnostic.Code);
        Assert.Equal(1, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_ReportsInstructionSnapshotsWithSourceLocations()
    {
        const string text = """
PRINT 1
PRINT 2
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();

        var (_, host, result) = ExecuteProgram(text, instructionObserver: snapshots.Add);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2"], host.Output);
        Assert.Contains(snapshots, static snapshot =>
            snapshot.RoutineId == 0
            && snapshot.RoutineName == "<global>"
            && snapshot.Opcode is Opcode.PushConst or Opcode.PushIntegerOne
            && snapshot.SourceLocation.Line == 1
            && snapshot.CallDepth == 1);
        Assert.Contains(snapshots, static snapshot =>
            snapshot.RoutineId == 0
            && snapshot.RoutineName == "<global>"
            && snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 2
            && snapshot.CallDepth == 1);
    }

    [Fact]
    public void Execute_StopsBeforeRequestedInstruction()
    {
        const string text = """
PRINT 1
PRINT 2
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();

        var (_, host, result) = ExecuteProgram(
            text,
            instructionObserver: snapshots.Add,
            shouldStopBeforeInstruction: static snapshot =>
                snapshot.Opcode == Opcode.PushConst
                && snapshot.SourceLocation.Line == 2);

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.NotNull(result.StoppedAt);
        Assert.Same(result.StopState.Snapshot, result.StoppedAt);
        Assert.Equal(Opcode.PushConst, result.StoppedAt!.Opcode);
        Assert.Equal(2, result.StoppedAt.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);
        Assert.Contains(snapshots, static snapshot =>
            snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 2);
    }

    [Fact]
    public void Execute_StopsAtSourceLineBreakpoint()
    {
        const string text = """
PRINT 1
PRINT 2
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.NotNull(result.StoppedAt);
        Assert.Same(result.StopState.Snapshot, result.StoppedAt);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);
    }

    [Fact]
    public void Execute_StopsAtSourcePositionBreakpoint()
    {
        const string text = """
PRINT 1
PRINT 2
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        ExecuteProgram(text, instructionObserver: snapshots.Add);

        var target = snapshots.Single(snapshot =>
            snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 2);

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromPositions((target.SourceLocation.Line, target.SourceLocation.Column)));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.NotNull(result.StoppedAt);
        Assert.Same(result.StopState.Snapshot, result.StoppedAt);
        Assert.Equal(target.SourceLocation.Line, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(target.SourceLocation.Column, result.StoppedAt.SourceLocation.Column);
        Assert.Equal(["1"], host.Output);
    }

    [Fact]
    public void Execute_IgnoresSourcePositionBreakpointWithDifferentColumn()
    {
        const string text = """
PRINT 1
PRINT 2
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        ExecuteProgram(text, instructionObserver: snapshots.Add);

        var target = snapshots.Single(snapshot =>
            snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 2);

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromPositions((target.SourceLocation.Line, target.SourceLocation.Column + 1)));

        Assert.True(result.Succeeded);
        Assert.False(result.Stopped);
        Assert.Null(result.StopState);
        Assert.Null(result.StoppedAt);
        Assert.Equal(["1", "2"], host.Output);
    }

    [Fact]
    public void Execute_ResumesStoppedRunFromSameInstruction()
    {
        const string text = """
PRINT 1
PRINT 2
PRINT 3
""";

        var (_, host, result) = ExecuteProgram(
            text,
            shouldStopBeforeInstruction: static snapshot =>
                snapshot.Opcode == Opcode.PushConst
                && snapshot.SourceLocation.Line == 2);

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.Equal(["1"], host.Output);

        var resumed = result.StopState.Resume();

        Assert.True(resumed.Succeeded);
        Assert.False(resumed.Stopped);
        Assert.Null(resumed.StopState);
        Assert.False(result.StopState.CanResume);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_StepOverFromSourceLineBreakpointStopsAtNextSourceLine()
    {
        const string text = """
PRINT 1
PRINT 2
PRINT 3
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);

        var stepped = result.StopState!.StepOver();

        Assert.True(stepped.Succeeded);
        Assert.True(stepped.Stopped);
        Assert.NotNull(stepped.StopState);
        Assert.Equal(3, stepped.StoppedAt!.SourceLocation.Line);
        Assert.False(result.StopState.CanResume);
        Assert.Equal(["1", "2"], host.Output);
    }

    [Fact]
    public void Execute_StepIntoFromCallStopsInsideRoutine()
    {
        const string text = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT AddOne(4)
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(5));

        Assert.True(result.Stopped);
        Assert.Equal(5, result.StoppedAt!.SourceLocation.Line);

        var stepped = result.StopState!.StepInto();

        Assert.True(stepped.Stopped);
        Assert.Equal(2, stepped.StoppedAt!.SourceLocation.Line);
        Assert.Equal(2, stepped.StoppedAt.CallDepth);
        Assert.True(stepped.StopState!.CanStepBack);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackAfterStepIntoRoutineReturnsToCallSite()
    {
        const string text = """
SUB PrintBanner(title AS STRING)
    PRINT "sub: begin"
    PRINT title
END SUB

PrintBanner("debug sample")
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(6));

        var insideRoutine = result.StopState!.StepInto();

        Assert.True(insideRoutine.Stopped);
        Assert.Equal(2, insideRoutine.StoppedAt!.SourceLocation.Line);
        Assert.True(insideRoutine.StopState!.CanStepBack);

        var backed = insideRoutine.StopState.StepBack();

        Assert.True(backed.Stopped);
        Assert.Equal(6, backed.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackInsideRoutineAfterPrintedOutputRestoresOutputCheckpoint()
    {
        const string text = """
SUB PrintBanner(title AS STRING)
    PRINT "sub: begin"
    PRINT title
    PRINT "sub: end"
END SUB

PrintBanner("debug sample")
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(7));

        var insideRoutine = result.StopState!.StepInto();
        var afterFirstPrint = insideRoutine.StopState!.StepOver();
        var afterSecondPrint = afterFirstPrint.StopState!.StepOver();

        Assert.True(afterSecondPrint.Stopped);
        Assert.Equal(4, afterSecondPrint.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["sub: begin", "debug sample"], host.Output);
        Assert.True(afterSecondPrint.StopState!.CanStepBack);

        var backed = afterSecondPrint.StopState.StepBack();

        Assert.True(backed.Stopped);
        Assert.Equal(3, backed.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["sub: begin"], host.Output);
    }

    [Fact]
    public void Execute_StepOutFromRoutineStopsAfterReturningToCaller()
    {
        const string text = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT AddOne(4)
PRINT "done"
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(5));
        var insideRoutine = result.StopState!.StepInto();

        Assert.True(insideRoutine.Stopped);
        Assert.Equal(2, insideRoutine.StoppedAt!.CallDepth);

        var steppedOut = insideRoutine.StopState!.StepOut();

        Assert.True(steppedOut.Stopped);
        Assert.Equal(1, steppedOut.StoppedAt!.CallDepth);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackFromBreakpointRestoresPreviousInstruction()
    {
        const string text = """
DIM value AS INTEGER = 1
value = value + 1
PRINT value
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(3));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanStepBack);
        Assert.Equal(3, result.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);

        var backed = result.StopState.StepBack();

        Assert.True(backed.Succeeded);
        Assert.True(backed.Stopped);
        Assert.NotNull(backed.StopState);
        Assert.Equal(2, backed.StoppedAt!.SourceLocation.Line);
        Assert.False(result.StopState.CanResume);
        Assert.False(result.StopState.CanStepBack);
        Assert.Empty(host.Output);

        var stoppedAgain = backed.StopState!.Resume();

        Assert.True(stoppedAgain.Succeeded);
        Assert.True(stoppedAgain.Stopped);
        Assert.Equal(3, stoppedAgain.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);

        var completed = stoppedAgain.StopState!.Resume();

        Assert.True(completed.Succeeded);
        Assert.False(completed.Stopped);
        Assert.Equal(["2"], host.Output);
    }

    [Fact]
    public void Execute_StepBackKeepsEarlierSnapshotAvailable()
    {
        const string text = """
DIM value AS INTEGER = 1
value = value + 1
PRINT value
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(3));

        var firstBack = result.StopState!.StepBack();
        var secondBack = firstBack.StopState!.StepBack();

        Assert.True(secondBack.Succeeded);
        Assert.True(secondBack.Stopped);
        Assert.NotNull(secondBack.StopState);
        Assert.Equal(1, secondBack.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);
        Assert.False(secondBack.StopState.CanStepBack);
    }

    [Fact]
    public void Execute_StepBackRemainsAvailableWhenProgramStateContainsArray()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(4));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanStepBack);
        Assert.Equal(4, result.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);

        var backed = result.StopState.StepBack();

        Assert.True(backed.Succeeded);
        Assert.True(backed.Stopped);
        Assert.NotNull(backed.StopState);
        Assert.Equal(3, backed.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackFromArrayElementPrintSkipsInternalExpressionSnapshots()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
PRINT values(1)
PRINT 99
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(4));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.Equal(4, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["10"], host.Output);

        var backed = result.StopState.StepBack();

        Assert.True(backed.Succeeded);
        Assert.True(backed.Stopped);
        Assert.NotNull(backed.StopState);
        Assert.Equal(3, backed.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);
        Assert.True(backed.StopState.CanStepBack);
    }

    [Fact]
    public void Execute_StepBackAcrossArraySortReturnsToPreviousProgramLines()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 3) AS INTEGER

board(1, 1) = 4
board(1, 2) = 2
board(1, 3) = 6
board(2, 1) = 3
board(2, 2) = 1
board(2, 3) = 5

ARRAYSORT(board, COLUMN)

PRINT board(1, 1)
PRINT board(1, 2)
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(13));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.Equal(13, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["2"], host.Output);
        Assert.True(result.StopState.CanStepBack);

        var toArraySort = result.StopState.StepBack();

        Assert.True(toArraySort.Succeeded);
        Assert.True(toArraySort.Stopped);
        Assert.NotNull(toArraySort.StopState);
        Assert.Equal(12, toArraySort.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);
        Assert.True(toArraySort.StopState.CanStepBack);

        var toLastAssignment = toArraySort.StopState.StepBack();

        Assert.True(toLastAssignment.Succeeded);
        Assert.True(toLastAssignment.Stopped);
        Assert.NotNull(toLastAssignment.StopState);
        Assert.Equal(10, toLastAssignment.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackThenResumePreservesArraySortResults()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 3) AS INTEGER
board(1, 1) = 4
board(1, 2) = 2
board(1, 3) = 6
board(2, 1) = 3
board(2, 2) = 1
board(2, 3) = 5
ARRAYSORT(board, COLUMN)
PRINT board(1, 1)
PRINT board(1, 2)
PRINT board(1, 3)
PRINT board(2, 1)
PRINT board(2, 2)
PRINT board(2, 3)
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(10));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.Equal(10, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["2"], host.Output);

        var atFirstPrint = result.StopState.StepBack();
        Assert.True(atFirstPrint.Succeeded);
        Assert.True(atFirstPrint.Stopped);
        Assert.Equal(9, atFirstPrint.StoppedAt!.SourceLocation.Line);

        var atArraySort = atFirstPrint.StopState!.StepBack();
        Assert.True(atArraySort.Succeeded);
        Assert.True(atArraySort.Stopped);
        Assert.Equal(8, atArraySort.StoppedAt!.SourceLocation.Line);

        var rerunPrint = atArraySort.StopState!.Resume();
        Assert.True(rerunPrint.Succeeded);
        Assert.True(rerunPrint.Stopped);
        Assert.Equal(10, rerunPrint.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["2"], host.Output);

        var completed = rerunPrint.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.False(completed.Stopped);
        Assert.Equal(["2", "4", "6", "1", "3", "5"], host.Output);
    }

    [Fact]
    public void Execute_StepBackUnavailableAfterInput()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
PRINT value
""";
        var host = new TestRuntimeHost(inputs: ["7"]);

        var (_, _, result) = ExecuteProgram(
            text,
            host,
            breakpoints: BytecodeBreakpointSet.FromLines(3));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.False(result.StopState!.CanStepBack);
        Assert.Equal(3, result.StoppedAt!.SourceLocation.Line);
        var request = Assert.Single(host.InputRequests);
        Assert.Null(request.PromptText);
        Assert.Equal(1, request.ValueCount);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_ResumeFromSourceLineBreakpointSkipsRestOfSameLine()
    {
        const string text = """
PRINT 1
PRINT 2
PRINT 3
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);

        var resumed = result.StopState.Resume();

        Assert.True(resumed.Succeeded);
        Assert.False(resumed.Stopped);
        Assert.Null(resumed.StopState);
        Assert.False(result.StopState.CanResume);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_ResumeFromSourceLineBreakpointStopsAgainOnNextLoopIteration()
    {
        const string text = """
FOR i = 1 TO 2
    PRINT i
NEXT
PRINT 99
""";

        var (_, host, firstStop) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(firstStop.Succeeded);
        Assert.True(firstStop.Stopped);
        Assert.NotNull(firstStop.StopState);
        Assert.Equal(2, firstStop.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);

        var secondStop = firstStop.StopState!.Resume();

        Assert.True(secondStop.Succeeded);
        Assert.True(secondStop.Stopped);
        Assert.NotNull(secondStop.StopState);
        Assert.False(firstStop.StopState.CanResume);
        Assert.Equal(2, secondStop.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);

        var completed = secondStop.StopState!.Resume();

        Assert.True(completed.Succeeded);
        Assert.False(completed.Stopped);
        Assert.Null(completed.StopState);
        Assert.False(secondStop.StopState.CanResume);
        Assert.Equal(["1", "2", "99"], host.Output);
    }

    [Fact]
    public void Execute_ResumeFromSourcePositionBreakpointCanStopAgainOnSameLine()
    {
        const string text = """
PRINT 0
PRINT 2 + 3
PRINT 9
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        ExecuteProgram(text, instructionObserver: snapshots.Add);

        var targets = snapshots
            .Where(static snapshot => snapshot.SourceLocation.Line == 2)
            .GroupBy(static snapshot => (snapshot.SourceLocation.Line, snapshot.SourceLocation.Column))
            .Select(static group => group.First())
            .Take(2)
            .ToArray();

        Assert.True(
            targets.Length >= 2,
            string.Join(", ", snapshots
                .Where(static snapshot => snapshot.SourceLocation.Line == 2)
                .Select(static snapshot => $"{snapshot.Opcode}@{snapshot.SourceLocation.Line}:{snapshot.SourceLocation.Column}")));

        var breakpoints = BytecodeBreakpointSet.FromPositions(targets
            .Select(static snapshot => (snapshot.SourceLocation.Line, snapshot.SourceLocation.Column))
            .ToArray());

        var (_, host, firstStop) = ExecuteProgram(text, breakpoints: breakpoints);

        Assert.True(firstStop.Succeeded);
        Assert.True(firstStop.Stopped);
        Assert.NotNull(firstStop.StopState);
        Assert.Equal(targets[0].SourceLocation.Line, firstStop.StoppedAt!.SourceLocation.Line);
        Assert.Equal(targets[0].SourceLocation.Column, firstStop.StoppedAt.SourceLocation.Column);
        Assert.Equal(["0"], host.Output);

        var secondStop = firstStop.StopState!.Resume();

        Assert.True(secondStop.Succeeded);
        Assert.True(secondStop.Stopped);
        Assert.NotNull(secondStop.StopState);
        Assert.False(firstStop.StopState.CanResume);
        Assert.Equal(targets[1].SourceLocation.Line, secondStop.StoppedAt!.SourceLocation.Line);
        Assert.Equal(targets[1].SourceLocation.Column, secondStop.StoppedAt.SourceLocation.Column);
        Assert.Equal(["0"], host.Output);

        var completed = secondStop.StopState!.Resume();

        Assert.True(completed.Succeeded);
        Assert.False(completed.Stopped);
        Assert.Null(completed.StopState);
        Assert.False(secondStop.StopState.CanResume);
        Assert.Equal(["0", "5", "9"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRemainingArithmeticOperators()
    {
        const string text = """
DIM mask AS INTEGER = 16
DIM wide AS LONG = 64
PRINT 7 \ 2
PRINT 2 ^ 3
PRINT 1 << 3
PRINT mask >> 2
PRINT wide >> 3
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["3", "8", "8", "4", "8"], host.Output);
    }

    [Fact]
    public void Execute_CompiledIndirectFunctionValueCall()
    {
        const string text = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

DIM op AS FUNCTION(INTEGER) AS INTEGER
op = AddOne
PRINT op(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_CompiledIntegralDivisionReportsDivisionByZero()
    {
        const string text = "PRINT 7 \\ 0";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3001", diagnostic.Code);
        Assert.Equal(1, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_CompiledForLoopProgram()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_CompiledForLoopReportsStepZero()
    {
        const string text = """
FOR i = 1 TO 3 STEP 0
    PRINT i
NEXT
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3004", diagnostic.Code);
        Assert.Contains("STEP 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(21, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_CompiledLocalFunctionWithoutCaptureProgram()
    {
        const string text = """
PRINT Outer(10)

FUNCTION Outer(base AS INTEGER) AS INTEGER
    RETURN AddTwo(base)

    FUNCTION AddTwo(value AS INTEGER) AS INTEGER
        RETURN value + 2
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_CompiledLocalSubWithoutCaptureProgram()
    {
        const string text = """
DIM total AS INTEGER = 0

Demo()
PRINT total

SUB Demo()
    AddOne()
    AddOne()

    SUB AddOne()
        total = total + 1
    END SUB
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2"], host.Output);
    }

    [Fact]
    public void Execute_CompiledFixedStringProgram()
    {
        const string text = """
DIM code AS STRING * 4

PRINT "[" + code + "]"

code = "AB"
PRINT "[" + code + "]"

code = "Alphabet"
PRINT "[" + code + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[    ]", "[AB  ]", "[Alph]"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRegexArrayBuiltins()
    {
        const string text = """
DIM matches() AS STRING
DIM extracted() AS STRING
DIM parts() AS STRING
DIM emptyParts() AS STRING

matches = REGEXMATCHES("A12 b34 C56", "[a-z]\d+", "i")
extracted = REGEXEXTRACTALL("A12 b34 C56", "([a-z])(\d+)", 2, "i")
parts = REGEXSPLIT("A12,b34;C56", "[,;]")
emptyParts = REGEXSPLIT("", ",")

PRINT ARRAYLENGTH(matches)
PRINT matches(1)
PRINT matches(3)
PRINT ARRAYLENGTH(extracted)
PRINT extracted(2)
PRINT ARRAYLENGTH(parts)
PRINT parts(2)
PRINT ARRAYLENGTH(emptyParts)
PRINT LBOUND(emptyParts)
PRINT UBOUND(emptyParts)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["3", "A12", "C56", "3", "34", "3", "b34", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRegexArrayBuiltinReportsInvalidMode()
    {
        const string text = """
DIM matches() AS STRING
matches = REGEXMATCHES("abc", "a", "q")
PRINT ARRAYLENGTH(matches)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REGEXMATCHES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modes", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledRegexScalarBuiltins()
    {
        const string text = """
PRINT REGEXISMATCH("A12", "\d+")
PRINT REGEXISMATCH("A12", "^a\d+", "i")
PRINT REGEXMATCH("Order 42 and 100", "\d+")
PRINT "[" + REGEXMATCH("Order", "\d+") + "]"
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT "[" + REGEXEXTRACT("A12", "([A-Z])(\d+)", 3) + "]"
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#", 1)
PRINT REGEXREPLACE("a12 b34", "[a-z]\d+", "#", "i", 1)
PRINT REGEXREPLACE("payload 42", "payload\s+", "payload ", "i")
PRINT REGEXREPLACE("abc", "x", "y", "i")
PRINT REGEXCOUNT("one 12 two 34", "\d+")
PRINT REGEXCOUNT("A12 b34", "[a-z]\d+", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["TRUE", "TRUE", "42", "[]", "12", "[]", "one # two 34", "# b34", "payload 42", "abc", "2", "2"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRegexScalarBuiltinReportsInvalidArgument()
    {
        const string text = """
PRINT REGEXREPLACE("abc", "a", "x", -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REGEXREPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledStringArrayBuiltins()
    {
        const string text = """
DIM parts() AS STRING
DIM emptyParts() AS STRING
DIM missing() AS STRING
DIM single(5 TO 5) AS STRING

parts = SPLIT("red,green,,blue", ",")
emptyParts = SPLIT("", ",")
single(5) = "solo"

PRINT ARRAYISALLOCATED(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
PRINT ARRAYLENGTH(parts)
PRINT parts(1)
PRINT parts(3)
PRINT parts(4)
PRINT JOIN(parts, "/")
PRINT ARRAYLENGTH(emptyParts)
PRINT LBOUND(emptyParts)
PRINT UBOUND(emptyParts)
PRINT "[" + JOIN(emptyParts) + "]"
PRINT "[" + JOIN(missing) + "]"
PRINT JOIN(single)
PRINT JOIN(single, "/")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["TRUE", "1", "4", "4", "red", "", "blue", "red/green//blue", "0", "1", "0", "[]", "[]", "solo", "solo"], host.Output);
    }

    [Fact]
    public void Execute_CompiledStringArrayBuiltinReportsEmptySeparator()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("abc", "")
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("SPLIT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("separator", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledPrintUsingProgram()
    {
        const string text = """
PRINT USING "##.##"; 3.14159
PRINT USING "! & \    \"; "Hello"; "World"; "xy"
PRINT USING "##"; 12;
PRINT "tail"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal([" 3.14", "H World xy    ", "12tail"], host.Output);
    }

    [Fact]
    public void Execute_CompiledPrintUsingReportsFieldTypeMismatch()
    {
        const string text = """
PRINT USING "!"; 1
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledInputProgram()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT "Pair"; firstName, age
PRINT firstName; age + 1
""";

        var (_, host, result) = ExecuteProgram(text, new TestRuntimeHost(inputs: ["Ada, 41"]));

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["Ada42"], host.Output);
        var request = Assert.Single(host.InputRequests);
        Assert.Equal("Pair", request.PromptText);
        Assert.Equal(2, request.ValueCount);
    }

    [Fact]
    public void Execute_CompiledInputReportsConversionFailure()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
""";

        var (_, _, result) = ExecuteProgram(text, new TestRuntimeHost(inputs: ["oops"]));

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3003", diagnostic.Code);
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_CompiledArrayProgram()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20

PRINT values(1)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)

REDIM PRESERVE values(2 TO 3)
PRINT values(2)
PRINT values(3)

ERASE values
PRINT ARRAYISALLOCATED(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "TRUE", "2", "1", "2", "20", "0", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMultidimensionalArrayProgram()
    {
        const string text = """
DIM grid(,) AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
grid(1, 10) = 7
grid(2, 11) = 9
PRINT ARRAYISALLOCATED(grid)
PRINT LBOUND(grid, 1)
PRINT UBOUND(grid, 1)
PRINT LBOUND(grid, 2)
PRINT UBOUND(grid, 2)
PRINT grid(1, 10)
PRINT grid(2, 11)
REDIM PRESERVE grid(2 TO 3, 11 TO 12)
PRINT grid(2, 11)
PRINT grid(3, 12)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "10", "11", "7", "9", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayMetadataBuiltinsReportRankAndVisibleBounds()
    {
        const string text = """
DIM cube(,,) AS INTEGER
DIM bounds(,) AS INTEGER
REDIM cube(2 TO 3, 10 TO 11, -1 TO 0)
PRINT ARRAYRANK(cube)
PRINT ARRAYRANK(bounds)
bounds = ARRAYBOUNDS(cube)
PRINT LBOUND(bounds, ROW)
PRINT UBOUND(bounds, ROW)
PRINT LBOUND(bounds, COLUMN)
PRINT UBOUND(bounds, COLUMN)
PRINT bounds(1, 1)
PRINT bounds(1, 2)
PRINT bounds(2, 1)
PRINT bounds(2, 2)
PRINT bounds(3, 1)
PRINT bounds(3, 2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "2", "1", "3", "1", "2", "2", "3", "10", "11", "-1", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayBoundsReportsUnallocatedArrayState()
    {
        const string text = """
DIM grid(,) AS INTEGER
PRINT ARRAYBOUNDS(grid)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("выдел", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledPartialMultidimensionalIndexingReturnsSharedRowView()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
DIM firstOnly() AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
grid(2, 10) = 7
grid(2, 11) = 9
rowView = grid(2)
rowView(10) = 70
firstOnly = ARRAYTAKE(rowView, 1)
PRINT grid(2, 10)
PRINT rowView(11)
PRINT LBOUND(rowView)
PRINT UBOUND(rowView)
PRINT ARRAYLENGTH(rowView)
PRINT firstOnly(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["70", "9", "10", "11", "2", "70"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRangeMultidimensionalIndexingReturnsSharedViews()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
DIM block(,) AS INTEGER
REDIM grid(1 TO 3, 10 TO 14)
grid(2, 11) = 21
grid(2, 12) = 22
grid(3, 12) = 32
rowView = grid(2, 11 TO 12)
block = grid(2 TO 3, 11 TO 12)
rowView(11) = 210
block(3, 12) = 320
PRINT grid(2, 11)
PRINT rowView(12)
PRINT grid(3, 12)
PRINT LBOUND(rowView)
PRINT UBOUND(rowView)
PRINT LBOUND(block, 1)
PRINT UBOUND(block, 1)
PRINT LBOUND(block, 2)
PRINT UBOUND(block, 2)
PRINT ARRAYLENGTH(block)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["210", "22", "320", "11", "12", "2", "3", "11", "12", "4"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRedimOnArrayViewReportsBuiltinArgumentDiagnostic()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
rowView = grid(2)
REDIM rowView(1 TO 2)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REDIM", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("представления", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledEraseOnArrayViewReportsBuiltinArgumentDiagnostic()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
rowView = grid(2)
ERASE rowView
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ERASE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("представления", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledPureArrayBuiltins()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM clone() AS INTEGER
DIM slice() AS INTEGER
DIM merged() AS INTEGER
DIM reversed() AS INTEGER
DIM taken() AS INTEGER
DIM skipped() AS INTEGER
DIM uniqueValues() AS INTEGER
DIM sortedValues() AS INTEGER

source(5) = 30
source(6) = 10
source(7) = 20

clone = ARRAYCLONE(source)
clone(5) = 99
slice = ARRAYSLICE(source, 6, 2)
merged = ARRAYCONCAT(slice, slice)
reversed = ARRAYTOREVERSED(source)
taken = ARRAYTAKE(source, 2)
skipped = ARRAYSKIP(source, 1)
uniqueValues = ARRAYDISTINCT(merged)
sortedValues = ARRAYTOSORTED(source)

PRINT LBOUND(clone)
PRINT UBOUND(clone)
PRINT source(5)
PRINT clone(5)
PRINT slice(1)
PRINT slice(2)
PRINT ARRAYLENGTH(merged)
PRINT merged(3)
PRINT reversed(1)
PRINT taken(2)
PRINT skipped(1)
PRINT ARRAYLENGTH(uniqueValues)
PRINT uniqueValues(1)
PRINT uniqueValues(2)
PRINT LBOUND(sortedValues)
PRINT UBOUND(sortedValues)
PRINT sortedValues(5)
PRINT sortedValues(7)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["5", "7", "30", "99", "10", "20", "4", "10", "20", "10", "10", "2", "10", "20", "5", "7", "10", "30"],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayDistinctOnMultidimensionalArrayReturnsCompactVisibleOrderResult()
    {
        const string text = """
DIM source(1 TO 2, 10 TO 12) AS INTEGER
DIM uniqueValues() AS INTEGER
source(1, 10) = 5
source(1, 11) = 7
source(1, 12) = 5
source(2, 10) = 9
source(2, 11) = 7
source(2, 12) = 11
uniqueValues = ARRAYDISTINCT(source)
PRINT LBOUND(uniqueValues)
PRINT UBOUND(uniqueValues)
PRINT ARRAYLENGTH(uniqueValues)
PRINT uniqueValues(1)
PRINT uniqueValues(2)
PRINT uniqueValues(3)
PRINT uniqueValues(4)
PRINT ARRAYISALLOCATED(uniqueValues)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "4", "4", "5", "7", "9", "11", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayFlattenOnMultidimensionalArrayReturnsCompactVisibleOrderResult()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM flat() AS INTEGER
board(1, 10) = 3
board(1, 11) = 1
board(1, 12) = 2
board(2, 10) = 6
board(2, 11) = 4
board(2, 12) = 5
flat = ARRAYFLATTEN(board)
PRINT ARRAYISALLOCATED(flat)
PRINT LBOUND(flat)
PRINT UBOUND(flat)
PRINT flat(1)
PRINT flat(2)
PRINT flat(3)
PRINT flat(4)
PRINT flat(5)
PRINT flat(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "6", "3", "1", "2", "6", "4", "5"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayReshapeRebuildsTemplateShapeFromSortedFlat()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM flat() AS INTEGER
DIM sorted() AS INTEGER
DIM reshaped(,) AS INTEGER
board(1, 10) = 3
board(1, 11) = 1
board(1, 12) = 2
board(2, 10) = 6
board(2, 11) = 4
board(2, 12) = 5
flat = ARRAYFLATTEN(board)
sorted = ARRAYTOSORTED(flat)
reshaped = ARRAYRESHAPE(sorted, board)
PRINT LBOUND(reshaped, ROW)
PRINT UBOUND(reshaped, ROW)
PRINT LBOUND(reshaped, COLUMN)
PRINT UBOUND(reshaped, COLUMN)
PRINT reshaped(1, 10)
PRINT reshaped(1, 11)
PRINT reshaped(1, 12)
PRINT reshaped(2, 10)
PRINT reshaped(2, 11)
PRINT reshaped(2, 12)
PRINT board(1, 10)
PRINT board(2, 12)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "10", "12", "1", "2", "3", "4", "5", "6", "3", "5"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayReshapeReportsMismatchedVisibleElementCount()
    {
        const string text = """
DIM source(1 TO 3) AS INTEGER
DIM template(1 TO 2, 1 TO 2) AS INTEGER
DIM reshaped(,) AS INTEGER
reshaped = ARRAYRESHAPE(source, template)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYRESHAPE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("template", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledArrayClonePreservesUnallocatedAndEmptyStates()
    {
        const string text = """
DIM pending() AS STRING
DIM copy() AS STRING

copy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(copy)

REDIM pending(1 TO 0)
copy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(copy)
PRINT ARRAYLENGTH(copy)
PRINT LBOUND(copy)
PRINT UBOUND(copy)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayCloneCopiesMultidimensionalVisibleShape()
    {
        const string text = """
DIM source(1 TO 3, 10 TO 12) AS INTEGER
DIM copy(,) AS INTEGER
source(1, 10) = 10
source(2, 11) = 20
source(3, 12) = 30
copy = ARRAYCLONE(source(2 TO 3, 11 TO 12))
copy(2, 11) = 99
PRINT LBOUND(copy, 1)
PRINT UBOUND(copy, 1)
PRINT LBOUND(copy, 2)
PRINT UBOUND(copy, 2)
PRINT source(2, 11)
PRINT copy(2, 11)
PRINT copy(3, 12)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "3", "11", "12", "20", "99", "30"], host.Output);
    }

    [Fact]
    public void Execute_CompiledPureArrayBuiltinsReportArgumentErrors()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYTAKE(source, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledPureArrayBuiltinsReportUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 1, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()
    {
        const string text = """
DIM board(1 TO 3, 10 TO 13) AS INTEGER
DIM slice(,) AS INTEGER
DIM taken(,) AS INTEGER
DIM skipped(,) AS INTEGER
DIM leftPart(,) AS INTEGER
DIM rightPart(,) AS INTEGER
DIM merged(,) AS INTEGER

board(1, 10) = 110
board(1, 11) = 111
board(1, 12) = 112
board(1, 13) = 113
board(2, 10) = 210
board(2, 11) = 211
board(2, 12) = 212
board(2, 13) = 213
board(3, 10) = 310
board(3, 11) = 311
board(3, 12) = 312
board(3, 13) = 313

slice = ARRAYSLICE(board, 11, 2, COLUMN)
taken = ARRAYTAKE(board, 2, ROW)
skipped = ARRAYSKIP(board, 1, ROW)
leftPart = ARRAYTAKE(board, 2, COLUMN)
rightPart = ARRAYSKIP(board, 2, COLUMN)
merged = ARRAYCONCAT(leftPart, rightPart, COLUMN)

PRINT LBOUND(slice, ROW)
PRINT UBOUND(slice, ROW)
PRINT LBOUND(slice, COLUMN)
PRINT UBOUND(slice, COLUMN)
PRINT slice(2, 11)
PRINT slice(3, 12)
PRINT LBOUND(taken, ROW)
PRINT UBOUND(taken, ROW)
PRINT taken(2, 13)
PRINT LBOUND(skipped, ROW)
PRINT UBOUND(skipped, ROW)
PRINT skipped(2, 10)
PRINT LBOUND(merged, COLUMN)
PRINT UBOUND(merged, COLUMN)
PRINT merged(3, 13)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "11", "12", "211", "312", "1", "2", "213", "2", "3", "210", "10", "13", "313"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayConcatReportsIncompatibleMultidimensionalShapes()
    {
        const string text = """
DIM leftPart(1 TO 2, 10 TO 11) AS INTEGER
DIM rightPart(2 TO 3, 12 TO 13) AS INTEGER
DIM merged(,) AS INTEGER
merged = ARRAYCONCAT(leftPart, rightPart, COLUMN)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYCONCAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledArrayToReversedSupportsMultidimensionalDimension()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM byColumn(,) AS INTEGER
DIM byRow(,) AS INTEGER
board(1, 10) = 110
board(1, 11) = 111
board(1, 12) = 112
board(2, 10) = 210
board(2, 11) = 211
board(2, 12) = 212
byColumn = ARRAYTOREVERSED(board, COLUMN)
byRow = ARRAYTOREVERSED(board, ROW)
byColumn(1, 10) = 999
PRINT LBOUND(byColumn, ROW)
PRINT UBOUND(byColumn, ROW)
PRINT LBOUND(byColumn, COLUMN)
PRINT UBOUND(byColumn, COLUMN)
PRINT byColumn(1, 10)
PRINT byColumn(2, 12)
PRINT byRow(1, 10)
PRINT byRow(2, 12)
PRINT board(1, 12)
PRINT board(2, 10)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "10", "12", "999", "210", "210", "112", "112", "210"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltins()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
DIM removed AS INTEGER
DIM popped AS INTEGER

ARRAYFILL(values, 42)
PRINT values(5)
PRINT values(6)

values(5) = 3
values(6) = 1
ARRAYPUSH(values, 2)
ARRAYINSERT(values, 6, 4)
removed = ARRAYREMOVE(values, 7)
popped = ARRAYPOP(values)

PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT removed
PRINT popped
PRINT values(5)
PRINT values(6)

ARRAYREVERSE(values)
PRINT values(5)
PRINT values(6)

ARRAYSORT(values)
PRINT values(5)
PRINT values(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "42", "5", "6", "1", "2", "3", "4", "4", "3", "3", "4"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltinsAllowCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM popped AS INTEGER

ARRAYFILL(values, 11)
ARRAYREVERSE(values)
ARRAYSORT(values)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)

ARRAYPUSH(values, 77)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(1)

popped = ARRAYPOP(values)
PRINT popped
PRINT ARRAYLENGTH(values)

ARRAYINSERT(values, 1, 55)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "1", "77", "77", "0", "55"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMultidimensionalDynamicArrayBuiltinsWorkAlongExplicitDimension()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 11) AS INTEGER
DIM newColumn(1 TO 2) AS INTEGER
DIM removedColumn() AS INTEGER
board(1, 10) = 110
board(1, 11) = 111
board(2, 10) = 210
board(2, 11) = 211
newColumn(1) = 112
newColumn(2) = 212
ARRAYPUSH(board, newColumn, COLUMN)
PRINT board(1, 12)
PRINT board(2, 12)
removedColumn = ARRAYPOP(board, COLUMN)
PRINT LBOUND(removedColumn)
PRINT UBOUND(removedColumn)
PRINT removedColumn(1)
PRINT removedColumn(2)
ARRAYINSERT(board, 11, newColumn, COLUMN)
removedColumn = ARRAYREMOVE(board, 10, COLUMN)
PRINT removedColumn(1)
PRINT removedColumn(2)
PRINT LBOUND(board, COLUMN)
PRINT UBOUND(board, COLUMN)
PRINT board(1, 10)
PRINT board(1, 11)
PRINT board(2, 10)
PRINT board(2, 11)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["112", "212", "1", "2", "112", "212", "110", "210", "10", "11", "112", "111", "212", "211"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMultidimensionalArrayInsertReportsIncompatibleValueShape()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 11) AS INTEGER
DIM wrongRow(10 TO 11) AS INTEGER
ARRAYINSERT(board, 10, wrongRow, COLUMN)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledArrayReverseSupportsMultidimensionalDimension()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
board(1, 10) = 110
board(1, 11) = 111
board(1, 12) = 112
board(2, 10) = 210
board(2, 11) = 211
board(2, 12) = 212
ARRAYREVERSE(board, COLUMN)
PRINT board(1, 10)
PRINT board(1, 11)
PRINT board(1, 12)
PRINT board(2, 10)
PRINT board(2, 11)
PRINT board(2, 12)
ARRAYREVERSE(board, ROW)
PRINT board(1, 10)
PRINT board(1, 11)
PRINT board(1, 12)
PRINT board(2, 10)
PRINT board(2, 11)
PRINT board(2, 12)
PRINT LBOUND(board, ROW)
PRINT UBOUND(board, ROW)
PRINT LBOUND(board, COLUMN)
PRINT UBOUND(board, COLUMN)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["112", "111", "110", "212", "211", "210", "212", "211", "210", "112", "111", "110", "1", "2", "10", "12"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayFillMutatesMultidimensionalVisibleElements()
    {
        const string text = """
DIM values(1 TO 3, 1 TO 3) AS INTEGER
values(1, 1) = 1
values(2, 2) = 2
values(3, 3) = 3
ARRAYFILL(values(2 TO 3, 2 TO 3), 8)
PRINT values(1, 1)
PRINT values(2, 2)
PRINT values(2, 3)
PRINT values(3, 2)
PRINT values(3, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "8", "8", "8", "8"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltinsReportUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYFILL(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltinsReportEmptyPop()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYPOP(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("array", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltinsReportInvalidInsertIndex()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYINSERT(values, 2, 10)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledArraySearchBuiltins()
    {
        const string text = """
DIM values(5 TO 9) AS INTEGER
DIM matches() AS INTEGER
DIM sorted(5 TO 9) AS INTEGER

values(5) = 10
values(6) = 20
values(7) = 10
values(8) = 30
values(9) = 20

sorted(5) = 10
sorted(6) = 20
sorted(7) = 20
sorted(8) = 30
sorted(9) = 40

matches = ARRAYFINDALL(values, 20)

PRINT ARRAYCONTAINS(values, 20)
PRINT ARRAYINDEXOF(values, 10)
PRINT ARRAYLASTINDEXOF(values, 20)
PRINT ARRAYCOUNT(values, 20)
PRINT ARRAYINDEXOF(values, 99)
PRINT LBOUND(matches)
PRINT UBOUND(matches)
PRINT matches(1)
PRINT matches(2)
PRINT ARRAYBINARYSEARCH(sorted, 20)
PRINT ARRAYBINARYSEARCH(sorted, 25)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "5", "9", "2", "4", "1", "2", "6", "9", "6", "4"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArraySearchBuiltinsTreatCanonicalEmptyArrayAsNotFound()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM matches() AS INTEGER

matches = ARRAYFINDALL(values, 1)

PRINT ARRAYCONTAINS(values, 1)
PRINT ARRAYCOUNT(values, 1)
PRINT ARRAYINDEXOF(values, 1)
PRINT ARRAYLASTINDEXOF(values, 1)
PRINT ARRAYBINARYSEARCH(values, 1)
PRINT ARRAYLENGTH(matches)
PRINT LBOUND(matches)
PRINT UBOUND(matches)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "0", "0", "0", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMultidimensionalSortingGroupBuiltinsSortAlongExplicitDimension()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 3) AS INTEGER
DIM sortedByColumn(1 TO 2, 1 TO 3) AS INTEGER
board(1, 1) = 4
board(1, 2) = 2
board(1, 3) = 6
board(2, 1) = 3
board(2, 2) = 1
board(2, 3) = 5
sortedByColumn = ARRAYTOSORTED(board, COLUMN)
PRINT board(1, 1)
PRINT sortedByColumn(1, 1)
PRINT sortedByColumn(1, 2)
PRINT sortedByColumn(1, 3)
PRINT sortedByColumn(2, 1)
PRINT sortedByColumn(2, 2)
PRINT sortedByColumn(2, 3)
ARRAYSORT(board, ROW)
PRINT board(1, 1)
PRINT board(1, 2)
PRINT board(1, 3)
PRINT board(2, 1)
PRINT board(2, 2)
PRINT board(2, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["4", "2", "4", "6", "1", "3", "5", "3", "1", "5", "4", "2", "6"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMultidimensionalArrayBinarySearchBuiltinReturnsCoordinateVectorOrEmptyArray()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 4) AS INTEGER
DIM firstHit() AS INTEGER
DIM missing() AS INTEGER
board(1, 1) = 1
board(1, 2) = 4
board(1, 3) = 4
board(1, 4) = 7
board(2, 1) = 2
board(2, 2) = 4
board(2, 3) = 5
board(2, 4) = 8
firstHit = ARRAYBINARYSEARCH(board, 4, ROW)
missing = ARRAYBINARYSEARCH(board, 6, ROW)
PRINT ARRAYISALLOCATED(firstHit)
PRINT LBOUND(firstHit)
PRINT UBOUND(firstHit)
PRINT firstHit(1)
PRINT firstHit(2)
PRINT ARRAYISALLOCATED(missing)
PRINT LBOUND(missing)
PRINT UBOUND(missing)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "1", "2", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayCoordinateSearchBuiltinsReturnCoordinateVectorsAndTables()
    {
        const string text = """
DIM grid(1 TO 2, 5 TO 7) AS INTEGER
DIM cube(0 TO 1, 10 TO 11, -1 TO 0) AS INTEGER
DIM firstHit() AS INTEGER
DIM lastHit() AS INTEGER
DIM allHits(,) AS INTEGER
DIM cubeHit() AS INTEGER
DIM missing() AS INTEGER
DIM noHits(,) AS INTEGER
grid(1, 5) = 10
grid(1, 6) = 20
grid(2, 6) = 20
grid(2, 7) = 20
cube(1, 11, 0) = 7
firstHit = ARRAYINDEXOF(grid, 20)
lastHit = ARRAYLASTINDEXOF(grid, 20)
allHits = ARRAYFINDALL(grid, 20)
cubeHit = ARRAYINDEXOF(cube, 7)
missing = ARRAYINDEXOF(grid, 99)
noHits = ARRAYFINDALL(grid, 99)
PRINT ARRAYISALLOCATED(firstHit)
PRINT LBOUND(firstHit)
PRINT UBOUND(firstHit)
PRINT firstHit(1)
PRINT firstHit(2)
PRINT lastHit(1)
PRINT lastHit(2)
PRINT LBOUND(allHits, 1)
PRINT UBOUND(allHits, 1)
PRINT LBOUND(allHits, 2)
PRINT UBOUND(allHits, 2)
PRINT allHits(1, 1)
PRINT allHits(1, 2)
PRINT allHits(2, 1)
PRINT allHits(2, 2)
PRINT allHits(3, 1)
PRINT allHits(3, 2)
PRINT cubeHit(1)
PRINT cubeHit(2)
PRINT cubeHit(3)
PRINT ARRAYISALLOCATED(missing)
PRINT LBOUND(missing)
PRINT UBOUND(missing)
PRINT ARRAYISALLOCATED(noHits)
PRINT LBOUND(noHits, 1)
PRINT UBOUND(noHits, 1)
PRINT LBOUND(noHits, 2)
PRINT UBOUND(noHits, 2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "1", "6", "2", "7", "1", "3", "1", "2", "1", "6", "2", "6", "2", "7", "1", "11", "0", "TRUE", "1", "0", "TRUE", "1", "0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArraySearchBuiltinsReportUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYINDEXOF(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
    }

    [Fact]
    public void Execute_CompiledArrayContainsAndCountScanMultidimensionalVisibleElements()
    {
        const string text = """
DIM values(1 TO 3, 1 TO 3) AS INTEGER
values(1, 1) = 5
values(1, 2) = 7
values(2, 2) = 7
values(3, 3) = 7
PRINT ARRAYCONTAINS(values, 7)
PRINT ARRAYCOUNT(values, 7)
PRINT ARRAYCONTAINS(values(2 TO 3, 2 TO 3), 5)
PRINT ARRAYCOUNT(values(2 TO 3, 2 TO 3), 7)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "3", "FALSE", "2"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayCallbackBuiltinsUseFunctionValuesAndClosures()
    {
        const string text = """
SUB RunCallbacks()
DIM values(1 TO 4) AS INTEGER
DIM filtered() AS INTEGER
DIM mapped() AS STRING
DIM threshold AS INTEGER = 2
DIM prefix AS STRING = "N"
DIM bonus AS INTEGER = 1

values(1) = 1
values(2) = 2
values(3) = 3
values(4) = 4

PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, IsLarge)

filtered = ARRAYFILTER(values, IsLarge)
mapped = ARRAYMAP(filtered, FormatValue)

PRINT ARRAYLENGTH(filtered)
PRINT mapped(1)
PRINT mapped(2)
PRINT ARRAYREDUCE(values, 10, AddWithBonus)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN prefix + STR(value)
END FUNCTION

FUNCTION AddWithBonus(accumulator AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN accumulator + value + bonus
END FUNCTION
END SUB

RunCallbacks()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "2", "N3", "N4", "24"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape()
    {
        const string text = """
DIM values(2 TO 3, 4 TO 5) AS INTEGER
DIM mapped(,) AS STRING
values(2, 4) = 10
values(2, 5) = 20
values(3, 4) = 30
values(3, 5) = 40

PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values(2 TO 3, 5 TO 5), IsLarge)
mapped = ARRAYMAP(values(2 TO 3, 5 TO 5), FormatValue)
PRINT LBOUND(mapped, 1)
PRINT UBOUND(mapped, 1)
PRINT LBOUND(mapped, 2)
PRINT UBOUND(mapped, 2)
PRINT mapped(2, 5)
PRINT mapped(3, 5)
PRINT ARRAYREDUCE(values(2 TO 3, 4 TO 5), "", AppendValue)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value >= 40
END FUNCTION

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN "N" + STR(value)
END FUNCTION

FUNCTION AppendValue(accumulator AS STRING, value AS INTEGER) AS STRING
    RETURN accumulator + "[" + STR(value) + "]"
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "2", "3", "5", "5", "N20", "N40", "[10][20][30][40]"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayFilterTraversesMultidimensionalVisibleElementsIntoCompactResult()
    {
        const string text = """
DIM values(2 TO 3, 4 TO 6) AS INTEGER
DIM filtered() AS INTEGER
values(2, 4) = 10
values(2, 5) = 21
values(2, 6) = 30
values(3, 4) = 41
values(3, 5) = 50
values(3, 6) = 61
filtered = ARRAYFILTER(values(2 TO 3, 4 TO 6), IsOdd)
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)
PRINT filtered(1)
PRINT filtered(2)
PRINT filtered(3)

FUNCTION IsOdd(value AS INTEGER) AS BOOLEAN
    RETURN value MOD 2 = 1
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["1", "3", "3", "21", "41", "61"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayMapPreservesMultidimensionalEmptyVisibleShape()
    {
        const string text = """
DIM values(1 TO 2, 1 TO 2) AS INTEGER
DIM mapped(,) AS INTEGER
mapped = ARRAYMAP(values(2 TO 1, 1 TO 2), DoubleIt)
PRINT ARRAYISALLOCATED(mapped)
PRINT LBOUND(mapped, 1)
PRINT UBOUND(mapped, 1)
PRINT LBOUND(mapped, 2)
PRINT UBOUND(mapped, 2)
PRINT ARRAYLENGTH(mapped)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "2", "1", "1", "2", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayCallbackBuiltinsReportEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 1) AS INTEGER
DIM predicate AS FUNCTION(INTEGER) AS BOOLEAN
PRINT ARRAYEVERY(values, predicate)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3009", diagnostic.Code);
    }

    [Fact]
    public void Execute_CompiledArrayCallbackBuiltinsUseBuiltinFunctionValues()
    {
        const string text = """
DIM names(1 TO 3) AS STRING
DIM upper() AS STRING
DIM lengths() AS INTEGER
DIM dateTexts(1 TO 3) AS STRING
DIM validDates() AS STRING

names(1) = "ada"
names(2) = "Grace"
names(3) = "linus"

dateTexts(1) = "2026-04-24"
dateTexts(2) = "not-a-date"
dateTexts(3) = "2026-05-01"

upper = ARRAYMAP(names, UCASE)
lengths = ARRAYMAP(names, LEN)
validDates = ARRAYFILTER(dateTexts, ISDATE)

PRINT upper(1)
PRINT upper(3)
PRINT lengths(2)
PRINT ARRAYLENGTH(validDates)
PRINT validDates(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["ADA", "LINUS", "5", "2", "2026-05-01"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeFieldsNestedRecordsAndFixedStrings()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Employee
    Name AS STRING * 5
    Age AS INTEGER
    Home AS Address
END TYPE

DIM first AS Employee
DIM second AS Employee

PRINT "[" + first.Name + "]"
PRINT first.Age
first.Name = "Ada"
first.Age = 36
first.Home.City = "London"
second = first
first.Name = "Grace"
PRINT "[" + second.Name + "]"
PRINT second.Age
PRINT second.Home.City
PRINT "[" + first.Name + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[     ]", "0", "[Ada  ]", "36", "London", "[Grace]"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeArrayAndArrayFields()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Scores() AS INTEGER
    Flags(1 TO 2) AS INTEGER
END TYPE

DIM people(1 TO 1) AS Person
DIM copy AS Person

people(1).Name = "Ada"
REDIM people(1).Scores(1 TO 2)
people(1).Scores(1) = 10
people(1).Scores(2) = 20
people(1).Flags(2) = 7
copy = people(1)
people(1).Name = "Grace"
people(1).Scores(1) = 99
people(1).Flags(2) = 42
PRINT copy.Name
PRINT copy.Scores(1)
PRINT copy.Scores(2)
PRINT copy.Flags(2)
PRINT people(1).Name
PRINT people(1).Scores(1)
PRINT people(1).Flags(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "99", "20", "7", "Grace", "99", "42"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeMultidimensionalArrayFields()
    {
        const string text = """
TYPE Person
    Scores(,) AS INTEGER
    Grid(1 TO 2, 4 TO 5) AS INTEGER
END TYPE

DIM user AS Person
DIM copy AS Person

REDIM user.Scores(1 TO 2, 3 TO 4)
user.Scores(1, 3) = 10
user.Scores(2, 4) = 20
user.Grid(1, 4) = 7
user.Grid(2, 5) = 9
copy = user
user.Scores(1, 3) = 99
user.Grid(1, 4) = 42

PRINT copy.Scores(1, 3)
PRINT copy.Scores(2, 4)
PRINT copy.Grid(1, 4)
PRINT copy.Grid(2, 5)
PRINT ARRAYLENGTH(copy.Grid)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "20", "7", "9", "4"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeArrayFieldViews()
    {
        const string text = """
TYPE Board
    Cells(,) AS INTEGER
END TYPE

DIM board AS Board
DIM lineView() AS INTEGER
DIM window(,) AS INTEGER

REDIM board.Cells(1 TO 3, 1 TO 4)
board.Cells(2, 3) = 23

lineView = board.Cells(2)
PRINT lineView(3)
lineView(4) = 24
PRINT board.Cells(2, 4)

window = board.Cells(2 TO 3, 3 TO 4)
PRINT LBOUND(window, 1)
PRINT UBOUND(window, 2)
PRINT ARRAYLENGTH(window)
window(3, 3) = 99
PRINT board.Cells(3, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["23", "24", "2", "4", "4", "99"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeNamedLiteral()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING
    Age AS INTEGER
    Home AS Address
END TYPE

DIM user AS Person
DIM blank AS Person
user = Person(Name := "Ada", Age := 36, Home := Address(City := "Paris"))
blank = Person()
PRINT user.Name
PRINT user.Age
PRINT user.Home.City
PRINT LEN(blank.Name)
PRINT blank.Age
PRINT blank.Home.City = ""
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "36", "Paris", "0", "0", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypePositionalLiteral()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING
    Age AS INTEGER
    Home AS Address
END TYPE

DIM user AS Person
user = Person("Ada", 36, Address("Paris"))
PRINT user.Name
PRINT user.Age
PRINT user.Home.City
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "36", "Paris"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeFieldDefaultValues()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Ada"
    Age AS INTEGER = 36
    Active AS BOOLEAN = TRUE
END TYPE

DIM user AS Person
DIM partial AS Person
user = Person()
partial = Person(Name := "Grace")
PRINT user.Name
PRINT user.Age
PRINT user.Active
PRINT partial.Name
PRINT partial.Age
PRINT partial.Active
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "36", "TRUE", "Grace", "36", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeStructuralEquality()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING = "Ada"
    Age AS INTEGER = 36
    Home AS Address
END TYPE

DIM left AS Person
DIM right AS Person
DIM changed AS Person
left.Home.City = "Paris"
right.Home.City = "Paris"
changed.Home.City = "Rome"
PRINT left = right
PRINT left <> right
PRINT left = changed
PRINT left <> changed
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "FALSE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeLocalDefaultCanBeCaptured()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

SUB ShowDefault()
    DIM user AS Person
    PRINT "[" + ReadName() + "]"

    FUNCTION ReadName() AS STRING
        RETURN user.Name
    END FUNCTION
END SUB

ShowDefault()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[]"], host.Output);
    }

    [Fact]
    public void Execute_CompiledUnallocatedArrayAccessReportsRuntimeState()
    {
        const string text = """
DIM values() AS INTEGER
PRINT values(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_CompiledStringSliceAndTrimBuiltins()
    {
        const string text = """
PRINT LEFT("Hello", 3)
PRINT RIGHT("Hello", 3)
PRINT RIGHT("Hello", 5)
PRINT RIGHT("Hello", 1)
PRINT MID("Hello", 2, 4)
PRINT TRIM("  hello  ")
PRINT TRIM("hello")
PRINT "[" + TRIM("hello   ") + "]"
PRINT "[" + TRIM("   hello") + "]"
PRINT "[" + TRIM("   ") + "]"
PRINT LTRIM("hello")
PRINT RTRIM("hello")
PRINT LTRIM("xyxyhello", "xy")
PRINT RTRIM("helloxyxy", "xy")
PRINT REVERSE("abcd")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Hel", "llo", "Hello", "o", "ello", "hello", "hello", "[hello]", "[hello]", "[]", "hello", "hello", "hello", "hello", "dcba"], host.Output);
    }

    [Fact]
    public void Execute_CompiledStringSearchBuiltins()
    {
        const string text = """
DIM oldItems(1 TO 2) AS STRING
DIM newItems(1 TO 2) AS STRING
oldItems(1) = "ab"
newItems(1) = "X"
oldItems(2) = "abc"
newItems(2) = "Y"
PRINT CONTAINS("Hello", "ell")
PRINT CONTAINS("", "x")
PRINT CONTAINS("Hello", "Hello")
PRINT CONTAINS("Hello", "Hello!")
PRINT CONTAINS("Hello", "o")
PRINT STARTSWITH("Hello", "He")
PRINT ENDSWITH("Hello", "lo")
PRINT STARTSWITH("Hello", "")
PRINT ENDSWITH("Hello", "")
PRINT STARTSWITH("Hello", "Hello")
PRINT ENDSWITH("Hello", "Hello")
PRINT STARTSWITH("Hello", "Hello!")
PRINT ENDSWITH("Hello", "Hello!")
PRINT REPLACE("one two two", "two", "three")
PRINT REPLACEMANY("abc ab", oldItems, newItems)
PRINT INSTR("banana", "na")
PRINT INSTR(4, "banana", "na")
PRINT INSTRREV("banana", "na")
PRINT INSTRREV(4, "banana", "na")
PRINT SUBSTRCOUNT("banana", "na")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE", "one three three", "Y X", "3", "5", "5", "3", "2"], host.Output);
    }

    [Fact]
    public void Execute_CompiledStringFillAndEditBuiltins()
    {
        const string text = """
PRINT REPEAT("ha", 3)
PRINT "[" + SPACE(4) + "]"
PRINT STRING(5, "ab")
PRINT "[" + PADLEFT("7", 3) + "]"
PRINT PADLEFT("word", 4)
PRINT PADRIGHT("7", 3, ".")
PRINT PADRIGHT("word", 4, ".")
PRINT "[" + CENTER("A", 4, ".") + "]"
PRINT "[" + CENTER("Hello", 5, ".") + "]"
PRINT INSERT("abcd", 3, "-")
PRINT INSERT("", 5, "seed")
PRINT INSERT("abcd", 2, "")
PRINT REMOVE("abcdef", 2, 3)
PRINT REVERSE("A")
PRINT LCASE("hello")
PRINT UCASE("HELLO")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hahaha", "[    ]", "aaaaa", "[  7]", "word", "7..", "word", "[.A..]", "[Hello]", "ab-cd", "seed", "abcd", "aef", "A", "hello", "HELLO"], host.Output);
    }

    [Fact]
    public void Execute_CompiledReplaceUsesOrdinalCaseSensitiveRules()
    {
        const string text = """
PRINT REPLACE("one two two", "two", "three")
PRINT REPLACE("One one", "one", "two")
PRINT REPLACE("abc", "x", "y")
PRINT REPLACE("hello", "he", "he")
PRINT REPLACE("hello", "hello", "done")
PRINT REPLACE("abba", "b", "c")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["one three three", "One two", "abc", "hello", "done", "acca"], host.Output);
    }

    [Fact]
    public void Execute_CompiledReplaceManyReportsInvalidRules()
    {
        const string text = """
DIM oldItems(1 TO 2) AS STRING
DIM newItems(1 TO 1) AS STRING
oldItems(1) = "a"
oldItems(2) = "b"
newItems(1) = "x"
PRINT REPLACEMANY("abc", oldItems, newItems)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPLACEMANY", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledStringCharBuiltins()
    {
        const string text = """
PRINT ASC("A")
PRINT CHR(65)
PRINT ASC("Я")
PRINT CHR(159)
PRINT ASC("█")
PRINT CHR(219)
PRINT ASC(CHR(1071))
PRINT ASC(CHR(128512))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["65", "A", "159", "Я", "219", "█", "159", "128512"], host.Output);
    }

    [Fact]
    public void Execute_CompiledUnicodeCharBuiltins()
    {
        const string text = """
PRINT ASC("Я")
PRINT ASCW("Я")
PRINT ASC("█")
PRINT ASCW("█")
PRINT ASCW(CHR(159))
PRINT ASCW(CHRW(1071))
PRINT CHRW(1071)
PRINT ASCW(CHRW(128512))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["159", "1071", "219", "9608", "1071", "1071", "Я", "128512"], host.Output);
    }

    [Fact]
    public void Execute_CompiledNumericConversionAndRoundingBuiltins()
    {
        const string text = """
PRINT INT(3.8)
PRINT INT(-3.2)
PRINT FLOOR(3.8)
PRINT CEIL(3.2)
PRINT ROUND(2.5)
PRINT ROUND(-2.5)
PRINT FIX(-3.8)
PRINT SETSIGN(5, -1)
PRINT SETSIGN(-5, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "-4", "3", "4", "3", "-3", "-3", "-5", "5"], host.Output);
    }

    [Fact]
    public void Execute_CompiledAutomaticNumericConversions()
    {
        const string text = """
DIM seed AS INTEGER = 1
DIM amount AS DECIMAL = seed
DIM half AS DECIMAL = 0.5
DIM wide AS LONG = seed
DIM angle AS INTEGER = 0
PRINT amount + half
PRINT wide + 7
PRINT SIN(angle)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1.5", "8", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledNumericMathBuiltins()
    {
        const string text = """
PRINT SIN(0)
PRINT COS(0)
PRINT TAN(0)
PRINT ATN(0)
PRINT ATN2(0, 1)
PRINT SQR(9)
PRINT CBRT(27)
PRINT EXP(0)
PRINT LOG(1)
PRINT LOG10(100)
PRINT LOG2(8)
PRINT POW(2, 3)
PRINT ASIN(0)
PRINT ACOS(1)
PRINT ASINH(0)
PRINT ACOSH(1)
PRINT ATANH(0)
PRINT SINH(0)
PRINT COSH(0)
PRINT TANH(0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["0", "1", "0", "0", "0", "3", "3", "1", "0", "2", "3", "8", "0", "0", "0", "0", "0", "0", "1", "0"],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledRndReturnsValueInHalfOpenRange()
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

    [Fact]
    public void Execute_CompiledCurrentDateTimeBuiltinsUseHostClock()
    {
        const string text = """
PRINT DATE()
PRINT TIME()
PRINT NOW()
PRINT UTCDATE()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT TIMER()
PRINT DATE("UTC")
PRINT TIME("UTC")
PRINT NOW("UTC")
PRINT UTCOFFSET("2026-04-20 14:05:06")
PRINT TOUTC("2026-04-20 14:05:06")
PRINT FROMUTC("2026-04-20 11:05:06")
""";

        var host = new TestRuntimeHost(
            new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3)),
            CreateTestFixedTimeZone("Axiom Runtime Tests East", 3));

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "2026-04-20",
                "14:05:06",
                "2026-04-20 14:05:06",
                "2026-04-20",
                "11:05:06",
                "2026-04-20 11:05:06",
                "50706",
                "2026-04-20",
                "11:05:06",
                "2026-04-20 11:05:06",
                "180",
                "2026-04-20 11:05:06",
                "2026-04-20 14:05:06"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledDateTimeTransformBuiltins()
    {
        const string text = """
PRINT DATESERIAL(2026, 4, 20)
PRINT TIMESERIAL(14, 5, 6)
PRINT YEAR("2026-04-20")
PRINT MONTH("2026-04-20 14:05:06")
PRINT DAY("2026-04-20")
PRINT HOUR("14:05:06")
PRINT MINUTE("2026-04-20 14:05:06")
PRINT SECOND("14:05:06")
PRINT WEEKDAY("2026-04-20")
PRINT MONTHNAME(4)
PRINT WEEKDAYNAME(7)
PRINT DATEADD("m", 1, "2026-01-31")
PRINT DATEADD("s", 10, "23:59:55")
PRINT DATEDIFF("d", "2026-04-20 23:59:59", "2026-04-21 00:00:00")
PRINT DATEPART("y", "2026-04-21")
PRINT DATEVALUE("2026-04-20 14:05:06")
PRINT TIMEVALUE("2026-04-20 14:05:06")
PRINT ISDATE("2026-04-20 14:05:06")
PRINT ISDATE("20.04.2026")
PRINT ISLEAPYEAR(2024)
PRINT DAYSINMONTH(2024, 2)
PRINT STARTOFMONTH("2026-04-20 14:05:06")
PRINT ENDOFMONTH("2024-02-11")
PRINT STARTOFYEAR("2026-04-20")
PRINT ENDOFYEAR("2026-04-20 14:05:06")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "2026-04-20",
                "14:05:06",
                "2026",
                "4",
                "20",
                "14",
                "5",
                "6",
                "1",
                "April",
                "Sunday",
                "2026-02-28",
                "00:00:05",
                "1",
                "111",
                "2026-04-20",
                "14:05:06",
                "TRUE",
                "FALSE",
                "TRUE",
                "29",
                "2026-04-01",
                "2024-02-29",
                "2026-01-01",
                "2026-12-31"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledDateTimeNamedZoneBuiltinsUseRequestedZone()
    {
        const string text = """
PRINT DATE("Axiom Runtime Tests West")
PRINT TIME("Axiom Runtime Tests West")
PRINT NOW("Axiom Runtime Tests West")
PRINT UTCOFFSET(NOW("Axiom Runtime Tests West"), "Axiom Runtime Tests West")
PRINT TOUTC("2026-04-19 16:30:45", "Axiom Runtime Tests West")
PRINT FROMUTC("2026-04-19 21:30:45", "Axiom Runtime Tests West")
""";

        var westTimeZone = CreateTestFixedTimeZone("Axiom Runtime Tests West", -5);
        var host = new TestRuntimeHost(
            new DateTimeOffset(2026, 04, 20, 00, 30, 45, TimeSpan.FromHours(3)),
            CreateTestFixedTimeZone("Axiom Runtime Tests East", 3),
            CreateNamedTimeZones(westTimeZone));

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "2026-04-19",
                "16:30:45",
                "2026-04-19 16:30:45",
                "-300",
                "2026-04-19 21:30:45",
                "2026-04-19 16:30:45"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledDateTimeBuiltinsReportArgumentErrors()
    {
        const string text = """
PRINT DATESERIAL(2025, 2, 29)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("DATESERIAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("day", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledNumericBuiltinsReportArgumentErrors()
    {
        const string text = """
PRINT SQR(-1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("SQR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledStringBuiltinsReportArgumentErrors()
    {
        const string text = """
PRINT REPLACE("abc", "", "x")
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("oldText", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledChrReportsInvalidCode()
    {
        const string text = """
PRINT CHR(55296)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("CHR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledChrWReportsInvalidCode()
    {
        const string text = """
PRINT CHRW(55296)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("CHRW", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledNestedBlockLocalProgram()
    {
        const string text = """
IF TRUE THEN
    DIM message AS STRING = "yes"
    PRINT message
ELSE
    DIM message AS STRING = "no"
    PRINT message
END IF
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["yes"], host.Output);
    }

    [Fact]
    public void Execute_CompiledElseIfBlockLocalProgram()
    {
        const string text = """
DIM mode AS INTEGER = 2
IF mode = 1 THEN
    DIM branchText AS STRING = "if-mode-1"
    PRINT branchText
ELSEIF mode = 2 THEN
    DIM branchText AS STRING = "elseif-branch"
    PRINT branchText
ELSE
    DIM branchText AS STRING = "if-mode-else"
    PRINT branchText
END IF

IF FALSE THEN
    DIM branchText AS STRING = "never"
    PRINT branchText
ELSE
    DIM branchText AS STRING = "else-branch"
    PRINT branchText
END IF
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["elseif-branch", "else-branch"], host.Output);
    }

    [Fact]
    public void Execute_CompiledSelectCaseBlockLocalProgram()
    {
        const string text = """
DIM code AS INTEGER = 2
SELECT CASE code
    CASE 1
        DIM caseText AS STRING = "case-one"
        PRINT caseText
    CASE 2
        DIM caseText AS STRING = "case-two"
        PRINT caseText
    CASE ELSE
        DIM caseText AS STRING = "case-other"
        PRINT caseText
END SELECT

DIM fallbackCode AS INTEGER = 9
SELECT CASE fallbackCode
    CASE 1
        DIM caseText AS STRING = "fallback-one"
        PRINT caseText
    CASE ELSE
        DIM caseText AS STRING = "case-else"
        PRINT caseText
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["case-two", "case-else"], host.Output);
    }

    [Fact]
    public void Execute_CompiledLoopBlockLocalProgram()
    {
        const string text = """
FOR i = 1 TO 2
    DIM loopText AS STRING = "for-" + STR(i)
    PRINT loopText
NEXT

DIM counter AS INTEGER = 0
WHILE counter < 2
    DIM whileText AS STRING = "while-" + STR(counter)
    PRINT whileText
    counter = counter + 1
END WHILE

DIM probe AS INTEGER = 0
DO
    DIM doText AS STRING = "do-" + STR(probe)
    PRINT doText
    probe = probe + 1
LOOP UNTIL probe = 2
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["for-1", "for-2", "while-0", "while-1", "do-0", "do-1"], host.Output);
    }

    [Fact]
    public void Execute_CompiledSubBlockLocalProgram()
    {
        const string text = """
PrintScopeRoutine()

SUB PrintScopeRoutine()
    PRINT "sub-start"

    FOR localIndex = 1 TO 2
        DIM subLoopText AS STRING = "sub-for-" + STR(localIndex)
        PRINT subLoopText
    NEXT

    IF TRUE THEN
        DIM subBranchText AS STRING = "sub-if"
        PRINT subBranchText
    END IF

    PRINT "sub-end"
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["sub-start", "sub-for-1", "sub-for-2", "sub-if", "sub-end"], host.Output);
    }

    [Fact]
    public void Execute_CompiledFunctionBlockLocalProgram()
    {
        const string text = """
PRINT "fn=" + STR(ComputeScopedValue(3))

FUNCTION ComputeScopedValue(value AS INTEGER) AS INTEGER
    IF value > 2 THEN
        DIM bonus AS INTEGER = 4
        RETURN value + bonus
    ELSE
        DIM bonus AS INTEGER = 1
        RETURN value + bonus
    END IF
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["fn=7"], host.Output);
    }

    [Fact]
    public void Execute_CompiledFileSystemBuiltinsProgram()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var sourcePath = Path.Combine(workspace, "source.txt");
            var copyPath = Path.Combine(workspace, "copy.txt");
            var renamedPath = Path.Combine(workspace, "renamed.txt");
            var linesPath = Path.Combine(workspace, "lines.txt");
            var nestedDirectory = Path.Combine(workspace, "nested");

            File.WriteAllText(linesPath, "one\ntwo\n");

            var text = $$"""
DIM source AS STRING = "{{EscapePath(sourcePath)}}"
DIM copyPath AS STRING = "{{EscapePath(copyPath)}}"
DIM renamedPath AS STRING = "{{EscapePath(renamedPath)}}"
DIM linesPath AS STRING = "{{EscapePath(linesPath)}}"
DIM nestedDirectory AS STRING = "{{EscapePath(nestedDirectory)}}"

PRINT FILEEXISTS(source)
PRINT WRITEFILE(source, "alpha")
PRINT APPENDFILE(source, "-beta")
PRINT READFILE(source)
PRINT FILESIZE(source)
PRINT COPYFILE(source, copyPath)
PRINT FILEEXISTS(copyPath)
PRINT RENAMEFILE(copyPath, "renamed.txt")
PRINT FILENAME(renamedPath)
PRINT EXTNAME(renamedPath)
PRINT MAKEDIR(nestedDirectory)
PRINT DIREXISTS(nestedDirectory)
DIM lines() AS STRING
lines = READALLLINES(linesPath)
PRINT ARRAYLENGTH(lines)
PRINT lines(2)
DIM files() AS STRING
files = GETFILES("{{EscapePath(workspace)}}", "*.txt")
PRINT ARRAYCONTAINS(files, renamedPath)
PRINT DELETEFILE(renamedPath)
PRINT DELETEDIR(nestedDirectory)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(
                result.Succeeded,
                string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
            Assert.Equal(
                [
                    "FALSE",
                    "TRUE",
                    "TRUE",
                    "alpha-beta",
                    "10",
                    "TRUE",
                    "TRUE",
                    "TRUE",
                    "renamed.txt",
                    ".txt",
                    "TRUE",
                    "TRUE",
                    "2",
                    "two",
                    "TRUE",
                    "TRUE",
                    "TRUE"
                ],
                host.Output);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledFileSystemBuiltinReportsMissingFile()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var missingPath = Path.Combine(workspace, "missing.txt");
            var text = $$"""
PRINT FILESIZE("{{EscapePath(missingPath)}}")
""";

            var (_, _, result) = ExecuteProgram(text);

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
            Assert.Contains("FILESIZE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("не существует", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledOpenInputFileProgram()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var inputPath = Path.Combine(workspace, "input.txt");
            File.WriteAllText(inputPath, string.Join(Environment.NewLine, ["one", "two", "three"]));

            var text = $$"""
DIM f AS FILE
OPEN "{{EscapePath(inputPath)}}" FOR INPUT AS f

PRINT ISOPEN(f)
PRINT FILEMODE(f)
PRINT READLINE(f)
PRINT LINENUMBER(f)
DIM rest() AS STRING
rest = READLINES(f, 2)
PRINT ARRAYLENGTH(rest)
PRINT rest(2)
PRINT EOF(f)
CLOSE f
PRINT ISOPEN(f)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(
                result.Succeeded,
                string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
            Assert.Equal(["TRUE", "INPUT", "one", "2", "2", "three", "TRUE", "FALSE"], host.Output);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledOpenOutputAndAppendFileProgram()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var outputPath = Path.Combine(workspace, "output.txt");
            var text = $$"""
DIM f AS FILE
OPEN "{{EscapePath(outputPath)}}" FOR OUTPUT AS f

PRINT ISOPEN(f)
PRINT FILEMODE(f)
PRINT WRITELINE(f, "alpha")
PRINT WRITE(f, "beta")
CLOSE f
PRINT ISOPEN(f)

OPEN "{{EscapePath(outputPath)}}" FOR APPEND AS f
PRINT WRITELINE(f, "-gamma")
CLOSE f
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(
                result.Succeeded,
                string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
            Assert.Equal(["TRUE", "OUTPUT", "TRUE", "TRUE", "FALSE", "TRUE"], host.Output);
            Assert.Equal($"alpha{Environment.NewLine}beta-gamma{Environment.NewLine}", File.ReadAllText(outputPath));
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledOpenFileReportsClosedFileRead()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var inputPath = Path.Combine(workspace, "input.txt");
            File.WriteAllText(inputPath, "one");

            var text = $$"""
DIM f AS FILE
OPEN "{{EscapePath(inputPath)}}" FOR INPUT AS f
CLOSE f
PRINT READLINE(f)
""";

            var (_, _, result) = ExecuteProgram(text);

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
            Assert.Contains("READLINE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledLocalFunctionCaptureProgram()
    {
        const string text = """
PRINT Outer(5)

FUNCTION Outer(seed AS INTEGER) AS INTEGER
    DIM offset AS INTEGER = 3
    RETURN Inner(4)

    FUNCTION Inner(multiplier AS INTEGER) AS INTEGER
        RETURN seed * multiplier + offset
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["23"], host.Output);
    }

    [Fact]
    public void Execute_CompiledLocalSubCaptureProgram()
    {
        const string text = """
SUB Outer(stepValue AS INTEGER)
    DIM total AS INTEGER = 2
    Increase()
    Increase()
    PRINT total

    SUB Increase()
        total = total + stepValue
    END SUB
END SUB

Outer(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["8"], host.Output);
    }

    [Fact]
    public void Execute_CompiledByRefRecordParameterMutatesCallerVariable()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Age AS INTEGER
END TYPE

SUB Birthday(BYREF user AS Person)
    user.Age = user.Age + 1
END SUB

DIM user AS Person
user.Name = "Ada"
user.Age = 36
Birthday(user)
PRINT user.Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["37"], host.Output);
    }

    [Fact]
    public void Execute_CompiledByValRecordParameterKeepsCallerVariableIndependent()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Age AS INTEGER
END TYPE

SUB Rewrite(user AS Person)
    user.Name = "Changed"
    user.Age = 99
END SUB

DIM user AS Person
user.Name = "Ada"
user.Age = 36
Rewrite(user)
PRINT user.Name
PRINT user.Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "36"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeMethodsMutateSelfAndReturnValues()
    {
        const string text = """
TYPE Counter
    Value AS INTEGER

    SUB Increment(stepValue AS INTEGER)
        SELF.Value = SELF.Value + stepValue
    END SUB

    FUNCTION Current() AS INTEGER
        RETURN SELF.Value
    END FUNCTION
END TYPE

DIM counter AS Counter
counter.Increment(2)
counter.Increment(3)
PRINT counter.Current()
PRINT counter.Value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "5"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeMethodsAcceptFieldAndArrayElementReceivers()
    {
        const string text = """
TYPE Counter
    Value AS INTEGER

    SUB Increment(stepValue AS INTEGER)
        SELF.Value = SELF.Value + stepValue
    END SUB

    FUNCTION Current() AS INTEGER
        RETURN SELF.Value
    END FUNCTION
END TYPE

TYPE Holder
    Item AS Counter
END TYPE

DIM holder AS Holder
DIM counters(1 TO 2) AS Counter
holder.Item.Increment(5)
PRINT holder.Item.Current()
counters(1).Increment(7)
PRINT counters(1).Current()
holder.Item.Value = holder.Item.Current() + 3
PRINT holder.Item.Current()
counters(1).Increment(2)
PRINT counters(1).Value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "7", "8", "9"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeMethodsAcceptTemporaryReceivers()
    {
        const string text = """
TYPE Counter
    Value AS INTEGER

    SUB Increment(stepValue AS INTEGER)
        SELF.Value = SELF.Value + stepValue
    END SUB

    FUNCTION Current() AS INTEGER
        RETURN SELF.Value
    END FUNCTION
END TYPE

FUNCTION MakeCounter(value AS INTEGER) AS Counter
    RETURN Counter(value)
END FUNCTION

PRINT Counter(1).Current()
PRINT MakeCounter(2).Current()
PRINT Counter(3).Current()
Counter(4).Increment(5)
PRINT Counter(4).Current()
PRINT MakeCounter(6).Current()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "3", "4", "6"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeMethodsSupportOverloadsBySignature()
    {
        const string text = """
TYPE Counter
    Value AS INTEGER
    Label AS STRING

    SUB Set(value AS INTEGER)
        SELF.Value = value
    END SUB

    SUB Set(label AS STRING)
        SELF.Label = label
    END SUB

    SUB Set(value AS INTEGER, label AS STRING)
        SELF.Value = value
        SELF.Label = label
    END SUB
END TYPE

DIM counter AS Counter
counter.Set(2)
PRINT counter.Value
counter.Set("Alpha")
PRINT counter.Label
counter.Set(7, "Beta")
PRINT counter.Value
PRINT counter.Label
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "Alpha", "7", "Beta"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeConstructorSupportsOverloadsAndZeroFormWhileNamedLiteralStaysLiteral()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Unknown"
    Age AS INTEGER = 18

    SUB New()
        SELF.Name = "Zero"
    END SUB

    SUB New(name AS STRING)
        SELF.Name = name
        SELF.Age = SELF.Age + 1
    END SUB

    SUB New(name AS STRING, age AS INTEGER)
        SELF.Name = name
        SELF.Age = age
    END SUB
END TYPE

FUNCTION BuildPerson(name AS STRING) AS Person
    RETURN Person(name)
END FUNCTION

DIM zeroUser AS Person = Person()
DIM user AS Person = Person("Ada")
PRINT zeroUser.Name
PRINT zeroUser.Age
PRINT user.Name
PRINT user.Age
PRINT Person("Lin", 20).Age
PRINT Person(Name := "Grace").Name
PRINT Person(Name := "Grace").Age
PRINT BuildPerson("Lin").Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Zero", "18", "Ada", "19", "20", "Grace", "18", "19"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeConstructorSupportsSameArityWithDifferentTypes()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Unknown"
    Age AS INTEGER = -1

    SUB New(name AS STRING)
        SELF.Name = name
    END SUB

    SUB New(age AS INTEGER)
        SELF.Age = age
    END SUB
END TYPE

PRINT Person("Ada").Name
PRINT Person(20).Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "20"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeConstructorRunsInvariantAfterConstruction()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Unknown"
    Age AS INTEGER = 0

    SUB New(name AS STRING, age AS INTEGER)
        SELF.Name = name
        SELF.Age = age
    END SUB

    FUNCTION IsValid() AS BOOLEAN
        RETURN SELF.Name <> "" AND SELF.Age >= 0
    END FUNCTION
END TYPE

PRINT Person("Ada", 20).Name
PRINT Person("Ada", 20).Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "20"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeConstructorReportsRuntimeFailureWhenInvariantReturnsFalse()
    {
        const string text = """
TYPE Person
    Age AS INTEGER

    SUB New(age AS INTEGER)
        SELF.Age = age
    END SUB

    FUNCTION IsValid() AS BOOLEAN
        RETURN SELF.Age >= 0
    END FUNCTION
END TYPE

PRINT Person(-1).Age
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("инвариант", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static (BytecodeModule module, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(
        string text,
        TestRuntimeHost? host = null,
        Action<BytecodeVmExecutionSnapshot>? instructionObserver = null,
        Func<BytecodeVmExecutionSnapshot, bool>? shouldStopBeforeInstruction = null,
        BytecodeBreakpointSet? breakpoints = null)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var loweringResult = new BoundProgramLowerer().Lower(program);
        Assert.True(
            loweringResult.Succeeded,
            string.Join(" | ", loweringResult.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));

        var compilationResult = new BytecodeCompiler().Compile(loweringResult.Program!);
        Assert.True(
            compilationResult.Succeeded,
            string.Join(" | ", compilationResult.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));

        host ??= new TestRuntimeHost();
        var result = BytecodeVm.Execute(compilationResult.Module!, host, instructionObserver, shouldStopBeforeInstruction, breakpoints);
        return (compilationResult.Module!, host, result);
    }

    private static string CreateTemporaryWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "axiom-vm-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTemporaryWorkspace(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static string EscapePath(string path)
    {
        return path.Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    private static TimeZoneInfo CreateTestFixedTimeZone(string id, int offsetHours)
    {
        return TimeZoneInfo.CreateCustomTimeZone(
            id,
            TimeSpan.FromHours(offsetHours),
            id,
            id);
    }

    private static IReadOnlyDictionary<string, TimeZoneInfo> CreateNamedTimeZones(params TimeZoneInfo[] timeZones)
    {
        return timeZones.ToDictionary(static timeZone => timeZone.Id, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class TestRuntimeHost : IRuntimeHost, IReversibleOutputWriter
    {
        private readonly Queue<string> _inputs;
        private readonly Queue<string> _keys;
        private readonly Queue<int> _mousePresses;
        private readonly Queue<MouseInputEvent> _mouseEvents;
        private readonly HashSet<string> _pressedKeys;
        private readonly HashSet<int> _pressedMouseButtons;
        private readonly Random _random = new();
        private readonly DateTimeOffset _localNow;
        private readonly TimeZoneInfo _localTimeZone;
        private readonly IReadOnlyDictionary<string, TimeZoneInfo> _namedTimeZones;
        private readonly long _mouseX;
        private readonly long _mouseY;
        private readonly Dictionary<(long X, long Y), int> _pixels = [];
        private readonly int _skippedMouseEventReads;
        private readonly int _cancelAfterReadKeyCalls;
        private readonly int _cancelAfterCancellationPolls;
        private int _mouseEventReads;
        private int _readKeyCalls;
        private int _cancellationPolls;
        private bool _isScreenActive;
        private int _foreground = 15;
        private int _background;
        private bool _isTextBackgroundTransparent;
        private long _currentGraphicsX;
        private long _currentGraphicsY;

        public TestRuntimeHost(
            DateTimeOffset? localNow = null,
            TimeZoneInfo? localTimeZone = null,
            IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones = null,
            IEnumerable<string>? inputs = null,
            IEnumerable<string>? keys = null,
            IEnumerable<string>? pressedKeys = null,
            long mouseX = -1,
            long mouseY = -1,
            IEnumerable<int>? pressedMouseButtons = null,
            IEnumerable<int>? mousePresses = null,
            IEnumerable<MouseInputEvent>? mouseEvents = null,
            int skippedMouseEventReads = 0,
            int cancelAfterReadKeyCalls = int.MaxValue,
            int cancelAfterCancellationPolls = int.MaxValue)
        {
            _inputs = new Queue<string>(inputs ?? []);
            _keys = new Queue<string>(keys ?? []);
            _mousePresses = new Queue<int>(mousePresses ?? []);
            _mouseEvents = new Queue<MouseInputEvent>(mouseEvents ?? (mousePresses ?? []).Select(static buttonNumber => new MouseInputEvent(MouseInputEventKind.Press, buttonNumber)));
            _pressedKeys = new HashSet<string>(
                (pressedKeys ?? []).Select(KeyboardInputNormalizer.Normalize),
                StringComparer.Ordinal);
            _pressedMouseButtons = new HashSet<int>(pressedMouseButtons ?? []);
            _localNow = localNow ?? DateTimeOffset.Now;
            _localTimeZone = localTimeZone ?? TimeZoneInfo.Local;
            _namedTimeZones = namedTimeZones ?? new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);
            _mouseX = mouseX;
            _mouseY = mouseY;
            _skippedMouseEventReads = skippedMouseEventReads;
            _cancelAfterReadKeyCalls = cancelAfterReadKeyCalls;
            _cancelAfterCancellationPolls = cancelAfterCancellationPolls;
            _isScreenActive = mouseX >= 0 || mouseY >= 0 || _pressedMouseButtons.Count > 0 || _mouseEvents.Count > 0 || _mousePresses.Count > 0;
        }

        public List<string> Output { get; } = [];

        public int OutputCheckpoint => Output.Count;

        public List<InputRequest> InputRequests { get; } = [];

        public List<CircleCall> Circles { get; } = [];

        public List<LineCall> Lines { get; } = [];

        public List<GraphicsPaintRequest> PaintRequests { get; } = [];

        public string ReadInput(InputRequest request)
        {
            InputRequests.Add(request);
            return _inputs.Count > 0 ? _inputs.Dequeue() : string.Empty;
        }

        public string ReadKey()
        {
            _readKeyCalls++;
            return _keys.Count > 0 ? _keys.Dequeue() : string.Empty;
        }

        public bool IsKeyDown(string keyText) => _pressedKeys.Contains(KeyboardInputNormalizer.Normalize(keyText));

        public long GetMouseX() => _mouseX;

        public long GetMouseY() => _mouseY;

        public bool IsMouseButtonDown(int buttonNumber) => _pressedMouseButtons.Contains(buttonNumber);

        public int ReadMouseButtonPress() => _mousePresses.Count > 0 ? _mousePresses.Dequeue() : 0;

        public MouseInputEvent ReadMouseEvent()
        {
            if (_mouseEventReads++ < _skippedMouseEventReads)
            {
                return default;
            }

            return _mouseEvents.Count > 0 ? _mouseEvents.Dequeue() : default;
        }

        public bool IsScreenActive => _isScreenActive;

        public int ScreenMode => _isScreenActive ? 1 : 0;

        public int ScreenWidth => _isScreenActive ? 320 : 0;

        public int ScreenHeight => _isScreenActive ? 200 : 0;

        public int CurrentForegroundColor => _foreground;

        public int CurrentBackgroundColor => _background;

        public bool IsTextBackgroundTransparent => _isTextBackgroundTransparent;

        public long CurrentGraphicsX => _currentGraphicsX;

        public long CurrentGraphicsY => _currentGraphicsY;

        public void ActivateScreen(int mode) => _isScreenActive = mode != 0;

        public void DeactivateScreen() => _isScreenActive = false;

        public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
        {
            _foreground = foreground;
            switch (backgroundMode)
            {
                case TextBackgroundMode.Color:
                    _background = background;
                    _isTextBackgroundTransparent = false;
                    break;
                case TextBackgroundMode.Transparent:
                    _isTextBackgroundTransparent = true;
                    break;
                default:
                    break;
            }
        }

        public void SetPixel(long x, long y, int color)
        {
            _pixels[(x, y)] = color;
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public void SetGraphicsCurrentPosition(long x, long y)
        {
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public int GetPixel(long x, long y) => _pixels.TryGetValue((x, y), out var color) ? color : 0;

        public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
        {
            Lines.Add(new LineCall(x1, y1, x2, y2, color, kind));
            _currentGraphicsX = x2;
            _currentGraphicsY = y2;
        }

        public void DrawCircle(
            long x,
            long y,
            double radius,
            int color,
            bool relativeCenter,
            double? startAngle,
            double? endAngle,
            double aspect,
            bool drawStartRadius,
            bool drawEndRadius)
        {
            if (relativeCenter)
            {
                _currentGraphicsX += x;
                _currentGraphicsY += y;
            }
            else
            {
                _currentGraphicsX = x;
                _currentGraphicsY = y;
            }

            Circles.Add(new CircleCall(x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius));
        }

        public void Paint(GraphicsPaintRequest request)
        {
            PaintRequests.Add(request);
            _currentGraphicsX = request.X;
            _currentGraphicsY = request.Y;
            GraphicsFloodFill.Paint(
                request,
                GetPixel,
                SetPixel,
                static (x, y, width, height) => x >= 0 && y >= 0 && x < width && y < height,
                ScreenWidth,
                ScreenHeight);
            _currentGraphicsX = request.X;
            _currentGraphicsY = request.Y;
        }

        public void WriteGraphicsText(string text, bool newLine) => Output.Add(text);

        public void WriteOutput(string text)
        {
            Output.Add(text);
        }

        public void RestoreOutputCheckpoint(int checkpoint)
        {
            if (checkpoint < 0 || checkpoint > Output.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is outside the current output range.");
            }

            Output.RemoveRange(checkpoint, Output.Count - checkpoint);
        }

        public bool IsCancellationRequested =>
            _readKeyCalls >= _cancelAfterReadKeyCalls
            || ++_cancellationPolls >= _cancelAfterCancellationPolls;

        bool IRuntimeCancellationProvider.IsCancellationRequested => IsCancellationRequested;

        public DateTimeOffset GetLocalNow() => _localNow;

        public TimeZoneInfo GetLocalTimeZone() => _localTimeZone;

        public double NextRandomDouble() => _random.NextDouble();

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            if (string.Equals(id, "LOCAL", StringComparison.OrdinalIgnoreCase))
            {
                timeZone = _localTimeZone;
                return true;
            }

            if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
            {
                timeZone = TimeZoneInfo.Utc;
                return true;
            }

            return _namedTimeZones.TryGetValue(id, out timeZone!);
        }
    }

    private sealed record CircleCall(
        long X,
        long Y,
        double Radius,
        int Color,
        bool RelativeCenter,
        double? StartAngle,
        double? EndAngle,
        double Aspect,
        bool DrawStartRadius,
        bool DrawEndRadius);

    private sealed record LineCall(
        long X1,
        long Y1,
        long X2,
        long Y2,
        int Color,
        GraphicsLineKind Kind);
}
