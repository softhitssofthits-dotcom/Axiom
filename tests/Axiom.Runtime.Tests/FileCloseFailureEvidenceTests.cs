using System.Text;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime.Tests;

public sealed class FileCloseFailureEvidenceTests
{
    [Fact]
    public void Dispose_WriteFailureStillClosesFile()
    {
        if (!OperatingSystem.IsWindows()) return;
        var path = Path.GetTempFileName();
        var file = new RuntimeTextFile(path, RuntimeTextFileMode.Output, new UTF8Encoding(false));
        try
        {
            file.Write("Данные", false);
            using (var blocker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                blocker.Lock(0, 4096);
                try
                {
                    Assert.Throws<IOException>(() => file.Dispose());
                    Assert.False(file.IsOpen);
                }
                finally { blocker.Unlock(0, 4096); }
            }
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { file.Dispose(); File.Delete(path); }
    }

    [Fact]
    public void CloseAll_WriteFailureStillReleasesEveryFile()
    {
        if (!OperatingSystem.IsWindows()) return;
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();
        var first = new RuntimeTextFile(firstPath, RuntimeTextFileMode.Output, new UTF8Encoding(false));
        var second = new RuntimeTextFile(secondPath, RuntimeTextFileMode.Output, new UTF8Encoding(false));
        var files = new HashSet<RuntimeTextFile> { first, second };
        try
        {
            first.Write("Первый", false);
            second.Write("Второй", false);
            using (var blocker = new FileStream(firstPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                blocker.Lock(0, 4096);
                try
                {
                    var helper = typeof(RuntimeTextFile).Assembly.GetType("Axiom.Runtime.Builtins.BuiltinFileIOHelpers")!;
                    var failure = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
                        helper.GetMethod("CloseAllOpenFiles")!.Invoke(null, [files, null]));
                    Assert.IsAssignableFrom<IOException>(failure.InnerException);
                    Assert.Empty(files);
                    Assert.False(first.IsOpen);
                    Assert.False(second.IsOpen);
                }
                finally { blocker.Unlock(0, 4096); }
            }
            Assert.Equal("Второй", File.ReadAllText(secondPath));
        }
        finally { first.Dispose(); second.Dispose(); File.Delete(firstPath); File.Delete(secondPath); }
    }
}
