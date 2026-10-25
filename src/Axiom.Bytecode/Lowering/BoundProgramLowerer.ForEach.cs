using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    // Источник вычисляется один раз и сохраняется вместе с его структурной
    // версией. BYVAL копирует значение в новую ячейку, BYREF связывает свежую
    // ячейку-адрес с текущим элементом. Последнее измерение меняется быстрее.
    private void LowerForEach(BoundForEachStatement forEachStatement, List<LoweredInstruction> instructions)
    {
        var sourceVariable = CreateInternalTemporary(forEachStatement.SourceArrayType, "__foreach_source");
        var structuralVersionVariable = CreateInternalTemporary(TypeSymbol.Long, "__foreach_revision");
        var rank = forEachStatement.SourceArrayType.Rank;
        var lowerBounds = new VariableSymbol[rank];
        var upperBounds = new VariableSymbol[rank];
        var indices = new VariableSymbol[rank];

        LowerExpression(forEachStatement.Source, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(forEachStatement.Location, sourceVariable));

        // Своя проверка выделенности вместо внутреннего LBOUND: сообщение должно
        // говорить про FOR EACH, а не про границы массива.
        EmitForEachAllocationCheck(sourceVariable, forEachStatement.SourceArrayType, forEachStatement.Location, instructions);

        instructions.Add(LoweredInstruction.LoadVariable(forEachStatement.Location, sourceVariable));
        instructions.Add(LoweredInstruction.GetArrayStructuralVersion(forEachStatement.Location));
        instructions.Add(LoweredInstruction.StoreVariable(forEachStatement.Location, structuralVersionVariable));

        for (var dimension = 0; dimension < rank; dimension++)
        {
            lowerBounds[dimension] = CreateInternalTemporary(TypeSymbol.Integer, "__foreach_lower");
            upperBounds[dimension] = CreateInternalTemporary(TypeSymbol.Integer, "__foreach_upper");
            indices[dimension] = CreateInternalTemporary(TypeSymbol.Integer, "__foreach_index");

            LowerArrayBoundCall(
                BuiltinFunctionId.LBound,
                sourceVariable,
                forEachStatement.SourceArrayType,
                dimension + 1,
                forEachStatement.Location,
                instructions);
            instructions.Add(LoweredInstruction.StoreVariable(forEachStatement.Location, lowerBounds[dimension]));

            LowerArrayBoundCall(
                BuiltinFunctionId.UBound,
                sourceVariable,
                forEachStatement.SourceArrayType,
                dimension + 1,
                forEachStatement.Location,
                instructions);
            instructions.Add(LoweredInstruction.StoreVariable(forEachStatement.Location, upperBounds[dimension]));
        }

        var exitLabel = CreateLabel();
        var endLabels = new string[rank];
        var continueLabels = new string[rank];
        for (var dimension = 0; dimension < rank; dimension++)
        {
            endLabels[dimension] = CreateLabel();
            continueLabels[dimension] = CreateLabel();
        }

        // EXIT FOR покидает обход целиком, CONTINUE FOR переходит к следующему
        // элементу, то есть к продвижению самого внутреннего счётчика.
        _forExitLabels.Push(new LoopExitContext(exitLabel, _activeFinallyContexts.Count));
        _forContinueLabels.Push(new LoopExitContext(continueLabels[rank - 1], _activeFinallyContexts.Count));

        try
        {
            EmitForEachLevel(
                forEachStatement,
                level: 0,
                rank,
                sourceVariable,
                structuralVersionVariable,
                lowerBounds,
                upperBounds,
                indices,
                endLabels,
                continueLabels,
                instructions);
        }
        finally
        {
            _forContinueLabels.Pop();
            _forExitLabels.Pop();
        }

        instructions.Add(LoweredInstruction.LabelMarker(forEachStatement.Location, exitLabel));
        instructions.Add(LoweredInstruction.ClearForEachItem(forEachStatement.Location, forEachStatement.Variable));
    }

    private void EmitForEachLevel(
        BoundForEachStatement forEachStatement,
        int level,
        int rank,
        VariableSymbol sourceVariable,
        VariableSymbol structuralVersionVariable,
        VariableSymbol[] lowerBounds,
        VariableSymbol[] upperBounds,
        VariableSymbol[] indices,
        string[] endLabels,
        string[] continueLabels,
        List<LoweredInstruction> instructions)
    {
        var location = forEachStatement.Location;

        if (level == rank)
        {
            LowerForEachIteration(
                forEachStatement,
                sourceVariable,
                structuralVersionVariable,
                indices,
                rank,
                instructions);
            return;
        }

        var startLabel = CreateLabel();

        // Счётчик измерения начинается с фактической нижней границы.
        instructions.Add(LoweredInstruction.LoadVariable(location, lowerBounds[level]));
        instructions.Add(LoweredInstruction.StoreVariable(location, indices[level]));

        instructions.Add(LoweredInstruction.LabelMarker(location, startLabel));
        if (level == 0)
        {
            EmitLoopIterationBoundary(location, instructions);
        }

        // Выход при пустом диапазоне и при исчерпании измерения.
        instructions.Add(LoweredInstruction.LoadVariable(location, indices[level]));
        instructions.Add(LoweredInstruction.LoadVariable(location, upperBounds[level]));
        instructions.Add(LoweredInstruction.BinaryOperation(location, SyntaxKind.GreaterToken));
        instructions.Add(LoweredInstruction.JumpIfTrue(location, endLabels[level]));

        EmitForEachLevel(
            forEachStatement,
            level + 1,
            rank,
            sourceVariable,
            structuralVersionVariable,
            lowerBounds,
            upperBounds,
            indices,
            endLabels,
            continueLabels,
            instructions);

        instructions.Add(LoweredInstruction.LabelMarker(location, continueLabels[level]));

        if (level == rank - 1)
        {
            EmitForEachStructuralCheck(
                sourceVariable,
                structuralVersionVariable,
                location,
                instructions);
        }

        // Продвижение выполняется только когда счётчик ещё не на верхней границе.
        // Иначе последний элемент с границей INTEGER_MAX увеличивался бы до
        // переполнения ещё до проверки исчерпания диапазона.
        instructions.Add(LoweredInstruction.LoadVariable(location, indices[level]));
        instructions.Add(LoweredInstruction.LoadVariable(location, upperBounds[level]));
        instructions.Add(LoweredInstruction.BinaryOperation(location, SyntaxKind.GreaterOrEqualsToken));
        instructions.Add(LoweredInstruction.JumpIfTrue(location, endLabels[level]));

        instructions.Add(LoweredInstruction.LoadVariable(location, indices[level]));
        instructions.Add(LoweredInstruction.LoadLiteral(location, 1, TypeSymbol.Integer));
        instructions.Add(LoweredInstruction.BinaryOperation(location, SyntaxKind.PlusToken));
        instructions.Add(LoweredInstruction.StoreVariable(location, indices[level]));
        instructions.Add(LoweredInstruction.Jump(location, startLabel));
        instructions.Add(LoweredInstruction.LabelMarker(location, endLabels[level]));
    }

    private void LowerForEachIteration(
        BoundForEachStatement forEachStatement,
        VariableSymbol sourceVariable,
        VariableSymbol structuralVersionVariable,
        VariableSymbol[] indices,
        int rank,
        List<LoweredInstruction> instructions)
    {
        var location = forEachStatement.Location;

        if (forEachStatement.ByRef)
        {
            instructions.Add(LoweredInstruction.BindArrayElement(
                location,
                forEachStatement.Variable,
                sourceVariable,
                structuralVersionVariable,
                indices));
        }
        else
        {
            instructions.Add(LoweredInstruction.FreshLocal(location, forEachStatement.Variable));
            instructions.Add(LoweredInstruction.LoadVariable(location, sourceVariable));
            foreach (var index in indices)
            {
                instructions.Add(LoweredInstruction.LoadVariable(location, index));
            }

            instructions.Add(LoweredInstruction.LoadElement(location, sourceVariable.Name, rank));

            if (forEachStatement.Variable.Type != forEachStatement.SourceArrayType.ElementType
                && forEachStatement.Variable.Type != TypeSymbol.Error)
            {
                instructions.Add(LoweredInstruction.Convert(location, forEachStatement.Variable.Type));
            }

            instructions.Add(LoweredInstruction.StoreVariable(location, forEachStatement.Variable));
        }

        foreach (var statement in forEachStatement.Statements)
        {
            LowerStatement(statement, instructions);
        }
    }

    private void EmitForEachStructuralCheck(
        VariableSymbol sourceVariable,
        VariableSymbol structuralVersionVariable,
        TextLocation location,
        List<LoweredInstruction> instructions)
    {
        var validLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LoadVariable(location, sourceVariable));
        instructions.Add(LoweredInstruction.GetArrayStructuralVersion(location));
        instructions.Add(LoweredInstruction.LoadVariable(location, structuralVersionVariable));
        instructions.Add(LoweredInstruction.BinaryOperation(location, SyntaxKind.EqualsToken));
        instructions.Add(LoweredInstruction.JumpIfTrue(location, validLabel));
        instructions.Add(LoweredInstruction.LoadLiteral(location, "MB3014", TypeSymbol.String));
        instructions.Add(LoweredInstruction.LoadLiteral(
            location,
            "Массив был переразмерен или освобождён во время обхода FOR EACH.",
            TypeSymbol.String));
        instructions.Add(LoweredInstruction.RaiseError(location, hasMessage: true));
        instructions.Add(LoweredInstruction.LabelMarker(location, validLabel));
    }

    // Проверка выделенности: обход невыделенного массива даёт MB3005 с
    // сообщением о FOR EACH, а не о границах. Выделенный пустой массив
    // допустим и даёт ноль итераций.
    private void EmitForEachAllocationCheck(
        VariableSymbol sourceVariable,
        ArrayTypeSymbol sourceType,
        TextLocation location,
        List<LoweredInstruction> instructions)
    {
        if (!BuiltinFunctions.TryGetDescriptors("ARRAYISALLOCATED", out var descriptors)
            || descriptors.FirstOrDefault(static descriptor => descriptor.Parameters.Count == 1) is not { } descriptor)
        {
            ReportUnsupported(location, "проверка выделенности FOR EACH");
            return;
        }

        var skipLabel = CreateLabel();
        var symbol = descriptor.CreateResolvedSymbol([sourceType], TypeSymbol.Boolean);

        instructions.Add(LoweredInstruction.LoadVariable(location, sourceVariable));
        instructions.Add(LoweredInstruction.CallFunction(location, symbol));
        instructions.Add(LoweredInstruction.JumpIfTrue(location, skipLabel));

        // Код и текст ошибки передаются в RaiseError; формат совпадает с ERROR.
        instructions.Add(LoweredInstruction.LoadLiteral(location, "MB3005", TypeSymbol.String));
        instructions.Add(LoweredInstruction.LoadLiteral(
            location,
            "FOR EACH: источник содержит массив, который ещё не выделен. Выделите массив через DIM с границами или присвойте ему значение.",
            TypeSymbol.String));
        instructions.Add(LoweredInstruction.RaiseError(location, hasMessage: true));

        instructions.Add(LoweredInstruction.LabelMarker(location, skipLabel));
    }

    private void LowerArrayBoundCall(
        BuiltinFunctionId id,
        VariableSymbol arrayVariable,
        ArrayTypeSymbol arrayType,
        int dimension,
        TextLocation location,
        List<LoweredInstruction> instructions)
    {
        if (!BuiltinFunctions.TryGetDescriptors(id == BuiltinFunctionId.LBound ? "LBOUND" : "UBOUND", out var descriptors)
            || descriptors.FirstOrDefault(static descriptor => descriptor.Parameters.Count == 2) is not { } descriptor)
        {
            ReportUnsupported(location, "границы массива в FOR EACH");
            return;
        }

        var symbol = descriptor.CreateResolvedSymbol([arrayType, TypeSymbol.Integer], TypeSymbol.Integer);
        instructions.Add(LoweredInstruction.LoadVariable(location, arrayVariable));
        instructions.Add(LoweredInstruction.LoadLiteral(location, dimension, TypeSymbol.Integer));
        instructions.Add(LoweredInstruction.CallFunction(location, symbol));
    }
}
