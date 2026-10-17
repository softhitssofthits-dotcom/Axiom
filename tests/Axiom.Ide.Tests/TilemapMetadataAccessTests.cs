using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
namespace Axiom.Ide.Tests;
public sealed class TilemapMetadataAccessTests
{
    public static IEnumerable<object[]> Cases()
    {
        var rows = new (bool Loaded, string Expression, string Expected)[]
        {
            (true, "MAPTILESETCOUNT(m)", "1"),
            (false, "MAPTILESETCOUNT(m)", "0"),
            (true, "MAPPROPERTY(m, \"role\")", "выход😀"),
            (false, "MAPPROPERTY(m, \"role\")", ""),
            (true, "MAPLAYERPROPERTY(m, 1, \"role\")", "стена"),
            (false, "MAPLAYERPROPERTY(m, 1, \"role\")", ""),
            (true, "MAPTILEPROPERTY(m, 1, 1, 1, \"solid\")", "TRUE"),
            (false, "MAPTILEPROPERTY(m, 1, 1, 1, \"solid\")", ""),
            (true, "MAPOBJECTPROPERTY(m, 3, 1, \"role\")", "переход"),
            (false, "MAPOBJECTPROPERTY(m, 3, 1, \"role\")", ""),
            (true, "MAPLAYERNAME(m, 1)", "Земля"),
            (false, "MAPLAYERNAME(m, 1)", ""),
            (true, "MAPLAYERTYPE(m, 3)", "OBJECT"),
            (false, "MAPLAYERTYPE(m, 3)", ""),
            (true, "MAPLAYERVISIBLE(m, 2)", "FALSE"),
            (false, "MAPLAYERVISIBLE(m, 2)", "FALSE"),
            (true, "MAPOBJECTCOUNT(m, 3)", "2"),
            (false, "MAPOBJECTCOUNT(m, 3)", "0"),
            (true, "MAPOBJECTNAME(m, 3, 1)", "Выход"),
            (false, "MAPOBJECTNAME(m, 3, 1)", ""),
            (true, "MAPOBJECTCLASS(m, 3, 1)", "portal"),
            (false, "MAPOBJECTCLASS(m, 3, 1)", ""),
            (true, "MAPOBJECTKIND(m, 3, 2)", "POINT"),
            (false, "MAPOBJECTKIND(m, 3, 2)", ""),
            (true, "MAPOBJECTVISIBLE(m, 3, 2)", "FALSE"),
            (false, "MAPOBJECTVISIBLE(m, 3, 2)", "FALSE"),
            (true, "MAPOBJECTX(m, 3, 1)", "12.5"),
            (false, "MAPOBJECTX(m, 3, 1)", "0"),
            (true, "MAPOBJECTY(m, 3, 1)", "24"),
            (false, "MAPOBJECTY(m, 3, 1)", "0"),
            (true, "MAPOBJECTWIDTH(m, 3, 1)", "8"),
            (false, "MAPOBJECTWIDTH(m, 3, 1)", "0"),
            (true, "MAPOBJECTHEIGHT(m, 3, 1)", "16"),
            (false, "MAPOBJECTHEIGHT(m, 3, 1)", "0"),
            (true, "MAPCOLUMN(m, 100, 116)", "2"),
            (false, "MAPCOLUMN(m, 100, 116)", "0"),
            (true, "MAPROW(m, 200, 215.5)", "1"),
            (false, "MAPROW(m, 200, 215.5)", "0"),
            (true, "MAPWORLDX(m, 100, 2)", "116"),
            (false, "MAPWORLDX(m, 100, 2)", "0"),
            (true, "MAPWORLDY(m, 200, 2)", "216"),
            (false, "MAPWORLDY(m, 200, 2)", "0"),
            (true, "MAPPROPERTY(m, \"Role\")", ""),
            (true, "MAPPROPERTY(m, \"exact\")", "9007199254740993"),
            (true, "MAPTILEPROPERTY(m, 1, 1, 2, \"solid\")", ""),
            (true, "MAPTILEPROPERTY(m, 1, 2, 1, \"solid\")", "FALSE"),
            (true, "MAPTILEPROPERTY(m, 3, 1, 1, \"solid\")", ""),
            (true, "MAPTILEPROPERTY(m, 1, 9223372036854775807, 1, \"solid\")", ""),
            (true, "MAPLAYERNAME(m, 0)", ""),
            (true, "MAPLAYERNAME(m, 9223372036854775807)", ""),
            (true, "MAPLAYERTYPE(m, 1)", "TILE"),
            (true, "MAPOBJECTCOUNT(m, 1)", "0"),
            (true, "MAPOBJECTNAME(m, 3, 0)", ""),
            (true, "MAPOBJECTNAME(m, 3, 9223372036854775807)", ""),
            (true, "MAPOBJECTWIDTH(m, 3, 2)", "0"),
            (true, "MAPOBJECTCLASS(m, 3, 2)", "spawn"),
            (true, "MAPCOLUMN(m, 100, 99.99)", "0"),
            (true, "MAPCOLUMN(m, 100, 100)", "1"),
            (true, "MAPCOLUMN(m, 100, 115.99)", "1"),
            (true, "MAPCOLUMN(m, 100, 132)", "0"),
            (true, "MAPROW(m, -32, -16)", "2"),
            (true, "MAPROW(m, -32, 0)", "0"),
            (true, "MAPWORLDX(m, 100, 0)", "0"),
            (true, "MAPWORLDX(m, 100, 3)", "0"),
        };

        foreach (var row in rows) yield return [row.Loaded, row.Expression, row.Expected];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void AccessHasLiteralResultsAndSafeMissingValues(bool loaded, string expression, string expected)
    {
        Check("DIM m AS TILEMAP\n" + (loaded ? "m = LOADTILEMAP(\"access_map.tmj\")\n" : "") + "PRINT " + expression, expected);
    }
    [Fact]
    public void TilePropertyFollowsChangedAndClearedCell()
    {
        Check("""
            DIM m AS TILEMAP
            m = LOADTILEMAP("access_map.tmj")
            PRINT MAPTILEPROPERTY(m, 1, 1, 1, "solid")
            SETMAPTILE m, 1, 1, 1, 1, 2
            PRINT MAPTILEPROPERTY(m, 1, 1, 1, "solid")
            SETMAPTILE m, 1, 1, 1, 0, 0
            PRINT "[" + MAPTILEPROPERTY(m, 1, 1, 1, "solid") + "]"
            """, string.Join(Environment.NewLine, "TRUE", "FALSE", "[]"));
    }
    [Fact]
    public void UnsupportedObjectKeepsReasonInLanguageCatch()
    {
        Check("""
            DIM m AS TILEMAP
            TRY
                m = LOADTILEMAP("unsupported_object.tmj")
            CATCH err
                PRINT err.Code
                PRINT err.Message
            END TRY
            """, string.Join(Environment.NewLine, "MB3005", "LOADTILEMAP: Objects ellipse are not supported."));
    }

    private static void Check(string source, string expected)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PROJECT.md"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var path = Path.Combine(directory.FullName, "samples/functions/loadtilemap/metadata_validation.axm");
        var service = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService());
        var result = service.Run(source, sourceFilePath: path);
        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(expected, result.OutputText);
    }
}
