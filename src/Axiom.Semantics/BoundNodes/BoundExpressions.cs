using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed class BoundLiteralExpression : BoundExpression
{
    public BoundLiteralExpression(TextLocation location, object? value, TypeSymbol type)
        : base(location, type)
    {
        Value = value;
    }

    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;

    public object? Value { get; }
}

public sealed class BoundFunctionExpression : BoundExpression
{
    public BoundFunctionExpression(TextLocation location, FunctionSymbol function)
        : base(location, FunctionTypeSymbol.FromFunction(function))
    {
        ArgumentNullException.ThrowIfNull(function);
        Function = function;
    }

    public override BoundNodeKind Kind => BoundNodeKind.FunctionExpression;

    public FunctionSymbol Function { get; }
}

public sealed class BoundVariableExpression : BoundExpression
{
    public BoundVariableExpression(TextLocation location, VariableSymbol variable)
        : base(location, variable.Type)
    {
        ArgumentNullException.ThrowIfNull(variable);
        Variable = variable;
    }

    public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;

    public VariableSymbol Variable { get; }
}

public sealed class BoundArrayAccessExpression : BoundExpression
{
    public BoundArrayAccessExpression(
        TextLocation location,
        BoundExpression target,
        IReadOnlyList<BoundExpression> indices,
        string displayName)
        : base(location, GetElementType(target))
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Target = target;
        Indices = indices;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ArrayAccessExpression;

    public BoundExpression Target { get; }

    public IReadOnlyList<BoundExpression> Indices { get; }

    public BoundExpression Index => Indices[0];

    public string DisplayName { get; }

    private static TypeSymbol GetElementType(BoundExpression target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return target.Type is ArrayTypeSymbol arrayType
            ? arrayType.ElementType
            : TypeSymbol.Error;
    }
}

public sealed class BoundFieldAccessExpression : BoundExpression
{
    public BoundFieldAccessExpression(TextLocation location, BoundExpression target, TypeFieldSymbol field)
        : base(location, field.Type)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(field);

        Target = target;
        Field = field;
    }

    public override BoundNodeKind Kind => BoundNodeKind.FieldAccessExpression;

    public BoundExpression Target { get; }

    public TypeFieldSymbol Field { get; }
}

public sealed class BoundUnaryExpression : BoundExpression
{
    public BoundUnaryExpression(TextLocation location, SyntaxKind operatorKind, BoundExpression operand, TypeSymbol type)
        : base(location, type)
    {
        ArgumentNullException.ThrowIfNull(operand);

        OperatorKind = operatorKind;
        Operand = operand;
    }

    public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;

    public SyntaxKind OperatorKind { get; }

    public BoundExpression Operand { get; }
}

public sealed class BoundBinaryExpression : BoundExpression
{
    public BoundBinaryExpression(TextLocation location, BoundExpression left, SyntaxKind operatorKind, BoundExpression right, TypeSymbol type)
        : base(location, type)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        Left = left;
        OperatorKind = operatorKind;
        Right = right;
    }

    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;

    public BoundExpression Left { get; }

    public SyntaxKind OperatorKind { get; }

    public BoundExpression Right { get; }
}

public sealed class BoundCallExpression : BoundExpression
{
    public BoundCallExpression(TextLocation location, BoundExpression target, IReadOnlyList<BoundExpression> arguments, TypeSymbol type)
        : base(location, type)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(arguments);

        Target = target;
        Arguments = arguments;
    }

    public override BoundNodeKind Kind => BoundNodeKind.CallExpression;

    public BoundExpression Target { get; }

    public IReadOnlyList<BoundExpression> Arguments { get; }
}

public sealed class BoundConversionExpression : BoundExpression
{
    public BoundConversionExpression(TextLocation location, TypeSymbol type, BoundExpression expression)
        : base(location, type)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression = expression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ConversionExpression;

    public BoundExpression Expression { get; }
}

public sealed class BoundErrorExpression : BoundExpression
{
    public BoundErrorExpression(TextLocation location)
        : base(location, TypeSymbol.Error)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;
}
