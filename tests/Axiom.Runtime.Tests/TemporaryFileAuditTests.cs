using System.Reflection;

namespace Axiom.Runtime.Tests;

public sealed class TemporaryFileAuditTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreationRetriesCollisionWithoutChangingExistingObject(bool directoryCollision)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "temp-audit", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var collision = Path.Combine(root, "collision");
        var created = Path.Combine(root, "created");
        if (directoryCollision) Directory.CreateDirectory(collision);
        else File.WriteAllText(collision, "Сохранить");
        try
        {
            var helper = typeof(RuntimeTextFile).Assembly.GetType("Axiom.Runtime.Builtins.BuiltinFileSystemHelpers")!;
            var method = helper.GetMethod("CreateTemporaryFile", [typeof(string), typeof(Func<string>)]);
            Assert.NotNull(method);
            var attempts = 0;
            Func<string> candidate = () => ++attempts == 1 ? collision : created;
            var result = method.Invoke(null, ["unused", candidate]);
            Assert.Equal(created, result);
            Assert.Equal(2, attempts);
            Assert.Equal(0, new FileInfo(created).Length);
            if (directoryCollision) Assert.True(Directory.Exists(collision));
            else Assert.Equal("Сохранить", File.ReadAllText(collision));
        }
        finally { Directory.Delete(root, true); }
    }
}
