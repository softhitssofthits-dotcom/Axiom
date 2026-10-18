using Axiom.Execution;

namespace Axiom.Execution.Tests;

/// <summary>
/// Типизированные метаданные файлов (DTA-007). Строковые формы FILEDATE,
/// FILECREATEDATE, FILEACCESSEDDATE и DIRDATE сохранены без изменений, но теряют
/// доли секунды и не говорят, в каком поясе записано время. Новые формы с
/// суффиксом UTC возвращают момент времени: он однозначен и хранит полную
/// точность файловой системы.
/// </summary>
public sealed class FileMetadataMomentContractTests
{
    [Fact]
    public void TypedFormsReturnUtcMomentsWithFullPrecision()
    {
        var root = CreateRoot();
        var path = Path.Combine(root, "данные.bin");
        File.WriteAllBytes(path, [1, 2, 3]);
        try
        {
            // Отметки задаются в UTC, чтобы ожидания не зависели от пояса машины.
            var write = new DateTime(2024, 1, 2, 3, 4, 5, 125, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, write);
            File.SetCreationTimeUtc(path, new DateTime(2024, 1, 1, 1, 2, 3, DateTimeKind.Utc));
            File.SetLastAccessTimeUtc(path, new DateTime(2024, 1, 3, 4, 5, 6, DateTimeKind.Utc));

            var source = $$"""
                PRINT FILEDATEUTC("{{path}}")
                PRINT FILECREATEDATEUTC("{{path}}")
                PRINT FILEACCESSEDDATEUTC("{{path}}")
                DIM moment AS UTCDATETIME = FILEDATEUTC("{{path}}")
                PRINT moment = FILEDATEUTC("{{path}}")
                PRINT LEN(FILEDATEUTC("{{path}}")) > 19
                """;

            var result = Run(source);

            Assert.Equal("2024-01-02T03:04:05.125Z", result.Output[0]);
            Assert.Equal("2024-01-01T01:02:03Z", result.Output[1]);
            Assert.Equal("2024-01-03T04:05:06Z", result.Output[2]);
            Assert.Equal("TRUE", result.Output[3]);
            Assert.Equal("TRUE", result.Output[4]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DirectoryMomentIsTyped()
    {
        var root = CreateRoot();
        try
        {
            Directory.SetLastWriteTimeUtc(root, new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc));

            var result = Run($$"""
                PRINT DIRDATEUTC("{{root}}")
                DIM moment AS UTCDATETIME = DIRDATEUTC("{{root}}")
                PRINT YEAR(FROMINSTANT(moment, "UTC")) = 2024
                """);

            Assert.Equal(["2024-05-06T07:08:09Z", "TRUE"], result.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void StringFormsKeepTheirPreviousContract()
    {
        var root = CreateRoot();
        var path = Path.Combine(root, "данные.bin");
        File.WriteAllBytes(path, [1]);
        try
        {
            File.SetLastWriteTimeUtc(path, new DateTime(2024, 1, 2, 3, 4, 5, 125, DateTimeKind.Utc));

            var result = Run($$"""
                PRINT FILEDATE("{{path}}")
                PRINT LEN(FILEDATE("{{path}}")) = 19
                """);

            // Строковая форма по-прежнему печатает местное время до секунды.
            Assert.Equal("TRUE", result.Output[1]);
            Assert.DoesNotContain('T', result.Output[0]);
            Assert.DoesNotContain('Z', result.Output[0]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TypedFormsRejectMissingResources()
    {
        var root = CreateRoot();
        var missing = Path.Combine(root, "нет.bin");
        try
        {
            foreach (var function in new[] { "FILEDATEUTC", "FILECREATEDATEUTC", "FILEACCESSEDDATEUTC", "DIRDATEUTC" })
            {
                var result = Run($$"""
                    TRY
                        PRINT {{function}}("{{missing}}")
                    CATCH err
                        PRINT err.Code
                    END TRY
                    PRINT "Продолжение"
                    """);

                Assert.Equal(["MB3007", "Продолжение"], result.Output);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TypedFormsRejectWrongResourceKind()
    {
        var root = CreateRoot();
        var path = Path.Combine(root, "файл.bin");
        File.WriteAllBytes(path, [1]);
        try
        {
            var asFile = Run($$"""
                TRY
                    PRINT DIRDATEUTC("{{path}}")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """);
            Assert.Equal(["MB3007", "Продолжение"], asFile.Output);

            var asDirectory = Run($$"""
                TRY
                    PRINT FILEDATEUTC("{{root}}")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """);
            Assert.Equal(["MB3007", "Продолжение"], asDirectory.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MomentsAreOrderableAndCompatibleWithInstantType()
    {
        var root = CreateRoot();
        var first = Path.Combine(root, "первый.bin");
        var second = Path.Combine(root, "второй.bin");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        try
        {
            File.SetLastWriteTimeUtc(first, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(second, new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));

            var result = Run($$"""
                PRINT FILEDATEUTC("{{first}}") < FILEDATEUTC("{{second}}")
                PRINT FILEDATEUTC("{{first}}") = FILEDATEUTC("{{second}}")
                PRINT FROMINSTANT(FILEDATEUTC("{{first}}"), "UTC")
                """);

            Assert.Equal(["TRUE", "FALSE", "2024-01-01 00:00:00"], result.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-moment-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static ExecutionResult Run(string source)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        return result;
    }
}
