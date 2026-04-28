using Axiom.Core;
using Axiom.Parsing;
using Axiom.Syntax;

namespace Axiom.Semantics.Tests;

public sealed class BinderTests
{
    [Fact]
    public void Bind_VariableDeclaration()
    {
        var program = BindProgram("DIM value AS INTEGER = 10", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal("value", declaration.Variable.Name);
        Assert.Equal(TypeSymbol.Integer, declaration.Variable.Type);
        Assert.IsType<BoundLiteralExpression>(declaration.Initializer);
    }

    [Fact]
    public void Bind_GroupedVariableDeclarations()
    {
        var program = BindProgram("DIM anotherVar, choice AS BOOLEAN, birthDate AS STRING", out var diagnostics);

        Assert.Empty(diagnostics);
        var declarations = Assert.IsType<BoundVariableDeclarationsStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(["anotherVar", "choice", "birthDate"], declarations.Declarations.Select(static statement => statement.Variable.Name));
        Assert.Equal([TypeSymbol.Boolean, TypeSymbol.Boolean, TypeSymbol.String], declarations.Declarations.Select(static statement => statement.Variable.Type));
    }

    [Fact]
    public void Bind_InferredVariableDeclaration()
    {
        var program = BindProgram("DIM value = 10", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal("value", declaration.Variable.Name);
        Assert.Equal(TypeSymbol.Integer, declaration.Variable.Type);
        Assert.IsType<BoundLiteralExpression>(declaration.Initializer);
    }

    [Fact]
    public void Bind_InferredLongVariableDeclaration()
    {
        var program = BindProgram("DIM value = 21474836479", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(TypeSymbol.Long, declaration.Variable.Type);
        Assert.IsType<BoundLiteralExpression>(declaration.Initializer);
    }

    [Fact]
    public void Bind_ExplicitDecimalVariableDeclaration()
    {
        var program = BindProgram("DIM price AS DECIMAL = 10.50", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(TypeSymbol.Decimal, declaration.Variable.Type);
        var initializer = Assert.IsType<BoundLiteralExpression>(declaration.Initializer);
        Assert.Equal(TypeSymbol.Decimal, initializer.Type);
        Assert.Equal(10.50m, Assert.IsType<decimal>(initializer.Value));
    }

    [Fact]
    public void Bind_GroupedInferredVariableDeclarations()
    {
        var program = BindProgram("DIM x, y = 10", out var diagnostics);

        Assert.Empty(diagnostics);
        var declarations = Assert.IsType<BoundVariableDeclarationsStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(["x", "y"], declarations.Declarations.Select(static statement => statement.Variable.Name));
        Assert.Equal([TypeSymbol.Integer, TypeSymbol.Integer], declarations.Declarations.Select(static statement => statement.Variable.Type));
    }

    [Fact]
    public void Bind_AllowsExplicitIntegerDeclarationForMinIntegerLiteral()
    {
        var program = BindProgram("DIM value AS INTEGER = -2147483648", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(TypeSymbol.Integer, declaration.Variable.Type);
    }

    [Fact]
    public void Bind_ReportsMissingVariableTypeWhenDimHasNoTypeAndNoInitializer()
    {
        BindProgram("DIM value", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2017");
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_DynamicArrayDeclarationLeavesBoundsUnbound()
    {
        var program = BindProgram("DIM items() AS STRING", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal("items", declaration.Variable.Name);
        var arrayType = Assert.IsType<ArrayTypeSymbol>(declaration.Variable.Type);
        Assert.Equal(TypeSymbol.String, arrayType.ElementType);
        Assert.True(declaration.Variable.IsArray);
        Assert.Null(declaration.LowerBound);
        Assert.Null(declaration.UpperBound);
    }

    [Fact]
    public void Bind_WholeArrayAssignmentUsesArrayType()
    {
        const string text = """
DIM source() AS INTEGER
DIM target() AS INTEGER
target = source
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var target = Assert.IsType<BoundVariableExpression>(assignment.Target);
        var expression = Assert.IsType<BoundVariableExpression>(assignment.Expression);

        Assert.IsType<ArrayTypeSymbol>(target.Type);
        Assert.IsType<ArrayTypeSymbol>(expression.Type);
    }

    [Fact]
    public void Bind_FunctionDeclarationWithArrayTypedParameterAndReturnType()
    {
        const string text = """
FUNCTION Echo(values AS INTEGER()) AS INTEGER()
    RETURN values
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var routine = Assert.Single(program.Routines);
        var parameterType = Assert.IsType<ArrayTypeSymbol>(routine.Symbol.Parameters[0].Type);
        Assert.Equal(TypeSymbol.Integer, parameterType.ElementType);

        var returnType = Assert.IsType<ArrayTypeSymbol>(routine.Symbol.ReturnType);
        Assert.Equal(TypeSymbol.Integer, returnType.ElementType);

        var returnStatement = Assert.IsType<BoundReturnStatement>(Assert.Single(routine.Statements));
        Assert.IsType<ArrayTypeSymbol>(returnStatement.Expression!.Type);
    }

    [Fact]
    public void Bind_RedimStatement()
    {
        const string text = """
DIM items() AS STRING
REDIM items(1 TO 3)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var redim = Assert.IsType<BoundRedimStatement>(program.GlobalStatements[1]);
        Assert.NotNull(redim.Variable);
        Assert.Equal("items", redim.Variable!.Name);
        Assert.False(redim.Preserve);
        var arrayType = Assert.IsType<ArrayTypeSymbol>(redim.Variable.Type);
        Assert.Equal(TypeSymbol.String, arrayType.ElementType);
        Assert.Equal(TypeSymbol.Integer, redim.LowerBound.Type);
        Assert.Equal(TypeSymbol.Integer, redim.UpperBound.Type);
    }

    [Fact]
    public void Bind_RedimPreserveStatement()
    {
        const string text = """
DIM items() AS STRING
REDIM PRESERVE items(2 TO 4)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var redim = Assert.IsType<BoundRedimStatement>(program.GlobalStatements[1]);
        Assert.NotNull(redim.Variable);
        Assert.Equal("items", redim.Variable!.Name);
        Assert.True(redim.Preserve);
        var arrayType = Assert.IsType<ArrayTypeSymbol>(redim.Variable.Type);
        Assert.Equal(TypeSymbol.String, arrayType.ElementType);
        Assert.Equal(TypeSymbol.Integer, redim.LowerBound.Type);
        Assert.Equal(TypeSymbol.Integer, redim.UpperBound.Type);
    }

    [Fact]
    public void Bind_EraseStatement()
    {
        const string text = """
DIM items() AS STRING
ERASE items
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var erase = Assert.IsType<BoundEraseStatement>(program.GlobalStatements[1]);
        Assert.NotNull(erase.Variable);
        Assert.Equal("items", erase.Variable!.Name);
        var arrayType = Assert.IsType<ArrayTypeSymbol>(erase.Variable.Type);
        Assert.Equal(TypeSymbol.String, arrayType.ElementType);
    }

    [Fact]
    public void Bind_EraseReportsScalarTarget()
    {
        const string text = """
DIM value AS INTEGER
ERASE value
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2011");
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_RedimReportsScalarTarget()
    {
        const string text = """
DIM value AS INTEGER
REDIM value(1 TO 3)
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2011");
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayInformationFunctions()
    {
        const string text = """
DIM values() AS INTEGER
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
PRINT ARRAYISALLOCATED(values)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Integer, BuiltinFunctionId.LBound);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Integer, BuiltinFunctionId.UBound);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.Integer, BuiltinFunctionId.ArrayLength);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.Boolean, BuiltinFunctionId.ArrayIsAllocated);
    }

    [Fact]
    public void Bind_BuiltinArrayInformationReportsScalarArgument()
    {
        BindProgram("PRINT LBOUND(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayLengthReportsWrongArgumentCount()
    {
        BindProgram("PRINT ARRAYLENGTH()", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("ARRAYLENGTH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayCloneReturnsSameArrayType()
    {
        const string text = """
DIM values() AS INTEGER
DIM copy() AS INTEGER
copy = ARRAYCLONE(values)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var cloneCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var cloneType = Assert.IsType<ArrayTypeSymbol>(cloneCall.Type);
        Assert.Equal(TypeSymbol.Integer, cloneType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArrayClone, AssertCalledFunction(cloneCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayCloneReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYCLONE(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayFirstMultidimensionalGroupAcceptsMultidimensionalArray()
    {
        const string text = """
DIM values(1 TO 2, 1 TO 2) AS INTEGER
DIM copy(,) AS INTEGER
copy = ARRAYCLONE(values)
ARRAYFILL(values, 7)
PRINT ARRAYCONTAINS(values, 7)
PRINT ARRAYCOUNT(values, 7)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var cloneCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var cloneType = Assert.IsType<ArrayTypeSymbol>(cloneCall.Type);
        Assert.Equal(2, cloneType.Rank);
        Assert.Equal(BuiltinFunctionId.ArrayClone, AssertCalledFunction(cloneCall).BuiltinId);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.Void, BuiltinFunctionId.ArrayFill);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.Boolean, BuiltinFunctionId.ArrayContains);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.Integer, BuiltinFunctionId.ArrayCount);
    }

    [Fact]
    public void Bind_BuiltinArraySliceReturnsSameArrayType()
    {
        const string text = """
DIM values() AS STRING
DIM part() AS STRING
part = ARRAYSLICE(values, 1, 2)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var sliceCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var sliceType = Assert.IsType<ArrayTypeSymbol>(sliceCall.Type);
        Assert.Equal(TypeSymbol.String, sliceType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArraySlice, AssertCalledFunction(sliceCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArraySliceReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYSLICE(1, 1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayConcatReturnsSameArrayType()
    {
        const string text = """
DIM left() AS INTEGER
DIM right() AS INTEGER
DIM merged() AS INTEGER
merged = ARRAYCONCAT(left, right)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[3]);
        var concatCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var concatType = Assert.IsType<ArrayTypeSymbol>(concatCall.Type);
        Assert.Equal(TypeSymbol.Integer, concatType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArrayConcat, AssertCalledFunction(concatCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayConcatReportsMismatchedArrayTypes()
    {
        const string text = """
DIM left() AS INTEGER
DIM right() AS STRING
PRINT ARRAYCONCAT(left, right)
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING()", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INTEGER()", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayToReversedReturnsSameArrayType()
    {
        const string text = """
DIM values() AS STRING
DIM reversed() AS STRING
reversed = ARRAYTOREVERSED(values)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var reverseCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var reverseType = Assert.IsType<ArrayTypeSymbol>(reverseCall.Type);
        Assert.Equal(TypeSymbol.String, reverseType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArrayToReversed, AssertCalledFunction(reverseCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayToReversedReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYTOREVERSED(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayTakeReturnsSameArrayType()
    {
        const string text = """
DIM values() AS INTEGER
DIM part() AS INTEGER
part = ARRAYTAKE(values, 2)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var takeCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var takeType = Assert.IsType<ArrayTypeSymbol>(takeCall.Type);
        Assert.Equal(TypeSymbol.Integer, takeType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArrayTake, AssertCalledFunction(takeCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayTakeReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYTAKE(1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArraySkipReturnsSameArrayType()
    {
        const string text = """
DIM values() AS STRING
DIM part() AS STRING
part = ARRAYSKIP(values, 2)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var skipCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var skipType = Assert.IsType<ArrayTypeSymbol>(skipCall.Type);
        Assert.Equal(TypeSymbol.String, skipType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArraySkip, AssertCalledFunction(skipCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArraySkipReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYSKIP(1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayDistinctReturnsSameArrayType()
    {
        const string text = """
DIM values() AS INTEGER
DIM uniqueValues() AS INTEGER
uniqueValues = ARRAYDISTINCT(values)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var distinctCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var distinctType = Assert.IsType<ArrayTypeSymbol>(distinctCall.Type);
        Assert.Equal(TypeSymbol.Integer, distinctType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArrayDistinct, AssertCalledFunction(distinctCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayToSortedReturnsSameArrayType()
    {
        const string text = """
DIM values() AS STRING
DIM sortedValues() AS STRING
sortedValues = ARRAYTOSORTED(values)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var sortedCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var sortedType = Assert.IsType<ArrayTypeSymbol>(sortedCall.Type);
        Assert.Equal(TypeSymbol.String, sortedType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArrayToSorted, AssertCalledFunction(sortedCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayDistinctReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYDISTINCT(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayToSortedReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYTOSORTED(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayToSortedRejectsBooleanArray()
    {
        BindProgram(
            """
DIM flags() AS BOOLEAN
flags = ARRAYTOSORTED(flags)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("ARRAYTOSORTED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BOOLEAN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayMutatingProcedures()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM names() AS STRING
ARRAYFILL(numbers, 1)
ARRAYREVERSE(numbers)
ARRAYSORT(numbers)
ARRAYSORT(names)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Void, BuiltinFunctionId.ArrayFill);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.Void, BuiltinFunctionId.ArrayReverse);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.Void, BuiltinFunctionId.ArraySort);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.Void, BuiltinFunctionId.ArraySort);
    }

    [Fact]
    public void Bind_BuiltinArrayPushAndPop()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM lastValue AS INTEGER
ARRAYPUSH(numbers, 1)
lastValue = ARRAYPOP(numbers)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Void, BuiltinFunctionId.ArrayPush);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[3]);
        var popCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal(TypeSymbol.Integer, popCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayPop, AssertCalledFunction(popCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayInsertAndRemove()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM removedValue AS INTEGER
ARRAYINSERT(numbers, 1, 10)
removedValue = ARRAYREMOVE(numbers, 1)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Void, BuiltinFunctionId.ArrayInsert);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[3]);
        var removeCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal(TypeSymbol.Integer, removeCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayRemove, AssertCalledFunction(removeCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayEverySomeAndFilter()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM allPositive AS BOOLEAN
DIM anyLarge AS BOOLEAN
DIM filtered() AS INTEGER
allPositive = ARRAYEVERY(numbers, IsPositive)
anyLarge = ARRAYSOME(numbers, IsLarge)
filtered = ARRAYFILTER(numbers, IsLarge)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value > 100
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var everyAssignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[4]);
        var everyCall = Assert.IsType<BoundCallExpression>(everyAssignment.Expression);
        Assert.Equal(TypeSymbol.Boolean, everyCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayEvery, AssertCalledFunction(everyCall).BuiltinId);

        var someAssignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[5]);
        var someCall = Assert.IsType<BoundCallExpression>(someAssignment.Expression);
        Assert.Equal(TypeSymbol.Boolean, someCall.Type);
        Assert.Equal(BuiltinFunctionId.ArraySome, AssertCalledFunction(someCall).BuiltinId);

        var filterAssignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[6]);
        var filterCall = Assert.IsType<BoundCallExpression>(filterAssignment.Expression);
        Assert.Equal(ArrayTypeSymbol.Create(TypeSymbol.Integer), filterCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayFilter, AssertCalledFunction(filterCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayMap()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM mapped() AS INTEGER
mapped = ARRAYMAP(numbers, DoubleIt)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var mapCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal(ArrayTypeSymbol.Create(TypeSymbol.Integer), mapCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayMap, AssertCalledFunction(mapCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayMapSupportsPolymorphicResultType()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM mapped() AS STRING
mapped = ARRAYMAP(numbers, FormatValue)

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN "item:" + STR(value)
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var mapCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal(ArrayTypeSymbol.Create(TypeSymbol.String), mapCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayMap, AssertCalledFunction(mapCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayReduce()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM total AS INTEGER
total = ARRAYREDUCE(numbers, 0, Add)

FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var reduceCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal(TypeSymbol.Integer, reduceCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayReduce, AssertCalledFunction(reduceCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayReduceSupportsPolymorphicAccumulatorType()
    {
        const string text = """
DIM words() AS STRING
DIM total AS INTEGER
total = ARRAYREDUCE(words, 0, AddLength)

FUNCTION AddLength(acc AS INTEGER, value AS STRING) AS INTEGER
    RETURN acc + LEN(value)
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var reduceCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal(TypeSymbol.Integer, reduceCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayReduce, AssertCalledFunction(reduceCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayReduceInfersAccumulatorTypeFromReducer()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM total AS LONG
total = ARRAYREDUCE(numbers, 0, AddLong)

FUNCTION AddLong(acc AS LONG, value AS INTEGER) AS LONG
    RETURN acc + value
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var reduceCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal(TypeSymbol.Long, reduceCall.Type);
        Assert.Equal(BuiltinFunctionId.ArrayReduce, AssertCalledFunction(reduceCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayPredicateFunctionsBindCallbackToElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
PRINT ARRAYFILTER(names, IsLongEnough)

FUNCTION IsLongEnough(value AS INTEGER) AS BOOLEAN
    RETURN value > 3
END FUNCTION
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("FUNCTION(STRING) AS BOOLEAN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayPredicateFunctionsRequireBooleanPredicateResult()
    {
        BindProgram(
            """
DIM values() AS INTEGER
PRINT ARRAYFILTER(values, ToNumber)

FUNCTION ToNumber(value AS INTEGER) AS INTEGER
    RETURN value
END FUNCTION
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("FUNCTION(INTEGER) AS BOOLEAN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayMapBindsSelectorToElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
DIM mapped() AS STRING
mapped = ARRAYMAP(names, AsNumber)

FUNCTION AsNumber(value AS INTEGER) AS INTEGER
    RETURN value
END FUNCTION
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("FUNCTION(STRING) AS INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayReduceBindsSeedAsElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
PRINT ARRAYREDUCE(names, 1, KeepLeft)

FUNCTION KeepLeft(acc AS STRING, value AS STRING) AS STRING
    RETURN acc
END FUNCTION
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayReduceBindsReducerToElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
PRINT ARRAYREDUCE(names, "", AppendLength)

FUNCTION AppendLength(acc AS STRING, value AS INTEGER) AS STRING
    RETURN acc
END FUNCTION
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("FUNCTION(STRING, STRING) AS STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayFillBindsValueAsElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
ARRAYFILL(names, 1)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayPushBindsValueAsElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
ARRAYPUSH(names, 1)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayInsertBindsValueAsElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
ARRAYINSERT(names, 1, 1)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayFillReportsScalarArgument()
    {
        BindProgram("ARRAYFILL(1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayPushReportsScalarArgument()
    {
        BindProgram("ARRAYPUSH(1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayInsertReportsScalarArgument()
    {
        BindProgram("ARRAYINSERT(1, 1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayReverseReportsScalarArgument()
    {
        BindProgram("ARRAYREVERSE(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayPopReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYPOP(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayRemoveReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYREMOVE(1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayPredicateFunctionsReportScalarArgument()
    {
        BindProgram(
            """
PRINT ARRAYFILTER(1, IsReady)

FUNCTION IsReady(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayMapReportsScalarArgument()
    {
        BindProgram(
            """
PRINT ARRAYMAP(1, Transform)

FUNCTION Transform(value AS INTEGER) AS INTEGER
    RETURN value
END FUNCTION
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayReduceReportsScalarArgument()
    {
        BindProgram(
            """
PRINT ARRAYREDUCE(1, 0, Add)

FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value
END FUNCTION
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArraySortReportsScalarArgument()
    {
        BindProgram("ARRAYSORT(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArraySortRejectsBooleanArray()
    {
        BindProgram(
            """
DIM flags() AS BOOLEAN
ARRAYSORT(flags)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("ARRAYSORT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BOOLEAN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArraySearchFunctions()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM names() AS STRING
PRINT ARRAYCONTAINS(numbers, 1)
PRINT ARRAYINDEXOF(numbers, 1)
PRINT ARRAYLASTINDEXOF(numbers, 1)
PRINT ARRAYCOUNT(numbers, 1)
PRINT ARRAYBINARYSEARCH(numbers, 1)
PRINT ARRAYCONTAINS(names, "Ann")
PRINT ARRAYBINARYSEARCH(names, "Ann")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Boolean, BuiltinFunctionId.ArrayContains);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.Integer, BuiltinFunctionId.ArrayIndexOf);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.Integer, BuiltinFunctionId.ArrayLastIndexOf);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.Integer, BuiltinFunctionId.ArrayCount);
        AssertBuiltinCall(program.GlobalStatements[6], TypeSymbol.Integer, BuiltinFunctionId.ArrayBinarySearch);
        AssertBuiltinCall(program.GlobalStatements[7], TypeSymbol.Boolean, BuiltinFunctionId.ArrayContains);
        AssertBuiltinCall(program.GlobalStatements[8], TypeSymbol.Integer, BuiltinFunctionId.ArrayBinarySearch);
    }

    [Fact]
    public void Bind_BuiltinArraySearchFunctionsBindValueAsElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
PRINT ARRAYCONTAINS(names, 1)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayFindAllReturnsIntegerArray()
    {
        const string text = """
DIM numbers() AS INTEGER
DIM names() AS STRING
DIM numberMatches() AS INTEGER
DIM nameMatches() AS INTEGER
numberMatches = ARRAYFINDALL(numbers, 1)
nameMatches = ARRAYFINDALL(names, "Ann")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var numberAssignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[4]);
        var numberCall = Assert.IsType<BoundCallExpression>(numberAssignment.Expression);
        var numberResultType = Assert.IsType<ArrayTypeSymbol>(numberCall.Type);
        Assert.Equal(TypeSymbol.Integer, numberResultType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArrayFindAll, AssertCalledFunction(numberCall).BuiltinId);

        var nameAssignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[5]);
        var nameCall = Assert.IsType<BoundCallExpression>(nameAssignment.Expression);
        var nameResultType = Assert.IsType<ArrayTypeSymbol>(nameCall.Type);
        Assert.Equal(TypeSymbol.Integer, nameResultType.ElementType);
        Assert.Equal(BuiltinFunctionId.ArrayFindAll, AssertCalledFunction(nameCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinArrayFindAllBindsValueAsElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
DIM matches() AS INTEGER
matches = ARRAYFINDALL(names, 1)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayBinarySearchBindsValueAsElementType()
    {
        BindProgram(
            """
DIM names() AS STRING
PRINT ARRAYBINARYSEARCH(names, 1)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArraySearchReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYINDEXOF(1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayFindAllReportsScalarArgument()
    {
        BindProgram("DIM matches() AS INTEGER\nmatches = ARRAYFINDALL(1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayBinarySearchReportsScalarArgument()
    {
        BindProgram("PRINT ARRAYBINARYSEARCH(1, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinArrayBinarySearchRejectsBooleanArray()
    {
        BindProgram(
            """
DIM flags() AS BOOLEAN
PRINT ARRAYBINARYSEARCH(flags, TRUE)
""",
            out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("ARRAYBINARYSEARCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BOOLEAN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinSplitReturnsStringArray()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("A,B", ",")
PRINT JOIN(parts)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        var splitCall = Assert.IsType<BoundCallExpression>(assignment.Expression);
        var splitType = Assert.IsType<ArrayTypeSymbol>(splitCall.Type);
        Assert.Equal(TypeSymbol.String, splitType.ElementType);
        Assert.Equal(BuiltinFunctionId.SplitSeparator, AssertCalledFunction(splitCall).BuiltinId);

        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.String, BuiltinFunctionId.Join);
    }

    [Fact]
    public void Bind_BuiltinJoinReportsNonStringArray()
    {
        const string text = """
DIM values() AS INTEGER
PRINT JOIN(values)
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER()", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("STRING()", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_Assignment()
    {
        const string text = """
DIM value AS INTEGER
value = 10
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        var target = Assert.IsType<BoundVariableExpression>(assignment.Target);
        Assert.Equal("value", target.Variable.Name);
    }

    [Fact]
    public void Bind_ReportsUndefinedVariable()
    {
        BindProgram("PRINT value", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2001");
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsDuplicateDeclaration()
    {
        const string text = """
DIM value AS INTEGER
DIM value AS INTEGER
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2002");
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_PrintList()
    {
        var program = BindProgram("PRINT 1; 2, 3", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(3, statement.Items.Count);
        Assert.Equal(PrintSeparatorKind.Semicolon, statement.Items[0].SeparatorKind);
        Assert.Equal(PrintSeparatorKind.Comma, statement.Items[1].SeparatorKind);
        Assert.Null(statement.Items[2].SeparatorKind);
    }

    [Fact]
    public void Bind_PrintUsingStatement()
    {
        const string text = """
DIM price AS DOUBLE = 10
DIM tax AS DOUBLE = 2
PRINT USING "##.##"; price; tax,
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundPrintUsingStatement>(program.GlobalStatements[^1]);
        Assert.Equal(TypeSymbol.String, statement.FormatExpression.Type);
        Assert.Equal(2, statement.Items.Count);
        Assert.Equal(PrintSeparatorKind.Semicolon, statement.Items[0].SeparatorKind);
        Assert.Equal(PrintSeparatorKind.Comma, statement.Items[1].SeparatorKind);
    }

    [Fact]
    public void Bind_PrintUsingRequiresStringFormat()
    {
        BindProgram("PRINT USING 123; 1", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_InputListWithPrompt()
    {
        const string text = """
DIM firstName AS STRING
DIM lastName AS STRING
INPUT "Name"; firstName, lastName
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundInputStatement>(program.GlobalStatements[2]);
        Assert.Equal("Name", statement.PromptText);
        Assert.Equal(["firstName", "lastName"], statement.Variables.Select(static variable => variable.Name));
    }

    [Fact]
    public void Bind_FileVariableDeclaration()
    {
        var program = BindProgram("DIM fileHandle AS FILE", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal("fileHandle", declaration.Variable.Name);
        Assert.Equal(TypeSymbol.File, declaration.Variable.Type);
    }

    [Fact]
    public void Bind_OpenStatement()
    {
        const string text = """
DIM fileHandle AS FILE
OPEN "notes.txt" FOR INPUT AS fileHandle
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundOpenStatement>(program.GlobalStatements[1]);
        Assert.Equal(BoundFileOpenMode.Input, statement.Mode);
        Assert.Equal("fileHandle", statement.FileVariable.Name);
        Assert.Equal(TypeSymbol.String, statement.PathExpression.Type);
    }

    [Fact]
    public void Bind_CloseStatement()
    {
        const string text = """
DIM fileHandle AS FILE
CLOSE fileHandle
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundCloseStatement>(program.GlobalStatements[1]);
        Assert.Equal("fileHandle", statement.FileVariable.Name);
    }

    [Fact]
    public void Bind_FileAssignmentReportsDiagnostic()
    {
        const string text = """
DIM leftFile AS FILE
DIM rightFile AS FILE
leftFile = rightFile
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("FILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OPEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_OpenReportsNonFileTarget()
    {
        const string text = """
DIM name AS STRING
OPEN "notes.txt" FOR INPUT AS name
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("FILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_FileIoBuiltinsReturnExpectedTypes()
    {
        const string text = """
DIM fileHandle AS FILE
PRINT ISOPEN(fileHandle)
PRINT FILEPATH(fileHandle)
PRINT FILEMODE(fileHandle)
PRINT FILEPOS(fileHandle)
PRINT FILELENGTH(fileHandle)
PRINT LINENUMBER(fileHandle)
PRINT EOF(fileHandle)
PRINT READTOEND(fileHandle)
PRINT READTEXT(fileHandle, 1)
PRINT READLINE(fileHandle)
PRINT READLINES(fileHandle, 2)
PRINT SKIPLINE(fileHandle)
PRINT WRITE(fileHandle, "x")
PRINT WRITELINE(fileHandle, "x")
PRINT NEWLINE(fileHandle)
PRINT FLUSH(fileHandle)
PRINT SEEKFILE(fileHandle, 0)
PRINT REWIND(fileHandle)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Boolean, BuiltinFunctionId.IsOpen);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.String, BuiltinFunctionId.FilePath);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.String, BuiltinFunctionId.FileMode);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.Long, BuiltinFunctionId.FilePos);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.Long, BuiltinFunctionId.FileLength);
        AssertBuiltinCall(program.GlobalStatements[6], TypeSymbol.Long, BuiltinFunctionId.LineNumber);
        AssertBuiltinCall(program.GlobalStatements[7], TypeSymbol.Boolean, BuiltinFunctionId.Eof);
        AssertBuiltinCall(program.GlobalStatements[8], TypeSymbol.String, BuiltinFunctionId.ReadToEnd);
        AssertBuiltinCall(program.GlobalStatements[9], TypeSymbol.String, BuiltinFunctionId.ReadText);
        AssertBuiltinCall(program.GlobalStatements[10], TypeSymbol.String, BuiltinFunctionId.ReadLine);
        AssertBuiltinCall(program.GlobalStatements[11], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.ReadLines);
        AssertBuiltinCall(program.GlobalStatements[12], TypeSymbol.Boolean, BuiltinFunctionId.SkipLine);
        AssertBuiltinCall(program.GlobalStatements[13], TypeSymbol.Boolean, BuiltinFunctionId.Write);
        AssertBuiltinCall(program.GlobalStatements[14], TypeSymbol.Boolean, BuiltinFunctionId.WriteLine);
        AssertBuiltinCall(program.GlobalStatements[15], TypeSymbol.Boolean, BuiltinFunctionId.NewLine);
        AssertBuiltinCall(program.GlobalStatements[16], TypeSymbol.Boolean, BuiltinFunctionId.Flush);
        AssertBuiltinCall(program.GlobalStatements[17], TypeSymbol.Boolean, BuiltinFunctionId.SeekFile);
        AssertBuiltinCall(program.GlobalStatements[18], TypeSymbol.Boolean, BuiltinFunctionId.Rewind);
    }

    [Fact]
    public void Bind_FunctionDeclarationAndCall()
    {
        const string text = """
DIM result AS INTEGER
result = Add(1, 2)

FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        var call = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal("Add", AssertCalledFunction(call).Name);
        Assert.Single(program.Routines);
    }

    [Fact]
    public void Bind_FunctionNameCanBeAssignedToFunctionTypedVariable()
    {
        const string text = """
DIM addRef AS FUNCTION(INTEGER, INTEGER) AS INTEGER
addRef = Add

FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        var value = Assert.IsType<BoundFunctionExpression>(assignment.Expression);
        Assert.Equal("Add", value.Function.Name);
        Assert.Equal(
            FunctionTypeSymbol.Create([TypeSymbol.Integer, TypeSymbol.Integer], TypeSymbol.Integer),
            value.Type);
    }

    [Fact]
    public void Bind_ProcedureNameCannotBeUsedAsValue()
    {
        const string text = """
DIM handler AS FUNCTION() AS INTEGER
handler = Work

SUB Work()
END SUB
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2019");
        Assert.Contains("Work", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_FunctionTypedVariableCanBeCalled()
    {
        const string text = """
DIM addRef AS FUNCTION(INTEGER, INTEGER) AS INTEGER
addRef = Add
PRINT addRef(1, 2)

FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[2]);
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        var target = Assert.IsType<BoundVariableExpression>(call.Target);
        Assert.Equal("addRef", target.Variable.Name);
        Assert.Equal(TypeSymbol.Integer, call.Type);
    }

    [Fact]
    public void Bind_ReportsCallingNonFunctionValue()
    {
        const string text = """
DIM value AS INTEGER = 10
PRINT value()
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2020");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_LocalFunctionCanBeReturnedAsValue()
    {
        const string text = """
FUNCTION MakeAdder() AS FUNCTION(INTEGER) AS INTEGER
    DIM baseValue AS INTEGER = 10

    FUNCTION AddBase(delta AS INTEGER) AS INTEGER
        RETURN baseValue + delta
    END FUNCTION

    RETURN AddBase
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var makeAdder = Assert.Single(program.Routines, static routine => routine.Symbol.Name == "MakeAdder");
        var returnStatement = Assert.IsType<BoundReturnStatement>(makeAdder.Statements[^1]);
        var returnedFunction = Assert.IsType<BoundFunctionExpression>(returnStatement.Expression);
        Assert.Equal("AddBase", returnedFunction.Function.Name);
        Assert.NotNull(returnedFunction.Function.ContainingRoutine);
        Assert.Equal("MakeAdder", returnedFunction.Function.ContainingRoutine!.Name);
    }

    [Fact]
    public void Bind_ReportsWrongArgumentCount()
    {
        const string text = """
DIM result AS INTEGER
result = Add(1)

FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("Add", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsWrongArgumentType()
    {
        const string text = """
DIM result AS INTEGER
result = Add("x", 2)

FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinLenReportsWrongArityWhenMissingArgument()
    {
        BindProgram("PRINT LEN()", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("LEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinLenReportsWrongArityWhenTooManyArguments()
    {
        BindProgram("PRINT LEN(\"A\", \"B\")", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("LEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinRightReportsWrongArityWhenMissingCount()
    {
        BindProgram("PRINT RIGHT(\"Hello\")", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("RIGHT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinRightReportsWrongArityWhenTooManyArguments()
    {
        BindProgram("PRINT RIGHT(\"Hello\", 1, 2)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("RIGHT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinRightReportsWrongTextType()
    {
        BindProgram("PRINT RIGHT(123, 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinLeftReportsWrongCountType()
    {
        BindProgram("PRINT LEFT(\"Hello\", \"x\")", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinUCaseReportsWrongType()
    {
        BindProgram("PRINT UCASE(123)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinLCaseReportsWrongType()
    {
        BindProgram("PRINT LCASE(TRUE)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinTrimAcceptsOneOrTwoArguments()
    {
        const string text = """
PRINT TRIM("  hello  ")
PRINT TRIM("--hello--", "-")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var firstPrint = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[0]);
        var firstCall = Assert.IsType<BoundCallExpression>(Assert.Single(firstPrint.Items).Expression);
        Assert.Equal(TypeSymbol.String, firstCall.Type);
        Assert.Equal(BuiltinFunctionId.TrimWhitespace, AssertCalledFunction(firstCall).BuiltinId);

        var secondPrint = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[1]);
        var secondCall = Assert.IsType<BoundCallExpression>(Assert.Single(secondPrint.Items).Expression);
        Assert.Equal(TypeSymbol.String, secondCall.Type);
        Assert.Equal(BuiltinFunctionId.TrimCharacters, AssertCalledFunction(secondCall).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinTrimReportsWrongArity()
    {
        BindProgram("PRINT TRIM(\"a\", \"b\", \"c\")", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("TRIM", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 или 2", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinTrimReportsWrongSecondArgumentType()
    {
        BindProgram("PRINT TRIM(\"hello\", 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinRepeatReportsWrongCountType()
    {
        BindProgram("PRINT REPEAT(\"ha\", \"3\")", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinStringReturnsString()
    {
        var program = BindProgram("PRINT STRING(3, \"x\")", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.String, call.Type);
        Assert.Equal(BuiltinFunctionId.StringFill, AssertCalledFunction(call).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinPadLeftAndPadRightAcceptOptionalFill()
    {
        const string text = """
PRINT PADLEFT("7", 3)
PRINT PADLEFT("7", 3, "0")
PRINT PADRIGHT("7", 3)
PRINT PADRIGHT("7", 3, ".")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.String, BuiltinFunctionId.PadLeft);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.String, BuiltinFunctionId.PadLeftFill);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.String, BuiltinFunctionId.PadRight);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.String, BuiltinFunctionId.PadRightFill);
    }

    [Fact]
    public void Bind_BuiltinCenterAcceptsOptionalFill()
    {
        const string text = """
PRINT CENTER("A", 3)
PRINT CENTER("A", 4, ".")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.String, BuiltinFunctionId.Center);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.String, BuiltinFunctionId.CenterFill);
    }

    [Fact]
    public void Bind_BuiltinInsertAndRemoveReturnString()
    {
        const string text = """
PRINT INSERT("abc", 2, "-")
PRINT REMOVE("abcdef", 2, 3)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.String, BuiltinFunctionId.Insert);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.String, BuiltinFunctionId.Remove);
    }

    [Fact]
    public void Bind_BuiltinAscAndChrReturnExpectedTypes()
    {
        const string text = """
PRINT ASC("A")
PRINT CHR(65)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.Integer, BuiltinFunctionId.Asc);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.String, BuiltinFunctionId.Chr);
    }

    [Fact]
    public void Bind_BuiltinSearchFunctionsReturnBoolean()
    {
        const string text = """
PRINT CONTAINS("Hello", "ell")
PRINT STARTSWITH("Hello", "He")
PRINT ENDSWITH("Hello", "lo")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.Boolean, BuiltinFunctionId.Contains);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Boolean, BuiltinFunctionId.StartsWith);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Boolean, BuiltinFunctionId.EndsWith);
    }

    [Fact]
    public void Bind_BuiltinReplaceReturnsString()
    {
        var program = BindProgram("PRINT REPLACE(\"one two\", \"two\", \"three\")", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(Assert.Single(program.GlobalStatements), TypeSymbol.String, BuiltinFunctionId.Replace);
    }

    [Fact]
    public void Bind_BuiltinRegexFunctionsReturnExpectedTypes()
    {
        const string text = """
PRINT REGEXISMATCH("A12", "\d+")
PRINT REGEXISMATCH("A12", "a\d+", "i")
PRINT REGEXMATCH("A12", "\d+")
PRINT REGEXMATCH("A12", "a\d+", "i")
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT REGEXEXTRACT("A12", "([a-z])(\d+)", 2, "i")
PRINT REGEXMATCHES("A12 B34", "\d+")
PRINT REGEXMATCHES("A12 b34", "[a-z]\d+", "i")
PRINT REGEXEXTRACTALL("A12 B34", "([A-Z])(\d+)", 2)
PRINT REGEXEXTRACTALL("A12 b34", "([a-z])(\d+)", 2, "i")
PRINT REGEXSPLIT("A12 B34", "\d+")
PRINT REGEXSPLIT("A12 b34", "[a-z]\d+", "i")
PRINT REGEXCOUNT("A12 B34", "\d+")
PRINT REGEXCOUNT("A12 b34", "[a-z]\d+", "i")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.Boolean, BuiltinFunctionId.RegexIsMatch);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Boolean, BuiltinFunctionId.RegexIsMatchModes);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.String, BuiltinFunctionId.RegexMatch);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.String, BuiltinFunctionId.RegexMatchModes);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.String, BuiltinFunctionId.RegexExtract);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.String, BuiltinFunctionId.RegexExtractModes);
        AssertBuiltinCall(program.GlobalStatements[6], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.RegexMatches);
        AssertBuiltinCall(program.GlobalStatements[7], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.RegexMatchesModes);
        AssertBuiltinCall(program.GlobalStatements[8], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.RegexExtractAll);
        AssertBuiltinCall(program.GlobalStatements[9], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.RegexExtractAllModes);
        AssertBuiltinCall(program.GlobalStatements[10], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.RegexSplit);
        AssertBuiltinCall(program.GlobalStatements[11], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.RegexSplitModes);
        AssertBuiltinCall(program.GlobalStatements[12], TypeSymbol.Integer, BuiltinFunctionId.RegexCount);
        AssertBuiltinCall(program.GlobalStatements[13], TypeSymbol.Integer, BuiltinFunctionId.RegexCountModes);
    }

    [Fact]
    public void Bind_BuiltinRegexReplaceAcceptsModesOrCount()
    {
        const string text = """
PRINT REGEXREPLACE("A12", "\d+", "#")
PRINT REGEXREPLACE("A12", "a\d+", "#", "i")
PRINT REGEXREPLACE("A12 B34", "\d+", "#", 1)
PRINT REGEXREPLACE("A12 B34", "[a-z]\d+", "#", "i", 1)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.String, BuiltinFunctionId.RegexReplace);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.String, BuiltinFunctionId.RegexReplaceModes);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.String, BuiltinFunctionId.RegexReplaceCount);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.String, BuiltinFunctionId.RegexReplaceModesCount);
    }

    [Fact]
    public void Bind_BuiltinRegexReportsWrongArgumentType()
    {
        BindProgram("PRINT REGEXEXTRACT(\"A12\", \"\\d+\", \"1\")", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinDateTimeFunctionsReturnExpectedTypes()
    {
        const string text = """
PRINT DATE()
PRINT TIME()
PRINT NOW()
PRINT TIMER()
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.String, BuiltinFunctionId.Date);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.String, BuiltinFunctionId.Time);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.String, BuiltinFunctionId.Now);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.Double, BuiltinFunctionId.Timer);
    }

    [Fact]
    public void Bind_BuiltinDateReportsTypeMismatchForNonStringTimeZone()
    {
        BindProgram("PRINT DATE(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinDateTimeStage2FunctionsReturnExpectedTypes()
    {
        const string text = """
PRINT DATESERIAL(2026, 4, 20)
PRINT TIMESERIAL(14, 5, 6)
PRINT YEAR("2026-04-20")
PRINT MONTH("2026-04-20")
PRINT MONTHNAME(4)
PRINT DAY("2026-04-20")
PRINT HOUR("14:05:06")
PRINT MINUTE("14:05:06")
PRINT SECOND("14:05:06")
PRINT WEEKDAY("2026-04-20")
PRINT WEEKDAYNAME(1)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.String, BuiltinFunctionId.DateSerial);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.String, BuiltinFunctionId.TimeSerial);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Integer, BuiltinFunctionId.Year);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.Integer, BuiltinFunctionId.Month);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.String, BuiltinFunctionId.MonthName);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.Integer, BuiltinFunctionId.Day);
        AssertBuiltinCall(program.GlobalStatements[6], TypeSymbol.Integer, BuiltinFunctionId.Hour);
        AssertBuiltinCall(program.GlobalStatements[7], TypeSymbol.Integer, BuiltinFunctionId.Minute);
        AssertBuiltinCall(program.GlobalStatements[8], TypeSymbol.Integer, BuiltinFunctionId.Second);
        AssertBuiltinCall(program.GlobalStatements[9], TypeSymbol.Integer, BuiltinFunctionId.Weekday);
        AssertBuiltinCall(program.GlobalStatements[10], TypeSymbol.String, BuiltinFunctionId.WeekdayName);
    }

    [Fact]
    public void Bind_BuiltinDateTimeStage3FunctionsReturnExpectedTypes()
    {
        const string text = """
PRINT DATEADD("d", 1, "2026-04-20")
PRINT DATEDIFF("d", "2026-04-20", "2026-04-21")
PRINT DATEPART("q", "2026-04-20")
PRINT DATEVALUE("2026-04-20 14:05:06")
PRINT TIMEVALUE("2026-04-20 14:05:06")
PRINT ISDATE("2026-04-20")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.String, BuiltinFunctionId.DateAdd);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Long, BuiltinFunctionId.DateDiff);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Integer, BuiltinFunctionId.DatePart);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.String, BuiltinFunctionId.DateValue);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.String, BuiltinFunctionId.TimeValue);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.Boolean, BuiltinFunctionId.IsDate);
    }

    [Fact]
    public void Bind_BuiltinDateTimeStage4CalendarHelpersReturnExpectedTypes()
    {
        const string text = """
PRINT ISLEAPYEAR(2024)
PRINT DAYSINMONTH(2024, 2)
PRINT STARTOFMONTH("2026-04-20 14:05:06")
PRINT ENDOFMONTH("2024-02-11")
PRINT STARTOFYEAR("2026-04-20")
PRINT ENDOFYEAR("2026-04-20 14:05:06")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.Boolean, BuiltinFunctionId.IsLeapYear);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Integer, BuiltinFunctionId.DaysInMonth);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.String, BuiltinFunctionId.StartOfMonth);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.String, BuiltinFunctionId.EndOfMonth);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.String, BuiltinFunctionId.StartOfYear);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.String, BuiltinFunctionId.EndOfYear);
    }

    [Fact]
    public void Bind_BuiltinDateTimeStage5UtcFunctionsReturnExpectedTypes()
    {
        const string text = """
PRINT DATE("UTC")
PRINT TIME("UTC")
PRINT NOW("UTC")
PRINT UTCDATE()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT UTCOFFSET()
PRINT UTCOFFSET("2026-04-20 14:05:06")
PRINT UTCOFFSET("2026-04-20 14:05:06", "UTC")
PRINT TOUTC("2026-04-20 14:05:06")
PRINT TOUTC("2026-04-20 14:05:06", "UTC")
PRINT FROMUTC("2026-04-20 11:05:06")
PRINT FROMUTC("2026-04-20 11:05:06", "UTC")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.String, BuiltinFunctionId.DateInZone);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.String, BuiltinFunctionId.TimeInZone);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.String, BuiltinFunctionId.NowInZone);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.String, BuiltinFunctionId.UtcDate);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.String, BuiltinFunctionId.UtcTime);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.String, BuiltinFunctionId.UtcNow);
        AssertBuiltinCall(program.GlobalStatements[6], TypeSymbol.Integer, BuiltinFunctionId.UtcOffset);
        AssertBuiltinCall(program.GlobalStatements[7], TypeSymbol.Integer, BuiltinFunctionId.UtcOffsetValue);
        AssertBuiltinCall(program.GlobalStatements[8], TypeSymbol.Integer, BuiltinFunctionId.UtcOffsetValueZone);
        AssertBuiltinCall(program.GlobalStatements[9], TypeSymbol.String, BuiltinFunctionId.ToUtc);
        AssertBuiltinCall(program.GlobalStatements[10], TypeSymbol.String, BuiltinFunctionId.ToUtcZone);
        AssertBuiltinCall(program.GlobalStatements[11], TypeSymbol.String, BuiltinFunctionId.FromUtc);
        AssertBuiltinCall(program.GlobalStatements[12], TypeSymbol.String, BuiltinFunctionId.FromUtcZone);
    }

    [Fact]
    public void Bind_BuiltinYearReportsWrongArgumentType()
    {
        BindProgram("PRINT YEAR(2026)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinFileSystemFunctionsReturnExpectedTypes()
    {
        const string text = """
PRINT FILEEXISTS("notes.txt")
PRINT DIREXISTS(".")
PRINT PATHEXISTS("notes.txt")
PRINT READFILE("notes.txt")
PRINT WRITEFILE("notes.txt", "Hello")
PRINT APPENDFILE("notes.txt", "!")
PRINT FILESIZE("notes.txt")
PRINT FILEDATE("notes.txt")
PRINT FILECREATEDATE("notes.txt")
PRINT FILEACCESSEDDATE("notes.txt")
PRINT DIRDATE("folder")
PRINT GETFILES("folder")
PRINT GETFILES("folder", "*.txt")
PRINT GETDIRS("folder")
PRINT GETDIRS("folder", "data*")
PRINT COMBINEPATH("folder", "notes.txt")
PRINT FILENAME("folder/notes.txt")
PRINT DIRNAME("folder/notes.txt")
PRINT EXTNAME("folder/notes.txt")
PRINT FULLPATH("notes.txt")
PRINT PATHROOT("notes.txt")
PRINT ISABSOLUTEPATH("notes.txt")
PRINT MAKEDIR("folder")
PRINT DELETEFILE("notes.txt")
PRINT COPYFILE("source.txt", "target.txt")
PRINT COPYFILE("source.txt", "target.txt", TRUE)
PRINT MOVEFILE("source.txt", "target.txt")
PRINT MOVEFILE("source.txt", "target.txt", TRUE)
PRINT RENAMEFILE("source.txt", "target.txt")
PRINT DELETEDIR("folder")
PRINT DELETEDIRRECURSIVE("folder")
PRINT COPYDIR("source", "target")
PRINT COPYDIR("source", "target", TRUE)
PRINT MOVEDIR("source", "target")
PRINT RENAMEDIR("source", "target")
PRINT CURRENTDIR()
PRINT CHANGEDIR("folder")
PRINT DIRSIZE("folder")
PRINT FREESPACE("folder")
PRINT TOTALFREESPACE("folder")
PRINT TOTALSPACE("folder")
PRINT ISREADONLY("folder")
PRINT ISHIDDEN("folder")
PRINT DRIVENAME("folder")
PRINT DRIVEFORMAT("folder")
PRINT DRIVETYPE("folder")
PRINT MAKETEMPFILE("demo_")
PRINT MAKETEMPDIR("demo_")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.Boolean, BuiltinFunctionId.FileExists);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Boolean, BuiltinFunctionId.DirExists);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Boolean, BuiltinFunctionId.PathExists);
        AssertBuiltinCall(program.GlobalStatements[3], TypeSymbol.String, BuiltinFunctionId.ReadFile);
        AssertBuiltinCall(program.GlobalStatements[4], TypeSymbol.Boolean, BuiltinFunctionId.WriteFile);
        AssertBuiltinCall(program.GlobalStatements[5], TypeSymbol.Boolean, BuiltinFunctionId.AppendFile);
        AssertBuiltinCall(program.GlobalStatements[6], TypeSymbol.Long, BuiltinFunctionId.FileSize);
        AssertBuiltinCall(program.GlobalStatements[7], TypeSymbol.String, BuiltinFunctionId.FileDate);
        AssertBuiltinCall(program.GlobalStatements[8], TypeSymbol.String, BuiltinFunctionId.FileCreateDate);
        AssertBuiltinCall(program.GlobalStatements[9], TypeSymbol.String, BuiltinFunctionId.FileAccessDate);
        AssertBuiltinCall(program.GlobalStatements[10], TypeSymbol.String, BuiltinFunctionId.DirDate);
        AssertBuiltinCall(program.GlobalStatements[11], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.GetFiles);
        AssertBuiltinCall(program.GlobalStatements[12], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.GetFilesPattern);
        AssertBuiltinCall(program.GlobalStatements[13], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.GetDirs);
        AssertBuiltinCall(program.GlobalStatements[14], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.GetDirsPattern);
        AssertBuiltinCall(program.GlobalStatements[15], TypeSymbol.String, BuiltinFunctionId.CombinePath);
        AssertBuiltinCall(program.GlobalStatements[16], TypeSymbol.String, BuiltinFunctionId.FileName);
        AssertBuiltinCall(program.GlobalStatements[17], TypeSymbol.String, BuiltinFunctionId.DirName);
        AssertBuiltinCall(program.GlobalStatements[18], TypeSymbol.String, BuiltinFunctionId.ExtName);
        AssertBuiltinCall(program.GlobalStatements[19], TypeSymbol.String, BuiltinFunctionId.FullPath);
        AssertBuiltinCall(program.GlobalStatements[20], TypeSymbol.String, BuiltinFunctionId.PathRoot);
        AssertBuiltinCall(program.GlobalStatements[21], TypeSymbol.Boolean, BuiltinFunctionId.IsAbsolutePath);
        AssertBuiltinCall(program.GlobalStatements[22], TypeSymbol.Boolean, BuiltinFunctionId.MakeDir);
        AssertBuiltinCall(program.GlobalStatements[23], TypeSymbol.Boolean, BuiltinFunctionId.DeleteFile);
        AssertBuiltinCall(program.GlobalStatements[24], TypeSymbol.Boolean, BuiltinFunctionId.CopyFile);
        AssertBuiltinCall(program.GlobalStatements[25], TypeSymbol.Boolean, BuiltinFunctionId.CopyFileOverwrite);
        AssertBuiltinCall(program.GlobalStatements[26], TypeSymbol.Boolean, BuiltinFunctionId.MoveFile);
        AssertBuiltinCall(program.GlobalStatements[27], TypeSymbol.Boolean, BuiltinFunctionId.MoveFileOverwrite);
        AssertBuiltinCall(program.GlobalStatements[28], TypeSymbol.Boolean, BuiltinFunctionId.RenameFile);
        AssertBuiltinCall(program.GlobalStatements[29], TypeSymbol.Boolean, BuiltinFunctionId.DeleteDir);
        AssertBuiltinCall(program.GlobalStatements[30], TypeSymbol.Boolean, BuiltinFunctionId.DeleteDirRecursive);
        AssertBuiltinCall(program.GlobalStatements[31], TypeSymbol.Boolean, BuiltinFunctionId.CopyDir);
        AssertBuiltinCall(program.GlobalStatements[32], TypeSymbol.Boolean, BuiltinFunctionId.CopyDirOverwrite);
        AssertBuiltinCall(program.GlobalStatements[33], TypeSymbol.Boolean, BuiltinFunctionId.MoveDir);
        AssertBuiltinCall(program.GlobalStatements[34], TypeSymbol.Boolean, BuiltinFunctionId.RenameDir);
        AssertBuiltinCall(program.GlobalStatements[35], TypeSymbol.String, BuiltinFunctionId.CurrentDir);
        AssertBuiltinCall(program.GlobalStatements[36], TypeSymbol.Boolean, BuiltinFunctionId.ChangeDir);
        AssertBuiltinCall(program.GlobalStatements[37], TypeSymbol.Long, BuiltinFunctionId.DirSize);
        AssertBuiltinCall(program.GlobalStatements[38], TypeSymbol.Long, BuiltinFunctionId.FreeSpace);
        AssertBuiltinCall(program.GlobalStatements[39], TypeSymbol.Long, BuiltinFunctionId.TotalFreeSpace);
        AssertBuiltinCall(program.GlobalStatements[40], TypeSymbol.Long, BuiltinFunctionId.TotalSpace);
        AssertBuiltinCall(program.GlobalStatements[41], TypeSymbol.Boolean, BuiltinFunctionId.IsReadOnly);
        AssertBuiltinCall(program.GlobalStatements[42], TypeSymbol.Boolean, BuiltinFunctionId.IsHidden);
        AssertBuiltinCall(program.GlobalStatements[43], TypeSymbol.String, BuiltinFunctionId.DriveName);
        AssertBuiltinCall(program.GlobalStatements[44], TypeSymbol.String, BuiltinFunctionId.DriveFormat);
        AssertBuiltinCall(program.GlobalStatements[45], TypeSymbol.String, BuiltinFunctionId.DriveType);
        AssertBuiltinCall(program.GlobalStatements[46], TypeSymbol.String, BuiltinFunctionId.MakeTempFile);
        AssertBuiltinCall(program.GlobalStatements[47], TypeSymbol.String, BuiltinFunctionId.MakeTempDir);
    }

    [Fact]
    public void Bind_BuiltinFileSystemReportsWrongArgumentType()
    {
        BindProgram("PRINT FILESIZE(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinReadAllLinesReturnsStringArray()
    {
        var program = BindProgram("PRINT READALLLINES(\"notes.txt\")", out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], ArrayTypeSymbol.Create(TypeSymbol.String), BuiltinFunctionId.ReadAllLines);
    }

    [Fact]
    public void Bind_BuiltinCurrentDirReportsWrongArgumentCount()
    {
        BindProgram("PRINT CURRENTDIR(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("CURRENTDIR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinChangeDirReportsWrongArgumentCount()
    {
        BindProgram("PRINT CHANGEDIR()", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("CHANGEDIR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinPathRootReportsWrongArgumentCount()
    {
        BindProgram("PRINT PATHROOT()", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("PATHROOT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinInStrAcceptsTwoOrThreeArguments()
    {
        const string text = """
PRINT INSTR("banana", "na")
PRINT INSTR(4, "banana", "na")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.Integer, BuiltinFunctionId.InStr);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Integer, BuiltinFunctionId.InStrFrom);
    }

    [Fact]
    public void Bind_BuiltinInStrReportsWrongStartType()
    {
        BindProgram("PRINT INSTR(\"4\", \"banana\", \"na\")", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinInStrRevAndSubStrCountReturnInteger()
    {
        const string text = """
PRINT INSTRREV("banana", "na")
PRINT INSTRREV(4, "banana", "na")
PRINT SUBSTRCOUNT("banana", "na")
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        AssertBuiltinCall(program.GlobalStatements[0], TypeSymbol.Integer, BuiltinFunctionId.InStrRev);
        AssertBuiltinCall(program.GlobalStatements[1], TypeSymbol.Integer, BuiltinFunctionId.InStrRevFrom);
        AssertBuiltinCall(program.GlobalStatements[2], TypeSymbol.Integer, BuiltinFunctionId.SubStrCount);
    }

    [Fact]
    public void Bind_BuiltinSubStrCountReportsWrongFragmentType()
    {
        BindProgram("PRINT SUBSTRCOUNT(\"banana\", 1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinReplaceReportsWrongThirdArgumentType()
    {
        BindProgram("PRINT REPLACE(\"one two\", \"two\", 3)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinRndReturnsDouble()
    {
        var program = BindProgram("PRINT RND()", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Double, call.Type);
        Assert.Equal(BuiltinFunctionId.Rnd, AssertCalledFunction(call).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinRndReportsWrongArityWhenArgumentIsPassed()
    {
        BindProgram("PRINT RND(1)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("RND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinAbsPreservesDecimalType()
    {
        const string text = """
DIM value AS DECIMAL = -10.5
PRINT ABS(value)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[1]);
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Decimal, call.Type);
        Assert.Equal(BuiltinFunctionId.Abs, AssertCalledFunction(call).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinSgnReturnsInteger()
    {
        var program = BindProgram("PRINT SGN(1.5)", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Integer, call.Type);
        Assert.Equal(BuiltinFunctionId.Sgn, AssertCalledFunction(call).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinMinPromotesIntegerAndLongToLong()
    {
        const string text = """
DIM value AS LONG = 21474836479
PRINT MIN(value, 1)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[1]);
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Long, call.Type);
    }

    [Fact]
    public void Bind_BuiltinSinReturnsDouble()
    {
        var program = BindProgram("PRINT SIN(1)", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Double, call.Type);
    }

    [Fact]
    public void Bind_BuiltinAcosReturnsDouble()
    {
        var program = BindProgram("PRINT ACOS(1)", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Double, call.Type);
        Assert.Equal(BuiltinFunctionId.Acos, AssertCalledFunction(call).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinSetSignPreservesFirstArgumentType()
    {
        const string text = """
DIM value AS DECIMAL = -10.5
PRINT SETSIGN(value, 1)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[1]);
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Decimal, call.Type);
        Assert.Equal(BuiltinFunctionId.SetSign, AssertCalledFunction(call).BuiltinId);
    }

    [Fact]
    public void Bind_BuiltinFloorPreservesDoubleType()
    {
        var program = BindProgram("PRINT FLOOR(1.5)", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Double, call.Type);
    }

    [Fact]
    public void Bind_BuiltinFixPreservesDecimalType()
    {
        const string text = """
DIM value AS DECIMAL = -10.5
PRINT FIX(value)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[1]);
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Decimal, call.Type);
    }

    [Fact]
    public void Bind_BuiltinClampReportsWrongArity()
    {
        BindProgram("PRINT CLAMP(1, 2)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("CLAMP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinClampRejectsMixedDecimalAndDoubleArguments()
    {
        const string text = """
DIM value AS DECIMAL = 10.5
PRINT CLAMP(value, 0.1, 20)
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinPowReportsWrongArgumentType()
    {
        BindProgram("PRINT POW(\"x\", 2)", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinAtanhReportsWrongArity()
    {
        BindProgram("PRINT ATANH()", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2004");
        Assert.Contains("ATANH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BuiltinSetSignRejectsWrongArgumentType()
    {
        BindProgram("PRINT SETSIGN(10, \"x\")", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_IfWithBooleanCondition()
    {
        const string text = """
IF TRUE THEN
    PRINT 1
END IF
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.IsType<BoundIfStatement>(Assert.Single(program.GlobalStatements));
    }

    [Fact]
    public void Bind_ElseIfChainAsNestedElseBranch()
    {
        const string text = """
DIM score AS INTEGER = 7
IF score > 10 THEN
    PRINT "high"
ELSEIF score > 5 THEN
    PRINT "mid"
ELSE
    PRINT "low"
END IF
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var ifStatement = Assert.IsType<BoundIfStatement>(program.GlobalStatements[1]);
        var elseIfStatement = Assert.IsType<BoundIfStatement>(Assert.Single(ifStatement.ElseStatements));
        Assert.Single(elseIfStatement.Statements);
        Assert.Single(elseIfStatement.ElseStatements);
    }

    [Fact]
    public void Bind_SelectCaseStatement()
    {
        const string text = """
DIM score AS INTEGER = 7
SELECT CASE score
    CASE 1, 5 TO 10, IS > 100
        PRINT "match"
    CASE ELSE
        PRINT "other"
END SELECT
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundSelectCaseStatement>(program.GlobalStatements[1]);
        var clause = Assert.Single(statement.Clauses);
        Assert.Equal(3, clause.Selectors.Count);
        Assert.IsType<BoundCaseValueSelector>(clause.Selectors[0]);
        Assert.IsType<BoundCaseRangeSelector>(clause.Selectors[1]);
        var comparisonSelector = Assert.IsType<BoundCaseComparisonSelector>(clause.Selectors[2]);
        Assert.Equal(SyntaxKind.GreaterToken, comparisonSelector.OperatorKind);
        Assert.Single(statement.ElseStatements);
    }

    [Fact]
    public void Bind_StringOrderedComparison()
    {
        const string text = """
DIM name AS STRING = "Mike"
IF name > "M" THEN
    PRINT "later"
END IF
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var ifStatement = Assert.IsType<BoundIfStatement>(program.GlobalStatements[1]);
        var condition = Assert.IsType<BoundBinaryExpression>(ifStatement.Condition);
        Assert.Equal(SyntaxKind.GreaterToken, condition.OperatorKind);
        Assert.Equal(TypeSymbol.Boolean, condition.Type);
    }

    [Fact]
    public void Bind_PromotesIntegerAndLongArithmeticToLong()
    {
        const string text = """
DIM value AS LONG = 21474836479
PRINT value + 1
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[1]);
        var expression = Assert.IsType<BoundBinaryExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Long, expression.Type);
    }

    [Fact]
    public void Bind_PromotesIntegerAndLongIntegralDivisionAndRemainderToLong()
    {
        const string text = """
DIM value AS LONG = 21474836479
PRINT value \ 2
PRINT value MOD 2
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var divisionPrint = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[1]);
        var division = Assert.IsType<BoundBinaryExpression>(Assert.Single(divisionPrint.Items).Expression);
        Assert.Equal(SyntaxKind.BackslashToken, division.OperatorKind);
        Assert.Equal(TypeSymbol.Long, division.Type);

        var remainderPrint = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[2]);
        var remainder = Assert.IsType<BoundBinaryExpression>(Assert.Single(remainderPrint.Items).Expression);
        Assert.Equal(SyntaxKind.ModKeyword, remainder.OperatorKind);
        Assert.Equal(TypeSymbol.Long, remainder.Type);
    }

    [Fact]
    public void Bind_ExponentiationReturnsDouble()
    {
        var program = BindProgram("PRINT 2 ^ 3", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var expression = Assert.IsType<BoundBinaryExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(SyntaxKind.CaretToken, expression.OperatorKind);
        Assert.Equal(TypeSymbol.Double, expression.Type);
    }

    [Fact]
    public void Bind_BitwiseOperatorsPromoteIntegralOperands()
    {
        const string text = """
DIM value AS LONG = 1
PRINT value XOR 2
PRINT value AND 2
PRINT value OR 2
PRINT value EQV 2
PRINT value IMP 2
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(TypeSymbol.Long, Assert.IsType<BoundBinaryExpression>(AssertSinglePrintExpression(program.GlobalStatements[1])).Type);
        Assert.Equal(TypeSymbol.Long, Assert.IsType<BoundBinaryExpression>(AssertSinglePrintExpression(program.GlobalStatements[2])).Type);
        Assert.Equal(TypeSymbol.Long, Assert.IsType<BoundBinaryExpression>(AssertSinglePrintExpression(program.GlobalStatements[3])).Type);
        Assert.Equal(TypeSymbol.Long, Assert.IsType<BoundBinaryExpression>(AssertSinglePrintExpression(program.GlobalStatements[4])).Type);
        Assert.Equal(TypeSymbol.Long, Assert.IsType<BoundBinaryExpression>(AssertSinglePrintExpression(program.GlobalStatements[5])).Type);
    }

    [Fact]
    public void Bind_ShiftsPreserveLeftOperandType()
    {
        const string text = """
PRINT 1 << 2
DIM value AS LONG = 1
PRINT value >> 2
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(TypeSymbol.Integer, Assert.IsType<BoundBinaryExpression>(AssertSinglePrintExpression(program.GlobalStatements[0])).Type);
        Assert.Equal(TypeSymbol.Long, Assert.IsType<BoundBinaryExpression>(AssertSinglePrintExpression(program.GlobalStatements[2])).Type);
    }

    [Fact]
    public void Bind_NotSupportsIntegralOperands()
    {
        var program = BindProgram("PRINT NOT 0", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var expression = Assert.IsType<BoundUnaryExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(SyntaxKind.NotKeyword, expression.OperatorKind);
        Assert.Equal(TypeSymbol.Integer, expression.Type);
    }

    [Theory]
    [InlineData("PRINT TRUE XOR 1", "XOR")]
    [InlineData("PRINT TRUE EQV 1", "EQV")]
    [InlineData("PRINT TRUE IMP 1", "IMP")]
    public void Bind_ReportsMixedBooleanAndIntegralBitwiseExpression(string text, string operatorText)
    {
        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains(operatorText, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_PromotesIntegerAndDecimalArithmeticToDecimal()
    {
        const string text = """
DIM price AS DECIMAL = 10.50
PRINT price + 1
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[1]);
        var expression = Assert.IsType<BoundBinaryExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(TypeSymbol.Decimal, expression.Type);
    }

    [Fact]
    public void Bind_ReportsDecimalAndDoubleMixedArithmetic()
    {
        const string text = """
DIM price AS DECIMAL = 10.50
PRINT price + 0.1
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("PRINT 7.5 \\ 2")]
    [InlineData("PRINT 7 MOD 2.5")]
    [InlineData("PRINT 7.5 MOD 2")]
    public void Bind_ReportsNonIntegralTypesForIntegerDivisionAndRemainder(string source)
    {
        BindProgram(source, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("оператор", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsDecimalLiteralOutOfRangeForExplicitDecimal()
    {
        BindProgram("DIM value AS DECIMAL = 792281625142643375935439503350.0", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2018");
        Assert.Contains("DECIMAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsInvalidBooleanOrderedComparison()
    {
        BindProgram("PRINT TRUE > FALSE", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("BOOLEAN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_SelectCaseStringRange()
    {
        const string text = """
DIM command AS STRING = "G"
SELECT CASE command
    CASE "A" TO "M"
        PRINT "first"
    CASE ELSE
        PRINT "other"
END SELECT
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundSelectCaseStatement>(program.GlobalStatements[1]);
        var clause = Assert.Single(statement.Clauses);
        Assert.IsType<BoundCaseRangeSelector>(Assert.Single(clause.Selectors));
    }

    [Fact]
    public void Bind_SelectCaseRangeOnBooleanReportsDiagnostic()
    {
        const string text = """
DIM flag AS BOOLEAN = TRUE
SELECT CASE flag
    CASE FALSE TO TRUE
        PRINT flag
END SELECT
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2015");
        Assert.Contains("CASE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsInvalidIfCondition()
    {
        const string text = """
IF 1 THEN
    PRINT 1
END IF
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2008");
        Assert.Contains("BOOLEAN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_WhileStatement()
    {
        const string text = """
WHILE TRUE
    PRINT 1
END WHILE
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.IsType<BoundWhileStatement>(Assert.Single(program.GlobalStatements));
    }

    [Fact]
    public void Bind_DoLoopWithLeadingWhileCondition()
    {
        const string text = """
DO WHILE TRUE
    PRINT 1
LOOP
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundDoLoopStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(BoundDoLoopConditionKind.While, statement.ConditionKind);
        Assert.False(statement.EvaluateConditionAfterBody);
    }

    [Fact]
    public void Bind_DoLoopWithLeadingUntilCondition()
    {
        const string text = """
DO UNTIL TRUE
    PRINT 1
LOOP
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundDoLoopStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(BoundDoLoopConditionKind.Until, statement.ConditionKind);
        Assert.False(statement.EvaluateConditionAfterBody);
    }

    [Fact]
    public void Bind_DoLoopWithTrailingWhileCondition()
    {
        const string text = """
DO
    PRINT 1
LOOP WHILE FALSE
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundDoLoopStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(BoundDoLoopConditionKind.While, statement.ConditionKind);
        Assert.True(statement.EvaluateConditionAfterBody);
    }

    [Fact]
    public void Bind_DoLoopWithoutCondition()
    {
        const string text = """
DO
    EXIT DO
LOOP
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundDoLoopStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(BoundDoLoopConditionKind.None, statement.ConditionKind);
        Assert.Null(statement.Condition);
    }

    [Fact]
    public void Bind_ForWithNumericControlVariable()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i
NEXT
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.IsType<BoundForStatement>(Assert.Single(program.GlobalStatements));
    }

    [Fact]
    public void Bind_ForWithLongControlVariable()
    {
        const string text = """
FOR i = 21474836479 TO 21474836481
    PRINT i
NEXT
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<BoundForStatement>(Assert.Single(program.GlobalStatements));
        Assert.Equal(TypeSymbol.Long, statement.Variable.Type);
    }

    [Fact]
    public void Bind_ExitForInsideFor()
    {
        const string text = """
FOR i = 1 TO 3
    EXIT FOR
NEXT
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var forStatement = Assert.IsType<BoundForStatement>(Assert.Single(program.GlobalStatements));
        Assert.IsType<BoundExitForStatement>(Assert.Single(forStatement.Statements));
    }

    [Fact]
    public void Bind_ForControlVariableIsNotVisibleAfterLoop()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i
NEXT
PRINT i
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2001");
        Assert.Contains("i", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_BlockLocalVariableIsNotVisibleAfterIf()
    {
        const string text = """
IF TRUE THEN
    DIM message AS STRING = "ok"
    PRINT message
END IF
PRINT message
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2001");
        Assert.Contains("message", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_SiblingBranchesCanReuseSameLocalName()
    {
        const string text = """
IF TRUE THEN
    DIM message AS STRING = "yes"
    PRINT message
ELSE
    DIM message AS STRING = "no"
    PRINT message
END IF
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.IsType<BoundIfStatement>(Assert.Single(program.GlobalStatements));
    }

    [Fact]
    public void Bind_ReportsShadowingEnclosingName()
    {
        const string text = """
DIM total AS INTEGER = 1
IF TRUE THEN
    DIM total AS INTEGER = 2
END IF
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2016");
        Assert.Contains("total", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsShadowingForLoopVariableWithForContext()
    {
        const string text = """
DIM i AS INTEGER = 10
FOR i = 1 TO 2
    PRINT i
NEXT
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2016");
        Assert.Contains("i", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FOR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ExitDoInsideDoLoop()
    {
        const string text = """
DO WHILE TRUE
    EXIT DO
LOOP
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var doLoopStatement = Assert.IsType<BoundDoLoopStatement>(Assert.Single(program.GlobalStatements));
        Assert.IsType<BoundExitDoStatement>(Assert.Single(doLoopStatement.Statements));
    }

    [Fact]
    public void Bind_ReportsInvalidForControlVariable()
    {
        const string text = """
DIM name AS STRING
FOR name = "a" TO "z"
    PRINT name
NEXT
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2009");
        Assert.Contains("FOR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsExitForOutsideFor()
    {
        const string text = """
WHILE TRUE
    EXIT FOR
END WHILE
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2013");
        Assert.Contains("EXIT FOR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsExitDoOutsideDo()
    {
        const string text = """
WHILE TRUE
    EXIT DO
END WHILE
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2014");
        Assert.Contains("EXIT DO", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReturnInFunction()
    {
        const string text = """
FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var routine = Assert.Single(program.Routines);
        var returnStatement = Assert.IsType<BoundReturnStatement>(Assert.Single(routine.Statements));
        Assert.NotNull(returnStatement.Expression);
    }

    [Fact]
    public void Bind_ReportsInvalidReturnInSub()
    {
        const string text = """
SUB Demo()
    RETURN 1
END SUB
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2006");
        Assert.Contains("SUB", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsMissingReturnValueInFunction()
    {
        const string text = """
FUNCTION Demo() AS INTEGER
    RETURN
END FUNCTION
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2007");
        Assert.Contains("FUNCTION", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsAssignmentToConst()
    {
        const string text = """
CONST answer AS INTEGER = 42
answer = 10
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2010");
        Assert.Contains("answer", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsUnknownCallable()
    {
        BindProgram("Missing()", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2005");
        Assert.Contains("Missing", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_LocalFunctionCallBeforeDeclaration()
    {
        const string text = """
DIM result AS INTEGER
Outer()

SUB Outer()
    result = AddLocal(1, 2)

    FUNCTION AddLocal(left AS INTEGER, right AS INTEGER) AS INTEGER
        RETURN left + right
    END FUNCTION
END SUB
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(2, program.Routines.Count);

        var outerRoutine = Assert.Single(program.Routines, static routine => routine.Symbol.Name == "Outer");
        var assignment = Assert.IsType<BoundAssignmentStatement>(Assert.Single(outerRoutine.Statements));
        var call = Assert.IsType<BoundCallExpression>(assignment.Expression);
        Assert.Equal("AddLocal", AssertCalledFunction(call).Name);
    }

    [Fact]
    public void Bind_LocalCallableIsNotVisibleOutsideEnclosingRoutine()
    {
        const string text = """
SUB Outer()
    FUNCTION Hidden() AS INTEGER
        RETURN 1
    END FUNCTION
END SUB

PRINT Hidden()
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2005");
        Assert.Contains("Hidden", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_LocalFunctionCanCaptureOuterLocalVariableDeclaredEarlier()
    {
        const string text = """
SUB Outer()
    DIM value AS INTEGER = 1
    PRINT Inner()

    FUNCTION Inner() AS INTEGER
        RETURN value
    END FUNCTION
END SUB
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var innerRoutine = Assert.Single(program.Routines, static routine => routine.Symbol.Name == "Inner");
        var returnStatement = Assert.IsType<BoundReturnStatement>(Assert.Single(innerRoutine.Statements));
        var variable = Assert.IsType<BoundVariableExpression>(returnStatement.Expression);
        Assert.Equal("value", variable.Variable.Name);
        Assert.NotNull(innerRoutine.Symbol.ContainingRoutine);
        Assert.Equal("Outer", innerRoutine.Symbol.ContainingRoutine!.Name);
    }

    [Fact]
    public void Bind_LocalFunctionCannotCaptureOuterLocalVariableDeclaredAfterItsDeclaration()
    {
        const string text = """
SUB Outer()
    FUNCTION Inner() AS INTEGER
        RETURN value
    END FUNCTION

    DIM value AS INTEGER = 1
    PRINT Inner()
END SUB
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2001");
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_TypeDeclarationFieldAccess()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Age AS INTEGER
END TYPE

DIM user AS Person
user.Name = "Ada"
user.Age = 36
PRINT user.Name
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var userType = Assert.IsType<UserDefinedTypeSymbol>(declaration.Variable.Type);
        Assert.Equal("Person", userType.Name);
        Assert.Equal(["Name", "Age"], userType.Fields.Select(static field => field.Name));

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        var fieldAccess = Assert.IsType<BoundFieldAccessExpression>(assignment.Target);
        Assert.Equal("Name", fieldAccess.Field.Name);
        Assert.Equal(TypeSymbol.String, fieldAccess.Type);

        var printExpression = AssertSinglePrintExpression(program.GlobalStatements[3]);
        Assert.IsType<BoundFieldAccessExpression>(printExpression);
    }

    [Fact]
    public void Bind_NestedTypeFieldAccess()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Home AS Address
END TYPE

DIM user AS Person
user.Home.City = "Moscow"
PRINT user.Home.City
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        var cityAccess = Assert.IsType<BoundFieldAccessExpression>(assignment.Target);
        Assert.Equal("City", cityAccess.Field.Name);
        Assert.Equal(TypeSymbol.String, cityAccess.Type);
    }

    [Fact]
    public void Bind_TypeArrayElementFieldAccess()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM people(1 TO 2) AS Person
people(1).Name = "Ada"
PRINT people(2).Name
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var arrayType = Assert.IsType<ArrayTypeSymbol>(declaration.Variable.Type);
        var userType = Assert.IsType<UserDefinedTypeSymbol>(arrayType.ElementType);
        Assert.Equal("Person", userType.Name);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        var fieldAssignmentTarget = Assert.IsType<BoundFieldAccessExpression>(assignment.Target);
        var arrayElementTarget = Assert.IsType<BoundArrayAccessExpression>(fieldAssignmentTarget.Target);
        Assert.Equal(userType, arrayElementTarget.Type);
        Assert.Equal("Name", fieldAssignmentTarget.Field.Name);
        Assert.Equal(TypeSymbol.String, fieldAssignmentTarget.Type);

        var printExpression = AssertSinglePrintExpression(program.GlobalStatements[2]);
        var fieldPrintTarget = Assert.IsType<BoundFieldAccessExpression>(printExpression);
        Assert.IsType<BoundArrayAccessExpression>(fieldPrintTarget.Target);
        Assert.Equal(TypeSymbol.String, fieldPrintTarget.Type);
    }

    [Fact]
    public void Bind_TypeDynamicArrayField()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE

DIM user AS Person
REDIM user.Scores(1 TO 2)
user.Scores(1) = 10
PRINT user.Scores(2)
ERASE user.Scores
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var userType = Assert.IsType<UserDefinedTypeSymbol>(declaration.Variable.Type);
        var field = Assert.Single(userType.Fields);
        Assert.Equal("Scores", field.Name);
        var fieldArrayType = Assert.IsType<ArrayTypeSymbol>(field.Type);
        Assert.Equal(TypeSymbol.Integer, fieldArrayType.ElementType);

        var redim = Assert.IsType<BoundRedimStatement>(program.GlobalStatements[1]);
        var redimTarget = Assert.IsType<BoundFieldAccessExpression>(redim.Target);
        Assert.Equal(field, redimTarget.Field);
        Assert.Equal(fieldArrayType, redimTarget.Type);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var arrayAccess = Assert.IsType<BoundArrayAccessExpression>(assignment.Target);
        var assignmentTarget = Assert.IsType<BoundFieldAccessExpression>(arrayAccess.Target);
        Assert.Equal(field, assignmentTarget.Field);
        Assert.Equal(TypeSymbol.Integer, arrayAccess.Type);

        var printExpression = AssertSinglePrintExpression(program.GlobalStatements[3]);
        var printArrayAccess = Assert.IsType<BoundArrayAccessExpression>(printExpression);
        Assert.IsType<BoundFieldAccessExpression>(printArrayAccess.Target);
        Assert.Equal(TypeSymbol.Integer, printArrayAccess.Type);

        var erase = Assert.IsType<BoundEraseStatement>(program.GlobalStatements[4]);
        Assert.IsType<BoundFieldAccessExpression>(erase.Target);
    }

    [Fact]
    public void Bind_TypeDynamicArrayFieldOnArrayElement()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE

DIM people(1 TO 1) AS Person
REDIM people(1).Scores(1 TO 2)
people(1).Scores(2) = 20
PRINT people(1).Scores(2)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var redim = Assert.IsType<BoundRedimStatement>(program.GlobalStatements[1]);
        var redimTarget = Assert.IsType<BoundFieldAccessExpression>(redim.Target);
        Assert.IsType<BoundArrayAccessExpression>(redimTarget.Target);
        Assert.IsType<ArrayTypeSymbol>(redimTarget.Type);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var assignmentTarget = Assert.IsType<BoundArrayAccessExpression>(assignment.Target);
        var assignmentFieldTarget = Assert.IsType<BoundFieldAccessExpression>(assignmentTarget.Target);
        Assert.IsType<BoundArrayAccessExpression>(assignmentFieldTarget.Target);

        var printExpression = AssertSinglePrintExpression(program.GlobalStatements[3]);
        var printTarget = Assert.IsType<BoundArrayAccessExpression>(printExpression);
        var printFieldTarget = Assert.IsType<BoundFieldAccessExpression>(printTarget.Target);
        Assert.IsType<BoundArrayAccessExpression>(printFieldTarget.Target);
    }

    [Fact]
    public void Bind_TypeFixedArrayField()
    {
        const string text = """
TYPE Person
    Scores(-1 TO 3) AS INTEGER
END TYPE

DIM user AS Person
user.Scores(0) = 10
PRINT user.Scores(3)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var userType = Assert.IsType<UserDefinedTypeSymbol>(declaration.Variable.Type);
        var field = Assert.Single(userType.Fields);
        var fieldArrayType = Assert.IsType<ArrayTypeSymbol>(field.Type);
        Assert.True(field.IsFixedArray);
        Assert.False(field.IsDynamicArray);
        Assert.Equal(-1, field.FixedArrayLowerBound);
        Assert.Equal(3, field.FixedArrayUpperBound);
        Assert.Equal(TypeSymbol.Integer, fieldArrayType.ElementType);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        var assignmentTarget = Assert.IsType<BoundArrayAccessExpression>(assignment.Target);
        var assignmentFieldTarget = Assert.IsType<BoundFieldAccessExpression>(assignmentTarget.Target);
        Assert.Equal(field, assignmentFieldTarget.Field);
        Assert.Equal(TypeSymbol.Integer, assignmentTarget.Type);

        var printExpression = AssertSinglePrintExpression(program.GlobalStatements[2]);
        var printTarget = Assert.IsType<BoundArrayAccessExpression>(printExpression);
        var printFieldTarget = Assert.IsType<BoundFieldAccessExpression>(printTarget.Target);
        Assert.Equal(field, printFieldTarget.Field);
    }

    [Fact]
    public void Bind_TypeFixedArrayFieldWithConstBounds()
    {
        const string text = """
CONST MinScore AS INTEGER = 1
CONST MaxScore AS LONG = MinScore + 2

TYPE Person
    Scores(MinScore TO MaxScore) AS INTEGER
END TYPE

DIM user AS Person
PRINT user.Scores(3)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[2]);
        var userType = Assert.IsType<UserDefinedTypeSymbol>(declaration.Variable.Type);
        var field = Assert.Single(userType.Fields);
        Assert.True(field.IsFixedArray);
        Assert.Equal(1, field.FixedArrayLowerBound);
        Assert.Equal(3, field.FixedArrayUpperBound);

        var printExpression = AssertSinglePrintExpression(program.GlobalStatements[3]);
        var printTarget = Assert.IsType<BoundArrayAccessExpression>(printExpression);
        Assert.Equal(field, Assert.IsType<BoundFieldAccessExpression>(printTarget.Target).Field);
    }

    [Fact]
    public void Bind_TypeFixedArrayFieldWithConstBoundsDeclaredLater()
    {
        const string text = """
TYPE Person
    Scores(MinScore TO MaxScore) AS INTEGER
END TYPE

CONST MinScore AS INTEGER = 1
CONST MaxScore AS LONG = MinScore + 2

DIM user AS Person
PRINT user.Scores(3)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[2]);
        var userType = Assert.IsType<UserDefinedTypeSymbol>(declaration.Variable.Type);
        var field = Assert.Single(userType.Fields);
        Assert.True(field.IsFixedArray);
        Assert.Equal(1, field.FixedArrayLowerBound);
        Assert.Equal(3, field.FixedArrayUpperBound);
    }

    [Fact]
    public void Bind_DimFixedLengthStringVariable()
    {
        const string text = """
DIM code AS STRING * 4
PRINT code
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        Assert.Equal(TypeSymbol.String, declaration.Variable.Type);
        Assert.Equal(4, declaration.Variable.FixedStringLength);

        var printExpression = AssertSinglePrintExpression(program.GlobalStatements[1]);
        var variable = Assert.IsType<BoundVariableExpression>(printExpression);
        Assert.Equal(4, variable.Variable.FixedStringLength);
    }

    [Fact]
    public void Bind_TypeFixedLengthStringField()
    {
        const string text = """
TYPE Person
    Name AS STRING * 20
    Aliases() AS STRING * 5
END TYPE

DIM user AS Person
PRINT user.Name
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var userType = Assert.IsType<UserDefinedTypeSymbol>(declaration.Variable.Type);
        Assert.Equal(2, userType.Fields.Count);

        var nameField = userType.Fields[0];
        var aliasesField = userType.Fields[1];

        Assert.Equal(TypeSymbol.String, nameField.Type);
        Assert.Equal(20, nameField.FixedStringLength);
        Assert.True(aliasesField.IsDynamicArray);
        Assert.Equal(5, aliasesField.FixedStringLength);

        var printExpression = AssertSinglePrintExpression(program.GlobalStatements[1]);
        var fieldAccess = Assert.IsType<BoundFieldAccessExpression>(printExpression);
        Assert.Equal(nameField, fieldAccess.Field);
    }

    [Fact]
    public void Bind_ReportsFixedStringConstDeclaration()
    {
        BindProgram("CONST Code AS STRING * 4 = \"AB\"", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2026");
        Assert.Contains("Code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsFixedStringFunctionParameter()
    {
        const string text = """
FUNCTION Pad(value AS STRING * 4) AS STRING
    RETURN value
END FUNCTION
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2026");
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsFixedStringFunctionReturnType()
    {
        const string text = """
FUNCTION Code() AS STRING * 4
    RETURN "AB"
END FUNCTION
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2026");
        Assert.Contains("Code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsNestedFixedStringInsideFunctionType()
    {
        BindProgram("DIM callback AS FUNCTION(STRING * 4) AS INTEGER", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2026");
        Assert.Contains("callback", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_TypeGroupedFields()
    {
        const string text = """
TYPE Person
    Name, Alias AS STRING
    Scores(), Flags(1 TO 2) AS INTEGER
END TYPE

DIM user AS Person
REDIM user.Scores(1 TO 3)
user.Alias = "Ada"
user.Flags(2) = 7
PRINT user.Name
PRINT user.Flags(2)
""";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var userType = Assert.IsType<UserDefinedTypeSymbol>(declaration.Variable.Type);
        Assert.Equal(["Name", "Alias", "Scores", "Flags"], userType.Fields.Select(static field => field.Name));

        var nameField = userType.Fields[0];
        var aliasField = userType.Fields[1];
        var scoresField = userType.Fields[2];
        var flagsField = userType.Fields[3];

        Assert.Equal(TypeSymbol.String, nameField.Type);
        Assert.Equal(TypeSymbol.String, aliasField.Type);
        Assert.True(scoresField.IsDynamicArray);
        Assert.True(flagsField.IsFixedArray);
        Assert.Equal(1, flagsField.FixedArrayLowerBound);
        Assert.Equal(2, flagsField.FixedArrayUpperBound);

        var redim = Assert.IsType<BoundRedimStatement>(program.GlobalStatements[1]);
        var redimTarget = Assert.IsType<BoundFieldAccessExpression>(redim.Target);
        Assert.Equal(scoresField, redimTarget.Field);

        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var assignmentTarget = Assert.IsType<BoundFieldAccessExpression>(assignment.Target);
        Assert.Equal(aliasField, assignmentTarget.Field);

        var arrayAssignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[3]);
        var arrayTarget = Assert.IsType<BoundArrayAccessExpression>(arrayAssignment.Target);
        var fieldTarget = Assert.IsType<BoundFieldAccessExpression>(arrayTarget.Target);
        Assert.Equal(flagsField, fieldTarget.Field);

        var firstPrintExpression = AssertSinglePrintExpression(program.GlobalStatements[4]);
        var firstPrintTarget = Assert.IsType<BoundFieldAccessExpression>(firstPrintExpression);
        Assert.Equal(nameField, firstPrintTarget.Field);

        var secondPrintExpression = AssertSinglePrintExpression(program.GlobalStatements[5]);
        var secondPrintTarget = Assert.IsType<BoundArrayAccessExpression>(secondPrintExpression);
        Assert.Equal(flagsField, Assert.IsType<BoundFieldAccessExpression>(secondPrintTarget.Target).Field);
    }

    [Fact]
    public void Bind_ReportsRedimAndEraseForFixedTypeArrayField()
    {
        const string text = """
TYPE Person
    Scores(1 TO 3) AS INTEGER
END TYPE

DIM user AS Person
REDIM user.Scores(1 TO 4)
ERASE user.Scores
""";

        BindProgram(text, out var diagnostics);

        var fixedArrayDiagnostics = diagnostics
            .Where(static d => d.Code == "MB2025")
            .ToArray();

        Assert.Equal(2, fixedArrayDiagnostics.Length);
        Assert.All(fixedArrayDiagnostics, diagnostic => Assert.Contains("Scores", diagnostic.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Bind_ReportsRecursiveTypeThroughArrayField()
    {
        const string text = """
TYPE Node
    Children(1 TO 1) AS Node
END TYPE
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2022");
        Assert.Contains("Node", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsUnknownType()
    {
        BindProgram("DIM user AS MissingType", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2021");
        Assert.Contains("MissingType", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsUnknownTypeField()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM user AS Person
PRINT user.Age
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2023");
        Assert.Contains("Age", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_ReportsInvalidFieldAccessReceiver()
    {
        const string text = """
DIM value AS INTEGER
PRINT value.Text
""";

        BindProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2024");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static BoundProgram BindProgram(string text, out DiagnosticBag diagnostics)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);
        return Binder.Bind(syntax, out diagnostics);
    }

    private static FunctionSymbol AssertCalledFunction(BoundCallExpression call)
    {
        var function = Assert.IsType<BoundFunctionExpression>(call.Target);
        return function.Function;
    }

    private static BoundExpression AssertSinglePrintExpression(BoundStatement statement)
    {
        return Assert.Single(Assert.IsType<BoundPrintStatement>(statement).Items).Expression;
    }

    private static void AssertBuiltinCall(BoundStatement statement, TypeSymbol expectedType, BuiltinFunctionId expectedId)
    {
        var expression = statement is BoundPrintStatement print
            ? Assert.Single(print.Items).Expression
            : Assert.IsType<BoundExpressionStatement>(statement).Expression;
        var call = Assert.IsType<BoundCallExpression>(expression);

        Assert.Equal(expectedType, call.Type);
        Assert.Equal(expectedId, AssertCalledFunction(call).BuiltinId);
    }
}
