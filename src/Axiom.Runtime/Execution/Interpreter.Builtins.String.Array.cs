using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateSplit(
        BoundCallExpression expression,
        string text,
        string separator,
        int separatorArgumentIndex)
    {
        if (separator.Length == 0)
        {
            ReportBuiltInArgumentError(
                "SPLIT",
                "separator",
                "не может быть пустой строкой.",
                GetBuiltInArgumentLocation(expression, separatorArgumentIndex < 0 ? 0 : separatorArgumentIndex));
            throw new RuntimeFailureException();
        }

        var parts = text.Length == 0
            ? []
            : text.Split([separator], StringSplitOptions.None);

        return CreateStringArrayValue(parts);
    }

    private static RuntimeValue CreateStringArrayValue(IReadOnlyList<string> values)
    {
        var arrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.String);
        var elements = values
            .Select(RuntimeValue.FromString)
            .ToArray();
        return CreateCompactArrayValue(arrayType, elements);
    }

    private static RuntimeValue EvaluateJoin(RuntimeArrayValue array, string separator)
    {
        if (!array.IsAllocated || array.Length == 0)
        {
            return RuntimeValue.FromString(string.Empty);
        }

        var values = new string[array.Length];

        for (var index = 0; index < array.Length; index++)
        {
            values[index] = array.GetValue(array.LowerBound + index).AsString();
        }

        return RuntimeValue.FromString(string.Join(separator, values));
    }
}
