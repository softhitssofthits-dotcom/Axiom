using Axiom.Ide.Editor.Formatting;

namespace Axiom.Ide.Tests;

public sealed class CodeFormattingServiceTests
{
    private readonly AxiomCodeFormattingService _service = new();

    [Fact]
    public void FormatLine_Classic_UppercasesKeywordsAndAddsOperatorSpacing()
    {
        var result = _service.FormatLine("dim a as integer=1", CodeFormattingStyle.Classic);

        Assert.Equal("DIM a AS INTEGER = 1", result);
    }

    [Fact]
    public void FormatLine_Classic_FormatsPrintExpression()
    {
        var result = _service.FormatLine("print a=b*2", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT a = b * 2", result);
    }

    [Fact]
    public void FormatLine_Classic_UppercasesLogicalOperatorKeywords()
    {
        var result = _service.FormatLine("print a eqv b imp c", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT a EQV b IMP c", result);
    }

    [Fact]
    public void FormatLine_Classic_UppercasesBuiltInCallsButNotPlainIdentifiers()
    {
        var builtInCall = _service.FormatLine("print len(\"x\")+1", CodeFormattingStyle.Classic);
        var plainIdentifier = _service.FormatLine("len=1", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT LEN(\"x\") + 1", builtInCall);
        Assert.Equal("len = 1", plainIdentifier);
    }

    [Fact]
    public void FormatLine_Classic_PreservesStringsAndUppercasesRemComments()
    {
        var result = _service.FormatLine("print \"rem stays\" rem note", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT \"rem stays\" REM note", result);
    }

    [Fact]
    public void FormatLine_Classic_LeavesMalformedStringUnchanged()
    {
        const string source = "print \"unterminated";

        var result = _service.FormatLine(source, CodeFormattingStyle.Classic);

        Assert.Equal(source, result);
    }

    [Fact]
    public void FormatLine_Modern_UsesTitleCaseForKeywordsTypesAndBuiltIns()
    {
        var result = _service.FormatLine("dim a as integer= len(\"x\")+1", CodeFormattingStyle.Modern);

        Assert.Equal("Dim a As Integer = Len(\"x\") + 1", result);
    }

    [Fact]
    public void FormatLine_Modern_NormalizesExtraSpacesAndRemComment()
    {
        var result = _service.FormatLine("   print   a  +   3   rem note", CodeFormattingStyle.Modern);

        Assert.Equal("   Print a + 3   Rem note", result);
    }

    [Fact]
    public void FormatDocument_Modern_FormatsAllLinesAndPreservesLineEndings()
    {
        const string source = "print len(\"x\")+1\r\ndim a as integer=1\r\n";

        var result = _service.FormatDocument(source, CodeFormattingStyle.Modern);

        Assert.Equal("Print Len(\"x\") + 1\r\nDim a As Integer = 1\r\n", result);
    }

    [Fact]
    public void FormatLine_Modern_FormatsFileKeywordsTypesAndBuiltIns()
    {
        var fileDeclaration = _service.FormatLine("dim f as file", CodeFormattingStyle.Modern);
        var fileStatement = _service.FormatLine("open path for output as f", CodeFormattingStyle.Modern);
        var builtInCall = _service.FormatLine("print copyfile(sourcePath,destinationPath,true)", CodeFormattingStyle.Modern);
        var arrayStatement = _service.FormatLine("redim preserve items(1 to 0)", CodeFormattingStyle.Modern);

        Assert.Equal("Dim f As File", fileDeclaration);
        Assert.Equal("Open path For Output As f", fileStatement);
        Assert.Equal("Print Copyfile(sourcePath, destinationPath, True)", builtInCall);
        Assert.Equal("Redim Preserve items(1 To 0)", arrayStatement);
    }

    [Fact]
    public void FormatLine_Classic_FormatsScreenKeywordsAndBuiltIns()
    {
        var screenStatement = _service.FormatLine("screen 5", CodeFormattingStyle.Classic);
        var graphicsStatement = _service.FormatLine("pset 10,20,color", CodeFormattingStyle.Classic);
        var sleepStatement = _service.FormatLine("sleep 500", CodeFormattingStyle.Classic);
        var builtInCall = _service.FormatLine("print screenmode(),screenwidth(),screenheight()", CodeFormattingStyle.Classic);

        Assert.Equal("SCREEN 5", screenStatement);
        Assert.Equal("PSET 10, 20, COLOR", graphicsStatement);
        Assert.Equal("SLEEP 500", sleepStatement);
        Assert.Equal("PRINT SCREENMODE(), SCREENWIDTH(), SCREENHEIGHT()", builtInCall);
    }

    [Fact]
    public void FormatLine_FormatsImageKeywordsTypesAndBuiltIns()
    {
        var classicDeclaration = _service.FormatLine("dim logo as image", CodeFormattingStyle.Classic);
        var classicLoad = _service.FormatLine("logo=loadimage(path)", CodeFormattingStyle.Classic);
        var classicDraw = _service.FormatLine("drawimage logo,16,24,64,64", CodeFormattingStyle.Classic);
        var modernDeclaration = _service.FormatLine("dim logo as image", CodeFormattingStyle.Modern);
        var modernSize = _service.FormatLine("print imagewidth(logo),imageheight(logo)", CodeFormattingStyle.Modern);
        var modernDraw = _service.FormatLine("drawimage logo,16,24", CodeFormattingStyle.Modern);

        Assert.Equal("DIM logo AS IMAGE", classicDeclaration);
        Assert.Equal("logo = LOADIMAGE(path)", classicLoad);
        Assert.Equal("DRAWIMAGE logo, 16, 24, 64, 64", classicDraw);
        Assert.Equal("Dim logo As Image", modernDeclaration);
        Assert.Equal("Print Imagewidth(logo), Imageheight(logo)", modernSize);
        Assert.Equal("Drawimage logo, 16, 24", modernDraw);
    }

    [Fact]
    public void FormatLine_FormatsSpriteKeywordsTypesAndBuiltIns()
    {
        var classicDeclaration = _service.FormatLine("dim walk as sprite", CodeFormattingStyle.Classic);
        var classicLoad = _service.FormatLine("walk=loadsprite(path,16,24)", CodeFormattingStyle.Classic);
        var classicDraw = _service.FormatLine("drawsprite walk,1,16,24,32,48", CodeFormattingStyle.Classic);
        var modernDeclaration = _service.FormatLine("dim walk as sprite", CodeFormattingStyle.Modern);
        var modernSize = _service.FormatLine("print spritewidth(walk),spriteheight(walk),spritecount(walk)", CodeFormattingStyle.Modern);
        var modernDraw = _service.FormatLine("drawsprite walk,1,16,24", CodeFormattingStyle.Modern);

        Assert.Equal("DIM walk AS SPRITE", classicDeclaration);
        Assert.Equal("walk = LOADSPRITE(path, 16, 24)", classicLoad);
        Assert.Equal("DRAWSPRITE walk, 1, 16, 24, 32, 48", classicDraw);
        Assert.Equal("Dim walk As Sprite", modernDeclaration);
        Assert.Equal("Print Spritewidth(walk), Spriteheight(walk), Spritecount(walk)", modernSize);
        Assert.Equal("Drawsprite walk, 1, 16, 24", modernDraw);
    }

    [Fact]
    public void FormatLine_FormatsCanvasKeywordsAndRgbBuiltIn()
    {
        var canvas = _service.FormatLine("canvas 320,180", CodeFormattingStyle.Classic);
        var frame = _service.FormatLine("beginframe", CodeFormattingStyle.Classic);
        var clear = _service.FormatLine("cls rgb(18,22,30)", CodeFormattingStyle.Classic);
        var draw = _service.FormatLine("drawrect 1,2,3,4,rgb(255,0,0)", CodeFormattingStyle.Classic);
        var fill = _service.FormatLine("fillrect 5,6,7,8,rgb(0,255,0)", CodeFormattingStyle.Modern);
        var present = _service.FormatLine("present", CodeFormattingStyle.Modern);

        Assert.Equal("CANVAS 320, 180", canvas);
        Assert.Equal("BEGINFRAME", frame);
        Assert.Equal("CLS RGB(18, 22, 30)", clear);
        Assert.Equal("DRAWRECT 1, 2, 3, 4, RGB(255, 0, 0)", draw);
        Assert.Equal("Fillrect 5, 6, 7, 8, Rgb(0, 255, 0)", fill);
        Assert.Equal("Present", present);
    }

    [Fact]
    public void FormatLine_FormatsTilesetKeywordsTypesAndBuiltIns()
    {
        var classicDeclaration = _service.FormatLine("dim tiles as tileset", CodeFormattingStyle.Classic);
        var classicLoad = _service.FormatLine("tiles=loadtileset(path,16,16,1,2)", CodeFormattingStyle.Classic);
        var classicDraw = _service.FormatLine("drawtile tiles,4,88,40", CodeFormattingStyle.Classic);
        var modernDeclaration = _service.FormatLine("dim tiles as tileset", CodeFormattingStyle.Modern);
        var modernInfo = _service.FormatLine("print tilecount(tiles),tilewidth(tiles),tileheight(tiles)", CodeFormattingStyle.Modern);

        Assert.Equal("DIM tiles AS TILESET", classicDeclaration);
        Assert.Equal("tiles = LOADTILESET(path, 16, 16, 1, 2)", classicLoad);
        Assert.Equal("DRAWTILE tiles, 4, 88, 40", classicDraw);
        Assert.Equal("Dim tiles As Tileset", modernDeclaration);
        Assert.Equal("Print Tilecount(tiles), Tilewidth(tiles), Tileheight(tiles)", modernInfo);
    }

    [Fact]
    public void FormatLine_FormatsTilemapKeywordsTypesAndBuiltIns()
    {
        var classicDeclaration = _service.FormatLine("dim level as tilemap", CodeFormattingStyle.Classic);
        var classicLoad = _service.FormatLine("level=loadtilemap(path)", CodeFormattingStyle.Classic);
        var classicDraw = _service.FormatLine("drawtilemap level,32,48", CodeFormattingStyle.Classic);
        var classicLayer = _service.FormatLine("drawtilelayer level,2,32,48", CodeFormattingStyle.Classic);
        var classicSet = _service.FormatLine("setmaptile level,1,2,2,1,4,3", CodeFormattingStyle.Classic);
        var modernDeclaration = _service.FormatLine("dim level as tilemap", CodeFormattingStyle.Modern);
        var modernInfo = _service.FormatLine("print mapwidth(level),maptile(level,1,1,1),maptileflags(level,1,1,1)", CodeFormattingStyle.Modern);

        Assert.Equal("DIM level AS TILEMAP", classicDeclaration);
        Assert.Equal("level = LOADTILEMAP(path)", classicLoad);
        Assert.Equal("DRAWTILEMAP level, 32, 48", classicDraw);
        Assert.Equal("DRAWTILELAYER level, 2, 32, 48", classicLayer);
        Assert.Equal("SETMAPTILE level, 1, 2, 2, 1, 4, 3", classicSet);
        Assert.Equal("Dim level As Tilemap", modernDeclaration);
        Assert.Equal("Print Mapwidth(level), Maptile(level, 1, 1, 1), Maptileflags(level, 1, 1, 1)", modernInfo);
    }

    [Fact]
    public void FormatLine_Classic_FormatsShiftPowerAndIntegerDivisionOperators()
    {
        var result = _service.FormatLine("print(a<<2)+(b>>1)+2^3+7\\2", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT (a << 2) + (b >> 1) + 2 ^ 3 + 7 \\ 2", result);
    }

    [Fact]
    public void FormatLine_Classic_CollapsesSplitShiftOperatorsWithoutInnerSpaces()
    {
        var result = _service.FormatLine("print a <    < 2, b >       > 1", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT a << 2, b >> 1", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesTabAlignedCommentGap()
    {
        var result = _service.FormatLine("DIM TILE AS INTEGER = 16\t\t' Размер клетки", CodeFormattingStyle.Classic);

        Assert.Equal("DIM TILE AS INTEGER = 16\t\t' Размер клетки", result);
    }

    [Fact]
    public void FormatLine_Modern_PreservesTabAlignedCommentGap()
    {
        var result = _service.FormatLine("DIM TILE AS INTEGER = 16\t\t' Размер клетки", CodeFormattingStyle.Modern);

        Assert.Equal("Dim TILE As Integer = 16\t\t' Размер клетки", result);
    }

    [Fact]
    public void FormatLine_Modern_PreservesSpaceAlignedCommentGap()
    {
        var result = _service.FormatLine("DIM TILE AS INTEGER = 16     ' Размер клетки", CodeFormattingStyle.Modern);

        Assert.Equal("Dim TILE As Integer = 16     ' Размер клетки", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesSingleSpaceCommentGap()
    {
        var result = _service.FormatLine("print a+1 ' comment", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT a + 1 ' comment", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesRemCommentGap()
    {
        var result = _service.FormatLine("DIM X AS INTEGER\t\tREM note", CodeFormattingStyle.Classic);

        Assert.Equal("DIM X AS INTEGER\t\tREM note", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesCyrillicIdentifiers()
    {
        var result = _service.FormatLine("dim счётчик as integer=42", CodeFormattingStyle.Classic);

        Assert.Equal("DIM счётчик AS INTEGER = 42", result);
    }

    [Fact]
    public void FormatLine_Modern_PreservesCyrillicIdentifiers()
    {
        var result = _service.FormatLine("dim имя as string=\"привет\"", CodeFormattingStyle.Modern);

        Assert.Equal("Dim имя As String = \"привет\"", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesCyrillicInStringsAndComments()
    {
        var result = _service.FormatLine("print \"Привет, мир!\" ' комментарий", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT \"Привет, мир!\" ' комментарий", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesEmojiInStrings()
    {
        var result = _service.FormatLine("print \"Hello 🌍 World\"", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT \"Hello 🌍 World\"", result);
    }

    [Fact]
    public void FormatLine_Classic_HandlesMixedCyrillicAndLatinIdentifiers()
    {
        var result = _service.FormatLine("print myVar+счётчик*2", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT myVar + счётчик * 2", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesNonBmpUnicodeInStrings()
    {
        var result = _service.FormatLine("print \"𝕳𝖊𝖑𝖑𝖔\"", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT \"𝕳𝖊𝖑𝖑𝖔\"", result);
    }

    [Fact]
    public void FormatLine_Classic_HandlesCyrillicKeywordLikePrefix()
    {
        var result = _service.FormatLine("print формат+1", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT формат + 1", result);
    }

    [Fact]
    public void FormatDocument_Classic_PreservesCyrillicAcrossMultipleLines()
    {
        const string source = "dim имя as string=\"Алиса\"\r\nprint имя\r\n";

        var result = _service.FormatDocument(source, CodeFormattingStyle.Classic);

        Assert.Equal("DIM имя AS STRING = \"Алиса\"\r\nPRINT имя\r\n", result);
    }

    [Fact]
    public void FormatLine_Classic_DimLineWithoutEquals()
    {
        var result = _service.FormatLine("dim a as integer", CodeFormattingStyle.Classic);

        Assert.Equal("DIM a AS INTEGER", result);
    }

    [Fact]
    public void IsLineSyntaxValid_DimLineWithoutEquals_ReturnsTrue()
    {
        Assert.True(_service.IsLineSyntaxValid("dim a as integer"));
    }

    [Fact]
    public void IsLineSyntaxValid_DimLineWithTrailingWhitespace_ReturnsTrue()
    {
        Assert.True(_service.IsLineSyntaxValid("dim a as integer  "));
    }

    [Fact]
    public void FormatLine_Classic_DimLineWithTrailingWhitespace()
    {
        var result = _service.FormatLine("dim a as integer  ", CodeFormattingStyle.Classic);

        Assert.Equal("DIM a AS INTEGER", result);
    }

    [Fact]
    public void FormatLine_Classic_DimWithEquals()
    {
        var result = _service.FormatLine("dim a as integer=1", CodeFormattingStyle.Classic);

        Assert.Equal("DIM a AS INTEGER = 1", result);
    }

    [Fact]
    public void IsLineSyntaxValid_DimWithEquals_ReturnsTrue()
    {
        Assert.True(_service.IsLineSyntaxValid("dim a as integer=1"));
    }

    [Fact]
    public void FormatLine_Classic_PreservesInterpolatedString()
    {
        var result = _service.FormatLine("print $\"Привет, {name}!\"", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT $\"Привет, {name}!\"", result);
    }

    [Fact]
    public void FormatLine_Modern_PreservesInterpolatedString()
    {
        var result = _service.FormatLine("print $\"Привет, {name}!\"", CodeFormattingStyle.Modern);

        Assert.Equal("Print $\"Привет, {name}!\"", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesInterpolatedStringWithExpressions()
    {
        var result = _service.FormatLine("print $\"{a} + {b} = {a + b}\"", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT $\"{a} + {b} = {a + b}\"", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesInterpolatedStringRepeatedFormatting()
    {
        var line = "print $\"Привет, {name}!\"";
        var result1 = _service.FormatLine(line, CodeFormattingStyle.Classic);
        var result2 = _service.FormatLine(result1, CodeFormattingStyle.Classic);
        var result3 = _service.FormatLine(result2, CodeFormattingStyle.Classic);

        Assert.Equal("PRINT $\"Привет, {name}!\"", result1);
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    [Fact]
    public void FormatLine_Classic_FormatsBuiltInInsideInterpolation()
    {
        var result = _service.FormatLine("print $\"Длина = {len(s)}\"", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT $\"Длина = {LEN(s)}\"", result);
    }

    [Fact]
    public void FormatLine_Modern_FormatsBuiltInInsideInterpolation()
    {
        var result = _service.FormatLine("print $\"Длина = {len(s)}\"", CodeFormattingStyle.Modern);

        Assert.Equal("Print $\"Длина = {Len(s)}\"", result);
    }

    [Fact]
    public void FormatLine_Classic_FormatsKeywordsInsideInterpolation()
    {
        var result = _service.FormatLine("print $\"{a mod 2}\"", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT $\"{a MOD 2}\"", result);
    }

    [Fact]
    public void FormatLine_Classic_FormatsMultipleExpressionsInInterpolation()
    {
        var result = _service.FormatLine("print $\"{len(a)} + {len(b)}\"", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT $\"{LEN(a)} + {LEN(b)}\"", result);
    }

    [Fact]
    public void FormatLine_Classic_PreservesTextPartsAndFormatsExpressions()
    {
        var result = _service.FormatLine("print $\"Длина строки = {len(stroka)} символов\"", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT $\"Длина строки = {LEN(stroka)} символов\"", result);
    }
}
