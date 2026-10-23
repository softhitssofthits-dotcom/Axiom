using Axiom.Core;
using Axiom.Parsing;
using Axiom.Syntax;

namespace Axiom.Semantics.Tests;

public sealed class SquareBracketBindingTests
{
    [Fact]
    public void WholeArrayRetainsVariableAndFieldBoundNodes()
    {
        var program = Bind("TYPE Bag\nfruits[] AS STRING\nEND TYPE\nDIM bagValue AS Bag\nDIM fruits[] AS STRING = SPLIT(\"яблоко,банан\", \",\")\nDIM basket[] = fruits[]\nbagValue.fruits[] = basket[]\nbasket[] = bagValue.fruits[]", out var diagnostics);
        Assert.Empty(diagnostics);
        var fruits = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[1]);
        var basket = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[2]);
        Assert.Same(fruits.Variable, Assert.IsType<BoundVariableExpression>(basket.Initializer).Variable);
        var fieldAssignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[3]);
        Assert.IsType<BoundFieldAccessExpression>(fieldAssignment.Target);
        Assert.Same(basket.Variable, Assert.IsType<BoundVariableExpression>(fieldAssignment.Expression).Variable);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[4]);
        Assert.Same(basket.Variable, Assert.IsType<BoundVariableExpression>(assignment.Target).Variable);
        Assert.IsType<BoundFieldAccessExpression>(assignment.Expression);
    }

    [Fact]
    public void IndicesRangesAndCallsRetainExistingBoundForms()
    {
        var program = Bind("DIM grid[-2 TO -1, 7 TO 8] AS INTEGER\nDIM rowValues[] = grid[-1]\nDIM windowValues[,] = grid[-2 TO -1, 7 TO 8]\nPRINT grid[-1, Choose()]\nPRINT Make()[1]\nFUNCTION Choose() AS INTEGER\nRETURN 7\nEND FUNCTION\nFUNCTION Make() AS STRING[]\nRETURN SPLIT(\"яблоко\", \",\")\nEND FUNCTION", out var diagnostics);
        Assert.Empty(diagnostics);
        var row = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[1]);
        var rowAccess = Assert.IsType<BoundArrayAccessExpression>(row.Initializer);
        AssertArray(rowAccess.Type, TypeSymbol.Integer, 1);
        var window = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[2]);
        var windowAccess = Assert.IsType<BoundArrayAccessExpression>(window.Initializer);
        AssertArray(windowAccess.Type, TypeSymbol.Integer, 2);
        Assert.All(windowAccess.Indices, index => Assert.IsType<BoundArrayRangeExpression>(index));
        var indexed = PrintExpression(program, 3);
        Assert.Equal(TypeSymbol.Integer, indexed.Type);
        Assert.IsType<BoundCallExpression>(Assert.IsType<BoundArrayAccessExpression>(indexed).Indices[1]);
        var result = Assert.IsType<BoundArrayAccessExpression>(PrintExpression(program, 4));
        Assert.IsType<BoundCallExpression>(result.Target);
        Assert.Equal(TypeSymbol.String, result.Type);
    }

    [Theory]
    [InlineData("SUB Show(values[] AS STRING)\nEND SUB", 1, false)]
    [InlineData("SUB Show(BYREF values[,] AS STRING)\nEND SUB", 2, true)]
    [InlineData("SUB Show(BYVAL values[,,] AS STRING)\nEND SUB", 3, false)]
    public void NamedParameterRanksBecomeArrayTypes(string source, int rank, bool byRef)
    {
        var program = Bind(source, out var diagnostics);
        Assert.Empty(diagnostics);
        var parameter = Assert.Single(Assert.Single(program.Routines).Symbol.Parameters);
        AssertArray(parameter.Type, TypeSymbol.String, rank);
        Assert.Equal(byRef, parameter.IsByRef);
    }

    [Fact]
    public void MethodsAndLocalRoutinesUseArrayParameterTypes()
    {
        const string source = "TYPE Bag\nSUB Show(BYREF values[] AS STRING)\nEND SUB\nEND TYPE\nSUB Outer()\nSUB Inner(values[,] AS INTEGER)\nEND SUB\nEND SUB";
        var program = Bind(source, out var diagnostics);
        Assert.Empty(diagnostics);
        var method = Assert.Single(program.Routines, routine => routine.Symbol.ShortName == "Show");
        AssertArray(method.Symbol.Parameters[1].Type, TypeSymbol.String, 1);
        Assert.True(method.Symbol.Parameters[1].IsByRef);
        var inner = Assert.Single(program.Routines, routine => routine.Symbol.Name == "Inner");
        AssertArray(Assert.Single(inner.Symbol.Parameters).Type, TypeSymbol.Integer, 2);
    }

    [Fact]
    public void WholeArrayByRefArgumentRetainsOriginalVariable()
    {
        var program = Bind("DIM fruits[] AS STRING\nRefill(fruits[])\nSUB Refill(BYREF values[] AS STRING)\nvalues[] = SPLIT(\"груша\", \",\")\nEND SUB", out var diagnostics);
        Assert.Empty(diagnostics);
        var variable = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]).Variable;
        var call = Assert.IsType<BoundCallExpression>(Assert.IsType<BoundExpressionStatement>(program.GlobalStatements[1]).Expression);
        Assert.Same(variable, Assert.IsType<BoundVariableExpression>(Assert.Single(call.Arguments)).Variable);
    }

    [Theory]
    [InlineData("DIM fruits[] AS STRING\nPRINT ARRAYLENGTH(fruits)", "fruits", 2, 19)]
    [InlineData("DIM fruits[] AS STRING\nfruits = SPLIT(\"яблоко\", \",\")", "fruits", 2, 1)]
    [InlineData("DIM fruits[] AS STRING\nPRINT fruits()", "fruits", 2, 7)]
    [InlineData("DIM fruits[] AS STRING\nPRINT fruits(1)", "fruits", 2, 7)]
    [InlineData("DIM fruits[] AS STRING\nfruits(1) = \"груша\"", "fruits", 2, 1)]
    [InlineData("TYPE Bag\nfruits[] AS STRING\nEND TYPE\nDIM bagValue AS Bag\nPRINT ARRAYLENGTH(bagValue.fruits)", "fruits", 5, 28)]
    [InlineData("TYPE Bag\nfruits[] AS STRING\nEND TYPE\nDIM bagValue AS Bag\nbagValue.fruits = SPLIT(\"яблоко\", \",\")", "fruits", 5, 10)]
    [InlineData("TYPE Bag\nfruits[] AS STRING\nEND TYPE\nDIM bagValue AS Bag\nPRINT bagValue.fruits()", "fruits", 5, 16)]
    public void StrictArrayNamesAndParenthesesHaveAddressedDiagnostics(string source, string name, int line, int column)
    {
        Bind(source, out var diagnostics);
        var diagnostic = Assert.Single(diagnostics, item => item.Code == "MB2003");
        Assert.Contains(name, diagnostic.Message);
        Assert.Contains("[]", diagnostic.Message);
        Assert.Equal(line, diagnostic.Location.Line);
        Assert.Equal(column, diagnostic.Location.Column);
        Assert.Equal(name, source.Substring(diagnostic.Location.Span.Start, diagnostic.Location.Span.Length));
    }

    [Theory]
    [InlineData("DIM fruits = SPLIT(\"яблоко\", \",\")")]
    [InlineData("DIM fruits AS STRING[] = SPLIT(\"яблоко\", \",\")")]
    [InlineData("DIM fruits AS STRING () = SPLIT(\"яблоко\", \",\")")]
    [InlineData("DIM fruits() AS STRING")]
    [InlineData("DIM grid(,) AS INTEGER")]
    [InlineData("DIM fruits(1 TO 2) AS STRING")]
    [InlineData("TYPE Bag\nfruits() AS STRING\nEND TYPE")]
    [InlineData("TYPE Bag\nfruits AS STRING[]\nEND TYPE")]
    [InlineData("SUB Show(values AS STRING[])\nEND SUB")]
    [InlineData("SUB Show(values AS STRING ())\nEND SUB")]
    [InlineData("SUB Show(values[] AS STRING[])\nEND SUB")]
    [InlineData("FUNCTION Make() AS STRING ()\nRETURN SPLIT(\"яблоко\", \",\")\nEND FUNCTION")]
    [InlineData("DIM maker AS FUNCTION(STRING ()) AS INTEGER")]
    [InlineData("DIM maker AS FUNCTION() AS STRING ()")]
    [InlineData("DIM fruits[] AS STRING\nREDIM fruits(1 TO 2)")]
    [InlineData("DIM fruits[] AS STRING\nERASE fruits")]
    public void StrictModeRejectsLegacyAndTypeSuffixDeclarations(string source)
    {
        Bind(source, out var diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "MB2003" && diagnostic.Message.Contains("[]", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("DIM wrong[] AS INTEGER = SPLIT(\"яблоко\", \",\")")]
    [InlineData("DIM wrong[,] = SPLIT(\"яблоко\", \",\")")]
    [InlineData("DIM wrong[] = 42")]
    [InlineData("DIM wrong[1 TO 2] AS STRING = SPLIT(\"яблоко,банан\", \",\")")]
    public void InitializerTypeRankAndExplicitBoundsRemainChecked(string source)
    {
        Bind(source, out var diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "MB2003");
    }

    [Theory]
    [InlineData("grid[1] = row[]")]
    [InlineData("grid[1 TO 2, 1] = row[]")]
    [InlineData("grid[1][] = row[]")]
    public void AssignmentRequiresFullIndicesRatherThanViews(string assignment)
    {
        Bind("DIM grid[1 TO 2, 1 TO 2] AS INTEGER\nDIM row[] AS INTEGER\n" + assignment, out var diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "MB2003" && diagnostic.Message.Contains("полных индексов", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("PRINT grid[1, 1, 1]", "MB2004")]
    [InlineData("PRINT grid[\"ошибка\"]", "MB2003")]
    [InlineData("Refill(grid[1])\nSUB Refill(BYREF values[] AS INTEGER)\nEND SUB", "MB2028")]
    public void InvalidIndicesAndByRefViewsRemainRejected(string statement, string expectedCode)
    {
        Bind("DIM grid[1 TO 2, 1 TO 2] AS INTEGER\n" + statement, out var diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == expectedCode);
    }

    [Fact]
    public void AnonymousFunctionTypesAllowSquareArrayArgumentsAndResults()
    {
        var program = Bind("DIM callback AS FUNCTION(INTEGER[,]) AS STRING[]", out var diagnostics);
        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        var callable = Assert.IsType<FunctionTypeSymbol>(declaration.Variable.Type);
        AssertArray(Assert.Single(callable.ParameterTypes), TypeSymbol.Integer, 2);
        AssertArray(callable.ReturnType, TypeSymbol.String, 1);
    }

    [Fact]
    public void SquareArrayTargetsDoNotRelaxNestedIndexExpressions()
    {
        Bind("DIM fruits[] AS STRING\nDIM indices[] AS INTEGER\nPRINT fruits[indices]", out var diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "MB2003" && diagnostic.Message.Contains("indices", StringComparison.Ordinal));
    }

    [Fact]
    public void ScalarSquareAccessReportsArrayDiagnosticRatherThanThrowing()
    {
        Bind("DIM fruit AS STRING = \"яблоко\"\nPRINT fruit[1]", out var diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "MB2011");
    }

    [Fact]
    public void RedimUsesItsOwnBoundsAsArrayContext()
    {
        Bind("TYPE Bag\nfruits[] AS STRING\nEND TYPE\nDIM bags[1 TO 1] AS Bag\nDIM fruits[] AS STRING\nREDIM fruits[1 TO 2]\nREDIM PRESERVE bags[1].fruits[-2 TO 3]\nERASE fruits[]\nERASE bags[1].fruits[]", out var diagnostics);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void MixedModulesKeepIndependentSyntaxPolicies()
    {
        var main = Parse("IMPORT shop\nDIM basket[] = fruits[]\nPRINT basket[1]\nPRINT LegacyLength()", "main.axm");
        var legacy = Parser.ParseLegacySyntax("PUBLIC DIM fruits AS STRING () = SPLIT(\"яблоко\", \",\")\nPUBLIC FUNCTION LegacyLength() AS INTEGER\nRETURN ARRAYLENGTH(fruits)\nEND FUNCTION", out var parsing, "shop.axm");
        Assert.Empty(parsing);
        Assert.False(legacy.UsesSquareArraySyntax);
        Binder.BindModules([new("main", main), new("shop", legacy)], "main", out var diagnostics, checkGraphicsModes: false);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void LegacyEntryKeepsItsPolicyWhenImportIsStrict()
    {
        var main = Parser.ParseLegacySyntax("IMPORT shop\nPRINT ARRAYLENGTH(fruits)\nPRINT fruits(1)", out var parsing, "main.axm");
        Assert.Empty(parsing);
        var shop = Parse("PUBLIC DIM fruits[] AS STRING = SPLIT(\"яблоко\", \",\")", "shop.axm");
        Binder.BindModules([new("main", main), new("shop", shop)], "main", out var diagnostics, checkGraphicsModes: false);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void StrictImportedModuleDoesNotInheritLegacyEntryPolicy()
    {
        var main = Parser.ParseLegacySyntax("IMPORT shop\nPRINT ARRAYLENGTH(fruits)", out var parsing, "main.axm");
        Assert.Empty(parsing);
        var shop = Parse("PUBLIC DIM fruits[] AS STRING\nPUBLIC FUNCTION Wrong() AS INTEGER\nRETURN ARRAYLENGTH(fruits)\nEND FUNCTION", "shop.axm");
        Binder.BindModules([new("main", main), new("shop", shop)], "main", out var diagnostics, checkGraphicsModes: false);
        var diagnostic = Assert.Single(diagnostics, item => item.Code == "MB2003");
        Assert.Equal("shop.axm", diagnostic.Location.Document);
    }

    [Fact]
    public void EditorOccurrencesKeepArrayTargetsOnWholeAccessReadsAndWrites()
    {
        const string source = "TYPE Bag\nfruits[] AS STRING\nEND TYPE\nDIM bagValue AS Bag\nDIM fruits[] AS STRING\nbagValue.fruits[] = fruits[]\nfruits[1] = bagValue.fruits[1]\nPRINT ARRAYLENGTH(fruits[])";
        var syntax = Parse(source, "main.axm");
        var program = Binder.BindModules([new("main", syntax)], "main", out var diagnostics, checkGraphicsModes: false, collectEditorSymbols: true);
        Assert.Empty(diagnostics);
        var variable = Assert.Single(program.EditorSymbols, occurrence => occurrence.IsDeclaration && occurrence.Symbol is VariableSymbol { Name: "fruits" });
        var field = Assert.Single(program.EditorSymbols, occurrence => occurrence.IsDeclaration && occurrence.Symbol is TypeFieldSymbol { Name: "fruits" });
        Assert.Equal(3, program.EditorSymbols.Count(occurrence => !occurrence.IsDeclaration && ReferenceEquals(occurrence.Symbol, variable.Symbol)));
        Assert.Equal(2, program.EditorSymbols.Count(occurrence => !occurrence.IsDeclaration && ReferenceEquals(occurrence.Symbol, field.Symbol)));
        Assert.All(program.EditorSymbols.Where(occurrence => occurrence.Symbol.Name == "fruits"), occurrence =>
            Assert.Equal("fruits", source.Substring(occurrence.Location.Span.Start, occurrence.Location.Span.Length)));
    }

    [Fact]
    public void LegacyModeRetainsBareArraysParenthesesAndTypeSuffixes()
    {
        const string source = "DIM fruits AS STRING () = SPLIT(\"яблоко,банан\", \",\")\nDIM basket() = fruits\nbasket(2) = \"груша\"\nShow(fruits())\nSUB Show(values AS STRING ())\nPRINT values(2)\nEND SUB";
        var syntax = Parser.ParseLegacySyntax(source, out var parsing);
        Assert.Empty(parsing);
        Assert.False(syntax.UsesSquareArraySyntax);
        Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("CONST values[] AS STRING = SPLIT(\"яблоко,банан\", \",\")", 1)]
    [InlineData("DIM source[1 TO 2, 1 TO 2] AS STRING\nCONST values[,] AS STRING = source[]", 2)]
    public void NamedArrayConstantsKeepRankAndReadOnlyBinding(string source, int rank)
    {
        var syntax = Parse(source);
        var declarationSyntax = Assert.IsType<ConstStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(syntax.Members[^1]).Statement).Declaration;
        Assert.Equal(rank, declarationSyntax.ArrayBounds!.Rank);
        Assert.True(declarationSyntax.ArrayBounds.IsDynamic);
        var program = Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);
        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[^1]);
        AssertArray(declaration.Variable.Type, TypeSymbol.String, rank);
        Assert.True(declaration.Variable.IsReadOnly);
    }

    [Theory]
    [InlineData("CONST values AS STRING[] = SPLIT(\"яблоко\", \",\")")]
    [InlineData("CONST values AS STRING () = SPLIT(\"яблоко\", \",\")")]
    [InlineData("CONST values[,] AS STRING = SPLIT(\"яблоко\", \",\")")]
    [InlineData("CONST values[] AS STRING = 42")]
    public void ArrayConstantsRejectTypeSuffixOrIncompatibleInitializer(string source)
    {
        Bind(source, out var diagnostics);
        Assert.Single(diagnostics, diagnostic => diagnostic.Code == "MB2003");
    }

    [Theory]
    [InlineData("values[] = SPLIT(\"груша\", \",\")")]
    [InlineData("values[1] = \"груша\"")]
    [InlineData("Refill(values[])\nSUB Refill(BYREF argument[] AS STRING)\nEND SUB")]
    public void ArrayConstantsRetainReadOnlyTargetRestrictions(string statement)
    {
        Bind("CONST values[] AS STRING = SPLIT(\"яблоко\", \",\")\n" + statement, out var diagnostics);
        Assert.Equal("MB2010", Assert.Single(diagnostics).Code);
    }

    [Fact]
    public void ArrayConstantsRejectExplicitBoundsDuringParsing()
    {
        Parser.ParseSquareBracketSyntax("CONST values[1 TO 2] AS STRING = SPLIT(\"яблоко\", \",\")", out var diagnostics);
        Assert.NotEmpty(diagnostics);
    }

    [Fact]
    public void LegacyArrayConstantsKeepTypeSuffix()
    {
        var syntax = Parser.ParseLegacySyntax("CONST values AS STRING () = SPLIT(\"яблоко\", \",\")", out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);
        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(Assert.Single(program.GlobalStatements));
        AssertArray(declaration.Variable.Type, TypeSymbol.String, 1);
        Assert.True(declaration.Variable.IsReadOnly);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FunctionResultCannotBeAssignmentTarget(bool square)
    {
        var source = square
            ? "DIM values[] AS STRING = SPLIT(\"яблоко\", \",\")\nMake() = values[]\nFUNCTION Make() AS STRING[]\nRETURN values[]\nEND FUNCTION"
            : "DIM values AS STRING () = SPLIT(\"яблоко\", \",\")\nMake() = values\nFUNCTION Make() AS STRING ()\nRETURN values\nEND FUNCTION";
        var syntax = square ? Parse(source) : Parser.ParseLegacySyntax(source, out _);
        Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MB2003", diagnostic.Code);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(1, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidAssignmentDoesNotDuplicateExistingTargetDiagnostics(bool square)
    {
        const string source = "missing() = 1";
        var syntax = square ? Parse(source) : Parser.ParseLegacySyntax(source, out _);
        Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);
        Assert.Equal("MB2001", Assert.Single(diagnostics).Code);
    }

    private static ProgramSyntax Parse(string source, string? document = null)
    {
        var syntax = Parser.ParseSquareBracketSyntax(source, out var parsing, document);
        Assert.Empty(parsing);
        Assert.True(syntax.UsesSquareArraySyntax);
        return syntax;
    }

    private static BoundProgram Bind(string source, out DiagnosticBag diagnostics) =>
        Binder.Bind(Parse(source), out diagnostics, checkGraphicsModes: false);

    private static BoundExpression PrintExpression(BoundProgram program, int index) =>
        Assert.Single(Assert.IsType<BoundPrintStatement>(program.GlobalStatements[index]).Items).Expression;

    private static void AssertArray(TypeSymbol type, TypeSymbol elementType, int rank)
    {
        var array = Assert.IsType<ArrayTypeSymbol>(type);
        Assert.Equal(elementType, array.ElementType);
        Assert.Equal(rank, array.Rank);
    }
}
