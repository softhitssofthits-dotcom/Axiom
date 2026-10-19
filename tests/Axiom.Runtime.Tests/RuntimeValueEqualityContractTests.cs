namespace Axiom.Runtime.Tests;

public sealed class RuntimeValueEqualityContractTests
{
    [Theory]
    [InlineData("string")]
    [InlineData("integer")]
    [InlineData("boolean")]
    [InlineData("double")]
    public void ComparingStringWithAnotherTypeReturnsFalseInsteadOfThrowing(string otherType)
    {
        var text = RuntimeValue.FromString("42");
        var other = otherType switch
        {
            "string" => RuntimeValue.FromString("42"),
            "integer" => RuntimeValue.FromInteger(42),
            "boolean" => RuntimeValue.FromBoolean(true),
            _ => RuntimeValue.FromDouble(42)
        };

        // Строковая ветвь должна вести себя как числовые и временные:
        // другой тип даёт неравенство, а не исключение.
        Assert.Equal(otherType == "string", RuntimeValueHelpers.ValuesEqual(text, other));
        Assert.Equal(otherType == "string", RuntimeValueHelpers.ValuesEqual(other, text));
    }

    [Fact]
    public void ComparingStringWithArrayAndVoidReturnsFalse()
    {
        var text = RuntimeValue.FromString("x");
        var arrayType = (Semantics.ArrayTypeSymbol)Semantics.ArrayTypeSymbol.Create(Semantics.TypeSymbol.String);
        var array = RuntimeValue.FromArray(
            arrayType,
            RuntimeArrayValue.CreateVisibleShape(
                Semantics.TypeSymbol.String,
                fixedStringLength: null,
                lowerBounds: new[] { 0 },
                upperBounds: new[] { 0 }));

        Assert.False(RuntimeValueHelpers.ValuesEqual(text, array));
        Assert.False(RuntimeValueHelpers.ValuesEqual(text, RuntimeValue.Void));
    }
}
