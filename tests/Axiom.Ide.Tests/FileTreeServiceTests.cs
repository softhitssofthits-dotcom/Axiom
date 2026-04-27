using Axiom.Ide.Files;

namespace Axiom.Ide.Tests;

public sealed class FileTreeServiceTests
{
    [Fact]
    public void BuildChildNodes_ReturnsDirectoriesFirstThenFiles()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(rootPath, "beta"));
            Directory.CreateDirectory(Path.Combine(rootPath, "alpha"));
            File.WriteAllText(Path.Combine(rootPath, "zeta.axm"), "PRINT 1");
            File.WriteAllText(Path.Combine(rootPath, "gamma.axm"), "PRINT 2");

            var service = new FileTreeService();

            var nodes = service.BuildChildNodes(rootPath);

            Assert.Collection(
                nodes,
                node =>
                {
                    Assert.True(node.IsDirectory);
                    Assert.Equal("alpha", node.DisplayName);
                },
                node =>
                {
                    Assert.True(node.IsDirectory);
                    Assert.Equal("beta", node.DisplayName);
                },
                node =>
                {
                    Assert.False(node.IsDirectory);
                    Assert.Equal("gamma.axm", node.DisplayName);
                },
                node =>
                {
                    Assert.False(node.IsDirectory);
                    Assert.Equal("zeta.axm", node.DisplayName);
                });
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void BuildChildNodes_UsesPlaceholderForExpandableDirectories()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var childDirectory = Path.Combine(rootPath, "examples");
            Directory.CreateDirectory(childDirectory);
            File.WriteAllText(Path.Combine(childDirectory, "nested.axm"), "PRINT 3");

            var service = new FileTreeService();

            var node = Assert.Single(service.BuildChildNodes(rootPath));

            Assert.True(node.IsDirectory);
            Assert.Equal("examples", node.DisplayName);
            var placeholder = Assert.Single(node.Children);
            Assert.True(placeholder.IsPlaceholder);
            Assert.False(node.IsLoaded);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void BuildTree_ReturnsDirectoryRoots()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var service = new FileTreeService();
            var nodes = service.BuildTree(rootPath);

            Assert.NotEmpty(nodes);
            Assert.All(nodes, node =>
            {
                Assert.True(node.IsDirectory);
                Assert.False(node.IsPlaceholder);
            });
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void BuildChildNodes_HidesHiddenFilesByDefault()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var hiddenFilePath = Path.Combine(rootPath, "hidden.txt");
            File.WriteAllText(hiddenFilePath, "hidden");
            File.SetAttributes(hiddenFilePath, File.GetAttributes(hiddenFilePath) | FileAttributes.Hidden);

            var service = new FileTreeService();

            var nodes = service.BuildChildNodes(rootPath);

            Assert.Empty(nodes);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void BuildChildNodes_CanShowHiddenFilesWhenRequested()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var hiddenFilePath = Path.Combine(rootPath, "hidden.txt");
            File.WriteAllText(hiddenFilePath, "hidden");
            File.SetAttributes(hiddenFilePath, File.GetAttributes(hiddenFilePath) | FileAttributes.Hidden);

            var service = new FileTreeService();

            var node = Assert.Single(service.BuildChildNodes(rootPath, showHiddenFiles: true));

            Assert.Equal("hidden.txt", node.DisplayName);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Axiom.FileTreeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
