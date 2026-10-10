using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(string text, params string[] inputs)
    {
        var host = new TestRuntimeHost(inputs);
        return RuntimeVmContractRunner.Execute(text, host);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(string text, TestRuntimeHost host)
    {
        return RuntimeVmContractRunner.Execute(text, host);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithKeys(string text, params string[] keys)
    {
        var host = new TestRuntimeHost([], keys: keys);
        return RuntimeVmContractRunner.Execute(text, host);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithPressedKeys(string text, params string[] pressedKeys)
    {
        var host = new TestRuntimeHost([], pressedKeys: pressedKeys);
        return RuntimeVmContractRunner.Execute(text, host);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithKeysAndPressedKeys(
        string text,
        IEnumerable<string> keys,
        IEnumerable<string> pressedKeys)
    {
        var host = new TestRuntimeHost([], keys: keys, pressedKeys: pressedKeys);
        return RuntimeVmContractRunner.Execute(text, host);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithMouseState(
        string text,
        long mouseX = -1,
        long mouseY = -1,
        IEnumerable<int>? pressedMouseButtons = null,
        IEnumerable<int>? mousePresses = null,
        IEnumerable<MouseInputEvent>? mouseEvents = null)
    {
        var host = new TestRuntimeHost([], mouseX: mouseX, mouseY: mouseY, pressedMouseButtons: pressedMouseButtons, mousePresses: mousePresses, mouseEvents: mouseEvents);
        return RuntimeVmContractRunner.Execute(text, host);
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
        var host = new TestRuntimeHost(inputs, localNow, localTimeZone, namedTimeZones);
        return RuntimeVmContractRunner.Execute(text, host);
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

    private static RuntimeTilemapValue CreateTestTilemap(RuntimeImageValue image)
    {
        var tileset = new RuntimeTilesetValue(image, tileWidth: 16, tileHeight: 8);
        var cells = new[]
        {
            new RuntimeTilemapCell(1, 1, RuntimeTilemapFlipFlags.None),
            new RuntimeTilemapCell(1, 2, RuntimeTilemapFlipFlags.None),
            default,
            new RuntimeTilemapCell(1, 3, RuntimeTilemapFlipFlags.None)
        };
        var visible = new RuntimeTilemapLayer("visible", RuntimeTilemapLayerKind.Tile, 2, 2, visible: true, opacity: 1d, offsetX: 0, offsetY: 0, parallaxX: 1d, parallaxY: 1d, cells);
        var hidden = new RuntimeTilemapLayer("hidden", RuntimeTilemapLayerKind.Tile, 2, 2, visible: false, opacity: 1d, offsetX: 0, offsetY: 0, parallaxX: 1d, parallaxY: 1d, cells);
        var objects = new RuntimeTilemapLayer("objects", RuntimeTilemapLayerKind.Object, 2, 2, visible: true, opacity: 1d, offsetX: 0, offsetY: 0, parallaxX: 1d, parallaxY: 1d, cells: null);
        return new RuntimeTilemapValue(
            2,
            2,
            16,
            8,
            RuntimeTilemapRenderOrder.RightDown,
            [new RuntimeTilemapTilesetReference(1, tileset)],
            [visible, hidden, objects]);
    }
}
