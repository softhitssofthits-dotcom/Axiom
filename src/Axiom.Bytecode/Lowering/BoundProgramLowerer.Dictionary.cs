using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    // Чтение по ключу: словарь и ключ вычисляются один раз, отсутствующий
    // ключ сообщает ошибку времени выполнения.
    private void LowerDictionaryAccess(BoundDictionaryAccessExpression access, List<LoweredInstruction> instructions)
    {
        LowerExpression(access.Target, instructions);
        LowerExpression(access.Key, instructions);
        instructions.Add(LoweredInstruction.LoadDictionaryValue(access.Location, access.DisplayName));
    }

    // Присваивание по ключу заменяет существующую запись либо добавляет
    // отсутствующую.
    private void LowerDictionaryAssignment(BoundAssignmentStatement assignment, BoundDictionaryAccessExpression access, List<LoweredInstruction> instructions)
    {
        LowerExpression(assignment.Expression, instructions);
        LowerExpression(access.Target, instructions);
        LowerExpression(access.Key, instructions);
        instructions.Add(LoweredInstruction.StoreDictionaryValue(assignment.Location, access.DisplayName));
    }

    private void LowerDictionaryExpression(BoundExpression expression, List<LoweredInstruction> instructions)
    {
        switch (expression)
        {
            case BoundDictionaryAccessExpression access:
                LowerDictionaryAccess(access, instructions);
                break;

            case BoundDictionaryCountExpression count:
                LowerExpression(count.Target, instructions);
                instructions.Add(LoweredInstruction.DictionaryCount(count.Location, count.DisplayName));
                break;

            case BoundDictionaryMethodCallExpression method:
                LowerDictionaryMethodCall(method, instructions);
                break;

            case BoundDictionaryTryGetValueExpression tryGetValue:
                LowerDictionaryTryGetValue(tryGetValue, instructions);
                break;

            default:
                ReportUnsupported(expression, "выражение словаря");
                break;
        }
    }

    private void LowerDictionaryMethodCall(BoundDictionaryMethodCallExpression method, List<LoweredInstruction> instructions)
    {
        switch (method.Method)
        {
            case BoundDictionaryMethodKind.Add:
                LowerExpression(method.Target, instructions);
                LowerExpression(method.Key!, instructions);
                LowerExpression(method.Value!, instructions);
                instructions.Add(LoweredInstruction.DictionaryAdd(method.Location, method.DisplayName));
                break;

            case BoundDictionaryMethodKind.Exists:
                LowerExpression(method.Target, instructions);
                LowerExpression(method.Key!, instructions);
                instructions.Add(LoweredInstruction.DictionaryExists(method.Location, method.DisplayName));
                break;

            case BoundDictionaryMethodKind.Remove:
                LowerExpression(method.Target, instructions);
                LowerExpression(method.Key!, instructions);
                instructions.Add(LoweredInstruction.DictionaryRemove(method.Location, method.DisplayName));
                break;

            case BoundDictionaryMethodKind.Keys:
                LowerExpression(method.Target, instructions);
                instructions.Add(LoweredInstruction.DictionaryKeys(method.Location, method.DisplayName));
                break;

            default:
                ReportUnsupported(method, "метод словаря");
                break;
        }
    }

    private void LowerDictionaryTryGetValue(BoundDictionaryTryGetValueExpression tryGetValue, List<LoweredInstruction> instructions)
    {
        if (tryGetValue.Destination is not BoundVariableExpression destination)
        {
            ReportUnsupported(tryGetValue, "переменная результата TryGetValue");
            return;
        }

        LowerExpression(tryGetValue.Target, instructions);
        LowerExpression(tryGetValue.Key, instructions);
        instructions.Add(LoweredInstruction.DictionaryTryGetValue(
            tryGetValue.Location,
            tryGetValue.DisplayName,
            destination.Variable));
    }
}
