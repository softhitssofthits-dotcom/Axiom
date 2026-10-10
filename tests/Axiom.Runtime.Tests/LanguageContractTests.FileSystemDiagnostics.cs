using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    // Аудит 2026-10-06 (AXD-003/AXD-017): тексты диагностики файловой подсистемы
    // Interpreter были mojibake и дублировали формулировки VM. Тесты фиксируют
    // читаемые русские тексты для всех исправленных валидаций и закрывают
    // тонкое покрытие (1-4 упоминания в тестах) для дисковых и временных built-in.

    [Fact]
    public void Execute_MakeTempFileWithPathPrefixReportsReadableRussianDiagnostic()
    {
        var (_, _, result) = ExecuteProgram("""PRINT MAKETEMPFILE("sub\\prefix")""");

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("должен быть простым префиксом имени без пути", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_GetFilesWithEmptyPatternReportsReadableRussianDiagnostic()
    {
        var (_, _, result) = ExecuteProgram("""PRINT GETFILES("C:\\", "")""");

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("не может быть пустой строкой", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_FreeSpaceResolvesRelativePathToHostDrive()
    {
        // Нормализация пути делает его абсолютным до DriveInfo, поэтому
        // FREESPACE для относительного пути возвращает место диска запуска.
        var (_, host, result) = ExecuteProgram("""PRINT FREESPACE("relative\path") > 0""");

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE"], host.Output);
    }

    [Fact]
    public void Execute_RenameDirOfRootReportsReadableRussianDiagnostic()
    {
        var text = """PRINT RENAMEDIR("C:\\", "renamed-root")""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("не содержит родительскую папку", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("СЃРѕРґРµСЂР¶РёС‚", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_ThinFileSystemBuiltinsReturnSaneValues()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "probe.txt");
            var text = $"""
DIM probeDir AS STRING = "{ToBasicString(root)}"
DIM probeFile AS STRING = "{ToBasicString(filePath)}"
PRINT WRITEFILE(probeFile, "probe")
PRINT LEN(FILEDATE(probeFile)) > 0
PRINT LEN(FILECREATEDATE(probeFile)) > 0
PRINT LEN(FILEACCESSEDDATE(probeFile)) > 0
PRINT LEN(DIRDATE(probeDir)) > 0
PRINT FILESIZE(probeFile)
PRINT DIRSIZE(probeDir) > 0
PRINT ISREADONLY(probeFile)
PRINT ISHIDDEN(probeFile)
PRINT DRIVENAME(probeFile)
PRINT LEN(DRIVEFORMAT(probeFile)) > 0
PRINT LEN(DRIVETYPE(probeFile)) > 0
PRINT FREESPACE(probeDir) > 0
PRINT TOTALSPACE(probeDir) > 0
PRINT TOTALFREESPACE(probeDir) > 0
PRINT ISABSOLUTEPATH(probeFile)
PRINT LEN(PATHROOT(probeFile)) > 0
PRINT LEN(FULLPATH("probe.txt")) > 0
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            var driveName = host.Output[9];
            Assert.Equal(
                ["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "5", "TRUE", "FALSE", "FALSE", driveName, "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"],
                host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_MakeTempFileAndDirCreateRealEntries()
    {
        var text = """
DIM tempFile AS STRING = MAKETEMPFILE("audit_")
DIM tempDir AS STRING = MAKETEMPDIR("audit_")
PRINT FILEEXISTS(tempFile)
PRINT DIREXISTS(tempDir)
PRINT DELETEFILE(tempFile)
PRINT DELETEDIRRECURSIVE(tempDir)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
    }
}
