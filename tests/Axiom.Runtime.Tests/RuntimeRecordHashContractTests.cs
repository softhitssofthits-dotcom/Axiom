using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed class RuntimeRecordHashContractTests
{
    private static readonly UserDefinedTypeSymbol ProductType = CreateProductType();
    private static readonly UserDefinedTypeSymbol ProductWithArrayType = CreateProductWithArrayType();
    private static readonly UserDefinedTypeSymbol ReceiptType = CreateReceiptType();
    private static readonly UserDefinedTypeSymbol MomentType = CreateMomentType();

    [Fact]
    public void EqualRecordInstancesHashIdenticallyAndStayStable()
    {
        var comparer = RuntimeValueEqualityComparer.Instance;
        var first = Product("яблоко", 30);
        var second = Product("яблоко", 30);

        Assert.True(comparer.Equals(first, second));

        var firstHash = comparer.GetHashCode(first);
        var secondHash = comparer.GetHashCode(second);

        Assert.Equal(firstHash, secondHash);
        // Повторные вызовы на изменяемой записи не используют кеш и остаются стабильными.
        Assert.Equal(firstHash, comparer.GetHashCode(first));
        Assert.Equal(firstHash, comparer.GetHashCode(Product("яблоко", 30)));
    }

    [Fact]
    public void DifferentFieldValuesHashDifferently()
    {
        var comparer = RuntimeValueEqualityComparer.Instance;

        Assert.NotEqual(
            comparer.GetHashCode(Product("яблоко", 30)),
            comparer.GetHashCode(Product("банан", 30)));
        Assert.NotEqual(
            comparer.GetHashCode(Product("яблоко", 30)),
            comparer.GetHashCode(Product("яблоко", 40)));
    }

    [Fact]
    public void HashSetSeparatesEqualAndUnequalRecords()
    {
        // Искусственные коллизии здесь не подбираются: проверяется главный
        // инвариант хеш-множества — равные записи схлопываются, неравные
        // остаются отдельными элементами даже при совпадающем хеше.
        var seen = new HashSet<RuntimeValue>(RuntimeValueEqualityComparer.Instance)
        {
            Product("яблоко", 30),
            Product("банан", 20),
            Product("яблоко", 30)
        };

        Assert.Equal(2, seen.Count);
        Assert.Contains(Product("банан", 20), seen);
    }

    [Fact]
    public void SupportsElementTypeAcceptsStructuralRecordAndRejectsArrayField()
    {
        Assert.True(RuntimeValueEqualityComparer.SupportsElementType(ProductType));
        Assert.True(RuntimeValueEqualityComparer.SupportsElementType(ReceiptType));
        Assert.False(RuntimeValueEqualityComparer.SupportsElementType(ProductWithArrayType));
        Assert.True(RuntimeValueEqualityComparer.SupportsElementType(TypeSymbol.Integer));
    }

    [Fact]
    public void NestedRecordHashFollowsInnerValues()
    {
        var comparer = RuntimeValueEqualityComparer.Instance;
        var first = Receipt(1, "чай", 40);
        var second = Receipt(1, "чай", 40);
        var changedInner = Receipt(1, "чай", 45);

        Assert.True(comparer.Equals(first, second));
        Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(second));
        Assert.False(comparer.Equals(first, changedInner));
        Assert.NotEqual(comparer.GetHashCode(first), comparer.GetHashCode(changedInner));
    }

    [Fact]
    public void TemporalFieldHashMatchesEquality()
    {
        var comparer = RuntimeValueEqualityComparer.Instance;
        var first = Moment("a", new DateTime(2024, 1, 1, 10, 0, 0));
        var second = Moment("a", new DateTime(2024, 1, 1, 10, 0, 0));
        var other = Moment("a", new DateTime(2024, 1, 1, 10, 0, 1));

        Assert.True(comparer.Equals(first, second));
        Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(second));
        Assert.False(comparer.Equals(first, other));
        Assert.NotEqual(comparer.GetHashCode(first), comparer.GetHashCode(other));
    }

    private static UserDefinedTypeSymbol CreateProductType()
    {
        var type = new UserDefinedTypeSymbol("Product");
        type.Complete(
        [
            new TypeFieldSymbol("Name", TypeSymbol.String),
            new TypeFieldSymbol("Price", TypeSymbol.Integer)
        ]);
        return type;
    }

    private static UserDefinedTypeSymbol CreateProductWithArrayType()
    {
        var type = new UserDefinedTypeSymbol("Tagged");
        type.Complete(
        [
            new TypeFieldSymbol("Name", TypeSymbol.String),
            new TypeFieldSymbol("Tags", ArrayTypeSymbol.Create(TypeSymbol.String))
        ]);
        return type;
    }

    private static UserDefinedTypeSymbol CreateReceiptType()
    {
        var inner = new UserDefinedTypeSymbol("ReceiptLine");
        inner.Complete(
        [
            new TypeFieldSymbol("Label", TypeSymbol.String),
            new TypeFieldSymbol("Price", TypeSymbol.Integer)
        ]);

        var outer = new UserDefinedTypeSymbol("Receipt");
        outer.Complete(
        [
            new TypeFieldSymbol("Number", TypeSymbol.Integer),
            new TypeFieldSymbol("Line", inner)
        ]);
        return outer;
    }

    private static UserDefinedTypeSymbol CreateMomentType()
    {
        var type = new UserDefinedTypeSymbol("Moment");
        type.Complete(
        [
            new TypeFieldSymbol("Tag", TypeSymbol.String),
            new TypeFieldSymbol("At", TypeSymbol.DateTime)
        ]);
        return type;
    }

    private static RuntimeValue Product(string name, int price)
    {
        var record = RuntimeRecordValue.CreateDefault(ProductType);
        record.SetField(ProductType.Fields[0], RuntimeValue.FromString(name));
        record.SetField(ProductType.Fields[1], RuntimeValue.FromInteger(price));
        return RuntimeValue.FromRecord(ProductType, record);
    }

    private static RuntimeValue Receipt(int number, string label, int price)
    {
        var innerType = (UserDefinedTypeSymbol)ReceiptType.Fields[1].Type;
        var record = RuntimeRecordValue.CreateDefault(ReceiptType);
        record.SetField(ReceiptType.Fields[0], RuntimeValue.FromInteger(number));

        var inner = record.GetField(ReceiptType.Fields[1]).AsRecord();
        inner.SetField(innerType.Fields[0], RuntimeValue.FromString(label));
        inner.SetField(innerType.Fields[1], RuntimeValue.FromInteger(price));

        return RuntimeValue.FromRecord(ReceiptType, record);
    }

    private static RuntimeValue Moment(string tag, DateTime at)
    {
        var record = RuntimeRecordValue.CreateDefault(MomentType);
        record.SetField(MomentType.Fields[0], RuntimeValue.FromString(tag));
        record.SetField(MomentType.Fields[1], RuntimeValue.FromDateTime(at));
        return RuntimeValue.FromRecord(MomentType, record);
    }
}
