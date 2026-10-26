using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    // Порядок вычисления фиксирован: сначала получатель, затем аргумент.
    // Побочный эффект в аргументе вычисляется один раз и не повторяется при
    // поиске или хешировании внутри VM.
    private void LowerSetExpression(BoundExpression expression, List<LoweredInstruction> instructions)
    {
        switch (expression)
        {
            case BoundSetCountExpression count:
                LowerExpression(count.Target, instructions);
                instructions.Add(LoweredInstruction.SetCount(count.Location, count.DisplayName));
                break;

            case BoundSetMethodCallExpression method:
                LowerSetMethodCall(method, instructions);
                break;

            default:
                ReportUnsupported(expression, "выражение множества");
                break;
        }
    }

    private void LowerSetMethodCall(BoundSetMethodCallExpression method, List<LoweredInstruction> instructions)
    {
        switch (method.Method)
        {
            case BoundSetMethodKind.Add:
                LowerSetWithElement(method, instructions, LoweredInstruction.SetAdd(method.Location, method.DisplayName));
                break;

            case BoundSetMethodKind.Contains:
                LowerSetWithElement(method, instructions, LoweredInstruction.SetContains(method.Location, method.DisplayName));
                break;

            case BoundSetMethodKind.Remove:
                LowerSetWithElement(method, instructions, LoweredInstruction.SetRemove(method.Location, method.DisplayName));
                break;

            case BoundSetMethodKind.ToArray:
                LowerExpression(method.Target, instructions);
                instructions.Add(LoweredInstruction.SetToArray(method.Location, method.DisplayName));
                break;

            case BoundSetMethodKind.UnionWith:
                LowerSetWithSet(method, instructions, LoweredInstruction.SetUnionWith(method.Location, method.DisplayName));
                break;

            case BoundSetMethodKind.IntersectWith:
                LowerSetWithSet(method, instructions, LoweredInstruction.SetIntersectWith(method.Location, method.DisplayName));
                break;

            default:
                ReportUnsupported(method, "метод множества");
                break;
        }
    }

    private void LowerSetWithElement(
        BoundSetMethodCallExpression method,
        List<LoweredInstruction> instructions,
        LoweredInstruction instruction)
    {
        if (method.Value is null)
        {
            ReportUnsupported(method, "аргумент метода множества");
            return;
        }

        LowerExpression(method.Target, instructions);
        LowerExpression(method.Value, instructions);
        instructions.Add(instruction);
    }

    private void LowerSetWithSet(
        BoundSetMethodCallExpression method,
        List<LoweredInstruction> instructions,
        LoweredInstruction instruction)
    {
        if (method.Value is null)
        {
            ReportUnsupported(method, "второе множество");
            return;
        }

        LowerExpression(method.Target, instructions);
        LowerExpression(method.Value, instructions);
        instructions.Add(instruction);
    }
}
