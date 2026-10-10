namespace Axiom.Execution.Tests;

public sealed class FileMetadataEvidenceTests
{
    [Fact]
    public void MetadataUsesByteLengthLocalTimesAndActualAttributes()
    {
        if (!OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), "axiom-metadata-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "данные😀.bin");
        byte[] bytes = [0, 255, 128, 13, 10];
        File.WriteAllBytes(path, bytes);
        var write = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local);
        var create = new DateTime(2024, 1, 1, 1, 2, 3, DateTimeKind.Local);
        var access = new DateTime(2024, 1, 3, 4, 5, 6, DateTimeKind.Local);
        try
        {
            File.SetCreationTime(path, create);
            File.SetLastWriteTime(path, write);
            File.SetLastAccessTime(path, access);
            Directory.SetLastWriteTime(root, write);
            File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.ReadOnly);
            Assert.Equal(create, File.GetCreationTime(path));
            Assert.Equal(write, File.GetLastWriteTime(path));
            Assert.Equal(access, File.GetLastAccessTime(path));
            Assert.Equal(write, Directory.GetLastWriteTime(root));
            var source = new System.Text.StringBuilder($$"""
                PRINT FILESIZE("{{path}}")
                PRINT FILEDATE("{{path}}")
                PRINT FILECREATEDATE("{{path}}")
                PRINT FILEACCESSEDDATE("{{path}}")
                PRINT DIRDATE("{{root}}")
                PRINT ISREADONLY("{{path}}")
                PRINT ISHIDDEN("{{path}}")
                """ + "\n");
            foreach (var function in new[] { "FILESIZE", "FILEDATE", "FILECREATEDATE", "FILEACCESSEDDATE", "DIRDATE", "ISREADONLY", "ISHIDDEN" })
                source.AppendLine($"TRY\nPRINT {function}(\"{root}/missing\")\nCATCH err\nPRINT err.Code\nEND TRY");
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source.ToString()));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(new[] { "5", "2024-01-02 03:04:05", "2024-01-01 01:02:03", "2024-01-03 04:05:06", "2024-01-02 03:04:05", "TRUE", "TRUE" }.Concat(Enumerable.Repeat("MB3007", 7)), result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            File.SetAttributes(path, FileAttributes.Normal);
            var reset = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT ISREADONLY(\"{path}\")\nPRINT ISHIDDEN(\"{path}\")"));
            Assert.Equal(ExecutionStatus.Success, reset.Status);
            Assert.Equal(new[] { "FALSE", "FALSE" }, reset.Output);
        }
        finally { File.SetAttributes(path, FileAttributes.Normal); Directory.Delete(root, true); }
    }
}
