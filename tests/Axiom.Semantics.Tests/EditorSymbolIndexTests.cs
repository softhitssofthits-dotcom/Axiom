using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class EditorSymbolIndexTests
{
    [Fact]
    public void IndexUsesSharedSymbolsAcrossModulesAndKeepsRoutineParametersSeparate()
    {
        var main = Parser.Parse("IMPORT helper\nDIM item AS Box\nPRINT total + Echo(1)\nFUNCTION Local(value AS INTEGER) AS INTEGER\nRETURN value\nEND FUNCTION", out _, "main.axm");
        var helper = Parser.Parse("PUBLIC DIM total AS INTEGER = 2\nPUBLIC TYPE Box\nvalue AS INTEGER\nEND TYPE\nPUBLIC FUNCTION Echo(value AS INTEGER) AS INTEGER\nRETURN value\nEND FUNCTION", out _, "helper.axm");
        var program = Binder.BindModules([new("main", main), new("helper", helper)], "main", out var errors,
            checkGraphicsModes: false, collectEditorSymbols: true);
        Assert.DoesNotContain(errors, d => d.Severity == Axiom.Core.DiagnosticSeverity.Error);
        var declarations = program.EditorSymbols.Where(s => s.IsDeclaration).ToArray();
        var total = Assert.Single(declarations, s => s.Symbol.Name == "total" && s.Location.Document == "helper.axm");
        Assert.Contains(program.EditorSymbols, s => !s.IsDeclaration && ReferenceEquals(s.Symbol, total.Symbol) && s.Location.Document == "main.axm");
        var parameter = Assert.Single(declarations, s => s.Symbol.Name == "value" && s.Location.Document == "main.axm");
        Assert.NotSame(total.Symbol, parameter.Symbol);
        var type = Assert.Single(declarations, s => s.Symbol.Name == "Box");
        Assert.Contains(program.EditorSymbols, s => !s.IsDeclaration && ReferenceEquals(s.Symbol, type.Symbol) && s.Location.Document == "main.axm");
        Assert.Contains(declarations, s => s.Symbol.Name == "Echo");
    }

    [Fact]
    public void NormalBindingDoesNotAllocateAnEditorIndex()
    {
        var tree=Parser.Parse("DIM value AS INTEGER\nPRINT value",out _,"main.axm");
        var program=Binder.BindModules([new("main",tree)],"main",out _,checkGraphicsModes:false);
        Assert.Empty(program.EditorSymbols);
    }

    [Fact]
    public void FieldsConstantsAndLoopCountersUseTheirDeclarations()
    {
        const string source="TYPE Box\nvalue AS INTEGER\nEND TYPE\nCONST amount AS INTEGER = 2\nDIM boxValue AS Box\nboxValue.value = amount\nFOR counter = 1 TO 2\nPRINT counter\nNEXT counter";
        var tree=Parser.Parse(source,out _,"main.axm");
        var program=Binder.BindModules([new("main",tree)],"main",out var errors,checkGraphicsModes:false,collectEditorSymbols:true);
        Assert.DoesNotContain(errors,d=>d.Severity==Axiom.Core.DiagnosticSeverity.Error);
        foreach(var name in new[]{"value","amount","counter"})
        {
            var declaration=Assert.Single(program.EditorSymbols,s=>s.Symbol.Name==name&&s.IsDeclaration);
            var expected = name switch
            {
                "value" => new[] { (6,10) },
                "amount" => new[] { (6,18) },
                _ => new[] { (8,7), (9,6) }
            };
            var uses=program.EditorSymbols.Where(s=>!s.IsDeclaration&&ReferenceEquals(s.Symbol,declaration.Symbol))
                .OrderBy(s=>s.Location.Line).ThenBy(s=>s.Location.Column).ToArray();
            Assert.Equal(expected,uses.Select(s=>(s.Location.Line,s.Location.Column)));
            foreach(var use in uses)
            {
                Assert.Equal("main.axm",use.Location.Document);
                Assert.Equal(name,source.Substring(use.Location.Span.Start,use.Location.Span.Length));
            }
        }
    }

    [Fact]
    public void RecordConstructionIndexesTypeAndNamedField()
    {
        var tree=Parser.Parse("TYPE Box\nvalue AS INTEGER\nEND TYPE\nDIM item AS Box = Box(value := 1)",out _,"main.axm");
        var program=Binder.BindModules([new("main",tree)],"main",out var errors,checkGraphicsModes:false,collectEditorSymbols:true);
        Assert.DoesNotContain(errors,d=>d.Severity==Axiom.Core.DiagnosticSeverity.Error);
        var type=Assert.Single(program.EditorSymbols,s=>s.IsDeclaration&&s.Symbol.Name=="Box");
        Assert.Equal(2,program.EditorSymbols.Count(s=>!s.IsDeclaration&&ReferenceEquals(s.Symbol,type.Symbol)));
        var field=Assert.Single(program.EditorSymbols,s=>s.IsDeclaration&&s.Symbol.Name=="value");
        Assert.Single(program.EditorSymbols,s=>!s.IsDeclaration&&ReferenceEquals(s.Symbol,field.Symbol));
    }

    [Theory]
    [InlineData("other", false)]
    [InlineData("COUNTER", true)]
    public void NextCounterIsIndexedOnlyWhenItNamesTheLoopVariable(string nextName, bool expected)
    {
        var source = $"FOR counter = 1 TO 2\nPRINT counter\nNEXT {nextName}";
        var tree = Parser.Parse(source, out _, "main.axm");
        var program = Binder.BindModules([new("main", tree)], "main", out _,
            checkGraphicsModes: false, collectEditorSymbols: true);
        Assert.Equal(expected, program.EditorSymbols.Any(s => s.Location.Span.Start == source.LastIndexOf(nextName, StringComparison.Ordinal)));
    }

    [Fact]
    public void PrivateNamesBuiltinsAndTextDoNotBecomeFalseReferences()
    {
        const string source = "IMPORT helper\nPRINT hidden\nPRINT ABS(-1)\nPRINT \"hidden\"\n' hidden";
        var main = Parser.Parse(source, out _, "main.axm");
        var helper = Parser.Parse("PRIVATE DIM hidden AS INTEGER = 1", out _, "helper.axm");
        var program = Binder.BindModules([new("main", main), new("helper", helper)], "main", out var errors,
            checkGraphicsModes: false, collectEditorSymbols: true);
        Assert.Contains(errors, d => d.Code == "MB2053");
        Assert.Contains(program.EditorSymbols, s => s.IsDeclaration && s.Symbol.Name == "hidden");
        Assert.DoesNotContain(program.EditorSymbols, s => s.Location.Document == "main.axm");
    }

    [Fact]
    public void OverloadedMethodsAndTheirParametersKeepSeparateIdentities()
    {
        const string source = "TYPE Counter\nValue AS INTEGER\nSUB Set(value AS INTEGER)\nSELF.Value = value\nEND SUB\nSUB Set(value AS STRING)\nPRINT value\nEND SUB\nEND TYPE\nDIM item AS Counter\nitem.Set(2)\nitem.Set(\"two\")";
        var tree = Parser.Parse(source, out _, "main.axm");
        var program = Binder.BindModules([new("main", tree)], "main", out var errors,
            checkGraphicsModes: false, collectEditorSymbols: true);
        Assert.DoesNotContain(errors, d => d.Severity == Axiom.Core.DiagnosticSeverity.Error);
        var methods = program.EditorSymbols.Where(s => s.IsDeclaration && s.Symbol is FunctionSymbol).ToArray();
        Assert.Equal(2, methods.Length);
        Assert.NotSame(methods[0].Symbol, methods[1].Symbol);
        foreach (var method in methods)
            Assert.Single(program.EditorSymbols, s => !s.IsDeclaration && ReferenceEquals(s.Symbol, method.Symbol));
        var parameters = program.EditorSymbols.Where(s => s.IsDeclaration && s.Symbol is ParameterSymbol).ToArray();
        Assert.Equal(2, parameters.Length);
        foreach (var parameter in parameters)
            Assert.Single(program.EditorSymbols, s => !s.IsDeclaration && ReferenceEquals(s.Symbol, parameter.Symbol));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void IoAndCatchIdentifiersUseExactDocumentRanges(string newline)
    {
        const string text = "DIM answer AS STRING\nINPUT \"answer\"; answer\nDIM stream AS FILE\nOPEN \"stream\" FOR OUTPUT AS stream\nCLOSE stream\nTRY\nERROR \"DEMO\"\nCATCH problem\nPRINT problem.Message\nEND TRY";
        var source = text.Replace("\n", newline);
        var syntax = Parser.Parse(source, out _, "main.axm");
        var program = Binder.BindModules([new("main", syntax)], "main", out var errors,
            checkGraphicsModes: false, collectEditorSymbols: true);
        Assert.DoesNotContain(errors, d => d.Severity == Axiom.Core.DiagnosticSeverity.Error);
        foreach (var (name, count) in new[] { ("answer", 2), ("stream", 3), ("problem", 2) })
        {
            var declaration = Assert.Single(program.EditorSymbols, s => s.IsDeclaration && s.Symbol.Name == name);
            var sites = program.EditorSymbols.Where(s => ReferenceEquals(s.Symbol, declaration.Symbol)).ToArray();
            Assert.Equal(count, sites.Length);
            Assert.All(sites, s =>
            {
                Assert.Equal("main.axm", s.Location.Document);
                Assert.Equal(name, source.Substring(s.Location.Span.Start, s.Location.Span.Length));
            });
        }
    }

    [Fact]
    public void ConstantsInFixedSizesKeepTheirReferencesIncludingForwardDeclarations()
    {
        const string source = "TYPE Box\nitems[1 TO width] AS INTEGER\nlabel AS STRING * width\nEND TYPE\nCONST width AS INTEGER = 8\nDIM text AS STRING * width\nSUB Show()\nCONST localWidth AS INTEGER = 3\nDIM localText AS STRING * localWidth\nEND SUB";
        var syntax = Parser.Parse(source, out _, "main.axm");
        var program = Binder.BindModules([new("main", syntax)], "main", out var errors,
            checkGraphicsModes: false, collectEditorSymbols: true);
        Assert.DoesNotContain(errors, d => d.Severity == Axiom.Core.DiagnosticSeverity.Error);
        foreach (var (name, count) in new[] { ("width", 3), ("localWidth", 1) })
        {
            var declaration = Assert.Single(program.EditorSymbols, s => s.IsDeclaration && s.Symbol.Name == name);
            Assert.Equal(count, program.EditorSymbols.Count(s => !s.IsDeclaration && ReferenceEquals(s.Symbol, declaration.Symbol)));
        }
    }

    [Theory]
    [InlineData("TYPE Box\nvalue AS INTEGER\nEND TYPE\nTYPE Box\nvalue AS STRING\nEND TYPE\nDIM item AS Box\nPRINT item.value")]
    [InlineData("TYPE Box\nvalue AS INTEGER\nvalue AS STRING\nEND TYPE\nDIM item AS Box\nPRINT item.value")]
    public void RejectedTypeAndFieldDeclarationsDoNotBecomeDefinitions(string source)
    {
        var syntax = Parser.Parse(source, out _, "main.axm");
        var program = Binder.BindModules([new("main", syntax)], "main", out var errors,
            checkGraphicsModes: false, collectEditorSymbols: true);
        Assert.Contains(errors, d => d.Severity == Axiom.Core.DiagnosticSeverity.Error);
        Assert.Single(program.EditorSymbols, s => s.IsDeclaration && s.Symbol.Name == "Box");
        Assert.Single(program.EditorSymbols, s => s.IsDeclaration && s.Symbol.Name == "value");
    }
}
