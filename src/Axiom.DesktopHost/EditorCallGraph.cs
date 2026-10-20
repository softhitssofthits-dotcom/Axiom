using System.IO;
using Axiom.Core;
using Axiom.Execution;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.DesktopHost;

// Только редакторская проекция уже связанного дерева; правила связывания не дублируются.
internal static class EditorCallGraph
{
    internal sealed record Routine(string Id, string Name, string Kind, int Line, int Column,
        int Start, bool IsBuiltin, string Signature, string? SourceFilePath);
    internal sealed record Edge(string Caller, string Callee, int Start, int Length, int Line, int Column, string Kind, string? SourceFilePath);
    internal sealed record IndirectCall(string Caller, int Start, int Length, int Line, int Column, string? SourceFilePath);
    internal sealed record Source(string? SourceFilePath, string SourceText);
    internal sealed record SymbolSite(string SymbolId, string Name, bool IsDeclaration, string? SourceFilePath, int Start, int Length, int Line, int Column);
    internal sealed record Graph(Routine[] Routines, Edge[] Calls, IndirectCall[] IndirectCalls, bool Complete, Source[] SourceDocuments, SymbolSite[] Symbols);

    public static Graph Analyse(string source, string? sourceFilePath = null, IReadOnlyList<SourceDocument>? dependencies = null)
    {
        var syntax = Parser.Parse(source, out var parseDiagnostics, sourceFilePath);
        dependencies ??= [];
        var modules = new List<ModuleSyntax> { new(Path.GetFileNameWithoutExtension(sourceFilePath ?? "main"), syntax) };
        foreach (var document in dependencies)
        {
            var tree = Parser.Parse(document.Text, out var errors, document.Name);
            parseDiagnostics.AddRange(errors);
            modules.Add(new(document.ModuleName, tree));
        }
        var program = Axiom.Semantics.Binder.BindModules(modules, modules[0].Name, out var bindDiagnostics,
            checkGraphicsModes: false, collectEditorSymbols: true);
        var sources = new[] { new Source(sourceFilePath, source) }
            .Concat(dependencies.Select(d => new Source(d.Name, d.Text))).ToArray();
        string MainId(string? document) => document == sourceFilePath ? "main" : "main:" + document;
        var routines = sources.Select(d => new Routine(MainId(d.SourceFilePath), "", "main", 1, 1, 0, false, "", d.SourceFilePath)).ToList();
        var ids = new Dictionary<FunctionSymbol, string>(ReferenceEqualityComparer.Instance);
        foreach (var routine in program.Routines)
        {
            var id = "routine:" + routine.Location.Document + ":" + routine.Location.Span.Start;
            ids[routine.Symbol] = id;
            routines.Add(Describe(id, routine.Symbol, routine.Location.Line, routine.Location.Column, routine.Location.Span.Start, routine.Location.Document));
        }
        var calls = new List<Edge>();
        var indirectCalls = new List<IndirectCall>();
        string? Resolve(FunctionSymbol symbol)
        {
            if (ids.TryGetValue(symbol, out var id)) return id;
            if (!symbol.IsBuiltIn) return null;
            id = "builtin:" + symbol.Name + ":" + string.Join(",", symbol.Parameters.Select(p => p.Type.Name));
            ids[symbol] = id;
            if (!routines.Any(r => r.Id == id)) routines.Add(Describe(id, symbol, 0, 0, 0));
            return id;
        }
        void Add(string caller, BoundFunctionExpression target, string kind)
        {
            var callee = Resolve(target.Function);
            if (callee is null) return;
            var location = target.Location;
            calls.Add(new(caller, callee, location.Span.Start, location.Span.Length,
                location.Line, location.Column, kind, location.Document));
        }
        void Visit(string caller, IEnumerable<BoundStatement> statements)
        {
            // Цель прямого вызова и зарегистрированный обработчик уже описаны
            // отдельным ребром: повторная ссылка на ту же функцию не создаётся.
            var described = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var value in BoundTreeTraversal.Descendants(statements))
            {
                if (value is BoundCallExpression call)
                {
                    if (call.Target is BoundFunctionExpression target) { Add(caller, target, "call"); described.Add(target); }
                    else
                    {
                        var location = call.Location;
                        indirectCalls.Add(new(caller, location.Span.Start, location.Span.Length, location.Line, location.Column, location.Document));
                    }
                }
                // Регистрация обработчика является связью, но не немедленным вызовом.
                if (value is BoundOnKeyStatement { HandlerExpression: not null } key) { Add(caller, key.HandlerExpression, "handler"); described.Add(key.HandlerExpression); }
                if (value is BoundOnMouseStatement { HandlerExpression: not null } mouse) { Add(caller, mouse.HandlerExpression, "handler"); described.Add(mouse.HandlerExpression); }
                if (value is BoundFunctionExpression reference && !described.Contains(reference)) Add(caller, reference, "reference");
            }
        }
        foreach (var group in program.GlobalStatements.GroupBy(s => s.Location.Document)) Visit(MainId(group.Key), group);
        foreach (var routine in program.Routines) Visit(ids[routine.Symbol], routine.Statements);
        var symbolIds = new Dictionary<Symbol, string>(ReferenceEqualityComparer.Instance);
        foreach (var declaration in program.EditorSymbols.Where(s => s.IsDeclaration))
            symbolIds.TryAdd(declaration.Symbol, declaration.Location.Document + ":" + declaration.Location.Span.Start);
        var symbols = program.EditorSymbols.Where(s => symbolIds.ContainsKey(s.Symbol)).Select(s =>
            new SymbolSite(symbolIds[s.Symbol], s.Symbol.Name, s.IsDeclaration, s.Location.Document,
                s.Location.Span.Start, s.Location.Span.Length, s.Location.Line, s.Location.Column)).Distinct().ToArray();
        return new(routines.ToArray(), calls.Distinct().OrderBy(c => c.Start).ToArray(), indirectCalls.Distinct().OrderBy(c=>c.Start).ToArray(),
            indirectCalls.Count == 0 && !parseDiagnostics.Any(d => d.Severity == Axiom.Core.DiagnosticSeverity.Error) &&
            !bindDiagnostics.Any(d => d.Severity == Axiom.Core.DiagnosticSeverity.Error), sources, symbols);
    }

    private static Routine Describe(string id, FunctionSymbol symbol, int line, int column, int start, string? document = null)
    {
        var names = new Stack<string>();
        for (FunctionSymbol? current = symbol; current is not null; current = current.ContainingRoutine) names.Push(current.Name);
        var name = string.Join(".", names);
        var parameters = symbol.VisibleParameters
            .Select(p => (p.IsByRef ? "BYREF " : "") + p.Name + " AS " + p.Type.Name);
        var signature = (symbol.IsSub ? "SUB " : "FUNCTION ") + name + "(" + string.Join(", ", parameters) + ")" +
            (symbol.IsSub ? "" : " AS " + symbol.ReturnType.Name);
        return new(id, name, symbol.IsSub ? "sub" : "function", line, column, start, symbol.IsBuiltIn, signature, document);
    }
}
