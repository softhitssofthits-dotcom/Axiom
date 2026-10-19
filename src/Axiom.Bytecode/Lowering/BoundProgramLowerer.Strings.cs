using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    private bool TryLowerStringAppendAssignment(
        BoundAssignmentStatement assignment,
        VariableSymbol targetVariable,
        List<LoweredInstruction> instructions)
    {
        if (targetVariable.Type != TypeSymbol.String
            || assignment.Expression is not BoundBinaryExpression binary
            || binary.Left is not BoundVariableExpression leftVariable
            || !ReferenceEquals(leftVariable.Variable, targetVariable))
        {
            return false;
        }

        var useDisplayString = binary.OperatorKind == SyntaxKind.AmpersandToken;
        if (!useDisplayString
            && (binary.OperatorKind != SyntaxKind.PlusToken || binary.Type != TypeSymbol.String))
        {
            return false;
        }

        // Присоединение читает слот после правой части. Вызов справа может
        // изменить слот через глобальную переменную, BYREF или замыкание.
        if (!IsSafeStringAppendOperand(binary.Right))
        {
            return false;
        }

        LowerExpression(binary.Right, instructions);
        instructions.Add(LoweredInstruction.AppendStringVariable(assignment.Location, targetVariable, useDisplayString));
        return true;
    }

    private static bool IsSafeStringAppendOperand(BoundExpression expression) => expression switch
    {
        BoundLiteralExpression or BoundVariableExpression or BoundFunctionExpression => true,
        BoundUnaryExpression unary => IsSafeStringAppendOperand(unary.Operand),
        BoundBinaryExpression binary => IsSafeStringAppendOperand(binary.Left) && IsSafeStringAppendOperand(binary.Right),
        BoundConversionExpression conversion => IsSafeStringAppendOperand(conversion.Expression),
        BoundFieldAccessExpression field => IsSafeStringAppendOperand(field.Target),
        BoundArrayAccessExpression array => IsSafeStringAppendOperand(array.Target)
            && array.Indices.All(IsSafeStringAppendOperand),
        BoundInterpolatedStringExpression text => text.Parts.All(part => IsSafeStringAppendOperand(part.Expression)),
        _ => false
    };
}
