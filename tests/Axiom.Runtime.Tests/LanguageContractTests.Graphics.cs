using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
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
    public void Execute_PSetStepSaturatesOverflowingCoordinates()
    {
        const string text = """
SCREEN 1
PSET (1, 1), 15
PSET STEP (9223372036854775807, 0), 15
PSET STEP (5, 4), 15
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(15, host.GetPixel(1, 1));
        Assert.Equal(0, host.GetPixel(long.MaxValue, 1));
        Assert.Equal(15, host.GetPixel(6, 5));
        Assert.Equal(6, host.CurrentGraphicsX);
        Assert.Equal(5, host.CurrentGraphicsY);
    }

    [Fact]
    public void Execute_PSetStepSaturatesNegativeOverflow()
    {
        const string text = """
SCREEN 1
PAINT STEP (-9223372036854775807, -2), 15
PSET STEP (-10, 0), 15
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(-9223372036854775807, host.CurrentGraphicsX);
        Assert.Equal(-2, host.CurrentGraphicsY);
        Assert.Equal(0, host.GetPixel(0, 0));
    }

    [Fact]
    public void Execute_PaintStepSaturatesOverflowingCoordinates()
    {
        const string text = """
SCREEN 1
PSET (1, 1), 15
PAINT STEP (9223372036854775807, 0), 15
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var request = Assert.Single(host.PaintRequests);
        Assert.Equal(long.MaxValue, request.X);
        Assert.Equal(1, request.Y);
        Assert.Equal(long.MaxValue, host.CurrentGraphicsX);
        Assert.Equal(1, host.CurrentGraphicsY);
        Assert.Equal(15, host.GetPixel(1, 1));
    }

    [Fact]
    public void Execute_PaintStepSaturatesNegativeOverflow()
    {
        const string text = """
SCREEN 1
PAINT STEP (-9223372036854775807, -2), 15
PAINT STEP (-10, 0), 15
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, host.PaintRequests.Count);
        Assert.Equal(long.MinValue, host.PaintRequests[1].X);
        Assert.Equal(-2, host.PaintRequests[1].Y);
        Assert.Equal(long.MinValue, host.CurrentGraphicsX);
        Assert.Equal(-2, host.CurrentGraphicsY);
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
    public void Execute_PaletteUsingUnallocatedArrayAbortsExecution()
    {
        const string text = """
SCREEN 1
DIM colors() AS INTEGER
PALETTE USING colors
PRINT "after"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Empty(host.Output);
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
    public void Execute_TilesetWidthHeightAndCountReturnLoadedAndEmptySizes()
    {
        const string text = """
DIM emptyTiles AS TILESET
PRINT TILECOUNT(emptyTiles)
PRINT TILEWIDTH(emptyTiles)
PRINT TILEHEIGHT(emptyTiles)
DIM plainTiles AS TILESET
DIM marginTiles AS TILESET
DIM spacedTiles AS TILESET
plainTiles = LOADTILESET("plain", 16, 8)
marginTiles = LOADTILESET("margin", 16, 8, 1)
spacedTiles = LOADTILESET("spaced", 16, 8, 1, 2)
PRINT TILECOUNT(plainTiles)
PRINT TILECOUNT(marginTiles)
PRINT TILECOUNT(spacedTiles)
PRINT TILEWIDTH(spacedTiles)
PRINT TILEHEIGHT(spacedTiles)
""";

        var host = new TestRuntimeHost([]);
        host.Images["plain"] = CreateTestImage(width: 32, height: 16);
        host.Images["margin"] = CreateTestImage(width: 34, height: 18);
        host.Images["spaced"] = CreateTestImage(width: 36, height: 20);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "0", "0", "4", "4", "4", "16", "8"], host.Output);
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
    public void Execute_DrawTileCallsModernGraphicsHostAndUpdatesCurrentPosition()
    {
        const string text = """
CANVAS 160, 90
DIM tiles AS TILESET
tiles = LOADTILESET("terrain", 16, 10, 1, 2)
DRAWTILE tiles, 4, 30, 40
""";

        var image = CreateTestImage(width: 36, height: 24);
        var host = new TestRuntimeHost([]);
        host.Images["terrain"] = image;

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                new DrawImageRegionCall(image, 19, 13, 16, 10, 30, 40, 16, 10)
            ],
            host.DrawImageRegions);
        Assert.Equal(30, host.CurrentGraphicsX);
        Assert.Equal(40, host.CurrentGraphicsY);
    }

    [Fact]
    public void Execute_TilemapFunctionsReturnMapMetrics()
    {
        const string text = """
DIM level AS TILEMAP
level = LOADTILEMAP("level")
PRINT MAPWIDTH(level)
PRINT MAPHEIGHT(level)
PRINT MAPPIXELWIDTH(level)
PRINT MAPPIXELHEIGHT(level)
PRINT MAPTILEWIDTH(level)
PRINT MAPTILEHEIGHT(level)
PRINT MAPLAYERCOUNT(level)
DIM empty AS TILEMAP
PRINT MAPWIDTH(empty)
""";

        var host = new TestRuntimeHost([]);
        host.Tilemaps["level"] = CreateTestTilemap(CreateTestImage(48, 8));

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "32", "16", "16", "8", "3", "0"], host.Output);
    }

    [Fact]
    public void Execute_MapTileFunctionsReadCellsAndEmptyOutside()
    {
        const string text = """
DIM level AS TILEMAP
level = LOADTILEMAP("level")
PRINT MAPTILE(level, 1, 1, 1)
PRINT MAPTILESET(level, 1, 1, 1)
PRINT MAPTILEFLAGS(level, 1, 1, 1)
PRINT MAPTILE(level, 1, 1, 2)
PRINT MAPTILE(level, 1, 3, 1)
PRINT MAPTILE(level, 3, 1, 1)
DIM empty AS TILEMAP
PRINT MAPTILE(empty, 1, 1, 1)
""";

        var host = new TestRuntimeHost([]);
        host.Tilemaps["level"] = CreateTestTilemap(CreateTestImage(48, 8));

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "1", "0", "0", "0", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_SetMapTileWritesClearsAndStoresFlags()
    {
        const string text = """
DIM level AS TILEMAP
level = LOADTILEMAP("level")
SETMAPTILE level, 1, 1, 2, 1, 2, 3
PRINT MAPTILE(level, 1, 1, 2)
PRINT MAPTILESET(level, 1, 1, 2)
PRINT MAPTILEFLAGS(level, 1, 1, 2)
SETMAPTILE level, 1, 1, 2, 0, 0
PRINT MAPTILE(level, 1, 1, 2)
""";

        var host = new TestRuntimeHost([]);
        host.Tilemaps["level"] = CreateTestTilemap(CreateTestImage(48, 8));

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "1", "3", "0"], host.Output);
    }

    [Fact]
    public void Execute_SetMapTileRejectsObjectLayerAndOutsideCell()
    {
        var host = new TestRuntimeHost([]);
        host.Tilemaps["level"] = CreateTestTilemap(CreateTestImage(48, 8));

        var (_, _, objectLayerResult) = ExecuteProgram("""
DIM level AS TILEMAP
level = LOADTILEMAP("level")
SETMAPTILE level, 3, 1, 1, 1, 1
""", host);

        Assert.False(objectLayerResult.Succeeded);
        Assert.Contains(objectLayerResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        host = new TestRuntimeHost([]);
        host.Tilemaps["level"] = CreateTestTilemap(CreateTestImage(48, 8));
        var (_, _, outsideResult) = ExecuteProgram("""
DIM level AS TILEMAP
level = LOADTILEMAP("level")
SETMAPTILE level, 1, 3, 1, 1, 1
""", host);

        Assert.False(outsideResult.Succeeded);
        Assert.Contains(outsideResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
    }

    [Fact]
    public void Execute_DrawTilemapDrawsVisibleTileLayersAndUpdatesCurrentPosition()
    {
        const string text = """
CANVAS 160, 90
DIM level AS TILEMAP
level = LOADTILEMAP("level")
DRAWTILEMAP level, 10, 20
""";

        var image = CreateTestImage(48, 8);
        var host = new TestRuntimeHost([]);
        host.Tilemaps["level"] = CreateTestTilemap(image);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                new DrawImageRegionCall(image, 0, 0, 16, 8, 10, 20, 16, 8),
                new DrawImageRegionCall(image, 16, 0, 16, 8, 26, 20, 16, 8),
                new DrawImageRegionCall(image, 32, 0, 16, 8, 26, 28, 16, 8)
            ],
            host.DrawImageRegions);
        Assert.Equal(10, host.CurrentGraphicsX);
        Assert.Equal(20, host.CurrentGraphicsY);
    }

    [Fact]
    public void Execute_DrawTileLayerDrawsRequestedTileLayerEvenWhenHidden()
    {
        const string text = """
CANVAS 160, 90
DIM level AS TILEMAP
level = LOADTILEMAP("level")
DRAWTILELAYER level, 2, 10, 20
""";

        var image = CreateTestImage(48, 8);
        var host = new TestRuntimeHost([]);
        host.Tilemaps["level"] = CreateTestTilemap(image);

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(3, host.DrawImageRegions.Count);
        Assert.Equal(new DrawImageRegionCall(image, 0, 0, 16, 8, 10, 20, 16, 8), host.DrawImageRegions[0]);
        Assert.Equal(10, host.CurrentGraphicsX);
        Assert.Equal(20, host.CurrentGraphicsY);
    }

    [Fact]
    public void Execute_DrawTileLayerRejectsObjectLayer()
    {
        const string text = """
CANVAS 160, 90
DIM level AS TILEMAP
level = LOADTILEMAP("level")
DRAWTILELAYER level, 3, 10, 20
""";

        var host = new TestRuntimeHost([]);
        host.Tilemaps["level"] = CreateTestTilemap(CreateTestImage(48, 8));

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");
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
    public void Execute_TilesetOperationsReportDocumentedDiagnostics()
    {
        var (_, _, emptyPathResult) = ExecuteProgram("PRINT LOADTILESET(\"\", 16, 8)");
        Assert.False(emptyPathResult.Succeeded);
        Assert.Contains(emptyPathResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, invalidSizeResult) = ExecuteProgram("PRINT LOADTILESET(\"terrain\", 0, 8)");
        Assert.False(invalidSizeResult.Succeeded);
        Assert.Contains(invalidSizeResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, invalidMarginResult) = ExecuteProgram("PRINT LOADTILESET(\"terrain\", 16, 8, -1)");
        Assert.False(invalidMarginResult.Succeeded);
        Assert.Contains(invalidMarginResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var (_, _, missingTilesetResult) = ExecuteProgram("PRINT LOADTILESET(\"missing\", 16, 8)");
        Assert.False(missingTilesetResult.Succeeded);
        Assert.Contains(missingTilesetResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        var invalidGridHost = new TestRuntimeHost([]);
        invalidGridHost.Images["bad-grid"] = CreateTestImage(width: 35, height: 24);
        var (_, _, invalidGridResult) = ExecuteProgram("PRINT LOADTILESET(\"bad-grid\", 16, 10, 1, 2)", invalidGridHost);
        Assert.False(invalidGridResult.Succeeded);
        Assert.Contains(invalidGridResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        var (_, _, emptyDrawResult) = ExecuteProgram("""
CANVAS 160, 90
DIM tiles AS TILESET
DRAWTILE tiles, 1, 0, 0
""");
        Assert.False(emptyDrawResult.Succeeded);
        Assert.Contains(emptyDrawResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3005");

        var invalidTileHost = new TestRuntimeHost([]);
        invalidTileHost.Images["terrain"] = CreateTestImage(width: 32, height: 16);
        var (_, _, invalidTileResult) = ExecuteProgram("""
CANVAS 160, 90
DIM tiles AS TILESET
tiles = LOADTILESET("terrain", 16, 8)
DRAWTILE tiles, 5, 0, 0
""", invalidTileHost);
        Assert.False(invalidTileResult.Succeeded);
        Assert.Contains(invalidTileResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");

        var noCanvasHost = new TestRuntimeHost([]);
        noCanvasHost.Images["terrain"] = CreateTestImage(width: 32, height: 16);
        var (_, _, noCanvasResult) = ExecuteProgram("""
SCREEN 1
DIM tiles AS TILESET
tiles = LOADTILESET("terrain", 16, 8)
DRAWTILE tiles, 1, 0, 0
""", noCanvasHost);
        Assert.False(noCanvasResult.Succeeded);
        Assert.Contains(noCanvasResult.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
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
    public void Execute_PointReadsPhysicalCanvasPixelsWithoutViewportOrCameraReprojection()
    {
        const string text = """
CANVAS 16, 16
CLS RGB(0, 0, 0)
VIEWPORT (8, 8)-(16, 16)
CAMERA 4, 4
FILLRECT 8, 8, 1, 1, RGB(255, 255, 255)
PRINT POINT(8, 8)
PRINT POINT(12, 12)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "15"], host.Output);
        Assert.Equal(0, host.GetAbsolutePixel(8, 8));
        Assert.Equal(15, host.GetAbsolutePixel(12, 12));
    }

    [Fact]
    public void Execute_PointSelectorStillRequiresScreenWhenCanvasIsActive()
    {
        var (_, _, result) = ExecuteProgram("""
CANVAS 16, 16
PRINT POINT(0)
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_PSetWritesCanvasPixelAtPhysicalViewportPoint()
    {
        const string text = """
CANVAS 16, 16
CLS RGB(0, 0, 0)
PSET (2, 3), 12
PRINT POINT(2, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
        Assert.Equal(12, host.GetAbsolutePixel(2, 3));
    }

    [Fact]
    public void Execute_PSetUsesCanvasWorldCoordinatesWithViewportAndCamera()
    {
        const string text = """
CANVAS 24, 18
CLS RGB(0, 0, 0)
VIEWPORT (10, 6)-(14, 10)
CAMERA 100, 50
PSET (100, 50), 12
PRINT POINT(10, 6)
PRINT POINT(9, 6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12", "0"], host.Output);
        Assert.Equal(12, host.GetAbsolutePixel(10, 6));
        Assert.Equal(0, host.GetAbsolutePixel(9, 6));
    }

    [Fact]
    public void Execute_PSetStillRequiresScreenOrCanvas()
    {
        var (_, _, result) = ExecuteProgram("""
PSET (2, 3), 12
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_PResetWritesCanvasBackgroundPixelAtPhysicalViewportPoint()
    {
        const string text = """
CANVAS 16, 16
CLS RGB(255, 255, 255)
PRESET (2, 3)
PRINT POINT(2, 3)
PRINT POINT(1, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "15"], host.Output);
        Assert.Equal(0, host.GetAbsolutePixel(2, 3));
        Assert.Equal(15, host.GetAbsolutePixel(1, 3));
    }

    [Fact]
    public void Execute_PResetUsesCanvasWorldCoordinatesWithViewportAndCamera()
    {
        const string text = """
CANVAS 24, 18
CLS RGB(0, 0, 0)
VIEWPORT (10, 6)-(14, 10)
CAMERA 100, 50
PRESET (100, 50), 12
PRINT POINT(10, 6)
PRINT POINT(9, 6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12", "0"], host.Output);
        Assert.Equal(12, host.GetAbsolutePixel(10, 6));
        Assert.Equal(0, host.GetAbsolutePixel(9, 6));
    }

    [Fact]
    public void Execute_PResetStillRequiresScreenOrCanvas()
    {
        var (_, _, result) = ExecuteProgram("""
PRESET (2, 3)
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
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
    public void Execute_GetAndPutSupportCanvasViewportAndCamera()
    {
        const string text = """
CANVAS 24, 18
CLS RGB(0, 0, 0)
VIEWPORT (10, 6)-(15, 11)
CAMERA 100, 50
DIM buffer(1 TO 2, 5 TO 6) AS INTEGER
PSET (100, 50), 6
PSET (101, 50), 9
PSET (100, 51), 12
PSET (101, 51), 15
GET (100, 50)-(101, 51), buffer
PRESET (100, 50), 0
PRESET (101, 50), 0
PRESET (100, 51), 0
PRESET (101, 51), 0
PUT (100, 50), buffer, PSET
PRINT buffer(1, 5)
PRINT buffer(2, 5)
PRINT buffer(1, 6)
PRINT buffer(2, 6)
PRINT POINT(10, 6)
PRINT POINT(11, 6)
PRINT POINT(10, 7)
PRINT POINT(11, 7)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6", "9", "12", "15", "6", "9", "12", "15"], host.Output);
        Assert.Equal(6, host.GetAbsolutePixel(10, 6));
        Assert.Equal(9, host.GetAbsolutePixel(11, 6));
        Assert.Equal(12, host.GetAbsolutePixel(10, 7));
        Assert.Equal(15, host.GetAbsolutePixel(11, 7));
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
    public void Execute_GetStillRequiresScreenOrCanvas()
    {
        var (_, _, result) = ExecuteProgram("""
DIM buffer(1 TO 2, 1 TO 2) AS INTEGER
GET (0, 0)-(1, 1), buffer
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
    }

    [Fact]
    public void Execute_PutStillRequiresScreenOrCanvas()
    {
        var (_, _, result) = ExecuteProgram("""
DIM buffer(1 TO 2, 1 TO 1) AS INTEGER
PUT (0, 0), buffer
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
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
    public void Execute_PaintFillsCanvasRegionWithViewportAndCamera()
    {
        const string text = """
CANVAS 24, 18
CLS RGB(0, 0, 0)
VIEWPORT (10, 6)-(15, 11)
CAMERA 100, 50
PSET (100, 50), 9
PSET (101, 50), 9
PSET (102, 50), 9
PSET (100, 51), 9
PSET (102, 51), 9
PSET (100, 52), 9
PSET (101, 52), 9
PSET (102, 52), 9
PAINT (101, 51), 6, 9
PRINT POINT(11, 7)
PRINT POINT(10, 6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(6, host.GetAbsolutePixel(11, 7));
        Assert.Equal(9, host.GetAbsolutePixel(10, 6));
        Assert.Equal(["6", "9"], host.Output);
    }

    [Fact]
    public void Execute_PaintStillRequiresScreenOrCanvas()
    {
        var (_, _, result) = ExecuteProgram("""
PAINT (3, 3), 6, 9
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3010");
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
    public void Execute_PaintWithEmptyPatternStringReportsArgumentDiagnostic()
    {
        var (_, _, result) = ExecuteProgram("""
SCREEN 1
PAINT (1, 1), ""
""");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB3007");
    }

    [Fact]
    public void Execute_CreatePatternWithEmptyRowsThrowsArgumentOutOfRange()
    {
        var fillException = Assert.Throws<ArgumentOutOfRangeException>(
            () => GraphicsPaintRequest.CreatePattern(0, 0, Array.Empty<byte>(), 15, 7, 0));
        Assert.Equal("fillPatternRows", fillException.ParamName);
        Assert.Contains("PAINT", fillException.Message, StringComparison.OrdinalIgnoreCase);

        var backgroundException = Assert.Throws<ArgumentOutOfRangeException>(
            () => GraphicsPaintRequest.CreatePattern(0, 0, [0b11111111], 15, 7, 0, Array.Empty<byte>()));
        Assert.Equal("backgroundPatternRows", backgroundException.ParamName);
        Assert.Contains("PAINT", backgroundException.Message, StringComparison.OrdinalIgnoreCase);
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
    public void Execute_OnMousePassesCanvasWorldCoordinates()
    {
        const string text = """
CANVAS 160, 90
VIEWPORT (10, 12)-(30, 28)
CAMERA 100, 50
ON MOUSE(3), HandleMouse
DIM marker AS INTEGER = 1

SUB HandleMouse(button AS INTEGER, x AS LONG, y AS LONG)
    PRINT button
    PRINT x
    PRINT y
END SUB
""";

        var (_, host, result) = ExecuteProgramWithMouseState(text, mouseX: 15, mouseY: 16, mousePresses: [3]);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "105", "54"], host.Output);
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
    public void Execute_OnMouseCoordinatesMatchMouseFunctionsDuringActiveCanvas()
    {
        const string text = """
CANVAS 160, 90
VIEWPORT (10, 12)-(30, 28)
CAMERA 100, 50
DIM handled AS BOOLEAN = FALSE
DIM eventX AS LONG = 0
DIM eventY AS LONG = 0
DIM readX AS LONG = 0
DIM readY AS LONG = 0
ON MOUSE("MOVE"), HandleMouse
DIM marker AS INTEGER = 1
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
            mouseX: 15,
            mouseY: 16,
            mouseEvents: [new MouseInputEvent(MouseInputEventKind.Move, 0)]);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "105", "105", "54", "54"], host.Output);
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

}
