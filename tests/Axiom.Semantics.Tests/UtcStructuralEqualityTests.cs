namespace Axiom.Semantics.Tests;

public sealed class UtcStructuralEqualityTests
{
    [Fact]
    public void UtcDateTimeIsSupportedAsScalarAndNestedField()
    {
        Assert.True(StructuralEqualitySupport.IsSupported(TypeSymbol.UtcDateTime));
        var inner = new UserDefinedTypeSymbol("Inner");
        inner.Complete([new TypeFieldSymbol("Instant", TypeSymbol.UtcDateTime)]);
        var outer = new UserDefinedTypeSymbol("Outer");
        outer.Complete([new TypeFieldSymbol("Item", inner)]);
        Assert.True(StructuralEqualitySupport.IsSupported(inner));
        Assert.True(StructuralEqualitySupport.IsSupported(outer));
    }
}
