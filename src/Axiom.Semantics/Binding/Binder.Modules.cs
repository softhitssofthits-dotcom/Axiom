using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

/// <summary>
/// Исходная единица модуля: имя модуля и разобранное дерево файла.
/// Имя модуля — имя файла без расширения; сравнение регистронезависимое.
/// </summary>
public sealed class ModuleSyntax
{
    public ModuleSyntax(string name, ProgramSyntax syntax)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Module name must be provided.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(syntax);

        Name = name;
        Syntax = syntax;
    }

    public string Name { get; }

    public ProgramSyntax Syntax { get; }

    public IReadOnlyList<ImportStatementSyntax> Imports =>
        Syntax.Members.OfType<ImportStatementSyntax>().ToArray();
}

/// <summary>
/// Связанный модуль: имя, объявленные символы и их доступность.
/// </summary>
public sealed class ModuleSymbol
{
    public ModuleSymbol(
        string name,
        IReadOnlyDictionary<string, FunctionSymbol> publicFunctions,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> publicTypes,
        IReadOnlyDictionary<string, VariableSymbol> publicVariables,
        IReadOnlySet<string> privateNames,
        IReadOnlyDictionary<string, long>? publicIntegralConstants = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(publicFunctions);
        ArgumentNullException.ThrowIfNull(publicTypes);
        ArgumentNullException.ThrowIfNull(publicVariables);
        ArgumentNullException.ThrowIfNull(privateNames);

        Name = name;
        PublicFunctions = publicFunctions;
        PublicTypes = publicTypes;
        PublicVariables = publicVariables;
        PrivateNames = privateNames;
        PublicIntegralConstants = publicIntegralConstants ?? new Dictionary<string, long>();
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, FunctionSymbol> PublicFunctions { get; }

    public IReadOnlyDictionary<string, UserDefinedTypeSymbol> PublicTypes { get; }

    public IReadOnlyDictionary<string, VariableSymbol> PublicVariables { get; }

    public IReadOnlySet<string> PrivateNames { get; }

    public IReadOnlyDictionary<string, long> PublicIntegralConstants { get; }

    public bool IsPrivateName(string name) => PrivateNames.Contains(name);
}

public sealed partial class Binder
{
    private readonly IReadOnlyList<ModuleSymbol> _imports;
    private readonly TextLocation _moduleLocation;
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

        var importGraph = new Dictionary<string, IReadOnlyList<ModuleSyntax>>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in modulesByName.Values)
        {
            var resolved = new List<ModuleSyntax>();
            foreach (var import in module.Imports)
            {
                if (string.Equals(import.ModuleName, module.Name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Report(
                        DiagnosticCodes.CyclicModuleImport,
                        $"Модуль '{module.Name}' импортирует сам себя.",
                        import.NameToken.Location);
                    continue;
                }

                if (!modulesByName.TryGetValue(import.ModuleName, out var imported))
                {
                    result.Report(
                        DiagnosticCodes.UnknownModule,
                        $"Модуль '{module.Name}' импортирует несуществующий модуль '{import.ModuleName}'.",
                        import.NameToken.Location);
                    continue;
                }

                if (!resolved.Contains(imported))
                {
                    resolved.Add(imported);
                }
            }

            importGraph[module.Name] = resolved;
        }

        var order = new List<ModuleSyntax>();
        var states = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Visit(entryModule);
        var reachableModules = order.Select(module => module.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var module in modulesByName.Values)
        {
            Visit(module);
        }

        void Visit(ModuleSyntax module)
        {
            if (states.TryGetValue(module.Name, out var state))
            {
                if (state == 1)
                {
                    result.Report(
                        DiagnosticCodes.CyclicModuleImport,
                        $"Циклический импорт модулей: '{module.Name}' уже находится в цепочке инициализации.",
                        GetModuleLocation(module));
                }

                return;
            }

            states[module.Name] = 1;
            foreach (var dependency in importGraph[module.Name])
            {
                Visit(dependency);
            }

            states[module.Name] = 2;
            order.Add(module);
        }

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

    private void InjectImportedSymbols()
    {
        foreach (var import in _imports)
        {
            foreach (var (name, function) in import.PublicFunctions)
            {
                if (!_injectedFunctionNames.Add(name))
                {
                    Report(
                        DiagnosticCodes.AmbiguousModuleMember,
                        $"Имя '{name}' экспортируется несколькими импортированными модулями. Уточните модуль или переименуйте объявление.",
                        _moduleLocation);
                    continue;
                }

                if (!_globalScope.TryDeclareFunction(function))
                {
                    Report(
                        DiagnosticCodes.ImportedNameConflict,
                        $"Импортируемое имя '{name}' из модуля '{import.Name}' конфликтует с уже доступным именем.",
                        _moduleLocation);
                }
            }

            foreach (var (name, variable) in import.PublicVariables)
            {
                if (!_injectedVariableNames.Add(name))
                {
                    Report(
                        DiagnosticCodes.AmbiguousModuleMember,
                        $"Имя '{name}' экспортируется несколькими импортированными модулями. Уточните модуль или переименуйте объявление.",
                        _moduleLocation);
                    continue;
                }

                if (!_globalScope.TryDeclareVariable(variable))
                {
                    Report(
                        DiagnosticCodes.ImportedNameConflict,
                        $"Импортируемое имя '{name}' из модуля '{import.Name}' конфликтует с уже доступным именем.",
                        _moduleLocation);
                }
            }

            foreach (var (name, type) in import.PublicTypes)
            {
                if (!_importedTypes.TryAdd(name, type))
                {
                    Report(
                        DiagnosticCodes.AmbiguousModuleMember,
                        $"Тип '{name}' экспортируется несколькими импортированными модулями. Уточните модуль или переименуйте объявление.",
                        _moduleLocation);
                }
            }

            foreach (var (name, value) in import.PublicIntegralConstants)
            {
                _globalIntegralConstants.TryAdd(name, value);
            }
        }
    }

    private void DeclareModule(ProgramSyntax program)
    {
        DeclareTypeNames(program.Members);
        PrepareTypeDeclarations(program.Members);
        DeclareFunctions(program.Members);
        DeclareTypeMethods(program.Members);
    }

    private void BindModuleGlobals(ProgramSyntax program)
    {
        foreach (var member in program.Members)
        {
            switch (member)
            {
                case GlobalStatementSyntax globalStatement:
                {
                    var bound = BindStatement(globalStatement.Statement);
                    _globalStatements.Add(bound);
                    RegisterGlobalVariableVisibility(bound, isPublic: false);
                    break;
                }

                case ModuleStatementSyntax moduleStatement:
                {
                    var bound = BindStatement(moduleStatement.Statement);
                    _globalStatements.Add(bound);
                    RegisterGlobalVariableVisibility(bound, moduleStatement.IsPublic);
                    break;
                }
            }
        }
    }

    private void RegisterGlobalVariableVisibility(BoundStatement statement, bool isPublic)
    {
        switch (statement)
        {
            case BoundVariableDeclarationStatement declaration:
                _globalVariableVisibility[declaration.Variable] = isPublic;
                break;

            case BoundVariableDeclarationsStatement declarations:
                foreach (var declaration in declarations.Declarations)
                {
                    _globalVariableVisibility[declaration.Variable] = isPublic;
                }

                break;
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

    private ModuleSymbol CreateModuleSymbol(ModuleSyntax module)
    {
        var publicFunctions = new Dictionary<string, FunctionSymbol>(StringComparer.OrdinalIgnoreCase);
        var privateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var publicTypes = new Dictionary<string, UserDefinedTypeSymbol>(StringComparer.OrdinalIgnoreCase);
        var publicVariables = new Dictionary<string, VariableSymbol>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in module.Syntax.Members)
        {
            switch (member)
            {
                case SubDeclarationSyntax subDeclaration:
                    RegisterFunction(subDeclaration.VisibilityKeyword, _declaredFunctions[subDeclaration]);
                    break;

                case FunctionDeclarationSyntax functionDeclaration:
                    RegisterFunction(functionDeclaration.VisibilityKeyword, _declaredFunctions[functionDeclaration]);
                    break;

                case TypeDeclarationSyntax typeDeclaration:
                {
                    if (!_declaredTypes.TryGetValue(typeDeclaration.Identifier.Text, out var type))
                    {
                        break;
                    }

                    if (IsPublic(typeDeclaration.VisibilityKeyword))
                    {
                        publicTypes[type.Name] = type;
                    }
                    else
                    {
                        privateNames.Add(type.Name);
                    }

                    break;
                }
            }
        }

        foreach (var (variable, isPublic) in _globalVariableVisibility)
        {
            if (isPublic)
            {
                publicVariables[variable.Name] = variable;
            }
            else
            {
                privateNames.Add(variable.Name);
            }
        }

        var constants = _boundIntegralConstants.Where(pair => publicVariables.ContainsKey(pair.Key.Name))
            .ToDictionary(pair => pair.Key.Name, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        return new ModuleSymbol(module.Name, publicFunctions, publicTypes, publicVariables, privateNames, constants);

        void RegisterFunction(SyntaxToken? visibilityKeyword, FunctionSymbol function)
        {
            if (IsPublic(visibilityKeyword))
            {
                publicFunctions[function.Name] = function;
            }
            else
            {
                privateNames.Add(function.Name);
            }
        }
    }

    private static bool IsPublic(SyntaxToken? visibilityKeyword)
    {
        return visibilityKeyword?.Kind == SyntaxKind.PublicKeyword;
    }

    private bool TryReportPrivateModuleMember(string name, TextLocation location)
    {
        foreach (var import in _imports)
        {
            if (import.IsPrivateName(name))
            {
                Report(
                    DiagnosticCodes.PrivateModuleMember,
                    $"Имя '{name}' объявлено в модуле '{import.Name}' как PRIVATE и недоступно снаружи. Объявите его как PUBLIC.",
                    location);
                return true;
            }
        }

        return false;
    }

    private void Report(string code, string message, TextLocation location)
    {
        _diagnostics.Report(code, message, location);
    }
}
