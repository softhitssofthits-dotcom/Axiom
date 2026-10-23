using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

// Координация модульного связывания: таблица модулей, порядок проходов и общие
// помощники. Публичные модели — ModuleModels.cs, граф импортов, порядок и циклы —
// Binder.Modules.Graph.cs, импорт, экспорт и приватность — Binder.Modules.Exports.cs.
public sealed partial class Binder
{
    /// <summary>
    /// Имя синтетического модуля для безымянного однофайлового вызова API.
    /// </summary>
    public const string SingleModuleName = "<single>";
    private readonly IReadOnlyList<ModuleSymbol> _imports;
    private readonly TextLocation _moduleLocation;
    private bool _usesSquareArraySyntax;
    private readonly Dictionary<string, UserDefinedTypeSymbol> _importedTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _injectedFunctionNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _injectedVariableNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<VariableSymbol, bool> _globalVariableVisibility = [];

    private Binder(IReadOnlyList<ModuleSymbol> imports, TextLocation moduleLocation)
        : this()
    {
        _imports = imports;
        _moduleLocation = moduleLocation;
        InjectImportedSymbols();
    }

    public static BoundProgram BindModules(
        IReadOnlyList<ModuleSyntax> modules,
        string entryModuleName,
        out DiagnosticBag diagnostics,
        bool checkGraphicsModes = true,
        InitialGraphicsContext initialGraphicsContext = InitialGraphicsContext.None,
        bool collectEditorSymbols = false)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryModuleName);

        var result = new DiagnosticBag();
        var modulesByName = new Dictionary<string, ModuleSyntax>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in modules)
        {
            if (!modulesByName.TryAdd(module.Name, module))
            {
                result.Report(
                    DiagnosticCodes.DuplicateModule,
                    $"Модуль '{module.Name}' объявлен несколькими файлами. Имя файла без расширения должно быть уникальным.",
                    GetModuleLocation(module));
            }
        }

        if (!modulesByName.TryGetValue(entryModuleName, out var entryModule))
        {
            result.Report(
                DiagnosticCodes.MissingEntryModule,
                $"Не найден модуль точки входа '{entryModuleName}'.",
                new TextLocation(new TextSpan(0, 0), 1, 1));
            diagnostics = result;
            return new BoundProgram(new TextLocation(new TextSpan(0, 0), 1, 1), [], []);
        }

        var importGraph = BuildImportGraph(modulesByName, result);
        var order = OrderModules(entryModule, modulesByName, importGraph, result, out var reachableModules);

        var allGlobalStatements = new List<BoundStatement>();
        var allRoutines = new List<BoundRoutine>();
        var boundModules = new Dictionary<string, ModuleSymbol>(StringComparer.OrdinalIgnoreCase);
        var editorSymbols = collectEditorSymbols ? new List<EditorSymbolOccurrence>() : null;

        foreach (var module in order)
        {
            // Циклическая зависимость уже отмечена диагностикой: ребро,
            // ведущее назад по цепочке, не участвует в порядке инициализации.
            var imports = importGraph[module.Name]
                .Where(dependency => boundModules.ContainsKey(dependency.Name))
                .Select(dependency => boundModules[dependency.Name])
                .ToArray();
            var binder = new Binder(imports, GetModuleLocation(module));
            if (collectEditorSymbols) { binder._editorStatements = []; binder._editorReferences = []; binder._editorGlobalConstantReferences = []; }

            binder.DeclareModule(module.Syntax);
            binder.BindModuleGlobals(module.Syntax);
            binder.BindModuleBodies(module.Syntax);

            foreach (var routine in binder._routines)
            {
                routine.Symbol.ContainingModule = module.Name;
            }
            foreach (var type in binder._declaredTypes.Values)
            {
                type.ContainingModule = module.Name;
            }
            foreach (var variable in binder._globalVariableVisibility.Keys)
            {
                variable.ContainingModule = module.Name;
            }

            result.AddRange(binder.Diagnostics);
            if (reachableModules.Contains(module.Name))
            {
                allGlobalStatements.AddRange(binder._globalStatements);
                allRoutines.AddRange(binder._routines);
                editorSymbols?.AddRange(binder.CollectEditorSymbols());
            }

            boundModules[module.Name] = binder.CreateModuleSymbol(module);
        }

        var boundProgram = new BoundProgram(GetModuleLocation(entryModule), allGlobalStatements, allRoutines, boundModules, editorSymbols);

        if (checkGraphicsModes)
        {
            var graphicsBinder = new Binder();
            graphicsBinder.CheckGraphicsModes(boundProgram, initialGraphicsContext);
            result.AddRange(graphicsBinder.Diagnostics);
        }

        diagnostics = result;
        return boundProgram;
    }

    private void DeclareModule(ProgramSyntax program)
    {
        _usesSquareArraySyntax = program.UsesSquareArraySyntax;
        DeclareTypeNames(program.Members);
        PrepareTypeDeclarations(program.Members);
        DeclareFunctions(program.Members);
        DeclareTypeMethods(program.Members);
    }

    /// <summary>
    /// Проход глобальных операторов модуля. Модульный путь регистрирует
    /// доступность глобальных переменных для таблицы экспорта, однофайловый
    /// путь не вычисляет экспорт и оставляет регистрацию выключенной.
    /// </summary>
    private void BindModuleGlobals(ProgramSyntax program, bool registerVariableVisibility = true)
    {
        foreach (var member in program.Members)
        {
            switch (member)
            {
                case GlobalStatementSyntax globalStatement:
                {
                    var bound = BindStatement(globalStatement.Statement);
                    _globalStatements.Add(bound);
                    if (registerVariableVisibility) RegisterGlobalVariableVisibility(bound, isPublic: false);
                    break;
                }

                case ModuleStatementSyntax moduleStatement:
                {
                    var bound = BindStatement(moduleStatement.Statement);
                    _globalStatements.Add(bound);
                    if (registerVariableVisibility) RegisterGlobalVariableVisibility(bound, moduleStatement.IsPublic);
                    break;
                }
            }
        }
    }

    private void BindModuleBodies(ProgramSyntax program)
    {
        foreach (var member in program.Members)
        {
            switch (member)
            {
                case TypeDeclarationSyntax typeDeclaration:
                    BindTypeMethods(typeDeclaration);
                    break;

                case SubDeclarationSyntax subDeclaration:
                    _routines.Add(BindRoutine(subDeclaration, _declaredFunctions[subDeclaration]));
                    break;

                case FunctionDeclarationSyntax functionDeclaration:
                    _routines.Add(BindRoutine(functionDeclaration, _declaredFunctions[functionDeclaration]));
                    break;
            }
        }
    }

    private static TextLocation GetModuleLocation(ModuleSyntax module)
    {
        foreach (var member in module.Syntax.Members)
        {
            return member switch
            {
                ImportStatementSyntax import => import.ImportKeyword.Location,
                TypeDeclarationSyntax type => type.TypeKeyword.Location,
                SubDeclarationSyntax sub => sub.SubKeyword.Location,
                FunctionDeclarationSyntax function => function.FunctionKeyword.Location,
                ModuleStatementSyntax modified => modified.VisibilityKeyword.Location,
                GlobalStatementSyntax global => GetStatementLocation(global.Statement),
                _ => module.Syntax.EndOfFileToken.Location
            };
        }

        return module.Syntax.EndOfFileToken.Location;
    }

    private static TextLocation GetStatementLocation(StatementSyntax statement)
    {
        return statement switch
        {
            DimStatementSyntax dim => dim.Declarations.Count > 0
                ? dim.Declarations[0].Declarators[0].Identifier.Location
                : dim.DimKeyword.Location,
            ConstStatementSyntax constant => constant.Declaration.Identifier.Location,
            _ => new TextLocation(new TextSpan(0, 0), 1, 1)
        };
    }

    private void Report(string code, string message, TextLocation location)
    {
        _diagnostics.Report(code, message, location);
    }
}
