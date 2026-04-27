using Axiom.Ide.Files;

namespace Axiom.Ide.Tests;

public sealed class FileOperationsServiceTests
{
    [Fact]
    public void CreateEmptyFile_CreatesFileOnDisk()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var service = new FileOperationsService();

            var filePath = service.CreateEmptyFile(rootPath, "sample.axm");

            Assert.True(File.Exists(filePath));
            Assert.Equal(0, new FileInfo(filePath).Length);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Rename_RenamesFileInSameDirectory()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var sourcePath = Path.Combine(rootPath, "before.txt");
            File.WriteAllText(sourcePath, "content");
            var service = new FileOperationsService();

            var targetPath = service.Rename(sourcePath, "after.txt");

            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(targetPath));
            Assert.Equal("content", File.ReadAllText(targetPath));
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void CopyToDirectory_CopiesDirectoryRecursively()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var sourceDirectoryPath = Path.Combine(rootPath, "source");
            var destinationDirectoryPath = Path.Combine(rootPath, "destination");
            Directory.CreateDirectory(sourceDirectoryPath);
            Directory.CreateDirectory(destinationDirectoryPath);
            File.WriteAllText(Path.Combine(sourceDirectoryPath, "one.txt"), "1");
            Directory.CreateDirectory(Path.Combine(sourceDirectoryPath, "nested"));
            File.WriteAllText(Path.Combine(sourceDirectoryPath, "nested", "two.txt"), "2");

            var service = new FileOperationsService();

            var copiedPath = service.CopyToDirectory(sourceDirectoryPath, destinationDirectoryPath);

            Assert.True(Directory.Exists(copiedPath));
            Assert.True(File.Exists(Path.Combine(copiedPath, "one.txt")));
            Assert.True(File.Exists(Path.Combine(copiedPath, "nested", "two.txt")));
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void MoveToDirectory_MovesFileToDestinationDirectory()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var sourceDirectoryPath = Path.Combine(rootPath, "source");
            var destinationDirectoryPath = Path.Combine(rootPath, "destination");
            Directory.CreateDirectory(sourceDirectoryPath);
            Directory.CreateDirectory(destinationDirectoryPath);
            var sourcePath = Path.Combine(sourceDirectoryPath, "program.axm");
            File.WriteAllText(sourcePath, "PRINT 1");

            var service = new FileOperationsService();

            var movedPath = service.MoveToDirectory(sourcePath, destinationDirectoryPath);

            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(movedPath));
            Assert.Equal("PRINT 1", File.ReadAllText(movedPath));
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Axiom.FileOperationsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
