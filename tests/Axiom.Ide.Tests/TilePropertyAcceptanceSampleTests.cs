using System.IO;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Tests;

public sealed class TilePropertyAcceptanceSampleTests
{
    public static IEnumerable<object[]> Samples()
    {
        var examples = new (string Path, string[] Expected)[]
        {
            ("imagewidth/loaded_resource_property.axm", new[] { "Результат IMAGEWIDTH:", "32" }),
            ("imageheight/loaded_resource_property.axm", new[] { "Результат IMAGEHEIGHT:", "32" }),
            ("loadimage/loaded_resource_property.axm", new[] { "Результат LOADIMAGE:", "32" }),
            ("spritewidth/loaded_resource_property.axm", new[] { "Результат SPRITEWIDTH:", "16" }),
            ("spriteheight/loaded_resource_property.axm", new[] { "Результат SPRITEHEIGHT:", "16" }),
            ("spritecount/loaded_resource_property.axm", new[] { "Результат SPRITECOUNT:", "4" }),
            ("loadsprite/loaded_resource_property.axm", new[] { "Результат LOADSPRITE:", "4" }),
            ("loadtileset/arithmetic_boundaries.axm", new[] { "Тайлов в атласе:", "1", "Ошибка отступа:", "MB3005", "Сохранено тайлов:", "1" }),
            ("loadtileset/loaded_resource_property.axm", new[] { "Результат LOADTILESET:", "4" }),
            ("maptilesetcount/map_access.axm", new[] { "Результат MAPTILESETCOUNT:", "1" }),
            ("mapproperty/map_access.axm", new[] { "Результат MAPPROPERTY:", "выход😀" }),
            ("maplayerproperty/map_access.axm", new[] { "Результат MAPLAYERPROPERTY:", "стена" }),
            ("maptileproperty/map_access.axm", new[] { "Результат MAPTILEPROPERTY:", "TRUE" }),
            ("mapobjectproperty/map_access.axm", new[] { "Результат MAPOBJECTPROPERTY:", "переход" }),
            ("maplayername/map_access.axm", new[] { "Результат MAPLAYERNAME:", "Земля" }),
            ("maplayertype/map_access.axm", new[] { "Результат MAPLAYERTYPE:", "OBJECT" }),
            ("maplayervisible/map_access.axm", new[] { "Результат MAPLAYERVISIBLE:", "FALSE" }),
            ("mapobjectcount/map_access.axm", new[] { "Результат MAPOBJECTCOUNT:", "2" }),
            ("mapobjectname/map_access.axm", new[] { "Результат MAPOBJECTNAME:", "Выход" }),
            ("mapobjectclass/map_access.axm", new[] { "Результат MAPOBJECTCLASS:", "portal" }),
            ("mapobjectkind/map_access.axm", new[] { "Результат MAPOBJECTKIND:", "POINT" }),
            ("mapobjectvisible/map_access.axm", new[] { "Результат MAPOBJECTVISIBLE:", "FALSE" }),
            ("mapobjectx/map_access.axm", new[] { "Результат MAPOBJECTX:", "12.5" }),
            ("mapobjecty/map_access.axm", new[] { "Результат MAPOBJECTY:", "24" }),
            ("mapobjectwidth/map_access.axm", new[] { "Результат MAPOBJECTWIDTH:", "8" }),
            ("mapobjectheight/map_access.axm", new[] { "Результат MAPOBJECTHEIGHT:", "16" }),
            ("mapcolumn/map_access.axm", new[] { "Результат MAPCOLUMN:", "2" }),
            ("maprow/map_access.axm", new[] { "Результат MAPROW:", "1" }),
            ("mapworldx/map_access.axm", new[] { "Результат MAPWORLDX:", "116" }),
            ("mapworldy/map_access.axm", new[] { "Результат MAPWORLDY:", "216" }),
            ("loadtilemap/loader_boundaries.axm", new[] { "Тайлсет и тайл:", "2", "1", "Тайл и флаги после очистки:", "1", "0", "Отказ для начала параллакса:", "MB3005", "LOADTILEMAP: Поле parallaxoriginx карты поддерживает только числовой ноль." }),
            ("loadtilemap/invalid_dimensions.axm", new[] { "Ошибка размеров карты:", "MB3005", "LOADTILEMAP: Поле width должно быть неотрицательным.", "Сохранена прежняя карта:", "1" }),
            ("loadtilemap/unsupported_tileset_metadata.axm", new[] { "MB3005", "LOADTILEMAP: Метаданные wangsets пока не поддерживаются.", "0" }),
            ("loadtilemap/nonfinite_layer.axm", new[] { "MB3005", "LOADTILEMAP: Поле offsetx слоя должно быть конечным числом DOUBLE.", "1" }),
            ("loadtilemap/unsupported_blend.axm", new[] { "MB3005", "LOADTILEMAP: Поле mode слоя поддерживает только строку normal.", "1" }),
            ("loadtilemap/metadata_validation.axm", new[] { "Слои карты:", "1", "Неподдержанный объект:", "MB3005", "LOADTILEMAP: Объекты ellipse пока не поддерживаются." }),
            ("loadtilemap/loaded_resource_property.axm", new[] { "Результат LOADTILEMAP:", "2" }),
            ("mapwidth/loaded_resource_property.axm", new[] { "Результат MAPWIDTH:", "2" }),
            ("mapheight/loaded_resource_property.axm", new[] { "Результат MAPHEIGHT:", "2" }),
            ("maplayercount/loaded_resource_property.axm", new[] { "Результат MAPLAYERCOUNT:", "3" }),
            ("maptile/loaded_resource_property.axm", new[] { "Результат MAPTILE:", "2" }),
            ("rgb/packed_channels.axm", new[] { "Результат RGB:", "66051" }),
            ("screenisactive/text_mode_state.axm", new[] { "Результат SCREENISACTIVE:", "FALSE" }),
            ("screenmode/text_mode_state.axm", new[] { "Результат SCREENMODE:", "0" }),
            ("screenwidth/text_mode_state.axm", new[] { "Результат SCREENWIDTH:", "0" }),
            ("screenheight/text_mode_state.axm", new[] { "Результат SCREENHEIGHT:", "0" }),
            ("tilecount/atlas_property.axm", new[] { "Число тайлов:", "4" }),
            ("tilewidth/atlas_property.axm", new[] { "Ширина тайла в пикселях:", "16" }),
            ("tileheight/atlas_property.axm", new[] { "Высота тайла в пикселях:", "16" }),
            ("mappixelwidth/map_property.axm", new[] { "Ширина карты в пикселях:", "32" }),
            ("mappixelheight/map_property.axm", new[] { "Высота карты в пикселях:", "32" }),
            ("maptilewidth/map_property.axm", new[] { "Ширина клетки в пикселях:", "16" }),
            ("maptileheight/map_property.axm", new[] { "Высота клетки в пикселях:", "16" }),
            ("maptileset/cell_tileset_index.axm", new[] { "Номера тайлсетов для трёх клеток:", "1", "0", "0" }),
            ("maptileflags/changed_cell_flags.axm", new[] { "Маски отражения клетки:", "0", "3", "0" })
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
            foreach (var example in examples)
                yield return new object[] { mode, example.Path, example.Expected };
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ExplicitScreenZeroEstablishesTextWindowDimensions(ExecutionMode mode)
    {
        var console = new ProgramConsoleService();
        Assert.Equal(0, console.ScreenWidth);
        Assert.Equal(0, console.ScreenHeight);
        var service = new ProgramRunService(new AxiomExecutionPipeline(), console);
        var run = service.Run("SCREEN 0", mode);
        Assert.Equal(ProgramRunStatus.Success, run.Status);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(640, console.ScreenWidth);
        Assert.Equal(300, console.ScreenHeight);
        Assert.False(console.IsScreenActive);
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void RepositorySampleRunsWithRealIdeResourceLoader(ExecutionMode mode, string relative, string[] expected)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PROJECT.md")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var path = Path.Combine(directory!.FullName, "samples", "functions", relative);
        var source = File.ReadAllText(path);
        var service = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService());
        var run = service.Run(source, mode, sourceFilePath: path);
        Assert.Equal(ProgramRunStatus.Success, run.Status);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(string.Join(Environment.NewLine, expected), run.OutputText);
    }
}
