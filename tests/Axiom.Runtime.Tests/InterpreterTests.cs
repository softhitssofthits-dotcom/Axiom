using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed class InterpreterTests
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
    public void Execute_EmptyPrint()
    {
        var (_, host, result) = ExecuteProgram("PRINT");

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
    }

    [Fact]
    public void Execute_PrintExpressionCanProduceEmptyString()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"\"");

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
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
    public void Execute_CircleSupportsStepArcAndAspect()
    {
        const string text = """
SCREEN 1
PSET (10, 20), 7
CIRCLE STEP (5, 6), 12, , -PI / 2, PI, 0.5
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        var circle = Assert.Single(host.Circles);
        Assert.Equal(15, circle.X);
        Assert.Equal(26, circle.Y);
        Assert.Equal(12d, circle.Radius);
        Assert.Equal(15, circle.Color);
        Assert.False(circle.RelativeCenter);
        Assert.True(circle.DrawStartRadius);
        Assert.False(circle.DrawEndRadius);
        Assert.Equal(Math.PI / 2, circle.StartAngle.GetValueOrDefault(), precision: 10);
        Assert.Equal(Math.PI, circle.EndAngle.GetValueOrDefault(), precision: 10);
        Assert.Equal(.5d, circle.Aspect);
    }

    [Fact]
    public void Execute_CircleValidationRejectsNonFiniteAngles()
    {
        Assert.False(GraphicsCircleValidation.HasValidArguments(12d, double.NaN, null, .5d));
        Assert.False(GraphicsCircleValidation.HasValidArguments(12d, null, double.PositiveInfinity, .5d));
        Assert.False(GraphicsCircleValidation.HasValidArguments(12d, double.NegativeInfinity, 0d, .5d));
        Assert.False(GraphicsCircleValidation.HasValidArguments(12d, 0d, Math.PI, 1_000_001d));
        Assert.True(GraphicsCircleValidation.HasValidArguments(12d, -Math.PI / 2, Math.PI, .5d));
    }

    [Fact]
    public void Execute_LineReportsOutOfRangeCoordinates()
    {
        var (_, _, result) = ExecuteProgram(
            """
SCREEN 1
LINE (1000000001, 0)-(0, 0), 10
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB3007");
    }

    [Fact]
    public void Execute_CircleReportsOutOfRangeCoordinatesAndAspect()
    {
        var (_, _, coordinateResult) = ExecuteProgram(
            """
SCREEN 1
CIRCLE (1000000001, 10), 4, 12
""");
        var (_, _, aspectResult) = ExecuteProgram(
            """
SCREEN 1
CIRCLE (10, 10), 4, 12, 0, PI, 1000001
""");

        Assert.False(coordinateResult.Succeeded);
        Assert.Contains(coordinateResult.Diagnostics, diagnostic => diagnostic.Code == "MB3007");
        Assert.False(aspectResult.Succeeded);
        Assert.Contains(aspectResult.Diagnostics, diagnostic => diagnostic.Code == "MB3007");
    }

    [Fact]
    public void Execute_PSetAndLineSupportStepAndPointPosition()
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
    public void Execute_ViewSupportsAbsoluteAndRelativeViewportCoordinates()
    {
        const string text = """
SCREEN 1
VIEW SCREEN (10, 10)-(20, 20), 3, 9
PSET (11, 11), 6
PRINT POINT(10, 10)
PRINT POINT(12, 12)
VIEW (1, 1)-(3, 3), 2, 8
PSET (0, 0), 7
PRINT POINT(0, 0)
PRINT POINT(0)
PRINT POINT(1)
VIEW
PRINT POINT(11, 11)
PRINT POINT(0)
PRINT POINT(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["9", "3", "7", "0", "0", "7", "11", "11"], host.Output);
        Assert.Equal(11, host.CurrentGraphicsX);
        Assert.Equal(11, host.CurrentGraphicsY);
        Assert.Equal(7, host.GetPixel(11, 11));
        Assert.Equal(8, host.GetPixel(13, 11));
    }

    [Fact]
    public void Execute_ViewPrintScrollsTextWithinGraphicsViewportAndPreservesOutsideRows()
    {
        const string text = """
SCREEN 1
VIEW PRINT 10 TO 12
LOCATE 16, 1
PRINT "status"
VIEW PRINT 10 TO 12
FOR i = 1 TO 5
    PRINT "row"; i
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal("row3", host.GetVisibleTextLine(10));
        Assert.Equal("row4", host.GetVisibleTextLine(11));
        Assert.Equal("row5", host.GetVisibleTextLine(12));
        Assert.Equal("status", host.GetVisibleTextLine(16));
    }

    [Fact]
    public void Execute_ViewPrintRejectsInvalidRowsAndMissingGraphicsScreen()
    {
        var (_, _, invalidRowsResult) = ExecuteProgram(
            """
SCREEN 1
VIEW PRINT 0 TO 20
""");

        var (_, _, missingScreenResult) = ExecuteProgram("VIEW PRINT 10 TO 12");

        Assert.False(invalidRowsResult.Succeeded);
        Assert.Contains(invalidRowsResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
        Assert.False(missingScreenResult.Succeeded);
        Assert.Contains(missingScreenResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_PaletteCallsGraphicsHostForColorAndReset()
    {
        const string text = """
SCREEN 1
PALETTE 1, 196
PALETTE
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal([(1, 196)], host.PaletteColors);
        Assert.Equal(1, host.PaletteResets);
    }

    [Fact]
    public void Execute_PaletteUsingAppliesVisibleArrayValues()
    {
        const string text = """
SCREEN 1
DIM colors(0 TO 2) AS INTEGER
colors(0) = 16
colors(1) = 196
colors(2) = 46
PALETTE USING colors
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal([(0, 16), (1, 196), (2, 46)], host.PaletteColors);
    }

    [Fact]
    public void Execute_PaletteRequiresGraphicsScreen()
    {
        var (_, _, result) = ExecuteProgram("PALETTE 1, 196");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_PaletteUsingRequiresGraphicsScreen()
    {
        const string text = """
DIM colors(0 TO 2) AS INTEGER
colors(0) = 16
colors(1) = 196
colors(2) = 46
PALETTE USING colors
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_PCopyCopiesBetweenScreenPages()
    {
        const string text = """
SCREEN 1, , 1, 0
PSET (5, 5), 12
PCOPY 1, 0
SCREEN 1, , 0, 0
PRINT POINT(5, 5)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
        Assert.Equal(12, host.GetPagePixel(0, 5, 5));
        Assert.Equal(12, host.GetPagePixel(1, 5, 5));
    }

    [Fact]
    public void Execute_PCopyRequiresGraphicsScreenAndValidPages()
    {
        var (_, _, missingScreenResult) = ExecuteProgram("PCOPY 1, 0");
        Assert.False(missingScreenResult.Succeeded);
        Assert.Contains(missingScreenResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");

        var (_, _, invalidPageResult) = ExecuteProgram("SCREEN 1\r\nPCOPY 4, 0");
        Assert.False(invalidPageResult.Succeeded);
        Assert.Contains(invalidPageResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
    }

    [Fact]
    public void Execute_ImageWidthAndHeightReturnLoadedAndEmptySizes()
    {
        const string text = """
DIM hero AS IMAGE
PRINT IMAGEWIDTH(hero)
PRINT IMAGEHEIGHT(hero)
hero = LOADIMAGE("hero")
PRINT IMAGEWIDTH(hero)
PRINT IMAGEHEIGHT(hero)
""";

        var host = new TestRuntimeHost([]);
        host.Images["hero"] = CreateTestImage(width: 3, height: 2);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "0", "3", "2"], host.Output);
    }

    [Fact]
    public void Execute_TextMetricsReturnCanvasPixelSize()
    {
        const string text = """
CANVAS 160, 90
PRINT TEXTWIDTH("HUD")
PRINT TEXTHEIGHT("HUD")
PRINT TEXTWIDTH("A" + CHR(10) + "BC")
PRINT TEXTHEIGHT("A" + CHR(10) + "BC")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["24", "12", "16", "24"], host.Output);
    }

    [Fact]
    public void Execute_DrawImageCallsGraphicsHostAndUpdatesCurrentPosition()
    {
        const string text = """
SCREEN 1
DIM hero AS IMAGE
hero = LOADIMAGE("hero")
DRAWIMAGE hero, 1, 2
DRAWIMAGE hero, 5, 6, 7, 8
""";

        var image = CreateTestImage(width: 3, height: 2);
        var host = new TestRuntimeHost([]);
        host.Images["hero"] = image;

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                new DrawImageCall(image, 1, 2, 3, 2),
                new DrawImageCall(image, 5, 6, 7, 8)
            ],
            host.DrawImages);
        Assert.Equal(5, host.CurrentGraphicsX);
        Assert.Equal(6, host.CurrentGraphicsY);
    }

    [Fact]
    public void Execute_ImageOperationsReportDocumentedDiagnostics()
    {
        var (_, _, emptyPathResult) = ExecuteProgram("PRINT LOADIMAGE(\"\")");
        Assert.False(emptyPathResult.Succeeded);
        Assert.Contains(emptyPathResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, missingImageResult) = ExecuteProgram("PRINT LOADIMAGE(\"missing\")");
        Assert.False(missingImageResult.Succeeded);
        Assert.Contains(missingImageResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        var (_, _, emptyDrawResult) = ExecuteProgram("""
SCREEN 1
DIM hero AS IMAGE
DRAWIMAGE hero, 0, 0
""");
        Assert.False(emptyDrawResult.Succeeded);
        Assert.Contains(emptyDrawResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        var invalidSizeHost = new TestRuntimeHost([]);
        invalidSizeHost.Images["hero"] = CreateTestImage(width: 3, height: 2);
        var (_, _, invalidSizeResult) = ExecuteProgram("""
SCREEN 1
DIM hero AS IMAGE
hero = LOADIMAGE("hero")
DRAWIMAGE hero, 0, 0, 0, 2
""", invalidSizeHost);
        Assert.False(invalidSizeResult.Succeeded);
        Assert.Contains(invalidSizeResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var noScreenHost = new TestRuntimeHost([]);
        noScreenHost.Images["hero"] = CreateTestImage(width: 3, height: 2);
        var (_, _, noScreenResult) = ExecuteProgram("""
DIM hero AS IMAGE
hero = LOADIMAGE("hero")
DRAWIMAGE hero, 0, 0
""", noScreenHost);
        Assert.False(noScreenResult.Succeeded);
        Assert.Contains(noScreenResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_SpriteWidthHeightAndCountReturnLoadedAndEmptySizes()
    {
        const string text = """
DIM walk AS SPRITE
PRINT SPRITEWIDTH(walk)
PRINT SPRITEHEIGHT(walk)
PRINT SPRITECOUNT(walk)
walk = LOADSPRITE("walk", 16, 24)
PRINT SPRITEWIDTH(walk)
PRINT SPRITEHEIGHT(walk)
PRINT SPRITECOUNT(walk)
""";

        var host = new TestRuntimeHost([]);
        host.Images["walk"] = CreateTestImage(width: 48, height: 48);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "0", "0", "16", "24", "6"], host.Output);
    }

    [Fact]
    public void Execute_DrawSpriteCallsGraphicsHostAndUpdatesCurrentPosition()
    {
        const string text = """
SCREEN 1
DIM walk AS SPRITE
walk = LOADSPRITE("walk", 16, 24)
DRAWSPRITE walk, 1, 10, 20
DRAWSPRITE walk, 5, 30, 40, 32, 48
""";

        var sprite = CreateTestSprite(imageWidth: 48, imageHeight: 48, frameWidth: 16, frameHeight: 24);
        var host = new TestRuntimeHost([]);
        host.Sprites["walk"] = sprite;

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                new DrawSpriteCall(sprite, 1, 10, 20, 16, 24),
                new DrawSpriteCall(sprite, 5, 30, 40, 32, 48)
            ],
            host.DrawSprites);
        Assert.Equal(30, host.CurrentGraphicsX);
        Assert.Equal(40, host.CurrentGraphicsY);
    }

    [Fact]
    public void Execute_SpriteOperationsReportDocumentedDiagnostics()
    {
        var (_, _, emptyPathResult) = ExecuteProgram("PRINT LOADSPRITE(\"\", 16, 24)");
        Assert.False(emptyPathResult.Succeeded);
        Assert.Contains(emptyPathResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, invalidFrameSizeResult) = ExecuteProgram("PRINT LOADSPRITE(\"walk\", 0, 24)");
        Assert.False(invalidFrameSizeResult.Succeeded);
        Assert.Contains(invalidFrameSizeResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, missingSpriteResult) = ExecuteProgram("PRINT LOADSPRITE(\"missing\", 16, 24)");
        Assert.False(missingSpriteResult.Succeeded);
        Assert.Contains(missingSpriteResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        var (_, _, emptyDrawResult) = ExecuteProgram("""
SCREEN 1
DIM walk AS SPRITE
DRAWSPRITE walk, 0, 0, 0
""");
        Assert.False(emptyDrawResult.Succeeded);
        Assert.Contains(emptyDrawResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        var invalidFrameHost = new TestRuntimeHost([]);
        invalidFrameHost.Sprites["walk"] = CreateTestSprite(imageWidth: 48, imageHeight: 48, frameWidth: 16, frameHeight: 24);
        var (_, _, invalidFrameResult) = ExecuteProgram("""
SCREEN 1
DIM walk AS SPRITE
walk = LOADSPRITE("walk", 16, 24)
DRAWSPRITE walk, 6, 0, 0
""", invalidFrameHost);
        Assert.False(invalidFrameResult.Succeeded);
        Assert.Contains(invalidFrameResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var emptySheetHost = new TestRuntimeHost([]);
        emptySheetHost.Images["walk"] = CreateTestImage(width: 8, height: 8);
        var (_, _, emptySheetResult) = ExecuteProgram("""
SCREEN 1
DIM walk AS SPRITE
walk = LOADSPRITE("walk", 16, 24)
DRAWSPRITE walk, 0, 0, 0
""", emptySheetHost);
        Assert.False(emptySheetResult.Succeeded);
        Assert.Contains(emptySheetResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        var invalidSizeHost = new TestRuntimeHost([]);
        invalidSizeHost.Sprites["walk"] = CreateTestSprite(imageWidth: 48, imageHeight: 48, frameWidth: 16, frameHeight: 24);
        var (_, _, invalidSizeResult) = ExecuteProgram("""
SCREEN 1
DIM walk AS SPRITE
walk = LOADSPRITE("walk", 16, 24)
DRAWSPRITE walk, 0, 0, 0, 0, 24
""", invalidSizeHost);
        Assert.False(invalidSizeResult.Succeeded);
        Assert.Contains(invalidSizeResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var noScreenHost = new TestRuntimeHost([]);
        noScreenHost.Sprites["walk"] = CreateTestSprite(imageWidth: 48, imageHeight: 48, frameWidth: 16, frameHeight: 24);
        var (_, _, noScreenResult) = ExecuteProgram("""
DIM walk AS SPRITE
walk = LOADSPRITE("walk", 16, 24)
DRAWSPRITE walk, 0, 0, 0
""", noScreenHost);
        Assert.False(noScreenResult.Succeeded);
        Assert.Contains(noScreenResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_CanvasFrameAndRectanglesCallGraphicsHost()
    {
        const string text = """
CANVAS 160, 90
BEGINFRAME
CLS RGB(18, 22, 30)
DRAWRECT 1, 2, 3, 4, RGB(255, 0, 0)
FILLRECT 5, 6, 7, 8, RGB(0, 255, 0)
PRESENT
""";

        var host = new TestRuntimeHost([]);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal([(160L, 90L)], host.CanvasActivations);
        Assert.Equal(1, host.BeginFrameCount);
        Assert.Equal([0x12161E], host.FrameClears);
        Assert.Equal([new DrawRectCall(1, 2, 3, 4, 0xFF0000)], host.DrawRects);
        Assert.Equal([new FillRectCall(5, 6, 7, 8, 0x00FF00)], host.FillRects);
        Assert.Equal(1, host.PresentFrameCount);
        Assert.Equal(5, host.CurrentGraphicsX);
        Assert.Equal(6, host.CurrentGraphicsY);
    }

    [Fact]
    public void Execute_CanvasImageAndSpriteUseModernGraphicsHost()
    {
        const string text = """
CANVAS 160, 90
DIM hero AS IMAGE
DIM walk AS SPRITE
hero = LOADIMAGE("hero")
walk = LOADSPRITE("walk", 16, 16)
DRAWIMAGE hero, 10, 20
DRAWSPRITE walk, 1, 30, 40
PRESENT
""";

        var image = CreateTestImage(width: 3, height: 2);
        var sprite = CreateTestSprite(imageWidth: 32, imageHeight: 32, frameWidth: 16, frameHeight: 16);
        var host = new TestRuntimeHost([]);
        host.Images["hero"] = image;
        host.Sprites["walk"] = sprite;

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal([new DrawImageCall(image, 10, 20, 3, 2)], host.DrawImages);
        Assert.Equal([new DrawSpriteCall(sprite, 1, 30, 40, 16, 16)], host.DrawSprites);
        Assert.Equal(1, host.PresentFrameCount);
    }

    [Fact]
    public void Execute_CanvasCameraAndModernPrimitivesCallGraphicsHost()
    {
        const string text = """
CANVAS 160, 90
CAMERA 16, 24
DRAWLINE 16, 24, 28, 24, RGB(255, 255, 0)
DRAWCIRCLE 32, 40, 6, RGB(255, 0, 0)
FILLCIRCLE 52, 40, 4, RGB(0, 255, 0)
RESETCAMERA
DRAWTEXT "Очки: 12", 8, 6, RGB(255, 255, 255)
PRESENT
""";

        var host = new TestRuntimeHost([]);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal([new CanvasCameraCall(16, 24)], host.CanvasCameraCalls);
        Assert.Equal([new CanvasLineCall(16, 24, 28, 24, 0xFFFF00)], host.CanvasDrawLines);
        Assert.Equal([new CanvasCircleCall(32, 40, 6, 0xFF0000)], host.CanvasDrawCircles);
        Assert.Equal([new CanvasCircleCall(52, 40, 4, 0x00FF00)], host.CanvasFillCircles);
        Assert.Equal(1, host.ResetCanvasCameraCount);
        Assert.Equal([new CanvasTextCall("Очки: 12", 8, 6, 0xFFFFFF)], host.CanvasDrawTexts);
        Assert.Equal(1, host.PresentFrameCount);
        Assert.Equal(8, host.CurrentGraphicsX);
        Assert.Equal(6, host.CurrentGraphicsY);
    }

    [Fact]
    public void Execute_CanvasOperationsReportDocumentedDiagnostics()
    {
        var (_, _, invalidCanvasResult) = ExecuteProgram("CANVAS 0, 90");
        Assert.False(invalidCanvasResult.Succeeded);
        Assert.Contains(invalidCanvasResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, invalidRgbResult) = ExecuteProgram("PRINT RGB(256, 0, 0)");
        Assert.False(invalidRgbResult.Succeeded);
        Assert.Contains(invalidRgbResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, missingTextWidthCanvasResult) = ExecuteProgram("PRINT TEXTWIDTH(\"HUD\")");
        Assert.False(missingTextWidthCanvasResult.Succeeded);
        Assert.Contains(missingTextWidthCanvasResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");

        var (_, _, missingTextHeightCanvasResult) = ExecuteProgram("PRINT TEXTHEIGHT(\"HUD\")");
        Assert.False(missingTextHeightCanvasResult.Succeeded);
        Assert.Contains(missingTextHeightCanvasResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");

        var (_, _, missingCanvasResult) = ExecuteProgram("DRAWRECT 1, 2, 3, 4, RGB(255, 0, 0)");
        Assert.False(missingCanvasResult.Succeeded);
        Assert.Contains(missingCanvasResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");

        var (_, _, invalidRectangleResult) = ExecuteProgram("""
CANVAS 160, 90
FILLRECT 1, 2, 0, 4, RGB(255, 0, 0)
""");
        Assert.False(invalidRectangleResult.Succeeded);
        Assert.Contains(invalidRectangleResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, missingCameraResult) = ExecuteProgram("CAMERA 1, 2");
        Assert.False(missingCameraResult.Succeeded);
        Assert.Contains(missingCameraResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");

        var (_, _, invalidCircleResult) = ExecuteProgram("""
CANVAS 160, 90
FILLCIRCLE 10, 10, -1, RGB(0, 255, 0)
""");
        Assert.False(invalidCircleResult.Succeeded);
        Assert.Contains(invalidCircleResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, invalidTextResult) = ExecuteProgram("""
CANVAS 160, 90
DRAWTEXT "x", 1000000001, 0, RGB(255, 255, 255)
""");
        Assert.False(invalidTextResult.Succeeded);
        Assert.Contains(invalidTextResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
    }

    [Fact]
    public void Execute_ViewportRectangleClipsModernDrawingAndPreservesCamera()
    {
        const string text = """
CANVAS 160, 90
VIEWPORT (10, 12)-(30, 28)
CAMERA 5, 7
DRAWLINE 5, 7, 40, 40, RGB(255, 255, 0)
RESETCAMERA
PRESENT
""";

        var host = new TestRuntimeHost([]);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.True(host.ViewportEnabled);
        Assert.Equal(10, host.ViewportLeft);
        Assert.Equal(12, host.ViewportTop);
        Assert.Equal(29, host.ViewportRight);
        Assert.Equal(27, host.ViewportBottom);
        Assert.Equal([new CanvasCameraCall(5, 7)], host.CanvasCameraCalls);
        Assert.Equal(1, host.ResetCanvasCameraCount);
        Assert.Equal([new CanvasLineCall(5, 7, 40, 40, 0xFFFF00)], host.CanvasDrawLines);
    }

    [Fact]
    public void Execute_ViewportResetRestoresFullCanvas()
    {
        const string text = """
CANVAS 160, 90
VIEWPORT (10, 12)-(30, 28)
VIEWPORT
PRESENT
""";

        var host = new TestRuntimeHost([]);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.False(host.ViewportEnabled);
        Assert.Equal(0, host.ViewportLeft);
        Assert.Equal(0, host.ViewportTop);
        Assert.Equal(159, host.ViewportRight);
        Assert.Equal(89, host.ViewportBottom);
    }

    [Fact]
    public void Execute_ViewportWithoutCanvasReportsRuntimeDiagnostic()
    {
        var (_, _, result) = ExecuteProgram("VIEWPORT (1, 2)-(10, 20)");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_ViewportRejectsEmptyRectangleAfterCanvasClipping()
    {
        var (_, _, result) = ExecuteProgram("""
CANVAS 160, 90
VIEWPORT (200, 10)-(210, 20)
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
    }

    [Fact]
    public void Execute_WindowSupportsDrawPointSelectorsAndViewRebase()
    {
        const string text = """
SCREEN 1
VIEW SCREEN (10, 10)-(20, 20)
WINDOW SCREEN (100, 200)-(110, 210)
PSET (104, 205), 6
DRAW "R1"
PRINT "draw pixel="; POINT(105, 205)
PRINT "screen pos="; POINT(0); ","; POINT(1)
PRINT "screen logical="; POINT(2); ","; POINT(3)
WINDOW
PRINT "reset pos="; POINT(0); ","; POINT(1); " logical="; POINT(2); ","; POINT(3)
VIEW (2, 3)-(8, 9)
PRINT "view pos="; POINT(0); ","; POINT(1); " logical="; POINT(2); ","; POINT(3)
WINDOW (100, 210)-(110, 200)
PSET (104, 205), 7
PRINT "cart pixel="; POINT(104, 205)
PRINT "cart pos="; POINT(0); ","; POINT(1)
PRINT "cart logical="; POINT(2); ","; POINT(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "draw pixel=15",
                "screen pos=15,15",
                "screen logical=105,205",
                "reset pos=15,15 logical=15,15",
                "view pos=3,2 logical=3,2",
                "cart pixel=7",
                "cart pos=2,3",
                "cart logical=104,205"
            ],
            host.Output);
        Assert.Equal(104, host.CurrentGraphicsX);
        Assert.Equal(205, host.CurrentGraphicsY);
        Assert.Equal(15, host.GetAbsolutePixel(15, 15));
        Assert.Equal(7, host.GetAbsolutePixel(14, 16));
    }

    [Fact]
    public void Execute_PMapUsesWindowAndViewportCoordinatesAndPreservesFractions()
    {
        const string text = """
SCREEN 1
VIEW SCREEN (10, 10)-(20, 20)
WINDOW SCREEN (100, 200)-(110, 210)
PRINT "abs="; PMAP(104, 0); ","; PMAP(205, 1)
PRINT "back="; PMAP(14, 2); ","; PMAP(15, 3)
VIEW (2, 3)-(8, 9)
WINDOW
PRINT "view only="; PMAP(3, 0); ","; PMAP(2, 1); " back="; PMAP(3, 2); ","; PMAP(2, 3)
WINDOW SCREEN (100, 200)-(110, 210)
PRINT "frac="; PMAP(102.5, 0); ","; PMAP(1.5, 2)
WINDOW (100, 210)-(110, 200)
PRINT "cart="; PMAP(105, 0); ","; PMAP(205, 1)
PRINT "cart back="; PMAP(3, 2); ","; PMAP(3, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "abs=14,15",
                "back=104,205",
                "view only=3,2 back=3,2",
                "frac=1.5,102.5",
                "cart=3,3",
                "cart back=105,205"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_PMapRejectsSelectorOutsideRange()
    {
        const string text = """
SCREEN 1
PRINT PMAP(1, 4)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PMAP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("selector", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_GetAndPutSupportWindowCoordinatesAndPreservePosition()
    {
        const string text = """
SCREEN 1
VIEW SCREEN (10, 10)-(30, 30)
WINDOW SCREEN (100, 200)-(120, 220)
DIM buffer(1 TO 2, 5 TO 6) AS INTEGER
PSET (104, 205), 6
PSET (105, 205), 9
PSET (104, 206), 12
PSET (105, 206), 15
PRINT POINT(0)
PRINT POINT(1)
GET (105, 206)-(104, 205), buffer
PRINT POINT(0)
PRINT POINT(1)
LINE (104, 205)-(105, 206), 0, BF
PUT (104, 205), buffer
PRINT POINT(0)
PRINT POINT(1)
PRINT buffer(1, 5)
PRINT buffer(2, 5)
PRINT buffer(1, 6)
PRINT buffer(2, 6)
PRINT POINT(104, 205)
PRINT POINT(105, 205)
PRINT POINT(104, 206)
PRINT POINT(105, 206)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["15", "16", "15", "16", "15", "16", "6", "9", "12", "15", "6", "9", "12", "15"], host.Output);
        Assert.Equal(6, host.GetAbsolutePixel(14, 15));
        Assert.Equal(9, host.GetAbsolutePixel(15, 15));
        Assert.Equal(12, host.GetAbsolutePixel(14, 16));
        Assert.Equal(15, host.GetAbsolutePixel(15, 16));
    }

    [Fact]
    public void Execute_PutSupportsRelativePresetAndNormalizesBufferValues()
    {
        const string text = """
SCREEN 1
DIM buffer(1 TO 2, 1 TO 1) AS DOUBLE
buffer(1, 1) = 1.9
buffer(2, 1) = -2.2
PUT (10, 10), buffer
PRINT POINT(10, 10)
PRINT POINT(11, 10)
PSET (12, 10), 0
PUT STEP (2, 0), buffer, PRESET
PRINT POINT(14, 10)
PRINT POINT(15, 10)
PRINT POINT(0)
PRINT POINT(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "254", "253", "12", "10"], host.Output);
        Assert.Equal(1, host.GetAbsolutePixel(10, 10));
        Assert.Equal(2, host.GetAbsolutePixel(11, 10));
        Assert.Equal(254, host.GetAbsolutePixel(14, 10));
        Assert.Equal(253, host.GetAbsolutePixel(15, 10));
    }

    [Fact]
    public void Execute_GetRejectsBufferShapeMismatch()
    {
        const string text = """
SCREEN 1
DIM buffer(1 TO 3, 1 TO 1) AS INTEGER
GET (10, 10)-(11, 11), buffer
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("GET", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DrawWithoutScreenReportsRuntimeDiagnostic()
    {
        var (_, _, result) = ExecuteProgram("DRAW \"R10\"");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_DrawSupportsMovementModifiersColorScaleAngleMoveAndPaint()
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
LINE (40, 40)-(45, 45), 15, B
DRAW "BM44,44 P4,15"
PRINT POINT(0)
PRINT POINT(1)
PRINT POINT(41, 41)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.True(host.Lines.Count >= 9);
        Assert.Contains(host.Lines, line => line is { X1: 0, Y1: 0, X2: 10, Y2: 0, Color: 6 });
        Assert.Contains(host.Lines, line => line is { X1: 15, Y1: 10, X2: 25, Y2: 10, Color: 12 });
        Assert.Contains(host.Lines, line => line.Color == 12 && line.X2 > line.X1);
        Assert.Contains(host.Lines, line => line.Color == 12 && line.Y2 == line.Y1);
        Assert.Contains(host.PaintRequests, request => request.X == 44 && request.Y == 44 && request.SolidFillColor == 4 && request.BorderColor == 15);
        Assert.Equal(["44", "44", "4"], host.Output);
    }

    [Theory]
    [InlineData("DRAW \"Q10\"")]
    [InlineData("DRAW \"R-5\"")]
    [InlineData("DRAW \"M10\"")]
    [InlineData("DRAW \"Xdemo\"")]
    public void Execute_DrawReportsRuntimeDiagnosticsForInvalidCommands(string statement)
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
    public void Execute_DrawRejectsMovementModifiersBeforeNonMovementCommands(string statement)
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
    public void Execute_DrawRejectsDanglingMovementModifiers(string statement)
    {
        var (_, _, result) = ExecuteProgram("SCREEN 1\r\n" + statement);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("должны предшествовать команде движения или M", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_DrawReportsUnsupportedHistoricalX()
    {
        var (_, _, result) = ExecuteProgram("SCREEN 1\r\nDRAW \"Xdemo\"");

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("X/VARPTR$", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_DrawTracksCurrentGraphicsPositionAcrossMovementModes()
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
    public void Execute_DrawUsesPaintDefaultBorderAndNormalizesColors()
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
        Assert.Contains(host.PaintRequests, request => request.X == 14 && request.Y == 14 && request.SolidFillColor == 1 && request.BorderColor == 2);
        Assert.Contains(host.Lines, line => line is { X1: 12, Y1: 12, X2: 13, Y2: 12, Color: 1 });
    }

    [Fact]
    public void Execute_DrawAppliesAAndTAAnglesInScreenCoordinates()
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
    public void Execute_DrawReportsControlledDiagnosticsForOverflowAndHugeColors(string statement, string messageFragment)
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
    public void Execute_DrawAcceptsWhitespaceLowercaseDefaultLengthsAndFractionalLengths()
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
    public void Execute_DrawReportsDiagnosticsForStringEdgeCases(string statement, string messageFragment)
    {
        var (_, _, result) = ExecuteProgram("SCREEN 1\r\n" + statement);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains(messageFragment, diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_PaintFillsBoundedRegion()
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

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(6, host.GetPixel(3, 3));
        Assert.Equal(9, host.GetPixel(2, 2));
        Assert.Equal(["6", "9"], host.Output);
    }

    [Fact]
    public void Execute_PaintStepUsesCurrentPositionAndBackgroundColorByDefault()
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
    public void Execute_PaintPatternAndBackgroundPatternRepaintRegion()
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
    public void Execute_ColorSupportsOpaqueTransparentAndUnchangedBackgroundModes()
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

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(13, host.CurrentForegroundColor);
        Assert.Equal(2, host.CurrentBackgroundColor);
        Assert.True(host.IsTextBackgroundTransparent);
    }

    [Fact]
    public void Execute_MousePressConsumesMatchingButtonOnce()
    {
        const string text = """
PRINT MOUSEPRESS(1)
PRINT MOUSEPRESS(1)
PRINT MOUSEPRESS(3)
""";

        var (_, host, result) = ExecuteProgramWithMouseState(text, mousePresses: [1, 3]);

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

        var (_, host, result) = ExecuteProgramWithMouseState(text, mousePresses: [2, 3]);

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

        var (_, host, result) = ExecuteProgramWithMouseState(text, mousePresses: [1]);

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

        var (_, host, result) = ExecuteProgramWithMouseState(text, mouseX: 320, mouseY: 240, mousePresses: [3]);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "320", "240"], host.Output);
    }

    [Fact]
    public void Execute_OnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen()
    {
        const string text = """
SCREEN 1
DIM handled AS BOOLEAN = FALSE
DIM eventX AS LONG = 0
DIM eventY AS LONG = 0
DIM readX AS LONG = 0
DIM readY AS LONG = 0
ON MOUSE("MOVE"), HandleMouse
DIM marker AS INTEGER = 1
SCREEN 0
PRINT handled
PRINT eventX = readX
PRINT eventY = readY
PRINT eventX
PRINT readX
PRINT eventY
PRINT readY

SUB HandleMouse(button AS INTEGER, x AS LONG, y AS LONG)
    handled = TRUE
    eventX = x
    eventY = y
    readX = MOUSEX()
    readY = MOUSEY()
END SUB
""";

        var (_, host, result) = ExecuteProgramWithMouseState(
            text,
            mouseX: 42,
            mouseY: 24,
            mouseEvents: [new MouseInputEvent(MouseInputEventKind.Move, 0)]);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "42", "42", "24", "24"], host.Output);
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

        var (_, host, result) = ExecuteProgramWithMouseState(
            text,
            mouseX: 42,
            mouseY: 24,
            mouseEvents:
            [
                new MouseInputEvent(MouseInputEventKind.Release, 1),
                new MouseInputEvent(MouseInputEventKind.Move, 0)
            ]);

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

        var (_, host, result) = ExecuteProgramWithMouseState(
            text,
            mouseX: 50,
            mouseY: 60,
            mouseEvents: [new MouseInputEvent(MouseInputEventKind.Wheel, -120)]);

        Assert.True(result.Succeeded);
        Assert.Equal(["-120", "50", "60", "ready"], host.Output);
    }

    [Fact]
    public void Execute_MousePressPreservesReleaseAndMoveEventsForOnMouse()
    {
        const string text = """
ON MOUSE(1, "UP"), HandleMouse
ON MOUSE("MOVE"), HandleMouse
PRINT MOUSEPRESS(1)
PRINT "done"

SUB HandleMouse(button AS INTEGER, x AS LONG, y AS LONG)
    PRINT "event"
    PRINT button
END SUB
""";

        var (_, host, result) = ExecuteProgramWithMouseState(
            text,
            mouseEvents:
            [
                new MouseInputEvent(MouseInputEventKind.Release, 1),
                new MouseInputEvent(MouseInputEventKind.Move, 0),
                new MouseInputEvent(MouseInputEventKind.Press, 1)
            ]);

        Assert.True(result.Succeeded);
        Assert.Equal(["event", "1", "event", "0", "TRUE", "done"], host.Output);
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

        var (_, host, result) = ExecuteProgramWithMouseState(text, mousePresses: [0, 0, 1, 1]);

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

        var (_, host, result) = ExecuteProgramWithKeys(text, "LEFT", "A");

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

        var (_, host, result) = ExecuteProgramWithKeys(text, "ESC");

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

        var (_, host, result) = ExecuteProgramWithKeys(text, "", "RIGHT", "A");

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

        var (_, host, result) = ExecuteProgramWithKeys(text, "", "", "LEFT", "RIGHT", "A");

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

        var (_, host, result) = ExecuteProgramWithKeys(text, "esc", "Page Up");

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

        var (_, host, result) = ExecuteProgramWithKeysAndPressedKeys(text, ["LEFT"], ["CTRL", "LEFT"]);

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

        var (_, host, result) = ExecuteProgramWithKeys(text, "", "", "LEFT", "LEFT");

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

        var (_, host, result) = ExecuteProgramWithKeys(text, "RIGHT");

        Assert.True(result.Succeeded);
        Assert.Equal(["RIGHT"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithSemicolonSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\";");

        Assert.True(result.Succeeded);
        Assert.Equal(["A"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaUsesSoftSeparator()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\", \"B\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["A B"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaSeparatesNumericValues()
    {
        var (_, host, result) = ExecuteProgram("PRINT 10, 20");

        Assert.True(result.Succeeded);
        Assert.Equal(["10 20"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithMultipleCommasUsesSingleSpaces()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\", \"B\", \"C\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["A B C"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithTrailingCommaSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\",");

        Assert.True(result.Succeeded);
        Assert.Equal(["A "], host.Output);
    }

    [Fact]
    public void Execute_PrintWithSemicolonConcatenatesValues()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\"; \"B\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["AB"], host.Output);
    }

    [Fact]
    public void Execute_AmpersandConcatenatesMixedScalarValues()
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
    public void Execute_PrintWithMixedListUsesSeparatorSemantics()
    {
        var (_, host, result) = ExecuteProgram("PRINT 1; 2, 3");

        Assert.True(result.Succeeded);
        Assert.Equal(["12 3"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaDoesNotUseFixedZones()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"ABCD\", \"E\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["ABCD E"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingFormatsNumericMask()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"##.##\"; 3.14159");

        Assert.True(result.Succeeded);
        Assert.Equal([" 3.14"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingFormatsStringMasks()
    {
        const string text = """
PRINT USING "! & \    \"; "Hello"; "World"; "xy"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["H World xy    "], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingTrailingSemicolonSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"##\"; 12;");

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingReportsStringFieldTypeMismatch()
    {
        var (_, _, result) = ExecuteProgram("PRINT USING \"!\"; 1");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_PrintUsingReportsNumericFieldTypeMismatch()
    {
        var (_, _, result) = ExecuteProgram("PRINT USING \"##\"; \"oops\"");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("числов", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DateTimeBuiltinsUseCanonicalLocalFormats()
    {
        const string text = """
PRINT DATE()
PRINT TIME()
PRINT NOW()
PRINT TIMER()
""";

        var localNow = new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();
        var (_, host, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        Assert.True(result.Succeeded);
        Assert.Equal(["2026-04-20", "14:05:06", "2026-04-20 14:05:06", "50706"], host.Output);
    }

    [Fact]
    public void Execute_TimerPreservesFractionalSeconds()
    {
        const string text = "PRINT TIMER()";
        var localNow = new DateTimeOffset(2026, 04, 20, 00, 00, 01, 250, TimeSpan.FromHours(3));

        var (_, host, result) = ExecuteProgramAt(text, localNow);

        Assert.True(result.Succeeded);
        Assert.Equal(["1.25"], host.Output);
    }

    [Fact]
    public void Execute_DateTimeStage2FunctionsUseCanonicalContracts()
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
PRINT WEEKDAY("2026-04-26")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2026-04-20", "14:05:06", "2026", "4", "20", "14", "5", "6", "1", "7"], host.Output);
    }

    [Fact]
    public void Execute_DateTimeStage2ReportsInvalidCanonicalText()
    {
        const string text = "PRINT YEAR(\"20.04.2026\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("YEAR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DateSerialReportsInvalidCalendarDate()
    {
        const string text = "PRINT DATESERIAL(2025, 2, 29)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DATESERIAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("day", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_TimeSerialReportsOutOfRangeHour()
    {
        const string text = "PRINT TIMESERIAL(24, 0, 0)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TIMESERIAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hour", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DateTimeStage3FunctionsUseCanonicalContracts()
    {
        const string text = """
PRINT DATEADD("m", 1, "2026-01-31")
PRINT DATEADD("h", 2, "2026-04-20 23:05:06")
PRINT DATEADD("s", 10, "23:59:55")
PRINT DATEDIFF("m", "2026-01-31", "2026-04-20")
PRINT DATEDIFF("d", "2026-04-20 23:59:59", "2026-04-21 00:00:00")
PRINT DATEDIFF("n", "14:05:59", "14:06:00")
PRINT DATEPART("q", "2026-04-20")
PRINT DATEPART("y", "2026-04-21")
PRINT DATEPART("w", "2026-04-26")
PRINT DATEPART("h", "2026-04-20 14:05:06")
PRINT DATEVALUE("2026-04-20 14:05:06")
PRINT TIMEVALUE("2026-04-20 14:05:06")
PRINT ISDATE("2026-04-20")
PRINT ISDATE("14:05:06")
PRINT ISDATE("2026-04-20 14:05:06")
PRINT ISDATE("20.04.2026")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "2026-02-28",
                "2026-04-21 01:05:06",
                "00:00:05",
                "3",
                "1",
                "1",
                "2",
                "111",
                "7",
                "14",
                "2026-04-20",
                "14:05:06",
                "TRUE",
                "TRUE",
                "TRUE",
                "FALSE"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_DateTimeNameHelpersReturnCanonicalEnglishNames()
    {
        const string text = """
PRINT MONTHNAME(4)
PRINT MONTHNAME(MONTH("2026-12-20"))
PRINT WEEKDAYNAME(1)
PRINT WEEKDAYNAME(WEEKDAY("2026-04-26"))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "April",
                "December",
                "Monday",
                "Sunday"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_DateAddReportsInvalidIntervalToken()
    {
        const string text = "PRINT DATEADD(\"ww\", 1, \"2026-04-20\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DATEADD", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("interval", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_TimeValueReportsInvalidCanonicalText()
    {
        const string text = "PRINT TIMEVALUE(\"2:24PM\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TIMEVALUE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_MonthNameReportsOutOfRangeMonth()
    {
        const string text = "PRINT MONTHNAME(13)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("MONTHNAME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("month", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_WeekdayNameReportsOutOfRangeWeekday()
    {
        const string text = "PRINT WEEKDAYNAME(0)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("WEEKDAYNAME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weekday", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CalendarHelperBuiltinsReturnExpectedResults()
    {
        const string text = """
PRINT ISLEAPYEAR(2024)
PRINT ISLEAPYEAR(2025)
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
                "TRUE",
                "FALSE",
                "29",
                "2026-04-01",
                "2024-02-29",
                "2026-01-01",
                "2026-12-31"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_IsLeapYearReportsOutOfRangeYear()
    {
        const string text = "PRINT ISLEAPYEAR(10000)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ISLEAPYEAR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("year", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DaysInMonthReportsOutOfRangeMonth()
    {
        const string text = "PRINT DAYSINMONTH(2026, 13)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DAYSINMONTH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("month", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_StartOfMonthReportsInvalidCanonicalText()
    {
        const string text = "PRINT STARTOFMONTH(\"20.04.2026\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("STARTOFMONTH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_UtcBuiltinsUseCurrentHostOffset()
    {
        const string text = """
PRINT UTCDATE()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT UTCOFFSET()
PRINT TOUTC("2026-04-20 14:05:06")
PRINT FROMUTC("2026-04-20 11:05:06")
""";

        var localNow = new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();
        var (_, host, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["2026-04-20", "11:05:06", "2026-04-20 11:05:06", "180", "2026-04-20 11:05:06", "2026-04-20 14:05:06"],
            host.Output);
    }

    [Fact]
    public void Execute_UtcOffsetArgumentAndUtcConversionsUseSpecificDateOffsetRules()
    {
        const string text = """
PRINT UTCOFFSET()
PRINT UTCOFFSET("2026-01-20 12:00:00")
PRINT UTCOFFSET("2026-07-20 12:00:00")
PRINT TOUTC("2026-01-20 12:00:00")
PRINT TOUTC("2026-07-20 12:00:00")
PRINT FROMUTC("2026-01-20 10:00:00")
PRINT FROMUTC("2026-07-20 09:00:00")
""";

        var localTimeZone = CreateTestSeasonalTimeZone();
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var (_, host, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["180", "120", "180", "2026-01-20 10:00:00", "2026-07-20 09:00:00", "2026-01-20 12:00:00", "2026-07-20 12:00:00"],
            host.Output);
    }

    [Fact]
    public void Execute_TimeZoneAwareCurrentBuiltinsUseRequestedZone()
    {
        const string text = """
PRINT DATE("UTC")
PRINT TIME("UTC")
PRINT NOW("UTC")
PRINT DATE("Axiom Runtime Tests West")
PRINT TIME("Axiom Runtime Tests West")
PRINT NOW("Axiom Runtime Tests West")
PRINT UTCOFFSET(NOW("Axiom Runtime Tests West"), "Axiom Runtime Tests West")
""";

        var localNow = new DateTimeOffset(2026, 04, 20, 00, 30, 45, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();
        var westTimeZone = CreateTestFixedTimeZone("Axiom Runtime Tests West", -5);
        var namedTimeZones = CreateNamedTimeZones(westTimeZone);
        var (_, host, result) = ExecuteProgramAt(text, localNow, localTimeZone, namedTimeZones);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["2026-04-19", "21:30:45", "2026-04-19 21:30:45", "2026-04-19", "16:30:45", "2026-04-19 16:30:45", "-300"],
            host.Output);
    }

    [Fact]
    public void Execute_TimeZoneAwareUtcConversionsUseRequestedZoneRules()
    {
        const string text = """
PRINT UTCOFFSET("2026-01-20 12:00:00", "Axiom Runtime Tests Seasonal")
PRINT UTCOFFSET("2026-07-20 12:00:00", "Axiom Runtime Tests Seasonal")
PRINT TOUTC("2026-01-20 12:00:00", "Axiom Runtime Tests Seasonal")
PRINT TOUTC("2026-07-20 12:00:00", "Axiom Runtime Tests Seasonal")
PRINT FROMUTC("2026-01-20 10:00:00", "Axiom Runtime Tests Seasonal")
PRINT FROMUTC("2026-07-20 09:00:00", "Axiom Runtime Tests Seasonal")
""";

        var seasonalTimeZone = CreateTestSeasonalTimeZone();
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var namedTimeZones = CreateNamedTimeZones(seasonalTimeZone);
        var (_, host, result) = ExecuteProgramAt(text, localNow, TimeZoneInfo.Utc, namedTimeZones);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["120", "180", "2026-01-20 10:00:00", "2026-07-20 09:00:00", "2026-01-20 12:00:00", "2026-07-20 12:00:00"],
            host.Output);
    }

    [Fact]
    public void Execute_ToUtcAndFromUtcCanCrossDateBoundary()
    {
        const string text = """
PRINT TOUTC("2026-04-20 01:15:00")
PRINT FROMUTC("2026-04-20 22:15:00")
""";

        var localNow = new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var (_, host, result) = ExecuteProgramAt(text, localNow);

        Assert.True(result.Succeeded);
        Assert.Equal(["2026-04-19 22:15:00", "2026-04-21 01:15:00"], host.Output);
    }

    [Fact]
    public void Execute_ToUtcReportsMissingTimePart()
    {
        const string text = "PRINT TOUTC(\"2026-04-20\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TOUTC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DateReportsUnknownTimeZoneIdentifier()
    {
        const string text = "PRINT DATE(\"Missing/Zone\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DATE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timeZone", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ToUtcReportsInvalidLocalDateTimeDuringSkippedTransition()
    {
        const string text = "PRINT TOUTC(\"2026-03-29 02:30:00\")";
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();

        var (_, _, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TOUTC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ToUtcWithTimeZoneReportsInvalidLocalDateTimeDuringSkippedTransition()
    {
        const string text = "PRINT TOUTC(\"2026-03-29 02:30:00\", \"Axiom Runtime Tests Seasonal\")";
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var seasonalTimeZone = CreateTestSeasonalTimeZone();
        var namedTimeZones = CreateNamedTimeZones(seasonalTimeZone);

        var (_, _, result) = ExecuteProgramAt(text, localNow, TimeZoneInfo.Utc, namedTimeZones);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TOUTC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_UtcOffsetReportsAmbiguousLocalDateTimeDuringRepeatedTransition()
    {
        const string text = "PRINT UTCOFFSET(\"2026-10-25 02:30:00\")";
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();

        var (_, _, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("UTCOFFSET", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_UtcOffsetWithTimeZoneReportsAmbiguousLocalDateTimeDuringRepeatedTransition()
    {
        const string text = "PRINT UTCOFFSET(\"2026-10-25 02:30:00\", \"Axiom Runtime Tests Seasonal\")";
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var seasonalTimeZone = CreateTestSeasonalTimeZone();
        var namedTimeZones = CreateNamedTimeZones(seasonalTimeZone);

        var (_, _, result) = ExecuteProgramAt(text, localNow, TimeZoneInfo.Utc, namedTimeZones);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("UTCOFFSET", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_FromUtcReportsOutOfRangeAfterConversion()
    {
        const string text = "PRINT FROMUTC(\"9999-12-31 23:59:59\")";
        var localNow = new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();

        var (_, _, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("FROMUTC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
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
    public void Execute_UnallocatedDynamicArrayAccessReportsRuntimeState()
    {
        const string text = """
DIM items() AS STRING
PRINT items(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("items", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_WholeArrayAssignmentSharesAllocatedArray()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM target() AS INTEGER
source(1) = 10
source(2) = 20
target = source
PRINT target(1)
PRINT target(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCanBePassedToSubAndReturnedFromFunction()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20

Bump(values)

DIM copy() AS INTEGER
copy = Echo(values)

PRINT values(1)
PRINT copy(2)

SUB Bump(items AS INTEGER())
    items(1) = items(1) + 1
END SUB

FUNCTION Echo(items AS INTEGER()) AS INTEGER()
    RETURN items
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["11", "20"], host.Output);
    }

    [Fact]
    public void Execute_RedimAllocatesDynamicArray()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 3)
values(1) = 10
values(2) = 20
values(3) = 30
PRINT values(1)
PRINT values(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "30"], host.Output);
    }

    [Fact]
    public void Execute_RedimResetsSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
source(1) = 10
alias = source
REDIM alias(5 TO 6)
PRINT source(5)
PRINT source(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimPreserveKeepsOverlappingValues()
    {
        const string text = """
DIM values(1 TO 3) AS INTEGER
values(1) = 10
values(2) = 20
values(3) = 30
REDIM PRESERVE values(2 TO 4)
PRINT values(2)
PRINT values(3)
PRINT values(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "30", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimPreserveMutatesSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
source(1) = 10
source(2) = 20
alias = source
REDIM PRESERVE alias(2 TO 3)
PRINT source(2)
PRINT source(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "0"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalArrayProgram()
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
    public void Execute_ArrayMetadataBuiltinsReportRankAndVisibleBounds()
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
    public void Execute_ArrayBoundsReportsUnallocatedArrayState()
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
    public void Execute_PartialMultidimensionalIndexingReturnsSharedRowView()
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
    public void Execute_RangeMultidimensionalIndexingReturnsSharedViews()
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

        Assert.True(result.Succeeded);
        Assert.Equal(["210", "22", "320", "11", "12", "2", "3", "11", "12", "4"], host.Output);
    }

    [Fact]
    public void Execute_RedimOnArrayViewReportsBuiltinArgumentDiagnostic()
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
    public void Execute_EraseOnArrayViewReportsBuiltinArgumentDiagnostic()
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
    public void Execute_RedimAllowsCanonicalEmptyAllocatedArray()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 0)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_DimAllowsCanonicalEmptyAllocatedArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimRejectsNonCanonicalEmptyRange()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(5 TO 3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 TO 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DimRejectsMinimumIntegerLowerBound()
    {
        const string text = """
DIM values(-2147483648 TO -2147483648) AS INTEGER
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("-2147483648", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimRejectsMinimumIntegerLowerBound()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(-2147483648 TO -2147483648)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("-2147483648", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimPreserveCanShrinkToCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20
REDIM PRESERVE values(1 TO 0)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_EraseMakesArrayUnallocatedAgain()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 2)
values(1) = 10
ERASE values
PRINT values(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayCloneCopiesBoundsAndElementsIntoIndependentArray()
    {
        const string text = """
DIM source(5 TO 6) AS INTEGER
DIM copy() AS INTEGER
source(5) = 10
source(6) = 20
copy = ARRAYCLONE(source)
copy(5) = 99
PRINT LBOUND(copy)
PRINT UBOUND(copy)
PRINT ARRAYLENGTH(copy)
PRINT source(5)
PRINT copy(5)
PRINT source(6)
PRINT copy(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "6", "2", "10", "99", "20", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayClonePreservesUnallocatedAndCanonicalEmptyStates()
    {
        const string text = """
DIM pending() AS STRING
DIM pendingCopy() AS STRING
pendingCopy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(pendingCopy)
REDIM pending(1 TO 0)
pendingCopy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(pendingCopy)
PRINT ARRAYLENGTH(pendingCopy)
PRINT LBOUND(pendingCopy)
PRINT UBOUND(pendingCopy)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneCopiesMultidimensionalVisibleShapeIntoIndependentArray()
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
    public void Execute_ArraySliceReturnsCompactIndependentSlice()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYSLICE(source, 6, 2)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(6)
PRINT part(1)
PRINT source(7)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "20", "99", "30", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceAllowsZeroCountAsCanonicalEmptyArray()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 999, 0)
PRINT ARRAYISALLOCATED(part)
PRINT ARRAYLENGTH(part)
PRINT LBOUND(part)
PRINT UBOUND(part)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 1, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSLICE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySliceReportsOutOfRangeSlice()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 7, 2)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYSLICE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayConcatReturnsCompactIndependentArray()
    {
        const string text = """
DIM left(5 TO 6) AS INTEGER
DIM right(10 TO 11) AS INTEGER
DIM merged() AS INTEGER
left(5) = 10
left(6) = 20
right(10) = 30
right(11) = 40
merged = ARRAYCONCAT(left, right)
merged(1) = 99
PRINT LBOUND(merged)
PRINT UBOUND(merged)
PRINT ARRAYLENGTH(merged)
PRINT left(5)
PRINT merged(1)
PRINT merged(2)
PRINT merged(3)
PRINT merged(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "4", "4", "10", "99", "20", "30", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatAllowsCanonicalEmptyOperands()
    {
        const string text = """
DIM left(1 TO 0) AS INTEGER
DIM right(10 TO 11) AS INTEGER
DIM merged() AS INTEGER
right(10) = 30
right(11) = 40
merged = ARRAYCONCAT(left, right)
PRINT ARRAYISALLOCATED(merged)
PRINT LBOUND(merged)
PRINT UBOUND(merged)
PRINT ARRAYLENGTH(merged)
PRINT merged(1)
PRINT merged(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "2", "30", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatReportsUnallocatedOperand()
    {
        const string text = """
DIM left() AS INTEGER
DIM right(1 TO 1) AS INTEGER
DIM merged() AS INTEGER
merged = ARRAYCONCAT(left, right)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCONCAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToReversedReturnsCompactIndependentArray()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM reversed() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
reversed = ARRAYTOREVERSED(source)
reversed(1) = 99
PRINT LBOUND(reversed)
PRINT UBOUND(reversed)
PRINT ARRAYLENGTH(reversed)
PRINT source(7)
PRINT reversed(1)
PRINT reversed(2)
PRINT reversed(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "3", "30", "99", "20", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToReversedAllowsCanonicalEmptyResult()
    {
        const string text = """
DIM source(1 TO 0) AS INTEGER
DIM reversed() AS INTEGER
reversed = ARRAYTOREVERSED(source)
PRINT ARRAYISALLOCATED(reversed)
PRINT ARRAYLENGTH(reversed)
PRINT LBOUND(reversed)
PRINT UBOUND(reversed)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToReversedReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM reversed() AS INTEGER
reversed = ARRAYTOREVERSED(source)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTOREVERSED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToReversedSupportsMultidimensionalDimension()
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
    public void Execute_ArrayTakeReturnsCompactIndependentPrefix()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYTAKE(source, 2)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(5)
PRINT part(1)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "10", "99", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeAllowsZeroAndExcessiveCount()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM emptyPart() AS INTEGER
DIM wholePart() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
emptyPart = ARRAYTAKE(source, 0)
wholePart = ARRAYTAKE(source, 99)
PRINT ARRAYISALLOCATED(emptyPart)
PRINT ARRAYLENGTH(emptyPart)
PRINT LBOUND(emptyPart)
PRINT UBOUND(emptyPart)
PRINT LBOUND(wholePart)
PRINT UBOUND(wholePart)
PRINT wholePart(1)
PRINT wholePart(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0", "1", "3", "10", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYTAKE(source, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayTakeReportsNegativeCount()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYTAKE(source, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySkipReturnsCompactIndependentSuffix()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYSKIP(source, 1)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(6)
PRINT part(1)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "20", "99", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySkipAllowsZeroAndExcessiveCount()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM samePart() AS INTEGER
DIM emptyPart() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
samePart = ARRAYSKIP(source, 0)
emptyPart = ARRAYSKIP(source, 99)
PRINT LBOUND(samePart)
PRINT UBOUND(samePart)
PRINT samePart(1)
PRINT samePart(3)
PRINT ARRAYISALLOCATED(emptyPart)
PRINT ARRAYLENGTH(emptyPart)
PRINT LBOUND(emptyPart)
PRINT UBOUND(emptyPart)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "10", "30", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySkipReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYSKIP(source, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySkipReportsNegativeCount()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYSKIP(source, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()
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
    public void Execute_ArrayShapeBuiltinsRequireExplicitDimensionForMultidimensionalArrays()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 2) AS INTEGER
DIM part(,) AS INTEGER
part = ARRAYTAKE(board, 1)
""";

        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);
        _ = Binder.Bind(syntax, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("dimension", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayConcatReportsIncompatibleMultidimensionalShapes()
    {
        const string text = """
DIM leftPart(1 TO 2, 10 TO 11) AS INTEGER
DIM rightPart(2 TO 3, 12 TO 13) AS INTEGER
DIM merged(,) AS INTEGER
merged = ARRAYCONCAT(leftPart, rightPart, COLUMN)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYCONCAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("right", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayTakeAndSkipCanProduceAllocatedEmptyMultidimensionalResult()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM emptyTake(,) AS INTEGER
DIM emptySkip(,) AS INTEGER
emptyTake = ARRAYTAKE(board, 0, COLUMN)
emptySkip = ARRAYSKIP(board, 99, COLUMN)
PRINT ARRAYISALLOCATED(emptyTake)
PRINT ARRAYLENGTH(emptyTake)
PRINT LBOUND(emptyTake, ROW)
PRINT UBOUND(emptyTake, ROW)
PRINT LBOUND(emptyTake, COLUMN)
PRINT UBOUND(emptyTake, COLUMN)
PRINT ARRAYISALLOCATED(emptySkip)
PRINT ARRAYLENGTH(emptySkip)
PRINT LBOUND(emptySkip, COLUMN)
PRINT UBOUND(emptySkip, COLUMN)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "2", "10", "9", "TRUE", "0", "13", "12"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctReturnsCompactUniqueArray()
    {
        const string text = """
DIM source(5 TO 9) AS INTEGER
DIM uniqueValues() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 10
source(8) = 30
source(9) = 20
uniqueValues = ARRAYDISTINCT(source)
uniqueValues(1) = 99
PRINT LBOUND(uniqueValues)
PRINT UBOUND(uniqueValues)
PRINT ARRAYLENGTH(uniqueValues)
PRINT source(5)
PRINT uniqueValues(1)
PRINT uniqueValues(2)
PRINT uniqueValues(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "3", "10", "99", "20", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctAllowsCanonicalEmptyResult()
    {
        const string text = """
DIM source(1 TO 0) AS INTEGER
DIM uniqueValues() AS INTEGER
uniqueValues = ARRAYDISTINCT(source)
PRINT ARRAYISALLOCATED(uniqueValues)
PRINT ARRAYLENGTH(uniqueValues)
PRINT LBOUND(uniqueValues)
PRINT UBOUND(uniqueValues)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM uniqueValues() AS INTEGER
uniqueValues = ARRAYDISTINCT(source)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYDISTINCT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayDistinctOnMultidimensionalArrayReturnsCompactVisibleOrderResult()
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
    public void Execute_ArrayFlattenOnMultidimensionalArrayReturnsCompactVisibleOrderResult()
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
    public void Execute_ArrayReshapeRebuildsTemplateShapeFromSortedFlat()
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
    public void Execute_ArrayReshapeReportsMismatchedVisibleElementCount()
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
    public void Execute_ArrayToSortedPreservesBoundsAndDoesNotMutateSource()
    {
        const string text = """
DIM source(5 TO 9) AS INTEGER
DIM sortedValues() AS INTEGER
source(5) = 30
source(6) = 10
source(7) = 20
source(8) = 20
source(9) = 10
sortedValues = ARRAYTOSORTED(source)
sortedValues(5) = 99
PRINT LBOUND(sortedValues)
PRINT UBOUND(sortedValues)
PRINT source(5)
PRINT source(6)
PRINT source(7)
PRINT source(8)
PRINT source(9)
PRINT sortedValues(5)
PRINT sortedValues(6)
PRINT sortedValues(7)
PRINT sortedValues(8)
PRINT sortedValues(9)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "9", "30", "10", "20", "20", "10", "99", "10", "20", "20", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToSortedSupportsStringsAndCanonicalEmptyArray()
    {
        const string text = """
DIM names(2 TO 5) AS STRING
DIM sortedNames() AS STRING
DIM emptyNames(1 TO 0) AS STRING
DIM emptySorted() AS STRING
names(2) = "Cara"
names(3) = "Ann"
names(4) = "Bob"
names(5) = "Bob"
sortedNames = ARRAYTOSORTED(names)
emptySorted = ARRAYTOSORTED(emptyNames)
PRINT LBOUND(sortedNames)
PRINT UBOUND(sortedNames)
PRINT sortedNames(2)
PRINT sortedNames(3)
PRINT sortedNames(4)
PRINT sortedNames(5)
PRINT ARRAYISALLOCATED(emptySorted)
PRINT LBOUND(emptySorted)
PRINT UBOUND(emptySorted)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "5", "Ann", "Bob", "Bob", "Cara", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToSortedReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM sortedValues() AS INTEGER
sortedValues = ARRAYTOSORTED(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTOSORTED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFillMutatesArrayInPlace()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 1
values(6) = 2
values(7) = 3
ARRAYFILL(values, 42)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "42", "42", "5", "7"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFillMutatesMultidimensionalVisibleElements()
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
    public void Execute_ArrayReverseMutatesArrayInPlace()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
ARRAYREVERSE(values)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT values(8)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["40", "30", "20", "10", "5", "8"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReverseSupportsMultidimensionalDimension()
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
    public void Execute_ArraySortMutatesArrayInPlaceAcrossTypes()
    {
        const string text = """
DIM numbers(5 TO 9) AS INTEGER
DIM names(2 TO 5) AS STRING
numbers(5) = 30
numbers(6) = 10
numbers(7) = 20
numbers(8) = 20
numbers(9) = 10
names(2) = "Cara"
names(3) = "Ann"
names(4) = "Bob"
names(5) = "Bob"
ARRAYSORT(numbers)
ARRAYSORT(names)
PRINT numbers(5)
PRINT numbers(6)
PRINT numbers(7)
PRINT numbers(8)
PRINT numbers(9)
PRINT LBOUND(numbers)
PRINT UBOUND(numbers)
PRINT names(2)
PRINT names(3)
PRINT names(4)
PRINT names(5)
PRINT LBOUND(names)
PRINT UBOUND(names)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "10", "20", "20", "30", "5", "9", "Ann", "Bob", "Bob", "Cara", "2", "5"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMutatingProceduresAllowCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYFILL(values, 1)
ARRAYREVERSE(values)
ARRAYSORT(values)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFillReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYFILL(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFILL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReverseReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYREVERSE(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREVERSE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySortReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYSORT(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSORT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPushAppendsValueAndPreservesLowerBound()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
values(5) = 10
values(6) = 20
ARRAYPUSH(values, 30)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "30", "5", "7", "3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushAllowsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYPUSH(values, 42)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "1", "1", "42"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPopReturnsLastElementAndShrinksArray()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
PRINT ARRAYPOP(values)
PRINT values(5)
PRINT values(6)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["30", "10", "20", "5", "6", "2"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPopNormalizesSingleElementArrayToCanonicalEmpty()
    {
        const string text = """
DIM values(5 TO 5) AS INTEGER
values(5) = 99
PRINT ARRAYPOP(values)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ARRAYPUSH(values, 77)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "TRUE", "1", "0", "0", "1", "1", "77"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYPUSH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYPUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPopReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYPOP(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPopReportsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYPOP(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPushReportsUpperBoundOverflow()
    {
        const string text = """
DIM values(2147483647 TO 2147483647) AS INTEGER
ARRAYPUSH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYPUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertShiftsTailAndPreservesLowerBound()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 10
values(6) = 30
values(7) = 40
ARRAYINSERT(values, 6, 20)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT values(8)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "30", "40", "5", "8", "4"], host.Output);
    }

    [Fact]
    public void Execute_ArrayInsertAllowsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYINSERT(values, 1, 42)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "1", "1", "42"], host.Output);
    }

    [Fact]
    public void Execute_ArrayRemoveReturnsRemovedElementAndShiftsTail()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
PRINT ARRAYREMOVE(values, 6)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "10", "30", "40", "5", "7", "3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayRemoveNormalizesSingleElementArrayToCanonicalEmpty()
    {
        const string text = """
DIM values(5 TO 5) AS INTEGER
values(5) = 99
PRINT ARRAYREMOVE(values, 5)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ARRAYINSERT(values, 1, 77)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "TRUE", "1", "0", "0", "1", "1", "77"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalDynamicArrayBuiltinsWorkAlongExplicitDimension()
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
    public void Execute_MultidimensionalArrayInsertReportsIncompatibleValueShape()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 11) AS INTEGER
DIM wrongRow(10 TO 11) AS INTEGER
ARRAYINSERT(board, 10, wrongRow, COLUMN)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayEveryAndSomeUseFunctionValuesAndClosures()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM threshold AS INTEGER = 25
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40

PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, GreaterThanThreshold)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION GreaterThanThreshold(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape()
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
    public void Execute_ArrayEveryTreatsCanonicalEmptyArrayAsTrueAndArraySomeAsFalse()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterReturnsCompactArrayAndPreservesSource()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM threshold AS INTEGER = 25
DIM filtered() AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
filtered = ARRAYFILTER(values, GreaterThanThreshold)
filtered(1) = 99
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)
PRINT values(7)
PRINT filtered(1)
PRINT filtered(2)

FUNCTION GreaterThanThreshold(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "30", "99", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterAllowsCanonicalEmptyInputAndResult()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM filtered() AS INTEGER
filtered = ARRAYFILTER(values, IsPositive)
PRINT ARRAYISALLOCATED(filtered)
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)

REDIM values(5 TO 6)
values(5) = 1
values(6) = 2
filtered = ARRAYFILTER(values, IsLarge)
PRINT ARRAYISALLOCATED(filtered)
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value > 100
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0", "TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterTraversesMultidimensionalVisibleElementsIntoCompactResult()
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
    public void Execute_ArrayFilterReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM filtered() AS INTEGER
filtered = ARRAYFILTER(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFILTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFilterReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM predicate AS FUNCTION(INTEGER) AS BOOLEAN
DIM filtered() AS INTEGER
values(1) = 10
values(2) = 20
filtered = ARRAYFILTER(values, predicate)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayMapPreservesTransformedArrayShapeAndPreservesSource()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM factor AS INTEGER = 3
DIM mapped() AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
mapped = ARRAYMAP(values, Multiply)
mapped(5) = 99
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT ARRAYLENGTH(mapped)
PRINT values(5)
PRINT mapped(5)
PRINT mapped(6)
PRINT mapped(7)
PRINT mapped(8)

FUNCTION Multiply(value AS INTEGER) AS INTEGER
    RETURN value * factor
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "8", "4", "10", "99", "60", "90", "120"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapSupportsPolymorphicResultType()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
DIM prefix AS STRING = "item:"
DIM mapped() AS STRING
values(5) = 10
values(6) = 20
values(7) = 30
mapped = ARRAYMAP(values, FormatValue)
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT mapped(5)
PRINT mapped(6)
PRINT mapped(7)

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN prefix + STR(value)
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "7", "item:10", "item:20", "item:30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapAllowsCanonicalEmptyInput()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM mapped() AS INTEGER
mapped = ARRAYMAP(values, DoubleIt)
PRINT ARRAYISALLOCATED(mapped)
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT ARRAYLENGTH(mapped)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapPreservesMultidimensionalEmptyVisibleShape()
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
    public void Execute_ArrayMapReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM mapped() AS INTEGER
mapped = ARRAYMAP(values, DoubleIt)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYMAP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayMapReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM selector AS FUNCTION(INTEGER) AS STRING
DIM mapped() AS STRING
values(1) = 10
values(2) = 20
mapped = ARRAYMAP(values, selector)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReduceUsesSeedAndClosure()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
DIM bonus AS INTEGER = 1
values(5) = 10
values(6) = 20
values(7) = 30
PRINT ARRAYREDUCE(values, 5, AddWithBonus)
PRINT values(5)

FUNCTION AddWithBonus(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value + bonus
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["68", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceSupportsPolymorphicAccumulatorType()
    {
        const string text = """
DIM words(3 TO 5) AS STRING
words(3) = "alpha"
words(4) = "bb"
words(5) = "ccc"
PRINT ARRAYREDUCE(words, 0, AddLength)

FUNCTION AddLength(acc AS INTEGER, value AS STRING) AS INTEGER
    RETURN acc + LEN(value)
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceReturnsSeedForCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYREDUCE(values, 7, Add)

FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["7"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYREDUCE(values, 0, Add)

FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREDUCE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReduceReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS STRING
DIM reducer AS FUNCTION(INTEGER, STRING) AS INTEGER
values(1) = "aa"
values(2) = "bbb"
PRINT ARRAYREDUCE(values, 0, reducer)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPredicateFunctionsReportUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYSOME(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSOME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPredicateFunctionsReportEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM predicate AS FUNCTION(INTEGER) AS BOOLEAN
values(1) = 10
values(2) = 20
PRINT ARRAYEVERY(values, predicate)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYINSERT(values, 1, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYREMOVE(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsInvalidIndex()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
ARRAYINSERT(values, 4, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("диапазон", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsInvalidIndex()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
PRINT ARRAYREMOVE(values, 7)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("существующий логический индекс", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYREMOVE(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустого выделенного массива", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsInvalidIndexForCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYINSERT(values, 2, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустого выделенного массива", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("равен 1", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsUpperBoundOverflow()
    {
        const string text = """
DIM values(2147483647 TO 2147483647) AS INTEGER
ARRAYINSERT(values, 2147483647, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySearchFunctionsUseLogicalIndexesAcrossLowerBounds()
    {
        const string text = """
DIM oneBased(1 TO 3) AS INTEGER
DIM zeroBased(0 TO 2) AS INTEGER
DIM shifted(5 TO 7) AS INTEGER
DIM negative(-3 TO -1) AS INTEGER
oneBased(1) = 10
oneBased(2) = 20
oneBased(3) = 20
zeroBased(0) = 10
zeroBased(1) = 20
zeroBased(2) = 30
shifted(5) = 10
shifted(6) = 20
shifted(7) = 30
negative(-3) = 10
negative(-2) = 20
negative(-1) = 20
PRINT ARRAYCONTAINS(oneBased, 20)
PRINT ARRAYCOUNT(oneBased, 20)
PRINT ARRAYINDEXOF(oneBased, 20)
PRINT ARRAYLASTINDEXOF(oneBased, 20)
PRINT ARRAYINDEXOF(oneBased, 99)
PRINT ARRAYINDEXOF(zeroBased, 20)
PRINT ARRAYINDEXOF(zeroBased, 99)
PRINT ARRAYINDEXOF(shifted, 20)
PRINT ARRAYINDEXOF(shifted, 99)
PRINT ARRAYLASTINDEXOF(negative, 20)
PRINT ARRAYINDEXOF(negative, 99)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "2", "2", "3", "0", "1", "-1", "6", "4", "-1", "-4"], host.Output);
    }

    [Fact]
    public void Execute_ArraySearchFunctionsTreatCanonicalEmptyArrayAsNotFound()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYCONTAINS(values, 1)
PRINT ARRAYCOUNT(values, 1)
PRINT ARRAYINDEXOF(values, 1)
PRINT ARRAYLASTINDEXOF(values, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayContainsAndCountScanMultidimensionalVisibleElements()
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
    public void Execute_ArrayFindAllReturnsCompactArrayOfLogicalIndexes()
    {
        const string text = """
DIM shifted(5 TO 8) AS INTEGER
DIM negative(-3 TO 0) AS INTEGER
DIM found() AS INTEGER
DIM negativeHits() AS INTEGER
DIM missing() AS INTEGER
shifted(5) = 10
shifted(6) = 20
shifted(7) = 10
shifted(8) = 30
negative(-3) = 20
negative(-2) = 10
negative(-1) = 20
negative(0) = 20
found = ARRAYFINDALL(shifted, 10)
negativeHits = ARRAYFINDALL(negative, 20)
missing = ARRAYFINDALL(shifted, 99)
PRINT ARRAYISALLOCATED(found)
PRINT LBOUND(found)
PRINT UBOUND(found)
PRINT found(1)
PRINT found(2)
PRINT ARRAYISALLOCATED(negativeHits)
PRINT LBOUND(negativeHits)
PRINT UBOUND(negativeHits)
PRINT negativeHits(1)
PRINT negativeHits(2)
PRINT negativeHits(3)
PRINT ARRAYISALLOCATED(missing)
PRINT LBOUND(missing)
PRINT UBOUND(missing)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "5", "7", "TRUE", "1", "3", "-3", "-1", "0", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCoordinateSearchFunctionsReturnCoordinateVectorsAndTables()
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
    public void Execute_ArrayBinarySearchReturnsFirstLogicalIndexAcrossLowerBounds()
    {
        const string text = """
DIM numbers(5 TO 9) AS INTEGER
DIM zeroBased(0 TO 3) AS INTEGER
DIM names(2 TO 5) AS STRING
numbers(5) = 10
numbers(6) = 20
numbers(7) = 20
numbers(8) = 20
numbers(9) = 30
zeroBased(0) = 10
zeroBased(1) = 20
zeroBased(2) = 30
zeroBased(3) = 40
names(2) = "Ann"
names(3) = "Bob"
names(4) = "Bob"
names(5) = "Cara"
PRINT ARRAYBINARYSEARCH(numbers, 20)
PRINT ARRAYBINARYSEARCH(numbers, 15)
PRINT ARRAYBINARYSEARCH(zeroBased, 10)
PRINT ARRAYBINARYSEARCH(zeroBased, 35)
PRINT ARRAYBINARYSEARCH(names, "Bob")
PRINT ARRAYBINARYSEARCH(names, "Aaron")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6", "4", "0", "-1", "3", "1"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBinarySearchTreatsCanonicalEmptyArrayAsNotFound()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYBINARYSEARCH(values, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalSortingGroupSortsAlongExplicitDimension()
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
    public void Execute_MultidimensionalArrayBinarySearchReturnsCoordinateVectorOrEmptyArray()
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
    public void Execute_ArrayContainsReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYCONTAINS(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCONTAINS", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayIndexOfReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYINDEXOF(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYINDEXOF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayLastIndexOfReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYLASTINDEXOF(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYLASTINDEXOF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayCountReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYCOUNT(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCOUNT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFindAllReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM matches() AS INTEGER
matches = ARRAYFINDALL(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFINDALL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayBinarySearchReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYBINARYSEARCH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYBINARYSEARCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_EraseMutatesSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
alias = source
ERASE alias
PRINT source(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("source", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinArrayInformationFunctionsReportCurrentState()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
REDIM values(3 TO 5)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ERASE values
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "TRUE", "3", "5", "3", "FALSE", "0"], host.Output);
    }

    [Fact]
    public void Execute_LBoundReportsUnallocatedArrayState()
    {
        const string text = """
DIM values() AS INTEGER
PRINT LBOUND(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("LBOUND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SplitReturnsStringArrayWithOneBasedBounds()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("red,green,,blue", ",")
PRINT ARRAYISALLOCATED(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
PRINT ARRAYLENGTH(parts)
PRINT parts(1)
PRINT parts(3)
PRINT parts(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "4", "4", "red", "", "blue"], host.Output);
    }

    [Fact]
    public void Execute_SplitEmptyStringReturnsCanonicalEmptyArray()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("", ",")
PRINT ARRAYISALLOCATED(parts)
PRINT ARRAYLENGTH(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_SplitReportsEmptySeparator()
    {
        const string text = """
PRINT SPLIT("abc", "")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SPLIT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("separator", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_JoinUsesStringArrayBounds()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("red|green|blue", "|")
PRINT JOIN(parts, "/")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["red/green/blue"], host.Output);
    }

    [Fact]
    public void Execute_JoinUnallocatedAndEmptyArraysReturnEmptyString()
    {
        const string text = """
DIM first() AS STRING
DIM second(1 TO 0) AS STRING
PRINT JOIN(first)
PRINT JOIN(second)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", ""], host.Output);
    }

    [Fact]
    public void Execute_JoinSingleElementReturnsThatElement()
    {
        const string text = """
DIM parts(5 TO 5) AS STRING
parts(5) = "solo"
PRINT JOIN(parts)
PRINT JOIN(parts, "/")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["solo", "solo"], host.Output);
    }

    [Fact]
    public void Execute_DecimalDeclarationAndArithmetic()
    {
        const string text = """
DIM price AS DECIMAL = 10.10
DIM tax AS DECIMAL = 0.20
PRINT price + tax
PRINT (price + tax) / 2
PRINT STR(price + tax)
PRINT INT(price + tax)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10.30", "5.15", "10.30", "10"], host.Output);
    }

    [Fact]
    public void Execute_IfElse()
    {
        const string text = """
DIM result AS STRING
IF TRUE THEN
    result = "yes"
ELSE
    result = "no"
END IF
PRINT result
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["yes"], host.Output);
    }

    [Fact]
    public void Execute_ElseIfChain()
    {
        const string text = """
DIM score AS INTEGER = 7
IF score > 10 THEN
    PRINT "high"
ELSEIF score > 5 THEN
    PRINT "mid"
ELSE
    PRINT "low"
END IF
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["mid"], host.Output);
    }

    [Fact]
    public void Execute_StringOrderedComparisonsUseOrdinalSemantics()
    {
        const string text = """
PRINT "Z" > "M"
PRINT "nuts" > "Nuts"
PRINT "abc" < "abd"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseMatchesFirstClauseOnly()
    {
        const string text = """
DIM x AS INTEGER = 5
SELECT CASE x
    CASE IS > 0
        PRINT "positive"
    CASE 5
        PRINT "five"
    CASE ELSE
        PRINT "other"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["positive"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseSupportsRangesAndStrings()
    {
        const string text = """
DIM score AS INTEGER = 8
DIM answer AS STRING = "y"

SELECT CASE score
    CASE 1, 3, 5
        PRINT "odd"
    CASE 6 TO 9
        PRINT "range"
    CASE IS > 100
        PRINT "big"
    CASE ELSE
        PRINT "other"
END SELECT

SELECT CASE answer
    CASE "Y", "y"
        PRINT "yes"
    CASE ELSE
        PRINT "no"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["range", "yes"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseSupportsStringRangesAndComparisons()
    {
        const string text = """
DIM command AS STRING = "Nuts"
DIM letter AS STRING = "K"

SELECT CASE command
    CASE IS > "M"
        PRINT "after"
    CASE ELSE
        PRINT "before"
END SELECT

SELECT CASE letter
    CASE "A" TO "M"
        PRINT "first-half"
    CASE ELSE
        PRINT "second-half"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["after", "first-half"], host.Output);
    }

    [Fact]
    public void Execute_While()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
WHILE i < 3
    total = total + i
    i = i + 1
END WHILE
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithLeadingWhileCondition()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
DO WHILE i < 3
    total = total + i
    i = i + 1
LOOP
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithLeadingUntilCondition()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
DO UNTIL i = 3
    total = total + i
    i = i + 1
LOOP
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithTrailingWhileConditionRunsAtLeastOnce()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    PRINT i
    i = i + 1
LOOP WHILE i < 3
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithTrailingUntilConditionRunsUntilConditionBecomesTrue()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    PRINT i
    i = i + 1
LOOP UNTIL i = 3
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithoutConditionUsesExitDo()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    IF i = 2 THEN
        EXIT DO
    END IF
    PRINT i
    i = i + 1
LOOP
PRINT "done"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "done"], host.Output);
    }

    [Fact]
    public void Execute_ForLoop()
    {
        const string text = """
DIM total AS INTEGER = 0
FOR i = 1 TO 3
    total = total + i
NEXT
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6"], host.Output);
    }

    [Fact]
    public void Execute_ExitForBreaksNearestLoop()
    {
        const string text = """
FOR i = 1 TO 2
    FOR j = 1 TO 3
        IF j = 2 THEN
            EXIT FOR
        END IF
        PRINT STR(i) + ":" + STR(j)
    NEXT
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1:1", "2:1"], host.Output);
    }

    [Fact]
    public void Execute_ExitDoBreaksNearestDoLoop()
    {
        const string text = """
DIM outer AS INTEGER = 0
DIM inner AS INTEGER
DO WHILE outer < 2
    inner = 0
    DO WHILE inner < 3
        IF inner = 1 THEN
            EXIT DO
        END IF
        PRINT STR(outer) + ":" + STR(inner)
        inner = inner + 1
    LOOP
    outer = outer + 1
LOOP
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0:0", "1:0"], host.Output);
    }

    [Fact]
    public void Execute_NestedForAndWhileLoops()
    {
        const string text = """
DIM j AS INTEGER
DIM total AS INTEGER = 0
FOR i = 1 TO 2
    j = 1
    WHILE j <= 2
        total = total + 1
        j = j + 1
    END WHILE
NEXT
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["4"], host.Output);
    }

    [Fact]
    public void Execute_NestedDoLoopAndForLoops()
    {
        const string text = """
DIM count AS INTEGER = 0
FOR i = 1 TO 2
    DO WHILE count < 2
        PRINT STR(i) + ":" + STR(count)
        count = count + 1
    LOOP
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1:0", "1:1"], host.Output);
    }

    [Fact]
    public void Execute_ReportsStepZero()
    {
        const string text = """
FOR i = 1 TO 3 STEP 0
    PRINT i
NEXT
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3004");
        Assert.Contains("STEP 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(21, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_FunctionCallWithReturn()
    {
        const string text = """
PRINT Add(2, 3)

FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_SubCall()
    {
        const string text = """
DIM total AS INTEGER = 0
Increment()
PRINT total

SUB Increment()
    total = total + 1
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1"], host.Output);
    }

    [Fact]
    public void Execute_LocalSubAndFunctionCanBeCalledBeforeTheirDeclaration()
    {
        const string text = """
DIM result AS INTEGER = 0
Outer()
PRINT result

SUB Outer()
    LocalIncrement()
    result = result + AddLocal(2, 3)

    SUB LocalIncrement()
        result = result + 1
    END SUB

    FUNCTION AddLocal(left AS INTEGER, right AS INTEGER) AS INTEGER
        RETURN left + right
    END FUNCTION
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6"], host.Output);
    }

    [Fact]
    public void Execute_LocalFunctionCanCallItselfRecursively()
    {
        const string text = """
PRINT Outer(4)

FUNCTION Outer(value AS INTEGER) AS INTEGER
    RETURN Factorial(value)

    FUNCTION Factorial(n AS INTEGER) AS INTEGER
        IF n <= 1 THEN
            RETURN 1
        END IF

        RETURN n * Factorial(n - 1)
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["24"], host.Output);
    }

    [Fact]
    public void Execute_LocalSubCanMutateOuterLocalVariable()
    {
        const string text = """
SUB Outer()
    DIM total AS INTEGER = 10
    AddTwo()
    PRINT total

    SUB AddTwo()
        total = total + 2
    END SUB
END SUB

Outer()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_LocalFunctionCanCaptureOuterParameterAndLocalVariable()
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
    public void Execute_FunctionCanBeStoredInVariableAndCalled()
    {
        const string text = """
DIM op AS FUNCTION(INTEGER, INTEGER) AS INTEGER
op = Add
PRINT op(2, 3)

FUNCTION Add(left AS INTEGER, right AS INTEGER) AS INTEGER
    RETURN left + right
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_FunctionCanBePassedAsParameter()
    {
        const string text = """
PRINT ApplyTwice(DoubleValue, 3)

FUNCTION ApplyTwice(op AS FUNCTION(INTEGER) AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN op(op(value))
END FUNCTION

FUNCTION DoubleValue(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionPreservesCapturedValueAfterOuterFunctionReturns()
    {
        const string text = """
DIM addTen AS FUNCTION(INTEGER) AS INTEGER
addTen = MakeAdder(10)
PRINT addTen(5)

FUNCTION MakeAdder(baseValue AS INTEGER) AS FUNCTION(INTEGER) AS INTEGER
    FUNCTION Add(delta AS INTEGER) AS INTEGER
        RETURN baseValue + delta
    END FUNCTION

    RETURN Add
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["15"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionCanMutateSharedCapturedState()
    {
        const string text = """
DIM counter AS FUNCTION() AS INTEGER
counter = MakeCounter()
PRINT counter()
PRINT counter()
PRINT counter()

FUNCTION MakeCounter() AS FUNCTION() AS INTEGER
    DIM current AS INTEGER = 0

    FUNCTION NextValue() AS INTEGER
        current = current + 1
        RETURN current
    END FUNCTION

    RETURN NextValue
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionCanBeCalledImmediately()
    {
        const string text = """
PRINT MakeAdder()(5)

FUNCTION MakeAdder() AS FUNCTION(INTEGER) AS INTEGER
    DIM offset AS INTEGER = 10

    FUNCTION AddOffset(value AS INTEGER) AS INTEGER
        RETURN offset + value
    END FUNCTION

    RETURN AddOffset
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["15"], host.Output);
    }

    [Fact]
    public void Execute_ReportsCallingEmptyFunctionValue()
    {
        const string text = """
DIM missing AS FUNCTION(INTEGER) AS INTEGER
PRINT missing(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinLen()
    {
        var (_, host, result) = ExecuteProgram("PRINT LEN(\"hello\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinStrAndVal()
    {
        var (_, host, result) = ExecuteProgram("PRINT STR(VAL(\"12.5\"))");

        Assert.True(result.Succeeded);
        Assert.Equal(["12.5"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinTrimVariants()
    {
        const string text = """
PRINT TRIM("  hello  ")
PRINT TRIM("--hello--", "-")
PRINT TRIM("hello")
PRINT "[" + TRIM("hello   ") + "]"
PRINT "[" + TRIM("   hello") + "]"
PRINT "[" + TRIM("   ") + "]"
PRINT LTRIM("  hello  ")
PRINT LTRIM("hello")
PRINT RTRIM("  hello  ")
PRINT RTRIM("hello")
PRINT LTRIM("xyxyhello", "xy")
PRINT RTRIM("helloxyxy", "xy")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hello", "hello", "hello", "[hello]", "[hello]", "[]", "hello  ", "hello", "  hello", "hello", "hello", "hello"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReverse()
    {
        var (_, host, result) = ExecuteProgram("PRINT REVERSE(\"abcd\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["dcba"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRepeatAndSpace()
    {
        const string text = """
PRINT REPEAT("ha", 3)
PRINT "[" + SPACE(4) + "]"
PRINT REPEAT("x", 0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hahaha", "[    ]", ""], host.Output);
    }

    [Fact]
    public void Execute_BuiltinStringUsesFirstFillCharacter()
    {
        var (_, host, result) = ExecuteProgram("PRINT STRING(5, \"ab\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["aaaaa"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinPadLeftAndPadRight()
    {
        const string text = """
PRINT "[" + PADLEFT("7", 3) + "]"
PRINT PADLEFT("7", 3, "0")
PRINT PADLEFT("word", 2, ".")
PRINT PADLEFT("word", 4)
PRINT "[" + PADRIGHT("7", 3) + "]"
PRINT PADRIGHT("7", 3, ".")
PRINT PADRIGHT("word", 2, ".")
PRINT PADRIGHT("word", 4, ".")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[  7]", "007", "word", "word", "[7  ]", "7..", "word", "word"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInsertAndRemove()
    {
        const string text = """
PRINT INSERT("abcd", 1, "<")
PRINT INSERT("abcd", 3, "-")
PRINT INSERT("abcd", 99, "!")
PRINT INSERT("", 5, "seed")
PRINT INSERT("abcd", 2, "")
PRINT REMOVE("abcdef", 2, 3)
PRINT REMOVE("abcdef", 6, 10)
PRINT REMOVE("abcdef", 99, 2)
PRINT REMOVE("abcdef", 3, 0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["<abcd", "ab-cd", "abcd!", "seed", "abcd", "aef", "abcde", "abcdef", "abcdef"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRepeatReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REPEAT(\"ha\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPEAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinPadLeftReportsNegativeWidth()
    {
        var (_, _, result) = ExecuteProgram("PRINT PADLEFT(\"7\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PADLEFT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinPadRightReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT PADRIGHT(\"7\", 3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PADRIGHT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinInsertReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSERT(\"abc\", 0, \"x\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRemoveReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REMOVE(\"abc\", 1, -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinStringReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT STRING(3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinSearchFunctionsUseOrdinalCaseSensitiveRules()
    {
        const string text = """
PRINT CONTAINS("Hello", "ell")
PRINT CONTAINS("Hello", "ELL")
PRINT STARTSWITH("Hello", "He")
PRINT ENDSWITH("Hello", "lo")
PRINT STARTSWITH("Hello", "")
PRINT ENDSWITH("Hello", "")
PRINT STARTSWITH("Hello", "Hello")
PRINT ENDSWITH("Hello", "Hello")
PRINT STARTSWITH("Hello", "Hello!")
PRINT ENDSWITH("Hello", "Hello!")
PRINT CONTAINS("Hello", "")
PRINT CONTAINS("", "x")
PRINT CONTAINS("Hello", "Hello")
PRINT CONTAINS("Hello", "Hello!")
PRINT CONTAINS("Hello", "o")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReplaceUsesOrdinalCaseSensitiveRules()
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
    public void Execute_BuiltinReplaceManyUsesOnePassLongestMatchRules()
    {
        const string text = """
DIM oldItems(1 TO 4) AS STRING
DIM newItems(1 TO 4) AS STRING
oldItems(1) = "ab"
newItems(1) = "X"
oldItems(2) = "abc"
newItems(2) = "Y"
oldItems(3) = "x"
newItems(3) = "z"
oldItems(4) = "one"
newItems(4) = "two"
PRINT REPLACEMANY("abc ab x One", oldItems, newItems)
PRINT REPLACEMANY("one", oldItems, newItems)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Y X z One", "two"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrReturnsOneBasedPositions()
    {
        const string text = """
PRINT INSTR("banana", "na")
PRINT INSTR(4, "banana", "na")
PRINT INSTR("banana", "NA")
PRINT INSTR(7, "banana", "")
PRINT INSTR(8, "banana", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "5", "0", "7", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSTR(0, \"abc\", \"a\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSTR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinInStrRevReturnsLastOneBasedPosition()
    {
        const string text = """
PRINT INSTRREV("banana", "na")
PRINT INSTRREV("banana", "NA")
PRINT INSTRREV("abc", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "0", "4"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrRevCanSearchBackFromStartPosition()
    {
        const string text = """
PRINT INSTRREV(4, "banana", "na")
PRINT INSTRREV(2, "banana", "na")
PRINT INSTRREV(100, "banana", "na")
PRINT INSTRREV(4, "banana", "")
PRINT INSTRREV(100, "banana", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "0", "5", "5", "7"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrRevReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSTRREV(0, \"abc\", \"a\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSTRREV", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscAndChrUseCp866ForByteCodesAndUnicodeAbove255()
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
    public void Execute_BuiltinAscWAndChrWUseUnicodeCodePoints()
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
    public void Execute_BuiltinCenterCentersText()
    {
        const string text = """
PRINT "[" + CENTER("A", 3) + "]"
PRINT "[" + CENTER("A", 4, ".") + "]"
PRINT "[" + CENTER("AB", 5, ".") + "]"
PRINT "[" + CENTER("Hello", 3, ".") + "]"
PRINT "[" + CENTER("Hello", 5, ".") + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[ A ]", "[.A..]", "[.AB..]", "[Hello]", "[Hello]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinCenterReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT CENTER(\"A\", 3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CENTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    public void Execute_BuiltinCenterReportsNegativeWidth(string widthText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CENTER(\"A\", {widthText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CENTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscReportsEmptyText()
    {
        var (_, _, result) = ExecuteProgram("PRINT ASC(\"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ASC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscWReportsEmptyText()
    {
        var (_, _, result) = ExecuteProgram("PRINT ASCW(\"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ASCW", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("55296")]
    [InlineData("1114112")]
    public void Execute_BuiltinChrReportsInvalidCode(string codeText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CHR({codeText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("55296")]
    [InlineData("1114112")]
    public void Execute_BuiltinChrWReportsInvalidCode(string codeText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CHRW({codeText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHRW", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinSubStrCountReturnsNonOverlappingCount()
    {
        const string text = """
PRINT SUBSTRCOUNT("banana", "na")
PRINT SUBSTRCOUNT("aaaa", "aa")
PRINT SUBSTRCOUNT("banana", "NA")
PRINT SUBSTRCOUNT("", "a")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinSubStrCountReportsEmptyFragment()
    {
        var (_, _, result) = ExecuteProgram("PRINT SUBSTRCOUNT(\"abc\", \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SUBSTRCOUNT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fragment", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinReplaceReportsEmptyOldText()
    {
        var (_, _, result) = ExecuteProgram("PRINT REPLACE(\"abc\", \"\", \"x\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("oldText", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не может быть пустой строкой", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_BuiltinReplaceManyReportsInvalidRules()
    {
        const string text = """
DIM oldItems(1 TO 1) AS STRING
DIM newItems(1 TO 2) AS STRING
oldItems(1) = ""
newItems(1) = "x"
newItems(2) = "y"
PRINT REPLACEMANY("abc", oldItems, newItems)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPLACEMANY", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexIsMatchSupportsModes()
    {
        const string text = """
PRINT REGEXISMATCH("A12", "\d+")
PRINT REGEXISMATCH("A12", "^a\d+")
PRINT REGEXISMATCH("A12", "^a\d+", "i")
PRINT REGEXISMATCH("one" + CHR(10) + "two", "^two", "m")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexMatchReturnsFirstMatchOrEmptyString()
    {
        const string text = """
PRINT REGEXMATCH("Order 42 and 100", "\d+")
PRINT "[" + REGEXMATCH("Order", "\d+") + "]"
PRINT REGEXMATCH("A12", "a\d+", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "[]", "A12"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractReturnsRequestedGroup()
    {
        const string text = """
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT "[" + REGEXEXTRACT("A12", "([A-Z])(\d+)", 3) + "]"
PRINT REGEXEXTRACT("a12", "([A-Z])(\d+)", 1, "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12", "[]", "a"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexReplaceSupportsGroupReferencesAndCount()
    {
        const string text = """
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#")
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#", 1)
PRINT REGEXREPLACE("A12 B34", "([A-Z])(\d+)", "$1-$2")
PRINT REGEXREPLACE("a12 b34", "[a-z]\d+", "#", "i", 1)
PRINT REGEXREPLACE("payload 42", "payload\s+", "payload ", "i")
PRINT REGEXREPLACE("abc", "x", "y", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["one # two #", "one # two 34", "A-12 B-34", "# b34", "payload 42", "abc"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexCountReturnsMatchCount()
    {
        const string text = """
PRINT REGEXCOUNT("one 12 two 34", "\d+")
PRINT REGEXCOUNT("A12 b34", "[a-z]\d+", "i")
PRINT REGEXCOUNT("text", "\d+")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexMatchesReturnsAllMatches()
    {
        const string text = """
DIM values() AS STRING
values = REGEXMATCHES("Order 42 and 100", "\d+")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXMATCHES("A12 b34", "[a-z]\d+", "i")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXMATCHES("Order", "\d+")
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "42", "100", "2", "A12", "b34", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractAllReturnsRequestedGroupForEveryMatch()
    {
        const string text = """
DIM values() AS STRING
values = REGEXEXTRACTALL("A12 B34", "([A-Z])(\d+)", 2)
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXEXTRACTALL("a12 b34", "([A-Z])(\d+)", 1, "i")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXEXTRACTALL("A12 B34", "([A-Z])(\d+)", 3)
PRINT "[" + values(1) + "]"
PRINT "[" + values(2) + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "12", "34", "2", "a", "b", "[]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexSplitReturnsStringArray()
    {
        const string text = """
DIM parts() AS STRING
parts = REGEXSPLIT("one, two;three", "[,;]\s*")
PRINT ARRAYLENGTH(parts)
PRINT parts(1)
PRINT parts(2)
PRINT parts(3)

parts = REGEXSPLIT("A12 b34", "[a-z]\d+", "i")
PRINT ARRAYLENGTH(parts)
PRINT "[" + parts(1) + "]"
PRINT "[" + parts(2) + "]"
PRINT "[" + parts(3) + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "one", "two", "three", "3", "[]", "[ ]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexReportsInvalidPattern()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXISMATCH(\"abc\", \"[\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXISMATCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pattern", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexReportsUnknownMode()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXISMATCH(\"abc\", \"a\", \"z\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXISMATCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modes", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexReplaceReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXREPLACE(\"abc\", \"a\", \"x\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXREPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractReportsNegativeGroup()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXEXTRACT(\"abc\", \"(a)\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXEXTRACT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractAllReportsNegativeGroup()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXEXTRACTALL(\"abc\", \"(a)\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXEXTRACTALL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemReadsWritesAndChecksPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "notes.txt");
            var dirPath = Path.Combine(root, "created");

            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"
DIM dirPath AS STRING = "{ToBasicString(dirPath)}"

PRINT FILEEXISTS(filePath)
PRINT WRITEFILE(filePath, "Hello")
PRINT FILEEXISTS(filePath)
PRINT READFILE(filePath)
PRINT APPENDFILE(filePath, "!")
PRINT READFILE(filePath)
PRINT FILESIZE(filePath) > 0
PRINT LEN(FILEDATE(filePath)) > 0
PRINT MAKEDIR(dirPath)
PRINT DIREXISTS(dirPath)
PRINT PATHEXISTS(dirPath)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["FALSE", "TRUE", "TRUE", "Hello", "TRUE", "Hello!", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinReadFileFallsBackToWindows1251WhenUtf8IsInvalid()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "legacy.txt");
            File.WriteAllBytes(filePath, Encoding.GetEncoding(1251).GetBytes("Привет"));

            var text = $"""PRINT READFILE("{ToBasicString(filePath)}")""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["Привет"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemPathHelpersReturnExpectedValues()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(root, "notes.txt");

        var text = $"""
DIM root AS STRING = "{ToBasicString(root)}"
DIM filePath AS STRING = "{ToBasicString(filePath)}"

PRINT COMBINEPATH(root, "notes.txt") = filePath
PRINT FILENAME(filePath)
PRINT EXTNAME(filePath)
PRINT FULLPATH(filePath) = filePath
PRINT LEN(DIRNAME(filePath)) > 0
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "notes.txt", ".txt", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinFileSystemEnumeratesFilesAndDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "a.txt"), "A");
            File.WriteAllText(Path.Combine(root, "b.log"), "B");
            Directory.CreateDirectory(Path.Combine(root, "alpha"));
            Directory.CreateDirectory(Path.Combine(root, "beta"));

            var text = $"""
DIM root AS STRING = "{ToBasicString(root)}"
DIM files() AS STRING
DIM textFiles() AS STRING
DIM dirs() AS STRING
DIM matchingDirs() AS STRING
DIM emptyFiles() AS STRING

files = GETFILES(root)
textFiles = GETFILES(root, "*.txt")
dirs = GETDIRS(root)
matchingDirs = GETDIRS(root, "b*")
emptyFiles = GETFILES(root, "*.none")

PRINT ARRAYLENGTH(files)
PRINT FILENAME(files(1))
PRINT FILENAME(files(2))
PRINT ARRAYLENGTH(textFiles)
PRINT FILENAME(textFiles(1))
PRINT ARRAYLENGTH(dirs)
PRINT FILENAME(dirs(1))
PRINT FILENAME(dirs(2))
PRINT ARRAYLENGTH(matchingDirs)
PRINT FILENAME(matchingDirs(1))
PRINT ARRAYISALLOCATED(emptyFiles)
PRINT ARRAYLENGTH(emptyFiles)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["2", "a.txt", "b.log", "1", "a.txt", "2", "alpha", "beta", "1", "beta", "TRUE", "0"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReadsAllLines()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "notes.txt");
            File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta", Encoding.UTF8);

            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"
DIM lines() AS STRING

lines = READALLLINES(filePath)

PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
PRINT lines(1)
PRINT lines(2)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "2", "1", "2", "alpha", "beta"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReadsAllLinesFromEmptyFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "empty.txt");
            File.WriteAllText(filePath, string.Empty, Encoding.UTF8);

            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"
DIM lines() AS STRING

lines = READALLLINES(filePath)

PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingFileForReadAllLines()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"), "missing.txt");

        var text = $"""
DIM lines() AS STRING
lines = READALLLINES("{ToBasicString(filePath)}")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READALLLINES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsEmptyEnumerationPattern()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var text = $"""
DIM root AS STRING = "{ToBasicString(root)}"
DIM files() AS STRING
files = GETFILES(root, "")
""";

            var (_, _, result) = ExecuteProgram(text);

            var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
            Assert.Contains("GETFILES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("pattern", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "notes.txt");

            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"

PRINT WRITEFILE(filePath, "Hello")
PRINT PATHROOT(filePath) <> ""
PRINT ISABSOLUTEPATH(filePath)
PRINT LEN(DRIVENAME(filePath)) > 0
PRINT LEN(DRIVEFORMAT(filePath)) > 0
PRINT LEN(DRIVETYPE(filePath)) > 0
PRINT TOTALSPACE(filePath) > 0
PRINT TOTALFREESPACE(filePath) > 0
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReturnsExtendedDatesAndAttributes()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var filePath = Path.Combine(root, "notes.txt");
        File.WriteAllText(filePath, "data");
        File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.Hidden | FileAttributes.ReadOnly);

        try
        {
            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"
DIM dirPath AS STRING = "{ToBasicString(root)}"

PRINT LEN(FILECREATEDATE(filePath)) > 0
PRINT LEN(FILEACCESSEDDATE(filePath)) > 0
PRINT LEN(DIRDATE(dirPath)) > 0
PRINT ISREADONLY(filePath)
PRINT ISHIDDEN(filePath)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(root, "missing.txt");
        var text = $"""PRINT READFILE("{ToBasicString(filePath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingObjectForIsHidden()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(root, "missing.txt");
        var text = $"""PRINT ISHIDDEN("{ToBasicString(filePath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ISHIDDEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "source.txt");
            var copyPath = Path.Combine(root, "copy.txt");
            var movedPath = Path.Combine(root, "moved.txt");
            var renamedPath = Path.Combine(root, "renamed.txt");

            var text = $"""
DIM sourcePath AS STRING = "{ToBasicString(sourcePath)}"
DIM copyPath AS STRING = "{ToBasicString(copyPath)}"
DIM movedPath AS STRING = "{ToBasicString(movedPath)}"
DIM renamedPath AS STRING = "{ToBasicString(renamedPath)}"

PRINT WRITEFILE(sourcePath, "one")
PRINT COPYFILE(sourcePath, copyPath)
PRINT READFILE(copyPath)
PRINT WRITEFILE(sourcePath, "two")
PRINT COPYFILE(sourcePath, copyPath, TRUE)
PRINT READFILE(copyPath)
PRINT MOVEFILE(copyPath, movedPath)
PRINT FILEEXISTS(copyPath)
PRINT FILEEXISTS(movedPath)
PRINT RENAMEFILE(movedPath, "renamed.txt")
PRINT FILEEXISTS(renamedPath)
PRINT DELETEFILE(renamedPath)
PRINT FILEEXISTS(renamedPath)
PRINT DELETEFILE(renamedPath)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "one", "TRUE", "TRUE", "two", "TRUE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesDirectories()
    {
            var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

        try
        {
            var sourceDir = Path.Combine(root, "source");
            var copiedPath = Path.Combine(root, "copy");
            var movedPath = Path.Combine(root, "moved");
            var renamedDir = Path.Combine(root, "renamed");
            var sourceFile = Path.Combine(sourceDir, "notes.txt");
            var copyFile = Path.Combine(copiedPath, "notes.txt");
            var renamedFile = Path.Combine(renamedDir, "notes.txt");
            var emptyDir = Path.Combine(root, "empty");

            var text = $"""
DIM sourceDir AS STRING = "{ToBasicString(sourceDir)}"
DIM copiedPath AS STRING = "{ToBasicString(copiedPath)}"
DIM movedPath AS STRING = "{ToBasicString(movedPath)}"
DIM renamedDir AS STRING = "{ToBasicString(renamedDir)}"
DIM sourceFile AS STRING = "{ToBasicString(sourceFile)}"
DIM copyFile AS STRING = "{ToBasicString(copyFile)}"
DIM renamedFile AS STRING = "{ToBasicString(renamedFile)}"
DIM emptyDir AS STRING = "{ToBasicString(emptyDir)}"

PRINT MAKEDIR(sourceDir)
PRINT WRITEFILE(sourceFile, "dir")
PRINT COPYDIR(sourceDir, copiedPath)
PRINT READFILE(copyFile)
PRINT MOVEDIR(copiedPath, movedPath)
PRINT DIREXISTS(copiedPath)
PRINT DIREXISTS(movedPath)
PRINT RENAMEDIR(movedPath, "renamed")
PRINT READFILE(renamedFile)
PRINT MAKEDIR(emptyDir)
PRINT DELETEDIR(emptyDir)
PRINT DIREXISTS(emptyDir)
PRINT DELETEDIR(emptyDir)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "dir", "TRUE", "FALSE", "TRUE", "TRUE", "dir", "TRUE", "TRUE", "FALSE", "FALSE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsCopyFileMissingSource()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(root, "missing.txt");
        var destinationPath = Path.Combine(root, "copy.txt");
        var text = $"""PRINT COPYFILE("{ToBasicString(sourcePath)}", "{ToBasicString(destinationPath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("COPYFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("source", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    #if false
    [Fact]
    public void Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "PLANS.md");
            var workPath = Path.Combine(root, "PLANS.axiom-work.md");
            var sourceText =
                CreateUtf8Cp1251Mojibake("# План работ", passes: 1) + "\r\n\r\n" +
                CreateUtf8Cp1251Mojibake("## Что уже завершено", passes: 1) + "\r\n\r\n" +
                "### Следующий точный шаг\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами �/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal("# План работ\r\n\r\n## Что уже завершено\r\n\r\n### Следующий точный шаг\r\n", File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    #endif

    [Fact]
    public void Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "PLANS.md");
            var workPath = Path.Combine(root, "PLANS.axiom-work.md");
            const string expectedText = "# План работ\r\n\r\n## Что уже завершено\r\n\r\n### Следующий точный шаг\r\n";
            var sourceText =
                CreateUtf8Cp1251Mojibake("# План работ", passes: 2) + "\r\n\r\n" +
                CreateUtf8Cp1251Mojibake("## Что уже завершено", passes: 1) + "\r\n\r\n" +
                "### Следующий точный шаг\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами �/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleLeavesValidRussianUtf8TextUnchanged()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "PLANS.md");
            var workPath = Path.Combine(root, "PLANS.axiom-work.md");
            const string sourceText =
                "# План работ\r\n" +
                "- при будущих правках вёрстки manual проверять отдельно desktop- и mobile-поведение боковой навигации.\r\n" +
                "- Добавлена четвёртая чистая функция над одномерными массивами.\r\n" +
                "- `FOR`-control variable теперь объявляется самой строкой `FOR` и живёт только внутри цикла.\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 0",
                    "Строк с невосстановимыми маркерами �/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(sourceText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleRepairsMixedUtf8AndWindows1251Bytes()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "PLANS.md");
            var workPath = Path.Combine(root, "PLANS.axiom-work.md");
            var windows1251 = Encoding.GetEncoding(1251);
            var sourceBytes = Encoding.UTF8.GetBytes("# План работ\r\n")
                .Concat(windows1251.GetBytes("## Обновление плана\r\n"))
                .ToArray();
            const string expectedText = "# План работ\r\n## Обновление плана\r\n";
            File.WriteAllBytes(sourcePath, sourceBytes);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 1",
                    "Строк с невосстановимыми маркерами �/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "ARCHITECTURE.md");
            var workPath = Path.Combine(root, "ARCHITECTURE.axiom-work.md");
            var sourceText =
                "## Обновление 2026-04-21 - " + CreateUtf8Cp1251Mojibake("Смешанный заголовок", passes: 1) + "\r\n" +
                "- helper-" + CreateUtf8Cp1251Mojibake("слой", passes: 1) + " и " + CreateUtf8Cp1251Mojibake("граница", passes: 2) + "\r\n" +
                "- Уже исправленная строка\r\n";
            const string expectedText =
                "## Обновление 2026-04-21 - Смешанный заголовок\r\n" +
                "- helper-слой и граница\r\n" +
                "- Уже исправленная строка\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами �/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForSpecification()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "SPECIFICATION.md");
            var workPath = Path.Combine(root, "SPECIFICATION.axiom-work.md");
            var sourceText =
                "# " + CreateUtf8Cp1251Mojibake("Спецификация", passes: 1) + " Axiom\r\n" +
                "- string-first " + CreateUtf8Cp1251Mojibake("договор", passes: 2) + "\r\n" +
                "- Уже исправленная строка\r\n";
            const string expectedText =
                "# Спецификация Axiom\r\n" +
                "- string-first договор\r\n" +
                "- Уже исправленная строка\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами �/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleRepairsWindows1251ControlByteInsideMojibake()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "WORKLOG4.md");
            var workPath = Path.Combine(root, "WORKLOG4.axiom-work.md");
            var sourceText =
                CreateUtf8Cp1251Mojibake("Исторические части журнала:", passes: 1) + "\r\n" +
                CreateUtf8Cp1251Mojibake("Изменённые файлы:", passes: 1) + "\r\n";
            const string expectedText =
                "Исторические части журнала:\r\n" +
                "Изменённые файлы:\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами �/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleReportsUnrecoverableReplacementMarkers()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "WORKLOG2.md");
            var workPath = Path.Combine(root, "WORKLOG2.axiom-work.md");
            const string sourceText =
                "### ����\r\n" +
                "- normal ASCII line\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 0",
                    "Строк с невосстановимыми маркерами �/U+FFFD: 1",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(sourceText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var nested = Path.Combine(root, "nested");
            var firstFile = Path.Combine(root, "a.txt");
            var secondFile = Path.Combine(nested, "b.txt");

            var text = $"""
DIM root AS STRING = "{ToBasicString(root)}"
DIM nested AS STRING = "{ToBasicString(nested)}"
DIM firstFile AS STRING = "{ToBasicString(firstFile)}"
DIM secondFile AS STRING = "{ToBasicString(secondFile)}"

PRINT LEN(CURRENTDIR()) > 0
PRINT MAKEDIR(nested)
PRINT WRITEFILE(firstFile, "1234")
PRINT WRITEFILE(secondFile, "12")
PRINT DIRSIZE(root)
PRINT FREESPACE(root) > 0
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "6", "TRUE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var nested = Path.Combine(root, "nested");
            var nestedFile = Path.Combine(nested, "notes.txt");

            var text = $"""
DIM oldDir AS STRING = CURRENTDIR()
DIM root AS STRING = "{ToBasicString(root)}"
DIM nested AS STRING = "{ToBasicString(nested)}"
DIM nestedFile AS STRING = "{ToBasicString(nestedFile)}"

PRINT MAKEDIR(nested)
PRINT WRITEFILE(nestedFile, "data")
PRINT CHANGEDIR(root)
PRINT CURRENTDIR() = FULLPATH(root)
PRINT FILEEXISTS("nested\\notes.txt")
PRINT CHANGEDIR(oldDir)
PRINT CURRENTDIR() = FULLPATH(oldDir)
PRINT DELETEDIRRECURSIVE(root)
PRINT DIREXISTS(root)
PRINT DELETEDIRRECURSIVE(root)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE"], host.Output);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingDirectoryForChangeDir()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var directoryPath = Path.Combine(root, "missing");
        var text = $"""PRINT CHANGEDIR("{ToBasicString(directoryPath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHANGEDIR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemCreatesTemporaryFileAndDirectory()
    {
        const string text = """
DIM tempFile AS STRING = MAKETEMPFILE("sample_")
DIM tempDir AS STRING = MAKETEMPDIR("sample_")
PRINT FILEEXISTS(tempFile)
PRINT DIREXISTS(tempDir)
PRINT FILENAME(tempFile) <> ""
PRINT DIRNAME(tempDir) <> ""
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingDirectoryForDirSize()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var directoryPath = Path.Combine(root, "missing");
        var text = $"""PRINT DIRSIZE("{ToBasicString(directoryPath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DIRSIZE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
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
    public void Execute_OpenReadLineAndEof()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + Environment.NewLine + "omega");

        var text = $"""
DIM f AS FILE
DIM line AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
WHILE NOT EOF(f)
    line = READLINE(f)
    PRINT "[" + line + "]"
END WHILE
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[alpha]", "[]", "[omega]"], host.Output);
    }

    [Fact]
    public void Execute_FileStateBuiltins()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "state.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
PRINT ISOPEN(f)
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT ISOPEN(f)
PRINT FILEMODE(f)
PRINT FILEPATH(f)
CLOSE f
PRINT ISOPEN(f)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "INPUT", filePath, "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_FilePositionAndLengthForInput()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "position-input.txt");
        const string content = "alpha\r\nbeta";
        File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var firstLineBytes = Encoding.UTF8.GetByteCount("alpha\r\n");
        var totalBytes = Encoding.UTF8.GetByteCount(content);

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
PRINT READLINE(f)
PRINT FILEPOS(f)
PRINT READLINE(f)
PRINT FILEPOS(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", totalBytes.ToString(), "alpha", firstLineBytes.ToString(), "beta", totalBytes.ToString()], host.Output);
    }

    [Fact]
    public void Execute_ReadToEndReturnsRemainingText()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readtoend.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READLINE(f)
PRINT READTOEND(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["alpha", "beta"], host.Output);
    }

    [Fact]
    public void Execute_ReadTextCountsCharactersWhilePositionStaysByteBased()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readtext.txt");
        const string content = "AБВ";
        File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var firstTwoCharsBytes = Encoding.UTF8.GetByteCount("AБ");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READTEXT(f, 2)
PRINT FILEPOS(f)
PRINT READTOEND(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["AБ", firstTwoCharsBytes.ToString(), "В"], host.Output);
    }

    [Fact]
    public void Execute_ReadLinesReadsUpToCountAndAdvancesPosition()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readlines.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta" + Environment.NewLine + "gamma");

        var text = $"""
DIM f AS FILE
DIM first() AS STRING
DIM rest() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
first = READLINES(f, 2)
rest = READLINES(f, 2)
PRINT ARRAYLENGTH(first)
PRINT first(1)
PRINT first(2)
PRINT LINENUMBER(f)
PRINT ARRAYLENGTH(rest)
PRINT rest(1)
PRINT EOF(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "alpha", "beta", "3", "1", "gamma", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ReadLinesReturnsEmptyArrayForZeroCountAndEndOfFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readlines-empty.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
lines = READLINES(f, 0)
PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
PRINT LINENUMBER(f)
lines = READLINES(f, 1)
lines = READLINES(f, 1)
PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0", "1", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_SkipLineAndLineNumberTrackInputPosition()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "lines.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta" + Environment.NewLine + "gamma");
        var secondLinePosition = Encoding.UTF8.GetByteCount("alpha" + Environment.NewLine);

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT LINENUMBER(f)
PRINT SKIPLINE(f)
PRINT LINENUMBER(f)
PRINT READLINE(f)
PRINT LINENUMBER(f)
PRINT SEEKFILE(f, {secondLinePosition})
PRINT LINENUMBER(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "TRUE", "2", "beta", "3", "TRUE", "2"], host.Output);
    }

    [Fact]
    public void Execute_NewLineWritesEmptyLine()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "newline.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITELINE(f, "alpha")
PRINT NEWLINE(f)
PRINT WRITELINE(f, "beta")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], host.Output);
        Assert.Equal($"alpha{Environment.NewLine}{Environment.NewLine}beta{Environment.NewLine}", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_OpenOutputAndWriteLine()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITELINE(f, "first")
PRINT WRITELINE(f, "second")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
        Assert.Equal($"first{Environment.NewLine}second{Environment.NewLine}", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_OpenAppendAndWrite()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append.txt");
        File.WriteAllText(filePath, "A");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT WRITE(f, "B")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE"], host.Output);
        Assert.Equal("AB", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_FlushMakesBufferedWriteVisible()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "flush.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITE(f, "A")
PRINT FILESIZE("{ToBasicString(filePath)}")
PRINT FLUSH(f)
PRINT FILESIZE("{ToBasicString(filePath)}")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "TRUE", "1"], host.Output);
        Assert.Equal("A", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_FilePositionAndSeekForOutput()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "position-output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT FILELENGTH(f)
PRINT WRITE(f, "ABCDE")
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
PRINT SEEKFILE(f, 2)
PRINT FILEPOS(f)
PRINT WRITE(f, "Z")
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "TRUE", "5", "5", "TRUE", "2", "TRUE", "3", "5"], host.Output);
        Assert.Equal("ABZDE", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_RewindMovesFileBackToBeginning()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "rewind.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READLINE(f)
PRINT REWIND(f)
PRINT FILEPOS(f)
PRINT READLINE(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["alpha", "TRUE", "0", "alpha"], host.Output);
    }

    [Fact]
    public void Execute_CloseReportsClosedFile()
    {
        const string text = """
DIM f AS FILE
CLOSE f
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CLOSE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_OpenReportsAlreadyOpenFileVariable()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var firstPath = Path.Combine(root, "first.txt");
        var secondPath = Path.Combine(root, "second.txt");
        File.WriteAllText(firstPath, "1");
        File.WriteAllText(secondPath, "2");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(firstPath)}" FOR INPUT AS f
OPEN "{ToBasicString(secondPath)}" FOR INPUT AS f
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("OPEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_WriteReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT WRITE(f, "beta")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("WRITE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("записи", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_FlushReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT FLUSH(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("FLUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("записи", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadToEndReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT READTOEND(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READTOEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("чтения", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadLinesReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
lines = READLINES(f, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READLINES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("чтения", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadLinesReportsNegativeCount()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
lines = READLINES(f, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READLINES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SkipLineReportsEndOfFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "skip-eof.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SKIPLINE(f)
PRINT SKIPLINE(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SKIPLINE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("конец", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadTextReportsInvalidUtf8Boundary()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "utf8-boundary.txt");
        File.WriteAllText(filePath, "AБ", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SEEKFILE(f, 2)
PRINT READTEXT(f, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READTEXT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UTF-8", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SeekFileReportsAppendMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT SEEKFILE(f, 0)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SEEKFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("APPEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RewindReportsAppendMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append-rewind.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT REWIND(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REWIND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("APPEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SeekFileReportsPositionBeyondEnd()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "beyond-end.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SEEKFILE(f, 3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SEEKFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("position", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReportsInvalidMultiInputHandling()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT firstName, age
""";

        var (_, _, result) = ExecuteProgram(text, "Ada");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3003");
        Assert.Contains("2", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDivisionByZero()
    {
        var (_, _, result) = ExecuteProgram("PRINT 10 / 0");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("ноль", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(10, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("PRINT 10 \\ 0")]
    [InlineData("PRINT 10 MOD 0")]
    public void Execute_ReportsDivisionByZeroForIntegralOperators(string source)
    {
        var (_, _, result) = ExecuteProgram(source);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("ноль", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(10, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_NestedRuntimeErrorReportsCallChain()
    {
        const string text = """
PRINT Outer()

FUNCTION Outer() AS DOUBLE
    RETURN Inner()

    FUNCTION Inner() AS DOUBLE
        RETURN 10 / 0
    END FUNCTION
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("Цепочка вызовов", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("локальная функция 'Inner'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("функция 'Outer'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("объявлена внутри функции 'Outer'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(7, diagnostic.Location.Line);
        Assert.Equal(19, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsIntegerArithmeticOverflow()
    {
        const string text = """
DIM value AS INTEGER = 2147483647
PRINT value + 1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsIntegerDivisionOverflow()
    {
        const string text = """
DIM value AS INTEGER = -2147483648
PRINT value \ -1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsLongArithmeticOverflow()
    {
        const string text = """
DIM value AS LONG = 9223372036854775807
PRINT value + 1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("LONG", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsLongRemainderOverflow()
    {
        const string text = """
DIM value AS LONG = -9223372036854775807
value = value - 1
PRINT value MOD -1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("LONG", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDoubleArithmeticOverflow()
    {
        const string text = """
DIM value AS DOUBLE = VAL("1e308")
PRINT value * 10
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDecimalArithmeticOverflow()
    {
        const string text = """
DIM value AS DECIMAL = 79228162514264337593543950335.0
PRINT value * 10
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DECIMAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsArrayBoundsViolation()
    {
        const string text = """
DIM arr(1 TO 2) AS INTEGER
PRINT arr(3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3002");
        Assert.Contains("arr", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsInvalidInputConversion()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
""";

        var (_, _, result) = ExecuteProgram(text, "oops");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_TypeFieldsAndNestedRecords()
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
PRINT user.Name
PRINT user.Age
user.Name = "Ada"
user.Age = 36
user.Home.City = "London"
PRINT user.Name
PRINT user.Age
PRINT user.Home.City
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "0", "Ada", "36", "London"], host.Output);
    }

    [Fact]
    public void Execute_TypeAssignmentCopiesRecordValue()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM first AS Person
DIM second AS Person
first.Name = "Ada"
second = first
first.Name = "Grace"
PRINT second.Name
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayElementsAndFields()
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

DIM people(1 TO 2) AS Person
PRINT people(1).Name
PRINT people(1).Age
people(1).Name = "Ada"
people(1).Age = 36
people(1).Home.City = "London"
people(2).Name = "Grace"
PRINT people(1).Name
PRINT people(1).Age
PRINT people(1).Home.City
PRINT people(2).Name
PRINT ARRAYLENGTH(people)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "0", "Ada", "36", "London", "Grace", "2"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayElementAssignmentCopiesRecordValue()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM first AS Person
DIM copy AS Person
DIM people(1 TO 1) AS Person
first.Name = "Ada"
people(1) = first
first.Name = "Grace"
copy = people(1)
people(1).Name = "Alan"
PRINT people(1).Name
PRINT copy.Name
PRINT first.Name
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Alan", "Ada", "Grace"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicArrayField()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE

DIM user AS Person
REDIM user.Scores(1 TO 2)
user.Scores(1) = 10
user.Scores(2) = 20
PRINT user.Scores(1)
PRINT user.Scores(2)
ERASE user.Scores
PRINT ARRAYISALLOCATED(user.Scores)
REDIM user.Scores(5 TO 5)
PRINT user.Scores(5)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "FALSE", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicArrayFieldOnArrayElement()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE

DIM people(1 TO 1) AS Person
REDIM people(1).Scores(2 TO 3)
people(1).Scores(2) = 7
people(1).Scores(3) = 9
PRINT people(1).Scores(2)
PRINT people(1).Scores(3)
REDIM PRESERVE people(1).Scores(3 TO 4)
PRINT people(1).Scores(3)
PRINT people(1).Scores(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["7", "9", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicMultidimensionalArrayField()
    {
        const string text = """
TYPE Person
    Scores(,) AS INTEGER
END TYPE

DIM user AS Person
REDIM user.Scores(1 TO 2, 3 TO 4)
user.Scores(1, 3) = 10
user.Scores(2, 4) = 20
PRINT user.Scores(1, 3)
PRINT user.Scores(2, 4)
PRINT ARRAYLENGTH(user.Scores)
ERASE user.Scores
PRINT ARRAYISALLOCATED(user.Scores)

DIM people(1 TO 1) AS Person
REDIM people(1).Scores(2 TO 3, 5 TO 6)
people(1).Scores(2, 5) = 7
people(1).Scores(3, 6) = 9
REDIM PRESERVE people(1).Scores(3 TO 4, 6 TO 7)
PRINT people(1).Scores(3, 6)
PRINT people(1).Scores(4, 7)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "4", "FALSE", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayFieldViews()
    {
        const string text = """
TYPE Board
    Cells(,) AS INTEGER
END TYPE

DIM board AS Board
DIM lineView() AS INTEGER
DIM region(,) AS INTEGER

REDIM board.Cells(1 TO 3, 1 TO 4)
board.Cells(2, 3) = 23

lineView = board.Cells(2)
PRINT lineView(3)
lineView(4) = 24
PRINT board.Cells(2, 4)

region = board.Cells(2 TO 3, 3 TO 4)
PRINT LBOUND(region, 1)
PRINT UBOUND(region, 2)
PRINT ARRAYLENGTH(region)
region(3, 3) = 99
PRINT board.Cells(3, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["23", "24", "2", "4", "4", "99"], host.Output);
    }

    [Fact]
    public void Execute_TypeNamedLiteral()
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
    public void Execute_TypePositionalLiteral()
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
    public void Execute_TypeFieldDefaultValues()
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
    public void Execute_TypeStructuralEquality()
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
    public void Execute_TypeFixedArrayField()
    {
        const string text = """
TYPE Person
    Scores(1 TO 3) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
DIM people(1 TO 1) AS Person
PRINT first.Scores(1)
first.Scores(1) = 10
first.Scores(3) = 30
second = first
first.Scores(1) = 99
people(1) = second
people(1).Scores(2) = 20
PRINT second.Scores(1)
PRINT second.Scores(3)
PRINT people(1).Scores(1)
PRINT people(1).Scores(2)
PRINT ARRAYLENGTH(people(1).Scores)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "10", "30", "10", "20", "3"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedMultidimensionalArrayField()
    {
        const string text = """
TYPE Person
    Grid(1 TO 2, 4 TO 5) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
DIM people(1 TO 1) AS Person

PRINT first.Grid(1, 4)
first.Grid(1, 4) = 10
first.Grid(2, 5) = 30
second = first
first.Grid(1, 4) = 99
people(1) = second
people(1).Grid(1, 5) = 20

PRINT second.Grid(1, 4)
PRINT second.Grid(2, 5)
PRINT ARRAYLENGTH(people(1).Grid)
PRINT people(1).Grid(1, 4)
PRINT people(1).Grid(1, 5)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "10", "30", "4", "10", "20"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayFieldWithConstBounds()
    {
        const string text = """
CONST MinScore AS INTEGER = 1
CONST MaxScore AS LONG = MinScore + 2

TYPE Person
    Scores(MinScore TO MaxScore) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
PRINT first.Scores(1)
PRINT ARRAYLENGTH(first.Scores)
first.Scores(3) = 30
second = first
first.Scores(3) = 99
PRINT second.Scores(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "3", "30"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayFieldWithConstBoundsDeclaredLater()
    {
        const string text = """
TYPE Person
    Scores(MinScore TO MaxScore) AS INTEGER
END TYPE

CONST MinScore AS INTEGER = 1
CONST MaxScore AS LONG = MinScore + 2

DIM first AS Person
DIM second AS Person
PRINT first.Scores(1)
PRINT ARRAYLENGTH(first.Scores)
first.Scores(3) = 30
second = first
first.Scores(3) = 99
PRINT second.Scores(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "3", "30"], host.Output);
    }

    [Fact]
    public void Execute_DimFixedLengthStringPadsAndTruncates()
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
    public void Execute_TypeFixedLengthStringFieldPadsAndTruncates()
    {
        const string text = """
TYPE Employee
    Name AS STRING * 5
    Role AS STRING * 8
END TYPE

DIM user AS Employee
PRINT "[" + user.Name + "]"
user.Name = "Ada"
user.Role = "Analyst"
PRINT "[" + user.Name + "]"
PRINT "[" + user.Role + "]"
user.Role = "Architect"
PRINT "[" + user.Role + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[     ]", "[Ada  ]", "[Analyst ]", "[Architec]"], host.Output);
    }

    [Fact]
    public void Execute_TypeGroupedFields()
    {
        const string text = """
TYPE Person
    Name, Alias AS STRING
    Scores(), Flags(1 TO 2) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
PRINT first.Name
PRINT first.Alias
PRINT first.Flags(1)
REDIM first.Scores(1 TO 2)
first.Name = "Ada"
first.Alias = "Analyst"
first.Scores(1) = 10
first.Scores(2) = 20
first.Flags(2) = 7
second = first
first.Alias = "Changed"
first.Scores(1) = 99
first.Flags(2) = 42
PRINT second.Name
PRINT second.Alias
PRINT second.Scores(1)
PRINT second.Scores(2)
PRINT second.Flags(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "", "0", "Ada", "Analyst", "99", "20", "7"], host.Output);
    }

    [Fact]
    public void Execute_ByRefRecordParameterMutatesCallerVariable()
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
    public void Execute_ByRefScalarParameterMutatesCallerVariable()
    {
        const string text = """
SUB AddOne(BYREF value AS INTEGER)
    value = value + 1
END SUB

DIM count AS INTEGER
count = 10
AddOne(count)
PRINT count
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["11"], host.Output);
    }

    [Fact]
    public void Execute_ByValRecordParameterKeepsCallerVariableIndependent()
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
    public void Execute_TypeMethodsMutateSelfAndReturnValues()
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
    public void Execute_TypeMethodsAcceptFieldAndArrayElementReceivers()
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
    public void Execute_TypeMethodsAcceptTemporaryReceivers()
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
    public void Execute_TypeMethodsSupportOverloadsBySignature()
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
    public void Execute_TypeConstructorSupportsOverloadsAndZeroFormWhileNamedLiteralStaysLiteral()
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
    public void Execute_TypeConstructorSupportsSameArityWithDifferentTypes()
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
    public void Execute_TypeConstructorRunsInvariantAfterConstruction()
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
    public void Execute_TypeConstructorReportsRuntimeFailureWhenInvariantReturnsFalse()
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

    [Fact]
    public void Execute_TryCatchHandlesRuntimeFailureAndExposesErrorFields()
    {
        const string text = """
TRY
    PRINT 10 \ 0
    PRINT "unreachable"
CATCH err
    PRINT err.Code
    PRINT err.Line
    PRINT err.Column
END TRY

PRINT "after"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["MB3001", "2", "14", "after"], host.Output);
    }

    [Fact]
    public void Execute_TryFinallyRunsAfterNormalBody()
    {
        const string text = """
TRY
    PRINT "body"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["body", "cleanup"], host.Output);
    }

    [Fact]
    public void Execute_TryCatchFinallyRunsFinallyAfterHandledFailure()
    {
        const string text = """
TRY
    PRINT 10 \ 0
CATCH err
    PRINT "caught"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["caught", "cleanup"], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementCanBeCaughtByTryCatch()
    {
        const string text = """
TRY
    ERROR "APP001", "Manual failure"
CATCH err
    PRINT err.Code
    PRINT err.Message
    PRINT err.Line
FINALLY
    PRINT "cleanup"
END TRY

PRINT "after"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["APP001", "Manual failure", "2", "cleanup", "after"], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementUsesDefaultMessage()
    {
        const string text = """
TRY
    ERROR "APP002"
CATCH err
    PRINT err.Message
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Пользовательская ошибка APP002."], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementRethrowsCaughtRuntimeError()
    {
        const string text = """
TRY
    TRY
        ERROR "APP003", "Original failure"
    CATCH err
        PRINT "inner"
        ERROR err
    END TRY
CATCH outerErr
    PRINT outerErr.Code
    PRINT outerErr.Message
    PRINT outerErr.Line
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["inner", "APP003", "Original failure", "3"], host.Output);
    }

    [Fact]
    public void Execute_TryFinallyRunsBeforeUnhandledFailureEscapes()
    {
        const string text = """
TRY
    PRINT 10 \ 0
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        Assert.Equal(["cleanup"], host.Output);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Equal(2, diagnostic.Location.Line);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(string text, params string[] inputs)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost(inputs);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(string text, TestRuntimeHost host)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithKeys(string text, params string[] keys)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost([], keys: keys);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithPressedKeys(string text, params string[] pressedKeys)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost([], pressedKeys: pressedKeys);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithKeysAndPressedKeys(
        string text,
        IEnumerable<string> keys,
        IEnumerable<string> pressedKeys)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost([], keys: keys, pressedKeys: pressedKeys);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithMouseState(
        string text,
        long mouseX = -1,
        long mouseY = -1,
        IEnumerable<int>? pressedMouseButtons = null,
        IEnumerable<int>? mousePresses = null,
        IEnumerable<MouseInputEvent>? mouseEvents = null)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost([], mouseX: mouseX, mouseY: mouseY, pressedMouseButtons: pressedMouseButtons, mousePresses: mousePresses, mouseEvents: mouseEvents);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        params string[] inputs)
    {
        return ExecuteProgramAt(text, localNow, TimeZoneInfo.Local, namedTimeZones: null, inputs);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        TimeZoneInfo localTimeZone,
        params string[] inputs)
    {
        return ExecuteProgramAt(text, localNow, localTimeZone, namedTimeZones: null, inputs);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        TimeZoneInfo localTimeZone,
        IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones,
        params string[] inputs)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost(inputs, localNow, localTimeZone, namedTimeZones);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static string ToBasicString(string value)
    {
        return value.Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    private static string SetRepairSamplePaths(string text, string sourcePath, string workPath)
    {
        const string sourceDeclarationPrefix = "DIM sourcePath AS STRING = ";
        const string workDeclarationPrefix = "DIM workPath AS STRING = ";
        const string timingDeclarationPrefix = "DIM showTiming AS BOOLEAN = ";
        var sourceReplacement = sourceDeclarationPrefix + "\"" + ToBasicString(sourcePath) + "\"";
        var workReplacement = workDeclarationPrefix + "\"" + ToBasicString(workPath) + "\"";
        var timingReplacement = timingDeclarationPrefix + "FALSE";
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var sourcePathSet = false;
        var workPathSet = false;
        var timingSet = false;

        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith(sourceDeclarationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = sourceReplacement;
                sourcePathSet = true;
                continue;
            }

            if (lines[index].StartsWith(workDeclarationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = workReplacement;
                workPathSet = true;
                continue;
            }

            if (lines[index].StartsWith(timingDeclarationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = timingReplacement;
                timingSet = true;
            }
        }

        if (!sourcePathSet || !workPathSet || !timingSet)
        {
            throw new InvalidOperationException("Could not locate source, work path, or timing declarations in repair sample.");
        }

        return string.Join("\n", lines);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Axiom.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Axiom.sln.");
    }

    private static string CreateUtf8Cp1251Mojibake(string text, int passes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(passes);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var utf8 = Encoding.UTF8;
        var windows1251 = Encoding.GetEncoding(1251);
        var current = text;

        for (var i = 0; i < passes; i++)
        {
            current = windows1251.GetString(utf8.GetBytes(current));
        }

        return current;
    }

    private static TimeZoneInfo CreateTestSeasonalTimeZone()
    {
        var daylightDelta = TimeSpan.FromHours(1);
        var daylightTransitionStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            5,
            DayOfWeek.Sunday);
        var daylightTransitionEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 3, 0, 0),
            10,
            5,
            DayOfWeek.Sunday);
        var adjustmentRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1),
            new DateTime(2099, 12, 31),
            daylightDelta,
            daylightTransitionStart,
            daylightTransitionEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "Axiom Runtime Tests Seasonal",
            TimeSpan.FromHours(2),
            "Axiom Runtime Tests Seasonal",
            "Axiom Runtime Tests Standard",
            "Axiom Runtime Tests Daylight",
            [adjustmentRule],
            disableDaylightSavingTime: false);
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

    private static RuntimeImageValue CreateTestImage(int width, int height)
    {
        var pixels = Enumerable.Repeat(0xFF0000FFu, width * height).ToArray();
        return new RuntimeImageValue(width, height, pixels);
    }

    private static RuntimeSpriteValue CreateTestSprite(int imageWidth, int imageHeight, int frameWidth, int frameHeight) =>
        new(CreateTestImage(imageWidth, imageHeight), frameWidth, frameHeight);

    private sealed class TestRuntimeHost : IRuntimeHost
    {
        private const int VisibleTextColumns = 40;
        private const int VisibleTextRows = 16;
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
        private readonly Dictionary<int, Dictionary<(long X, long Y), int>> _screenPages = [];
        private readonly int _cancelAfterReadKeyCalls;
        private readonly int _cancelAfterCancellationPolls;
        private int _readKeyCalls;
        private int _cancellationPolls;
        private bool _isScreenActive;
        private bool _isCanvasActive;
        private long _canvasWidth;
        private long _canvasHeight;
        private int _activeScreenPage;
        private int _visualScreenPage;
        private int _foreground = 15;
        private int _background;
        private bool _isTextBackgroundTransparent;
        private long _currentGraphicsX;
        private long _currentGraphicsY;
        private bool _viewportEnabled;
        private bool _viewportRelativeCoordinates;
        private long _viewportLeft;
        private long _viewportTop;
        private long _viewportRight = -1;
        private long _viewportBottom = -1;
        private bool _windowEnabled;
        private bool _windowUsesScreenCoordinates;
        private double _windowLeft;
        private double _windowTop;
        private double _windowRight;
        private double _windowBottom;
        private readonly List<string> _visibleTextLines = [];
        private int _textCursorRow = 1;
        private int _textCursorColumn = 1;
        private int _textViewportTop = 1;
        private int _textViewportBottom = VisibleTextRows;
        private bool _pendingTextViewportScroll;

        public TestRuntimeHost(
            IEnumerable<string> inputs,
            DateTimeOffset? localNow = null,
            TimeZoneInfo? localTimeZone = null,
            IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones = null,
            IEnumerable<string>? keys = null,
            IEnumerable<string>? pressedKeys = null,
            long mouseX = -1,
            long mouseY = -1,
            IEnumerable<int>? pressedMouseButtons = null,
            IEnumerable<int>? mousePresses = null,
            IEnumerable<MouseInputEvent>? mouseEvents = null,
            int cancelAfterReadKeyCalls = int.MaxValue,
            int cancelAfterCancellationPolls = int.MaxValue)
        {
            _inputs = new Queue<string>(inputs);
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
            _cancelAfterReadKeyCalls = cancelAfterReadKeyCalls;
            _cancelAfterCancellationPolls = cancelAfterCancellationPolls;
            _isScreenActive = mouseX >= 0 || mouseY >= 0 || _pressedMouseButtons.Count > 0 || _mouseEvents.Count > 0 || _mousePresses.Count > 0;
        }

        public List<InputRequest> InputRequests { get; } = [];

        public List<string> Output { get; } = [];

        public List<CircleCall> Circles { get; } = [];

        public List<LineCall> Lines { get; } = [];

        public List<GraphicsPaintRequest> PaintRequests { get; } = [];

        public List<(int Index, int Color)> PaletteColors { get; } = [];

        public Dictionary<string, RuntimeImageValue> Images { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, RuntimeSpriteValue> Sprites { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<DrawImageCall> DrawImages { get; } = [];

        public List<DrawSpriteCall> DrawSprites { get; } = [];

        public List<(long Width, long Height)> CanvasActivations { get; } = [];

        public List<int> FrameClears { get; } = [];

        public List<DrawRectCall> DrawRects { get; } = [];

        public List<FillRectCall> FillRects { get; } = [];

        public List<CanvasCameraCall> CanvasCameraCalls { get; } = [];

        public List<CanvasLineCall> CanvasDrawLines { get; } = [];

        public List<CanvasCircleCall> CanvasDrawCircles { get; } = [];

        public List<CanvasCircleCall> CanvasFillCircles { get; } = [];

        public List<CanvasTextCall> CanvasDrawTexts { get; } = [];

        public int BeginFrameCount { get; private set; }

        public int PresentFrameCount { get; private set; }

        public int ResetCanvasCameraCount { get; private set; }

        public int PaletteResets { get; private set; }

        public string GetVisibleTextLine(int row)
        {
            if (row < 1 || row > VisibleTextRows)
            {
                return string.Empty;
            }

            return row <= _visibleTextLines.Count ? _visibleTextLines[row - 1] : string.Empty;
        }

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

        public MouseInputEvent ReadMouseEvent() => _mouseEvents.Count > 0 ? _mouseEvents.Dequeue() : default;

        public bool IsScreenActive => _isScreenActive;

        public bool IsCanvasActive => _isCanvasActive;

        public int ActiveScreenPage => _activeScreenPage;

        public int VisualScreenPage => _visualScreenPage;

        public int ScreenMode => _isScreenActive ? 1 : 0;

        public int ScreenWidth => _isScreenActive ? 320 : _isCanvasActive ? (int)_canvasWidth : 0;

        public int ScreenHeight => _isScreenActive ? 200 : _isCanvasActive ? (int)_canvasHeight : 0;

        public int CurrentForegroundColor => _foreground;

        public int CurrentBackgroundColor => _background;

        public bool IsTextBackgroundTransparent => _isTextBackgroundTransparent;

        public long CurrentGraphicsX => _currentGraphicsX;

        public long CurrentGraphicsY => _currentGraphicsY;

        public bool ViewportEnabled => _viewportEnabled;

        public bool ViewportRelativeCoordinates => _viewportRelativeCoordinates;

        public long ViewportLeft => _viewportLeft;

        public long ViewportTop => _viewportTop;

        public long ViewportRight => _viewportRight;

        public long ViewportBottom => _viewportBottom;

        public bool WindowEnabled => _windowEnabled;

        public bool WindowUsesScreenCoordinates => _windowUsesScreenCoordinates;

        public double WindowLeft => _windowLeft;

        public double WindowTop => _windowTop;

        public double WindowRight => _windowRight;

        public double WindowBottom => _windowBottom;

        public void ActivateScreen(int mode)
        {
            ActivateScreen(mode, activePage: 0, visualPage: 0);
        }

        public void ActivateScreen(int mode, int activePage, int visualPage)
        {
            _isScreenActive = mode != 0;
            _isCanvasActive = false;
            _canvasWidth = 0;
            _canvasHeight = 0;
            _activeScreenPage = _isScreenActive ? activePage : 0;
            _visualScreenPage = _isScreenActive ? visualPage : 0;
            if (_isScreenActive)
            {
                _screenPages.TryAdd(_activeScreenPage, []);
                _screenPages.TryAdd(_visualScreenPage, []);
                _pixels.Clear();
                foreach (var item in _screenPages[_activeScreenPage])
                {
                    _pixels[item.Key] = item.Value;
                }
            }
            ResetViewport();
            ResetWindow();
            ResetTextViewport();
            _visibleTextLines.Clear();
            _textCursorRow = 1;
            _textCursorColumn = 1;
        }

        public void ActivateCanvas(long width, long height)
        {
            _isScreenActive = false;
            _isCanvasActive = true;
            _canvasWidth = width;
            _canvasHeight = height;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            _pixels.Clear();
            _screenPages.Clear();
            ResetViewport();
            ResetWindow();
            ResetTextViewport();
            CanvasActivations.Add((width, height));
        }

        public void BeginFrame()
        {
            BeginFrameCount++;
        }

        public void PresentFrame()
        {
            PresentFrameCount++;
        }

        public void ClearFrame(int color)
        {
            FrameClears.Add(color);
        }

        public void DrawRect(long x, long y, long width, long height, int color)
        {
            DrawRects.Add(new DrawRectCall(x, y, width, height, color));
        }

        public void FillRect(long x, long y, long width, long height, int color)
        {
            FillRects.Add(new FillRectCall(x, y, width, height, color));
        }

        public void SetCanvasCamera(long x, long y)
        {
            CanvasCameraCalls.Add(new CanvasCameraCall(x, y));
        }

        public void ResetCanvasCamera()
        {
            ResetCanvasCameraCount++;
        }

        public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color)
        {
            CanvasDrawLines.Add(new CanvasLineCall(x1, y1, x2, y2, color));
        }

        public void DrawCanvasCircle(long x, long y, long radius, int color)
        {
            CanvasDrawCircles.Add(new CanvasCircleCall(x, y, radius, color));
        }

        public void FillCanvasCircle(long x, long y, long radius, int color)
        {
            CanvasFillCircles.Add(new CanvasCircleCall(x, y, radius, color));
        }

        public void DrawCanvasText(string text, long x, long y, int color)
        {
            CanvasDrawTexts.Add(new CanvasTextCall(text, x, y, color));
        }

        public void DeactivateScreen()
        {
            _isScreenActive = false;
            _isCanvasActive = false;
            _canvasWidth = 0;
            _canvasHeight = 0;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            ResetViewport();
            ResetWindow();
            ResetTextViewport();
            _visibleTextLines.Clear();
        }

        public void ClearGraphics()
        {
            _pixels.Clear();
            if (_isScreenActive)
            {
                _screenPages[_activeScreenPage] = [];
            }
            _visibleTextLines.Clear();
            _textCursorRow = _textViewportTop;
            _textCursorColumn = 1;
        }

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

        public void LocateText(int row, int column)
        {
            _textCursorRow = Math.Max(1, row);
            _textCursorColumn = Math.Max(1, column);
            _pendingTextViewportScroll = false;
        }

        public void ResetPalette()
        {
            PaletteResets++;
        }

        public void SetPaletteColor(int index, int color)
        {
            PaletteColors.Add((index, color));
        }

        public RuntimeImageValue? LoadImage(string path) => Images.TryGetValue(path, out var image) ? image : null;

        public RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight) =>
            Sprites.TryGetValue(path, out var sprite)
                ? sprite
                : Images.TryGetValue(path, out var image)
                    ? new RuntimeSpriteValue(image, frameWidth, frameHeight)
                    : null;

        public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height)
        {
            DrawImages.Add(new DrawImageCall(image, x, y, width, height));
        }

        public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
        {
            DrawSprites.Add(new DrawSpriteCall(sprite, frame, x, y, width, height));
        }

        public bool SetTextViewport(int topRow, int bottomRow)
        {
            if (!_isScreenActive || topRow < 1 || bottomRow < topRow || bottomRow > VisibleTextRows)
            {
                return false;
            }

            _textViewportTop = topRow;
            _textViewportBottom = bottomRow;
            _textCursorRow = topRow;
            _textCursorColumn = 1;
            _pendingTextViewportScroll = false;
            return true;
        }

        public void ResetTextViewport()
        {
            _textViewportTop = 1;
            _textViewportBottom = VisibleTextRows;
            _textCursorRow = 1;
            _textCursorColumn = 1;
            _pendingTextViewportScroll = false;
        }

        public void SetPixel(long x, long y, int color)
        {
            if (!TryTranslateLogicalToAbsolute(x, y, out var absoluteX, out var absoluteY))
            {
                return;
            }

            _pixels[(absoluteX, absoluteY)] = color;
            if (_isScreenActive)
            {
                _screenPages.TryAdd(_activeScreenPage, []);
                _screenPages[_activeScreenPage][(absoluteX, absoluteY)] = color;
            }
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public void SetAbsolutePixel(long x, long y, int color)
        {
            if (IsInsideScreen(x, y))
            {
                _pixels[(x, y)] = color;
                if (_isScreenActive)
                {
                    _screenPages.TryAdd(_activeScreenPage, []);
                    _screenPages[_activeScreenPage][(x, y)] = color;
                }
            }
        }

        public bool CopyScreenPage(int sourcePage, int destinationPage)
        {
            if (!_isScreenActive || sourcePage is < 0 or > 3 || destinationPage is < 0 or > 3)
            {
                return false;
            }

            _screenPages.TryAdd(sourcePage, []);
            _screenPages[destinationPage] = new Dictionary<(long X, long Y), int>(_screenPages[sourcePage]);
            if (destinationPage == _activeScreenPage)
            {
                _pixels.Clear();
                foreach (var item in _screenPages[destinationPage])
                {
                    _pixels[item.Key] = item.Value;
                }
            }

            return true;
        }

        public int GetPagePixel(int page, long x, long y) =>
            _screenPages.TryGetValue(page, out var pixels) && pixels.TryGetValue((x, y), out var color) ? color : 0;

        public void SetGraphicsCurrentPosition(long x, long y)
        {
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public int GetPixel(long x, long y)
        {
            if (!TryTranslateLogicalToAbsolute(x, y, out var absoluteX, out var absoluteY))
            {
                return 0;
            }

            return _pixels.TryGetValue((absoluteX, absoluteY), out var color) ? color : 0;
        }

        public int GetAbsolutePixel(long x, long y) =>
            _pixels.TryGetValue((x, y), out var color) ? color : 0;

        public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
        {
            if ((!_isScreenActive && !_isCanvasActive) || left > right || top > bottom)
            {
                return false;
            }

            var baseLeft = _viewportEnabled ? _viewportLeft : 0L;
            var baseTop = _viewportEnabled ? _viewportTop : 0L;
            var absoluteLeft = useScreenCoordinates ? left : GraphicsLineMath.SaturatingAdd(baseLeft, left);
            var absoluteTop = useScreenCoordinates ? top : GraphicsLineMath.SaturatingAdd(baseTop, top);
            var absoluteRight = useScreenCoordinates ? right : GraphicsLineMath.SaturatingAdd(baseLeft, right);
            var absoluteBottom = useScreenCoordinates ? bottom : GraphicsLineMath.SaturatingAdd(baseTop, bottom);
            if (!IsInsideActiveGraphicsSurface(absoluteLeft, absoluteTop) || !IsInsideActiveGraphicsSurface(absoluteRight, absoluteBottom))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            if (fillColor.HasValue)
            {
                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    for (var x = absoluteLeft; x <= absoluteRight; x++)
                    {
                        _pixels[(x, y)] = fillColor.Value;
                    }
                }
            }

            if (borderColor.HasValue)
            {
                for (var x = absoluteLeft; x <= absoluteRight; x++)
                {
                    _pixels[(x, absoluteTop)] = borderColor.Value;
                    _pixels[(x, absoluteBottom)] = borderColor.Value;
                }

                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    _pixels[(absoluteLeft, y)] = borderColor.Value;
                    _pixels[(absoluteRight, y)] = borderColor.Value;
                }
            }

            _viewportEnabled = true;
            _viewportRelativeCoordinates = !useScreenCoordinates;
            _viewportLeft = absoluteLeft;
            _viewportTop = absoluteTop;
            _viewportRight = absoluteRight;
            _viewportBottom = absoluteBottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
            return true;
        }

        public void ResetViewport()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _viewportEnabled = false;
            _viewportRelativeCoordinates = false;
            _viewportLeft = 0;
            _viewportTop = 0;
            _viewportRight = ScreenWidth - 1L;
            _viewportBottom = ScreenHeight - 1L;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates)
        {
            var viewport = _viewportEnabled
                ? new GraphicsViewport(_viewportLeft, _viewportTop, _viewportRight, _viewportBottom, _viewportRelativeCoordinates)
                : new GraphicsViewport(0, 0, ScreenWidth - 1L, ScreenHeight - 1L, UsesRelativeCoordinates: false);
            if (!_isScreenActive || viewport.Width < 2 || viewport.Height < 2)
            {
                return false;
            }

            if (!GraphicsCoordinateSpaceMath.TryCreateWindow(left, top, right, bottom, useScreenCoordinates, out var window))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = true;
            _windowUsesScreenCoordinates = window.UsesScreenCoordinates;
            _windowLeft = window.Left;
            _windowTop = window.Top;
            _windowRight = window.Right;
            _windowBottom = window.Bottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }

            return true;
        }

        public void ResetWindow()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = false;
            _windowUsesScreenCoordinates = false;
            _windowLeft = 0d;
            _windowTop = 0d;
            _windowRight = 0d;
            _windowBottom = 0d;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
        {
            Lines.Add(new LineCall(x1, y1, x2, y2, color, kind));
            GraphicsHostDrawing.DrawLine(this, x1, y1, x2, y2, color, kind);
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
            Circles.Add(new CircleCall(x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius));
            GraphicsHostDrawing.DrawCircle(this, x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius);
        }

        public void Paint(GraphicsPaintRequest request)
        {
            PaintRequests.Add(request);
            GraphicsHostDrawing.Paint(this, request);
            _currentGraphicsX = request.X;
            _currentGraphicsY = request.Y;
        }

        public void WriteGraphicsText(string text, bool newLine)
        {
            NormalizeViewportCursorBeforeWrite();
            WriteVisibleText(text);
            if (newLine)
            {
                Output.Add(text);
                AdvanceTextCursorAfterNewLine();
            }
            else
            {
                _textCursorColumn += text.Length;
            }
        }

        public void WriteOutput(string text)
        {
            Output.Add(text);
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

        private bool TryTranslateLogicalToAbsolute(long x, long y, out long absoluteX, out long absoluteY)
            => GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out absoluteX, out absoluteY);

        private void WriteVisibleText(string text)
        {
            if (_textCursorRow < 1 || _textCursorRow > VisibleTextRows)
            {
                return;
            }

            EnsureVisibleTextRow(_textCursorRow);
            var line = _visibleTextLines[_textCursorRow - 1];
            var columnIndex = Math.Max(0, _textCursorColumn - 1);
            if (columnIndex >= VisibleTextColumns)
            {
                return;
            }

            if (line.Length < columnIndex)
            {
                line = line.PadRight(columnIndex);
            }

            var buffer = new StringBuilder(line);
            while (buffer.Length < columnIndex)
            {
                buffer.Append(' ');
            }

            var visibleLength = Math.Min(text.Length, VisibleTextColumns - columnIndex);
            for (var index = 0; index < visibleLength; index++)
            {
                var targetIndex = columnIndex + index;
                if (targetIndex < buffer.Length)
                {
                    buffer[targetIndex] = text[index];
                }
                else
                {
                    buffer.Append(text[index]);
                }
            }

            _visibleTextLines[_textCursorRow - 1] = buffer.ToString().TrimEnd();
        }

        private void AdvanceTextCursorAfterNewLine()
        {
            _textCursorColumn = 1;
            if (_textCursorRow >= _textViewportTop && _textCursorRow <= _textViewportBottom)
            {
                if (_textCursorRow < _textViewportBottom)
                {
                    _textCursorRow++;
                    _pendingTextViewportScroll = false;
                    return;
                }

                _textCursorRow = _textViewportBottom + 1;
                _pendingTextViewportScroll = true;
                return;
            }

            _textCursorRow++;
            _pendingTextViewportScroll = false;
        }

        private void ScrollViewportUp()
        {
            EnsureVisibleTextRow(_textViewportBottom);
            for (var row = _textViewportTop; row < _textViewportBottom; row++)
            {
                _visibleTextLines[row - 1] = _visibleTextLines[row];
            }

            _visibleTextLines[_textViewportBottom - 1] = string.Empty;
        }

        private void EnsureVisibleTextRow(int row)
        {
            while (_visibleTextLines.Count < row)
            {
                _visibleTextLines.Add(string.Empty);
            }
        }

        private void NormalizeViewportCursorBeforeWrite()
        {
            if (!_pendingTextViewportScroll)
            {
                return;
            }

            ScrollViewportUp();
            _textCursorRow = _textViewportBottom;
            _textCursorColumn = 1;
            _pendingTextViewportScroll = false;
        }

        private (long X, long Y) ResolveCurrentAbsolutePosition()
            => GraphicsCoordinateSpaceMath.TryResolveCurrentAbsolutePosition(this, out var absoluteX, out var absoluteY)
                ? (absoluteX, absoluteY)
                : (0L, 0L);

        private bool IsInsideScreen(long x, long y) => x >= 0 && y >= 0 && x < ScreenWidth && y < ScreenHeight;

        private bool IsInsideActiveGraphicsSurface(long x, long y) => x >= 0 && y >= 0 && x < ScreenWidth && y < ScreenHeight;
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

    private sealed record DrawImageCall(
        RuntimeImageValue Image,
        long X,
        long Y,
        int Width,
        int Height);

    private sealed record DrawSpriteCall(
        RuntimeSpriteValue Sprite,
        int Frame,
        long X,
        long Y,
        int Width,
        int Height);

    private sealed record DrawRectCall(long X, long Y, long Width, long Height, int Color);

    private sealed record FillRectCall(long X, long Y, long Width, long Height, int Color);

    private sealed record CanvasCameraCall(long X, long Y);

    private sealed record CanvasLineCall(long X1, long Y1, long X2, long Y2, int Color);

    private sealed record CanvasCircleCall(long X, long Y, long Radius, int Color);

    private sealed record CanvasTextCall(string Text, long X, long Y, int Color);
}
