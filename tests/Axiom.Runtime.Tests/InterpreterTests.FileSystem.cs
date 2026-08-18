using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
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

}
