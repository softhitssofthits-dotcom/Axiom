using Axiom.Core;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private static readonly VariableSymbol PredefinedRowVariable = new("ROW", TypeSymbol.Integer, isReadOnly: true, isGlobal: true);
    private static readonly VariableSymbol PredefinedColumnVariable = new("COLUMN", TypeSymbol.Integer, isReadOnly: true, isGlobal: true);
    private static readonly VariableSymbol PredefinedPiVariable = new("PI", TypeSymbol.Double, isReadOnly: true, isGlobal: true);

    private static bool TryBindPredefinedConstant(string name, TextLocation location, out BoundExpression expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Equals("ROW", StringComparison.OrdinalIgnoreCase))
        {
            expression = new BoundLiteralExpression(location, 1, TypeSymbol.Integer);
            return true;
        }

        if (name.Equals("COLUMN", StringComparison.OrdinalIgnoreCase))
        {
            expression = new BoundLiteralExpression(location, 2, TypeSymbol.Integer);
            return true;
        }

        if (name.Equals("PI", StringComparison.OrdinalIgnoreCase))
        {
            expression = new BoundLiteralExpression(location, Math.PI, TypeSymbol.Double);
            return true;
        }

        expression = null!;
        return false;
    }

    private static bool TryLookupPredefinedConstantVariable(string name, out VariableSymbol variable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Equals("ROW", StringComparison.OrdinalIgnoreCase))
        {
            variable = PredefinedRowVariable;
            return true;
        }

        if (name.Equals("COLUMN", StringComparison.OrdinalIgnoreCase))
        {
            variable = PredefinedColumnVariable;
            return true;
        }

        if (name.Equals("PI", StringComparison.OrdinalIgnoreCase))
        {
            variable = PredefinedPiVariable;
            return true;
        }

        variable = null!;
        return false;
    }

    private static bool TryResolvePredefinedIntegralConstant(string name, out long value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Equals("ROW", StringComparison.OrdinalIgnoreCase))
        {
            value = 1;
            return true;
        }

        if (name.Equals("COLUMN", StringComparison.OrdinalIgnoreCase))
        {
            value = 2;
            return true;
        }

        value = 0;
        return false;
    }
}
