using Axiom.Execution;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode.Tests;

public sealed class BoundProgramLowererTests
{
    [Fact]
    public void Lower_ScalarStatements_ProducesLinearEntryPoint()
    {
        var result = Lower("""
            DIM x AS INTEGER = 1
            x = x + 2
            PRINT x
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var entryPoint = result.Program.EntryPoint;
        Assert.True(entryPoint.IsEntryPoint);
        Assert.Equal(LoweredProgram.EntryPointName, entryPoint.Name);
        Assert.Equal(TypeSymbol.Void, entryPoint.ResultType);

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.Return
            ],
            entryPoint.Instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.All(entryPoint.Instructions, static instruction => Assert.True(instruction.Location.Line >= 1));
        Assert.Equal("x", entryPoint.Instructions[1].Variable?.Name);
        Assert.Equal(SyntaxKind.PlusToken, entryPoint.Instructions[4].OperatorKind);
    }

    [Fact]
    public void Lower_RepeatedProgram_ProducesDeterministicInstructionShape()
    {
        const string source = """
            DIM total AS INTEGER = 1
            total = total * 10
            PRINT total
            """;

        var first = Lower(source).Program!;
        var second = Lower(source).Program!;

        Assert.Equal(Project(first), Project(second));
    }

    [Fact]
    public void Lower_PrintWithTrailingComma_DoesNotEmitPrintLine()
    {
        var result = Lower("PRINT 1,");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintSeparator,
                LoweredInstructionKind.Return
            ],
            result.Program.EntryPoint.Instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal(PrintSeparatorKind.Comma, result.Program.EntryPoint.Instructions[2].SeparatorKind);
    }

    [Fact]
    public void Lower_PrintWithTrailingSemicolon_DoesNotEmitPrintLine()
    {
        var result = Lower("PRINT 1;");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintSeparator,
                LoweredInstructionKind.Return
            ],
            result.Program.EntryPoint.Instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal(PrintSeparatorKind.Semicolon, result.Program.EntryPoint.Instructions[2].SeparatorKind);
    }

    [Fact]
    public void Lower_UserRoutine_AddsStableRoutineAfterEntryPoint()
    {
        var result = Lower("""
            FUNCTION AddOne(value AS INTEGER) AS INTEGER
                RETURN value + 1
            END FUNCTION

            PRINT AddOne(2)
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var routine = Assert.Single(result.Program.Routines);
        Assert.Equal(1, routine.Id);
        Assert.Equal("AddOne", routine.Name);
        Assert.Equal(TypeSymbol.Integer, routine.ResultType);
        Assert.False(routine.IsEntryPoint);
        Assert.Equal(
            [
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.Return
            ],
            routine.Instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Contains(
            result.Program.EntryPoint.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.CallFunction
                && instruction.Function?.Name == "AddOne");
    }

    [Fact]
    public void Lower_IndirectFunctionValueCall_EmitsCallValue()
    {
        var result = Lower("""
            FUNCTION AddOne(value AS INTEGER) AS INTEGER
                RETURN value + 1
            END FUNCTION

            DIM op AS FUNCTION(INTEGER) AS INTEGER
            op = AddOne
            PRINT op(4)
            """);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));

        var instructions = result.Program!.EntryPoint.Instructions;
        Assert.Contains(
            instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.CallValue
                && instruction.ValueCount == 1
                && instruction.ValueType == TypeSymbol.Integer);
    }

    [Fact]
    public void Lower_ScalarDeclarationWithoutInitializer_EmitsDefaultStore()
    {
        var result = Lower("""
            DIM ready AS BOOLEAN
            DIM name AS STRING
            PRINT ready
            PRINT name
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal(false, instructions[0].LiteralValue);
        Assert.Equal(TypeSymbol.Boolean, instructions[0].ValueType);
        Assert.Equal("ready", instructions[1].Variable?.Name);
        Assert.Equal(string.Empty, instructions[2].LiteralValue);
        Assert.Equal(TypeSymbol.String, instructions[2].ValueType);
        Assert.Equal("name", instructions[3].Variable?.Name);
    }

    [Fact]
    public void Lower_PrintUsing_EmitsFormatValuesAndSinglePrintUsingInstruction()
    {
        var result = Lower("""
            PRINT USING "##.##"; 3.14159,
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.PrintUsing,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("##.##", instructions[0].LiteralValue);
        Assert.Equal(3.14159d, instructions[1].LiteralValue);
        Assert.Equal(1, instructions[2].ValueCount);
        Assert.Equal(PrintSeparatorKind.Comma, instructions[2].SeparatorKind);
    }

    [Fact]
    public void Lower_Input_EmitsSingleInputInstructionWithTargets()
    {
        var result = Lower("""
            DIM firstName AS STRING
            DIM age AS INTEGER
            INPUT "Pair"; firstName, age
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var input = Assert.Single(
            result.Program.EntryPoint.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.Input);

        Assert.Equal("Pair", input.PromptText);
        Assert.Equal(2, input.ValueCount);
        Assert.Equal(["firstName", "age"], input.Variables.Select(static variable => variable.Name).ToArray());
    }

    [Fact]
    public void Lower_UserRoutineWithLocalDeclarationWithoutInitializer_EmitsDefaultStoreBeforeReturn()
    {
        var result = Lower("""
            FUNCTION AddBonus(value AS INTEGER) AS INTEGER
                DIM bonus AS INTEGER
                RETURN value + bonus
            END FUNCTION

            PRINT AddBonus(5)
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var routine = Assert.Single(result.Program.Routines);

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.Return
            ],
            routine.Instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal(0, routine.Instructions[0].LiteralValue);
        Assert.Equal(TypeSymbol.Integer, routine.Instructions[0].ValueType);
        Assert.Equal("bonus", routine.Instructions[1].Variable?.Name);
        Assert.Equal("value", routine.Instructions[2].Variable?.Name);
        Assert.Equal("bonus", routine.Instructions[3].Variable?.Name);
        Assert.Equal(SyntaxKind.PlusToken, routine.Instructions[4].OperatorKind);
    }

    [Fact]
    public void Lower_LocalNestedRoutines_AppearAsStableDirectCallRoutines()
    {
        var result = Lower("""
            SUB Outer()
                DIM offset AS INTEGER

                SUB LocalIncrement()
                    offset = offset + 1
                END SUB

                FUNCTION AddLocal(left AS INTEGER, right AS INTEGER) AS INTEGER
                    RETURN left + right + offset
                END FUNCTION

                LocalIncrement()
                PRINT AddLocal(2, 3)
            END SUB

            Outer()
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        Assert.Equal(
            ["LocalIncrement", "AddLocal", "Outer"],
            result.Program.Routines.Select(static routine => routine.Name).ToArray());

        var localIncrementRoutine = result.Program.Routines[0];
        var addLocalRoutine = result.Program.Routines[1];
        var outerRoutine = result.Program.Routines[2];

        Assert.Contains(
            outerRoutine.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.CallFunction
                && instruction.Function?.Name == "LocalIncrement");
        Assert.Contains(
            outerRoutine.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.CallFunction
                && instruction.Function?.Name == "AddLocal");
        Assert.DoesNotContain(
            outerRoutine.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.Pop);

        Assert.Equal(
            [
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Return
            ],
            localIncrementRoutine.Instructions.Select(static instruction => instruction.Kind).ToArray());
        Assert.Equal("offset", localIncrementRoutine.Instructions[0].Variable?.Name);
        Assert.Equal(1, localIncrementRoutine.Instructions[1].LiteralValue);
        Assert.Equal(SyntaxKind.PlusToken, localIncrementRoutine.Instructions[2].OperatorKind);
        Assert.Equal("offset", localIncrementRoutine.Instructions[3].Variable?.Name);

        Assert.Equal(
            [
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.Return
            ],
            addLocalRoutine.Instructions.Select(static instruction => instruction.Kind).ToArray());
        Assert.Equal("left", addLocalRoutine.Instructions[0].Variable?.Name);
        Assert.Equal("right", addLocalRoutine.Instructions[1].Variable?.Name);
        Assert.Equal(SyntaxKind.PlusToken, addLocalRoutine.Instructions[2].OperatorKind);
        Assert.Equal("offset", addLocalRoutine.Instructions[3].Variable?.Name);
        Assert.Equal(SyntaxKind.PlusToken, addLocalRoutine.Instructions[4].OperatorKind);
    }

    [Fact]
    public void Lower_LocalRecursiveFunction_EmitsDirectCallToSelf()
    {
        var result = Lower("""
            FUNCTION Outer(value AS INTEGER) AS INTEGER
                RETURN Factorial(value)

                FUNCTION Factorial(n AS INTEGER) AS INTEGER
                    IF n <= 1 THEN
                        RETURN 1
                    END IF

                    RETURN n * Factorial(n - 1)
                END FUNCTION
            END FUNCTION

            PRINT Outer(4)
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        Assert.Equal(
            ["Factorial", "Outer"],
            result.Program.Routines.Select(static routine => routine.Name).ToArray());

        var factorialRoutine = result.Program.Routines[0];
        var outerRoutine = result.Program.Routines[1];

        Assert.Contains(
            outerRoutine.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.CallFunction
                && instruction.Function?.Name == "Factorial");

        Assert.Contains(
            factorialRoutine.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.CallFunction
                && instruction.Function?.Name == "Factorial");
        Assert.Contains(
            factorialRoutine.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.BinaryOperation
                && instruction.OperatorKind == SyntaxKind.LessOrEqualsToken);
        Assert.Contains(
            factorialRoutine.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.BinaryOperation
                && instruction.OperatorKind == SyntaxKind.MinusToken);
        Assert.Contains(
            factorialRoutine.Instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.BinaryOperation
                && instruction.OperatorKind == SyntaxKind.StarToken);
    }

    [Fact]
    public void Lower_LocalFunction_CanReadOuterVariables()
    {
        var result = Lower("""
            FUNCTION Outer(seed AS INTEGER) AS INTEGER
                DIM offset AS INTEGER = 2
                RETURN Inner(4)

                FUNCTION Inner(multiplier AS INTEGER) AS INTEGER
                    RETURN (seed + offset) * multiplier
                END FUNCTION
            END FUNCTION

            PRINT Outer(3)
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        Assert.Equal(
            ["Inner", "Outer"],
            result.Program.Routines.Select(static routine => routine.Name).ToArray());

        var innerRoutine = result.Program.Routines[0];

        Assert.Equal(
            [
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.Return
            ],
            innerRoutine.Instructions.Select(static instruction => instruction.Kind).ToArray());
        Assert.Equal("seed", innerRoutine.Instructions[0].Variable?.Name);
        Assert.Equal("offset", innerRoutine.Instructions[1].Variable?.Name);
        Assert.Equal(SyntaxKind.PlusToken, innerRoutine.Instructions[2].OperatorKind);
        Assert.Equal("multiplier", innerRoutine.Instructions[3].Variable?.Name);
        Assert.Equal(SyntaxKind.StarToken, innerRoutine.Instructions[4].OperatorKind);
    }

    [Fact]
    public void Lower_IfWithoutElse_EmitsConditionalJumpAndEndLabel()
    {
        var result = Lower("""
            DIM value AS INTEGER = 1
            IF value > 0 THEN
                PRINT value
            END IF
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfFalse,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[5].Label);
        Assert.Equal("L0001", instructions[9].Label);
        Assert.Equal(SyntaxKind.GreaterToken, instructions[4].OperatorKind);
        Assert.All(instructions.Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.JumpIfFalse),
            static instruction => Assert.True(instruction.Location.Line >= 1));
    }

    [Fact]
    public void Lower_IfWithElse_EmitsBranchAndEndLabels()
    {
        var result = Lower("""
            IF 1 = 1 THEN
                PRINT 1
            ELSE
                PRINT 2
            END IF
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfFalse,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[3].Label);
        Assert.Equal("L0002", instructions[7].Label);
        Assert.Equal("L0001", instructions[8].Label);
        Assert.Equal("L0002", instructions[12].Label);
    }

    [Fact]
    public void Lower_ElseIf_UsesNestedDeterministicLabels()
    {
        var result = Lower("""
            IF 1 = 0 THEN
                PRINT 1
            ELSEIF 2 = 2 THEN
                PRINT 2
            ELSE
                PRINT 3
            END IF
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "JumpIfFalse:L0001",
                "Jump:L0002",
                "Label:L0001",
                "JumpIfFalse:L0003",
                "Jump:L0004",
                "Label:L0003",
                "Label:L0004",
                "Label:L0002"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_While_EmitsLoopLabelsAndBackJump()
    {
        var result = Lower("""
            DIM i AS INTEGER = 0
            WHILE i < 3
                PRINT i
                i = i + 1
            END WHILE
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfFalse,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[2].Label);
        Assert.Equal("L0002", instructions[6].Label);
        Assert.Equal("L0001", instructions[14].Label);
        Assert.Equal("L0002", instructions[15].Label);
        Assert.Equal(SyntaxKind.LessToken, instructions[5].OperatorKind);
        Assert.Equal(SyntaxKind.PlusToken, instructions[12].OperatorKind);
    }

    [Fact]
    public void Lower_NestedWhile_UsesDeterministicLabels()
    {
        var result = Lower("""
            WHILE 1 = 1
                WHILE 2 = 2
                    PRINT 1
                END WHILE
            END WHILE
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "JumpIfFalse:L0002",
                "Label:L0003",
                "JumpIfFalse:L0004",
                "Jump:L0003",
                "Label:L0004",
                "Jump:L0001",
                "Label:L0002"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_DoWhile_EmitsLoopLabelsAndBackJump()
    {
        var result = Lower("""
            DIM i AS INTEGER = 0
            DO WHILE i < 2
                PRINT i
                i = i + 1
            LOOP
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfFalse,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[2].Label);
        Assert.Equal("L0002", instructions[6].Label);
        Assert.Equal("L0001", instructions[14].Label);
        Assert.Equal("L0002", instructions[15].Label);
        Assert.Equal(SyntaxKind.LessToken, instructions[5].OperatorKind);
        Assert.Equal(SyntaxKind.PlusToken, instructions[12].OperatorKind);
    }

    [Fact]
    public void Lower_NestedDoWhile_UsesDeterministicLabels()
    {
        var result = Lower("""
            DO WHILE 1 = 1
                DO WHILE 2 = 2
                    PRINT 1
                LOOP
            LOOP
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "JumpIfFalse:L0002",
                "Label:L0003",
                "JumpIfFalse:L0004",
                "Jump:L0003",
                "Label:L0004",
                "Jump:L0001",
                "Label:L0002"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_DoUntil_EmitsTruthExitAndBackJump()
    {
        var result = Lower("""
            DIM i AS INTEGER = 0
            DO UNTIL i >= 2
                PRINT i
                i = i + 1
            LOOP
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfTrue,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[2].Label);
        Assert.Equal("L0002", instructions[6].Label);
        Assert.Equal("L0001", instructions[14].Label);
        Assert.Equal("L0002", instructions[15].Label);
        Assert.Equal(SyntaxKind.GreaterOrEqualsToken, instructions[5].OperatorKind);
        Assert.Equal(SyntaxKind.PlusToken, instructions[12].OperatorKind);
    }

    [Fact]
    public void Lower_NestedDoUntil_UsesDeterministicLabels()
    {
        var result = Lower("""
            DO UNTIL 1 = 0
                DO UNTIL 2 = 3
                    PRINT 1
                LOOP
            LOOP
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "JumpIfTrue:L0002",
                "Label:L0003",
                "JumpIfTrue:L0004",
                "Jump:L0003",
                "Label:L0004",
                "Jump:L0001",
                "Label:L0002"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_DoLoopWhile_EmitsPostCheckBackJump()
    {
        var result = Lower("""
            DIM i AS INTEGER = 0
            DO
                PRINT i
                i = i + 1
            LOOP WHILE i < 2
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfTrue,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[2].Label);
        Assert.Equal("L0001", instructions[13].Label);
        Assert.Equal("L0002", instructions[14].Label);
        Assert.Equal(SyntaxKind.PlusToken, instructions[8].OperatorKind);
        Assert.Equal(SyntaxKind.LessToken, instructions[12].OperatorKind);
    }

    [Fact]
    public void Lower_NestedDoLoopWhile_UsesDeterministicLabels()
    {
        var result = Lower("""
            DO
                DO
                    PRINT 1
                LOOP WHILE 2 = 2
            LOOP WHILE 1 = 1
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "Label:L0003",
                "JumpIfTrue:L0003",
                "Label:L0004",
                "JumpIfTrue:L0001",
                "Label:L0002"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_DoLoopUntil_EmitsPostCheckFalseBackJump()
    {
        var result = Lower("""
            DIM i AS INTEGER = 0
            DO
                PRINT i
                i = i + 1
            LOOP UNTIL i >= 2
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfFalse,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[2].Label);
        Assert.Equal("L0001", instructions[13].Label);
        Assert.Equal("L0002", instructions[14].Label);
        Assert.Equal(SyntaxKind.PlusToken, instructions[8].OperatorKind);
        Assert.Equal(SyntaxKind.GreaterOrEqualsToken, instructions[12].OperatorKind);
    }

    [Fact]
    public void Lower_NestedDoLoopUntil_UsesDeterministicLabels()
    {
        var result = Lower("""
            DO
                DO
                    PRINT 1
                LOOP UNTIL 2 = 3
            LOOP UNTIL 1 = 0
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "Label:L0003",
                "JumpIfFalse:L0003",
                "Label:L0004",
                "JumpIfFalse:L0001",
                "Label:L0002"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_DoLoopWithoutCondition_EmitsInfiniteBackJump()
    {
        var result = Lower("""
            DIM i AS INTEGER = 0
            DO
                PRINT i
                i = i + 1
            LOOP
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[2].Label);
        Assert.Equal("L0001", instructions[10].Label);
        Assert.Equal("L0002", instructions[11].Label);
        Assert.Equal(SyntaxKind.PlusToken, instructions[8].OperatorKind);
    }

    [Fact]
    public void Lower_NestedDoLoopWithoutCondition_UsesDeterministicLabels()
    {
        var result = Lower("""
            DO
                DO
                    PRINT 1
                LOOP
            LOOP
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "Label:L0003",
                "Jump:L0003",
                "Label:L0004",
                "Jump:L0001",
                "Label:L0002"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_ExitDo_EmitsJumpToNearestDoEndLabel()
    {
        var result = Lower("""
            DO
                PRINT 1
                EXIT DO
                PRINT 2
            LOOP
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("L0001", instructions[0].Label);
        Assert.Equal("L0002", instructions[4].Label);
        Assert.Equal("L0001", instructions[8].Label);
        Assert.Equal("L0002", instructions[9].Label);
    }

    [Fact]
    public void Lower_NestedExitDo_TargetsNearestDoLoop()
    {
        var result = Lower("""
            DO
                DO
                    EXIT DO
                LOOP
                PRINT 1
            LOOP
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "Label:L0003",
                "Jump:L0004",
                "Jump:L0003",
                "Label:L0004",
                "Jump:L0001",
                "Label:L0002"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_ForNext_EmitsLoopComparisonAndIncrement()
    {
        var result = Lower("""
            FOR i = 1 TO 3
                PRINT i
            NEXT
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Equal(
            [
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.ValidateForStep,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfFalse,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfTrue,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.JumpIfTrue,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.BinaryOperation,
                LoweredInstructionKind.StoreVariable,
                LoweredInstructionKind.Jump,
                LoweredInstructionKind.Label,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.Equal("i", instructions[1].Variable?.Name);
        Assert.StartsWith("__for_i_end_L0001", instructions[3].Variable?.Name, StringComparison.Ordinal);
        Assert.StartsWith("__for_i_step_L0001", instructions[5].Variable?.Name, StringComparison.Ordinal);
        Assert.StartsWith("__for_i_step_L0001", instructions[6].Variable?.Name, StringComparison.Ordinal);
        Assert.Equal("L0001", instructions[8].Label);
        Assert.Equal(SyntaxKind.GreaterToken, instructions[11].OperatorKind);
        Assert.Equal("L0002", instructions[12].Label);
        Assert.Equal(SyntaxKind.GreaterToken, instructions[15].OperatorKind);
        Assert.Equal("L0004", instructions[16].Label);
        Assert.Equal("L0003", instructions[17].Label);
        Assert.Equal("L0002", instructions[18].Label);
        Assert.Equal(SyntaxKind.LessToken, instructions[21].OperatorKind);
        Assert.Equal("L0004", instructions[22].Label);
        Assert.Equal("L0003", instructions[23].Label);
        Assert.Equal(SyntaxKind.PlusToken, instructions[29].OperatorKind);
        Assert.Equal("i", instructions[30].Variable?.Name);
        Assert.Equal("L0001", instructions[31].Label);
        Assert.Equal("L0004", instructions[32].Label);
    }

    [Fact]
    public void Lower_ForNextWithNegativeStep_UsesSameDeterministicBranchShape()
    {
        var result = Lower("""
            FOR i = 3 TO 1 STEP -1
                PRINT i
            NEXT
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "JumpIfFalse:L0002",
                "JumpIfTrue:L0004",
                "Jump:L0003",
                "Label:L0002",
                "JumpIfTrue:L0004",
                "Label:L0003",
                "Jump:L0001",
                "Label:L0004"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_NestedForNext_UsesDeterministicLabels()
    {
        var result = Lower("""
            FOR i = 1 TO 2
                FOR j = 1 TO 2
                    PRINT i
                NEXT
            NEXT
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "JumpIfFalse:L0002",
                "JumpIfTrue:L0004",
                "Jump:L0003",
                "Label:L0002",
                "JumpIfTrue:L0004",
                "Label:L0003",
                "Label:L0005",
                "JumpIfFalse:L0006",
                "JumpIfTrue:L0008",
                "Jump:L0007",
                "Label:L0006",
                "JumpIfTrue:L0008",
                "Label:L0007",
                "Jump:L0005",
                "Label:L0008",
                "Jump:L0001",
                "Label:L0004"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_ExitFor_EmitsJumpToNearestForEndLabel()
    {
        var result = Lower("""
            FOR i = 1 TO 3
                PRINT i
                EXIT FOR
                PRINT 99
            NEXT
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "JumpIfFalse:L0002",
                "JumpIfTrue:L0004",
                "Jump:L0003",
                "Label:L0002",
                "JumpIfTrue:L0004",
                "Label:L0003",
                "Jump:L0004",
                "Jump:L0001",
                "Label:L0004"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_NestedExitFor_TargetsNearestForLoop()
    {
        var result = Lower("""
            FOR i = 1 TO 2
                FOR j = 1 TO 2
                    EXIT FOR
                NEXT
                PRINT i
            NEXT
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var branchInstructions = result.Program.EntryPoint.Instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "Label:L0001",
                "JumpIfFalse:L0002",
                "JumpIfTrue:L0004",
                "Jump:L0003",
                "Label:L0002",
                "JumpIfTrue:L0004",
                "Label:L0003",
                "Label:L0005",
                "JumpIfFalse:L0006",
                "JumpIfTrue:L0008",
                "Jump:L0007",
                "Label:L0006",
                "JumpIfTrue:L0008",
                "Label:L0007",
                "Jump:L0008",
                "Jump:L0005",
                "Label:L0008",
                "Jump:L0001",
                "Label:L0004"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_SelectCase_EmitsFirstMatchBranchesAndElse()
    {
        var result = Lower("""
            DIM value AS INTEGER = 2
            SELECT CASE value
                CASE 1
                    PRINT 1
                CASE 2, 3
                    PRINT 2
                CASE ELSE
                    PRINT 9
            END SELECT
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Contains(
            instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.StoreVariable
                && instruction.Variable?.Name == "__select_case_test_L0001");

        var branchInstructions = instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "JumpIfTrue:L0002",
                "Jump:L0003",
                "Label:L0002",
                "Jump:L0001",
                "Label:L0003",
                "JumpIfTrue:L0004",
                "JumpIfTrue:L0004",
                "Jump:L0005",
                "Label:L0004",
                "Jump:L0001",
                "Label:L0005",
                "Label:L0001"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());

        Assert.Equal(
            3,
            instructions.Count(static instruction => instruction.Kind == LoweredInstructionKind.LoadVariable
                && instruction.Variable?.Name == "__select_case_test_L0001"));
    }

    [Fact]
    public void Lower_SelectCaseRangeAndCaseIs_EmitsSelectorComparisons()
    {
        var result = Lower("""
            DIM value AS INTEGER = 5
            SELECT CASE value
                CASE 1 TO 10
                    PRINT 1
                CASE IS > 20
                    PRINT 2
            END SELECT
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;

        Assert.Contains(
            instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.BinaryOperation
                && instruction.OperatorKind == SyntaxKind.GreaterOrEqualsToken);
        Assert.Contains(
            instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.BinaryOperation
                && instruction.OperatorKind == SyntaxKind.LessOrEqualsToken);
        Assert.Contains(
            instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.BinaryOperation
                && instruction.OperatorKind == SyntaxKind.AndKeyword);
        Assert.Contains(
            instructions,
            static instruction => instruction.Kind == LoweredInstructionKind.BinaryOperation
                && instruction.OperatorKind == SyntaxKind.GreaterToken);

        var branchInstructions = instructions
            .Where(static instruction => instruction.Kind is LoweredInstructionKind.Label or LoweredInstructionKind.Jump or LoweredInstructionKind.JumpIfFalse or LoweredInstructionKind.JumpIfTrue)
            .ToArray();

        Assert.Equal(
            [
                "JumpIfTrue:L0002",
                "Jump:L0003",
                "Label:L0002",
                "Jump:L0001",
                "Label:L0003",
                "JumpIfTrue:L0004",
                "Jump:L0005",
                "Label:L0004",
                "Jump:L0001",
                "Label:L0005",
                "Label:L0001"
            ],
            branchInstructions.Select(static instruction => $"{instruction.Kind}:{instruction.Label}").ToArray());
    }

    [Fact]
    public void Lower_ArrayStatements_EmitArrayInstructions()
    {
        var result = Lower("""
            DIM values(1 TO 3) AS INTEGER
            values(1) = 10
            PRINT values(1)
            REDIM PRESERVE values(1 TO 4)
            ERASE values
            """);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Program);

        var instructions = result.Program.EntryPoint.Instructions;
        Assert.Equal(
            [
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.RedimArray,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.StoreElement,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.LoadElement,
                LoweredInstructionKind.PrintValue,
                LoweredInstructionKind.PrintLine,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.LoadLiteral,
                LoweredInstructionKind.RedimArray,
                LoweredInstructionKind.LoadVariable,
                LoweredInstructionKind.EraseArray,
                LoweredInstructionKind.Return
            ],
            instructions.Select(static instruction => instruction.Kind).ToArray());

        Assert.False(instructions[3].Preserve);
        Assert.True(instructions[16].Preserve);
        Assert.Equal("values", instructions[7].DisplayName);
        Assert.Equal("values", instructions[10].DisplayName);
        Assert.Equal("values", instructions[18].DisplayName);
        Assert.Empty(result.Diagnostics);
    }

    private static LoweringResult Lower(string source)
    {
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest(source));

        Assert.True(compilation.Succeeded);
        Assert.NotNull(compilation.CompiledProgram);

        return new BoundProgramLowerer().Lower(compilation.CompiledProgram.BoundProgram);
    }

    private static string[] Project(LoweredProgram program)
    {
        return program.AllRoutines
            .SelectMany(static routine => routine.Instructions.Select(instruction => string.Join(
                "|",
                routine.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                instruction.Kind.ToString(),
                instruction.LiteralValue?.ToString() ?? string.Empty,
                instruction.Variable?.Name ?? string.Empty,
                instruction.Function?.Name ?? string.Empty,
                instruction.OperatorKind?.ToString() ?? string.Empty,
                instruction.SeparatorKind?.ToString() ?? string.Empty,
                instruction.ValueType?.Name ?? string.Empty,
                instruction.Label ?? string.Empty)))
            .ToArray();
    }
}
