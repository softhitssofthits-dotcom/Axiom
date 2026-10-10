namespace Axiom.Execution.Tests;

public sealed class PathBaseResolutionEvidenceTests
{
    [Theory]
    [InlineData(@"D:\AxiomBase\Проект", @"\assets\тест.txt", @"D:\assets\тест.txt")]
    [InlineData(@"C:\AxiomBase\Проект", @"C:assets\тест.txt", @"C:\AxiomBase\Проект\assets\тест.txt")]
    [InlineData(@"D:\AxiomBase\Проект", @"D:assets\тест.txt", @"D:\AxiomBase\Проект\assets\тест.txt")]
    [InlineData(@"\\server\share\Проект", @"\assets\тест.txt", @"\\server\share\assets\тест.txt")]
    public void PartiallyQualifiedPathsUseProgramDirectoryRatherThanProcessDirectory(string directory, string input, string expected)
    {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT FULLPATH(\"{input}\")", resourceBaseDirectory: directory));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, Assert.Single(run.Output));
    }
    [Theory]
    [InlineData(@"D:\AxiomBase\Проект", @"assets\тест.txt", @"D:\AxiomBase\Проект\assets\тест.txt")]
    [InlineData(@"D:\AxiomBase\Проект", @"..\тест.txt", @"D:\AxiomBase\тест.txt")]
    [InlineData(@"D:\AxiomBase\Проект", @".\тест.txt", @"D:\AxiomBase\Проект\тест.txt")]
    [InlineData(@"D:\AxiomBase\Проект", @"", @"D:\AxiomBase\Проект")]
    [InlineData(@"D:\AxiomBase\Проект", @"C:\Другой\тест.txt", @"C:\Другой\тест.txt")]
    [InlineData(@"D:\AxiomBase\Проект", @"C:assets\тест.txt", @"C:\assets\тест.txt")]
    [InlineData(@"\\server\share\Проект", @"..\тест.txt", @"\\server\share\тест.txt")]
    [InlineData(@"C:\AxiomBase", @"assets/папка/../тест😀.txt", @"C:\AxiomBase\assets\тест😀.txt")]
    public void OrdinaryAndFullyQualifiedPathContractsRemainStable(string directory, string input, string expected)
    {

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT FULLPATH(\"{input}\")", resourceBaseDirectory: directory));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, Assert.Single(run.Output));
        }
    }

    [Fact]
    public void DriveRelativeReadFileUsesActualProgramDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "axiom-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        const string name = "данные😀.txt";
        var file = Path.Combine(directory, name);
        try
        {
            File.WriteAllText(file, "Привет😀");
            var input = Path.GetPathRoot(directory)![..2] + name;

            {
                var source = $"PRINT READFILE(\"{input}\")\nPRINT PATHROOT(\"{input}\")\nPRINT DIRNAME(\"{input}\")";
                var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, resourceBaseDirectory: directory));
                Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
                Assert.Equal(new[] { "Привет😀", Path.GetPathRoot(directory)!, directory }, run.Output);
            }
        }
        finally
        {
            File.Delete(file);
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void CombinePathRemainsLexicalAndDoesNotBuildRelativePath()
    {
        const string source = """
PRINT COMBINEPATH("C:\base\one", "..\two")
PRINT COMBINEPATH("C:\base", "D:\other\file.txt")
PRINT FULLPATH(COMBINEPATH("C:\base\one", "..\two"))
""";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded);
            Assert.Equal(new[] { @"C:\base\one\..\two", @"D:\other\file.txt", @"C:\base\two" }, run.Output);
        }
    }

    [Fact]
    public void RunnableDriveRelativeSamplePassesInBytecodeVm()
    {
        var source = TestProgramLoader.LoadText("samples/functions/fullpath/drive_relative_scope.axm");

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.True(run.Output.Count >= 5);
            Assert.All(run.Output, line => Assert.EndsWith(": TRUE", line));
        }
    }

}
