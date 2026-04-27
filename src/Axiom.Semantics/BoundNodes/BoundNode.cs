using Axiom.Core;

namespace Axiom.Semantics;

public abstract class BoundNode
{
    protected BoundNode(TextLocation location)
    {
        Location = location;
    }

    public abstract BoundNodeKind Kind { get; }

    public TextLocation Location { get; }
}

public abstract class BoundStatement : BoundNode
{
    protected BoundStatement(TextLocation location)
        : base(location)
    {
    }
}

public abstract class BoundExpression : BoundNode
{
    protected BoundExpression(TextLocation location, TypeSymbol type)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
    }

    public TypeSymbol Type { get; }
}
