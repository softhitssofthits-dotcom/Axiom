using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed class UtcValueEqualityTests
{
    [Fact]
    public void UtcValuesHaveMatchingHashesAndUseHashSetPath()
    {
        var instant = new DateTime(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc).AddTicks(1);
        var left = RuntimeValue.FromUtcDateTime(instant);
        var equal = RuntimeValue.FromUtcDateTime(instant);
        var different = RuntimeValue.FromUtcDateTime(instant.AddTicks(1));
        var comparer = RuntimeValueEqualityComparer.Instance;
        Assert.True(comparer.Equals(left, equal));
        Assert.False(comparer.Equals(left, different));
        Assert.Equal(comparer.GetHashCode(left), comparer.GetHashCode(equal));
        Assert.Equal(instant.GetHashCode(), comparer.GetHashCode(left));
        Assert.True(RuntimeValueEqualityComparer.SupportsElementType(TypeSymbol.UtcDateTime));
        var seen = new HashSet<RuntimeValue>(comparer) { left, equal, different };
        Assert.Equal(2, seen.Count);
    }
}
