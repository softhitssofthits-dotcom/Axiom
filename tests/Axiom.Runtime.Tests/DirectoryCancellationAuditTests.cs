using System.Reflection;

namespace Axiom.Runtime.Tests;

public sealed class DirectoryCancellationAuditTests
{
    [Fact]
    public void CancelledCopyDoesNotCreateDestination()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "directory-cancel", Guid.NewGuid().ToString("N")));
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        Directory.CreateDirectory(source);
        try
        {
            var helper = typeof(RuntimeTextFile).Assembly.GetType("Axiom.Runtime.Builtins.BuiltinDirectoryHelpers")!;
            Action cancel = () => throw new OperationCanceledException();
            var error = Assert.Throws<TargetInvocationException>(() =>
                helper.GetMethod("CopyDirectory")!.Invoke(null, [source, destination, false, cancel]));
            Assert.IsType<OperationCanceledException>(error.InnerException);
            Assert.False(Directory.Exists(destination));
        }
        finally { Directory.Delete(root, true); }
    }
}
