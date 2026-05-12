using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private static bool EvaluateContains(string text, string fragment)
    {
        return text.Contains(fragment, StringComparison.Ordinal);
    }

    private static bool EvaluateStartsWith(string text, string fragment)
    {
        return text.StartsWith(fragment, StringComparison.Ordinal);
    }

    private static bool EvaluateEndsWith(string text, string fragment)
    {
        return text.EndsWith(fragment, StringComparison.Ordinal);
    }

    private string EvaluateReplace(BoundCallExpression expression, string text, string oldText, string newText)
    {
        if (oldText.Length == 0)
        {
            ReportBuiltInArgumentError(
                "REPLACE",
                "oldText",
                "не может быть пустой строкой.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        return text.Replace(oldText, newText, StringComparison.Ordinal);
    }

    private string EvaluateReplaceMany(
        BoundCallExpression expression,
        string text,
        RuntimeArrayValue oldItems,
        RuntimeArrayValue newItems)
    {
        var oldRules = ReadReplaceManyRules(expression, "oldItems", argumentIndex: 1, oldItems);
        var newRules = ReadReplaceManyRules(expression, "newItems", argumentIndex: 2, newItems);

        if (oldRules.Length != newRules.Length)
        {
            ReportBuiltInArgumentError(
                "REPLACEMANY",
                "newItems",
                "должен иметь ту же длину, что и oldItems.",
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }

        for (var index = 0; index < oldRules.Length; index++)
        {
            if (oldRules[index].Length == 0)
            {
                ReportBuiltInArgumentError(
                    "REPLACEMANY",
                    "oldItems",
                    "не должен содержать пустые строки.",
                    GetBuiltInArgumentLocation(expression, 1));
                throw new RuntimeFailureException();
            }
        }

        return RuntimeStringReplaceMany.Replace(text, oldRules, newRules);
    }

    private string[] ReadReplaceManyRules(
        BoundCallExpression expression,
        string argumentName,
        int argumentIndex,
        RuntimeArrayValue array)
    {
        if (!array.IsAllocated)
        {
            return [];
        }

        if (array.Rank != 1)
        {
            ReportBuiltInArgumentError(
                "REPLACEMANY",
                argumentName,
                "должен быть одномерным массивом строк.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        var values = new string[array.Length];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = array.GetValue(array.LowerBound + index).AsString();
        }

        return values;
    }

    private static int EvaluateInStr(int start, string text, string fragment)
    {
        if (start > text.Length + 1)
        {
            return 0;
        }

        var index = text.IndexOf(fragment, start - 1, StringComparison.Ordinal);
        return index < 0 ? 0 : index + 1;
    }

    private static int EvaluateInStrRev(int start, string text, string fragment)
    {
        var searchStart = Math.Min(start, text.Length);

        if (fragment.Length == 0)
        {
            return searchStart + 1;
        }

        if (text.Length == 0)
        {
            return 0;
        }

        var index = text.LastIndexOf(fragment, searchStart - 1, StringComparison.Ordinal);
        return index < 0 ? 0 : index + 1;
    }

    private int EvaluateSubStrCount(BoundCallExpression expression, string text, string fragment)
    {
        if (fragment.Length == 0)
        {
            ReportBuiltInArgumentError(
                "SUBSTRCOUNT",
                "fragment",
                "не может быть пустым.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        var count = 0;
        var startIndex = 0;

        while (startIndex <= text.Length - fragment.Length)
        {
            var index = text.IndexOf(fragment, startIndex, StringComparison.Ordinal);

            if (index < 0)
            {
                break;
            }

            count++;
            startIndex = index + fragment.Length;
        }

        return count;
    }
}
