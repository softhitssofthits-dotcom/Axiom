using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    private void LowerVariableDeclaration(BoundVariableDeclarationStatement declaration, List<LoweredInstruction> instructions)
    {
        if (declaration.Variable.IsArray)
        {
            LowerArrayDeclaration(declaration, instructions);
            return;
        }

        if (declaration.LowerBound is not null || declaration.UpperBound is not null)
        {
            ReportUnsupported(declaration, "объявление массива");
            return;
        }

        if (declaration.Initializer is null)
        {
            if (declaration.Variable.Type is UserDefinedTypeSymbol recordType)
            {
                instructions.Add(LoweredInstruction.CreateRecord(declaration.Location, recordType));
                instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
                return;
            }

            // Объявление словаря создаёт пустой готовый словарь: тип ключа и
            // значения уже заданы в объявлении.
            if (declaration.Variable.Type is DictionaryTypeSymbol dictionaryType)
            {
                instructions.Add(LoweredInstruction.CreateDictionary(declaration.Location, dictionaryType));
                instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
                return;
            }

            // Объявление множества создаёт пустое готовое множество: тип
            // элемента уже задан в объявлении.
            if (declaration.Variable.Type is SetTypeSymbol setType)
            {
                instructions.Add(LoweredInstruction.CreateSet(declaration.Location, setType));
                instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
                return;
            }

            if (declaration.Variable.Type == TypeSymbol.Date
                || declaration.Variable.Type == TypeSymbol.Time
                || declaration.Variable.Type == TypeSymbol.DateTime
                || declaration.Variable.Type == TypeSymbol.UtcDateTime)
            {
                var initialText = declaration.Variable.Type == TypeSymbol.Date ? "0001-01-01"
                    : declaration.Variable.Type == TypeSymbol.Time ? "00:00:00"
                    : declaration.Variable.Type == TypeSymbol.UtcDateTime ? "0001-01-01T00:00:00Z"
                    : "0001-01-01 00:00:00";
                instructions.Add(LoweredInstruction.LoadLiteral(declaration.Location, initialText, TypeSymbol.String));
                instructions.Add(LoweredInstruction.Convert(declaration.Location, declaration.Variable.Type));
                instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
                return;
            }

            if (!TryCreateDefaultLiteral(declaration.Variable.Type, out var defaultValue))
            {
                if (CanUseImplicitDefaultSlotValue(declaration.Variable.Type))
                {
                    return;
                }

                ReportUnsupported(declaration, "объявление переменной без инициализатора этого типа");
                return;
            }

            instructions.Add(LoweredInstruction.LoadLiteral(declaration.Location, defaultValue, declaration.Variable.Type));
            instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
            return;
        }

        LowerExpression(declaration.Initializer, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
    }

    private void LowerAssignment(BoundAssignmentStatement assignment, List<LoweredInstruction> instructions)
    {
        if (assignment.Target is BoundVariableExpression directVariable)
        {
            if (TryLowerStringAppendAssignment(assignment, directVariable.Variable, instructions))
            {
                return;
            }

            LowerExpression(assignment.Expression, instructions);
            instructions.Add(LoweredInstruction.StoreVariable(assignment.Location, directVariable.Variable));
            return;
        }

        if (assignment.Target is BoundArrayAccessExpression arrayAccess)
        {
            LowerExpression(assignment.Expression, instructions);
            LowerExpression(arrayAccess.Target, instructions);
            foreach (var index in arrayAccess.Indices)
            {
                LowerExpression(index, instructions);
            }

            instructions.Add(LoweredInstruction.StoreElement(assignment.Location, arrayAccess.DisplayName, arrayAccess.Indices.Count));
            return;
        }

        if (assignment.Target is BoundDictionaryAccessExpression dictionaryAccess)
        {
            LowerDictionaryAssignment(assignment, dictionaryAccess, instructions);
            return;
        }

        if (assignment.Target is BoundFieldAccessExpression fieldAccess)
        {
            LowerExpression(assignment.Expression, instructions);
            LowerExpression(fieldAccess.Target, instructions);
            instructions.Add(LoweredInstruction.StoreField(assignment.Location, fieldAccess.Field.Name));
            return;
        }

        if (assignment.Target is not BoundVariableExpression variable)
        {
            ReportUnsupported(assignment, "цель присваивания");
            return;
        }

        LowerExpression(assignment.Expression, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(assignment.Location, variable.Variable));
    }


    private void LowerArrayDeclaration(BoundVariableDeclarationStatement declaration, List<LoweredInstruction> instructions)
    {
        if (declaration.Initializer is not null)
        {
            LowerExpression(declaration.Initializer, instructions);
            instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
            return;
        }

        if (declaration.ArrayBounds.Count == 0)
        {
            return;
        }

        instructions.Add(LoweredInstruction.LoadVariable(declaration.Location, declaration.Variable));
        foreach (var arrayBound in declaration.ArrayBounds)
        {
            LowerExpression(arrayBound.LowerBound, instructions);
            LowerExpression(arrayBound.UpperBound, instructions);
        }

        instructions.Add(LoweredInstruction.RedimArray(
            declaration.Location,
            declaration.Variable.Name,
            preserve: false,
            declaration.ArrayBounds.Count));
    }

    private void LowerErase(BoundEraseStatement erase, List<LoweredInstruction> instructions)
    {
        LowerExpression(erase.Target, instructions);
        instructions.Add(LoweredInstruction.EraseArray(erase.Location, erase.DisplayName));
    }

    private void LowerRedim(BoundRedimStatement redim, List<LoweredInstruction> instructions)
    {
        LowerExpression(redim.Target, instructions);
        foreach (var arrayBound in redim.ArrayBounds)
        {
            LowerExpression(arrayBound.LowerBound, instructions);
            LowerExpression(arrayBound.UpperBound, instructions);
        }

        instructions.Add(LoweredInstruction.RedimArray(
            redim.Location,
            redim.DisplayName,
            redim.Preserve,
            redim.ArrayBounds.Count));
    }

    private void LowerPrint(BoundPrintStatement print, List<LoweredInstruction> instructions)
    {
        foreach (var item in print.Items)
        {
            LowerExpression(item.Expression, instructions);
            instructions.Add(LoweredInstruction.PrintValue(item.Expression.Location));

            if (item.SeparatorKind is { } separatorKind)
            {
                instructions.Add(LoweredInstruction.PrintSeparator(item.Expression.Location, separatorKind));
            }
        }

        var trailingSeparator = print.Items.Count == 0
            ? null
            : print.Items[^1].SeparatorKind;

        if (trailingSeparator is null)
        {
            instructions.Add(LoweredInstruction.PrintLine(print.Location));
        }
    }

    private void LowerPrintUsing(BoundPrintUsingStatement printUsing, List<LoweredInstruction> instructions)
    {
        LowerExpression(printUsing.FormatExpression, instructions);

        foreach (var item in printUsing.Items)
        {
            LowerExpression(item.Expression, instructions);
        }

        var trailingSeparator = printUsing.Items.Count == 0
            ? null
            : printUsing.Items[^1].SeparatorKind;

        instructions.Add(LoweredInstruction.PrintUsing(printUsing.Location, printUsing.Items.Count, trailingSeparator));
    }

}
