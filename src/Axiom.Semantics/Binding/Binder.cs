using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    // Human-facing block labels stay centralized so every future block scope
    // can reuse the same MB2016 template without inventing a new diagnostic.
    private static class DeclarationContexts
    {
        public const string RoutineParameters = "параметрах процедуры или функции";
        public const string IfBlock = "блоке IF";
        public const string ElseIfBranch = "ветви ELSEIF";
        public const string ElseBranch = "ветви ELSE";
        public const string TryBlock = "блоке TRY";
        public const string CatchBlock = "блоке CATCH";
        public const string FinallyBlock = "блоке FINALLY";
        public const string WhileBlock = "блоке WHILE";
        public const string DoBlock = "блоке DO";
        public const string ForLoop = "цикле FOR (счётчик FOR всегда локальный)";
        public const string CaseBranch = "ветви CASE";
        public const string CaseElseBranch = "ветви CASE ELSE";
    }

    private readonly DiagnosticBag _diagnostics = new();
    private readonly Dictionary<MemberSyntax, FunctionSymbol> _declaredFunctions = [];
    private readonly Dictionary<MemberSyntax, UserDefinedTypeSymbol> _declaredMethodOwners = [];
    private readonly Dictionary<string, UserDefinedTypeSymbol> _declaredTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConstStatementSyntax> _globalIntegralConstantDeclarations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _globalIntegralConstants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<VariableSymbol, long> _boundIntegralConstants = [];
    private readonly HashSet<VariableSymbol> _implicitForVariables = [];
    private readonly Dictionary<UserDefinedTypeSymbol, bool> _structuralEqualityCache = [];
    private readonly List<BoundStatement> _globalStatements = [];
    private readonly List<BoundRoutine> _routines = [];
    private readonly Stack<string?> _declarationContexts = [];
    private readonly BoundScope _globalScope;
    private BoundScope _scope;
    private FunctionSymbol? _currentFunction;
    private UserDefinedTypeSymbol? _currentMethodOwner;
    private int _forDepth;
    private int _whileDepth;
    private int _doLoopDepth;

    private Binder()
    {
        _imports = [];
        _moduleLocation = new TextLocation(new TextSpan(0, 0), 1, 1);
        _globalScope = new BoundScope(enclosingScope: null);
        _scope = _globalScope;

        foreach (var builtIn in BuiltinFunctions.All)
        {
            _globalScope.TryDeclareFunction(builtIn);
        }
    }

    public DiagnosticBag Diagnostics => _diagnostics;

    public static BoundProgram Bind(ProgramSyntax program, out DiagnosticBag diagnostics,
        bool checkGraphicsModes = true, InitialGraphicsContext initialGraphicsContext = InitialGraphicsContext.None)
    {
        ArgumentNullException.ThrowIfNull(program);

        if (program.Members.Any(member => member is ImportStatementSyntax or ModuleStatementSyntax))
        {
            const string singleModuleName = "<single>";
            return BindModules([new ModuleSyntax(singleModuleName, program)], singleModuleName, out diagnostics,
                checkGraphicsModes, initialGraphicsContext);
        }

        var binder = new Binder();
        var boundProgram = binder.BindProgram(program);
        if (checkGraphicsModes) binder.CheckGraphicsModes(boundProgram, initialGraphicsContext);
        diagnostics = binder.Diagnostics;
        return boundProgram;
    }

    private BoundProgram BindProgram(ProgramSyntax program)
    {
        DeclareTypeNames(program.Members);
        PrepareTypeDeclarations(program.Members);
        DeclareFunctions(program.Members);
        DeclareTypeMethods(program.Members);

        // Проход A: связываем все глобальные операторы (DIM/CONST и др.) раньше,
        // чем тела процедур, чтобы процедуры и методы TYPE видели глобальные
        // имена независимо от порядка их объявления в исходнике.
        foreach (var member in program.Members)
        {
            if (member is GlobalStatementSyntax globalStatement)
            {
                _globalStatements.Add(BindStatement(globalStatement.Statement));
            }
            else if (member is ModuleStatementSyntax moduleStatement)
            {
                _globalStatements.Add(BindStatement(moduleStatement.Statement));
            }
        }

        // Проход B: связываем тела процедур и методы TYPE.
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

        var boundProgram = new BoundProgram(GetProgramLocation(program), _globalStatements, _routines);
        return boundProgram;
    }
}

