using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private const string UnknownNameCode = "MB2001";
    private const string DuplicateDeclarationCode = "MB2002";
    private const string TypeMismatchCode = "MB2003";
    private const string WrongArgumentCountCode = "MB2004";
    private const string UnknownCallableCode = "MB2005";
    private const string ReturnValueInSubCode = "MB2006";
    private const string MissingReturnValueCode = "MB2007";
    private const string InvalidConditionCode = "MB2008";
    private const string InvalidForControlCode = "MB2009";
    private const string AssignToConstCode = "MB2010";
    private const string InvalidArrayAccessCode = "MB2011";
    private const string InvalidReturnContextCode = "MB2012";
    private const string InvalidExitForContextCode = "MB2013";
    private const string InvalidExitDoContextCode = "MB2014";
    private const string InvalidSelectCaseRangeCode = "MB2015";
    private const string ShadowedDeclarationCode = "MB2016";
    private const string MissingVariableTypeCode = "MB2017";
    private const string ConstantOutOfRangeCode = "MB2018";
    private const string ProcedureValueCode = "MB2019";
    private const string ValueNotCallableCode = "MB2020";
    private const string UnknownTypeCode = "MB2021";
    private const string InvalidTypeMemberCode = "MB2022";
    private const string UnknownFieldCode = "MB2023";
    private const string InvalidFieldAccessCode = "MB2024";
    private const string FixedTypeArrayFieldOperationCode = "MB2025";
    private const string InvalidFixedStringDeclarationCode = "MB2026";
    private const string InvalidTypeFieldDefaultValueCode = "MB2027";
    private const string InvalidByRefArgumentCode = "MB2028";
    private const string UnsupportedByRefCallableValueCode = "MB2029";

    // Human-facing block labels stay centralized so every future block scope
    // can reuse the same MB2016 template without inventing a new diagnostic.
    private static class DeclarationContexts
    {
        public const string RoutineParameters = "параметрах процедуры или функции";
        public const string IfBlock = "блоке IF";
        public const string ElseIfBranch = "ветви ELSEIF";
        public const string ElseBranch = "ветви ELSE";
        public const string WhileBlock = "блоке WHILE";
        public const string DoBlock = "блоке DO";
        public const string ForLoop = "цикле FOR";
        public const string CaseBranch = "ветви CASE";
        public const string CaseElseBranch = "ветви CASE ELSE";
    }

    private readonly DiagnosticBag _diagnostics = new();
    private readonly Dictionary<MemberSyntax, FunctionSymbol> _declaredFunctions = [];
    private readonly Dictionary<string, UserDefinedTypeSymbol> _declaredTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConstStatementSyntax> _globalIntegralConstantDeclarations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _globalIntegralConstants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<VariableSymbol, long> _boundIntegralConstants = [];
    private readonly List<BoundStatement> _globalStatements = [];
    private readonly List<BoundRoutine> _routines = [];
    private readonly Stack<string?> _declarationContexts = [];
    private readonly BoundScope _globalScope;
    private BoundScope _scope;
    private FunctionSymbol? _currentFunction;
    private int _forDepth;
    private int _doLoopDepth;

    private Binder()
    {
        _globalScope = new BoundScope(enclosingScope: null);
        _scope = _globalScope;

        foreach (var builtIn in BuiltinFunctions.All)
        {
            _globalScope.TryDeclareFunction(builtIn);
        }
    }

    public DiagnosticBag Diagnostics => _diagnostics;

    public static BoundProgram Bind(ProgramSyntax program, out DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(program);

        var binder = new Binder();
        var boundProgram = binder.BindProgram(program);
        diagnostics = binder.Diagnostics;
        return boundProgram;
    }

    private BoundProgram BindProgram(ProgramSyntax program)
    {
        DeclareTypeNames(program.Members);
        PrepareTypeDeclarations(program.Members);
        DeclareFunctions(program.Members);

        foreach (var member in program.Members)
        {
            switch (member)
            {
                case TypeDeclarationSyntax:
                    break;

                case GlobalStatementSyntax globalStatement:
                    _globalStatements.Add(BindStatement(globalStatement.Statement));
                    break;

                case SubDeclarationSyntax subDeclaration:
                    _routines.Add(BindRoutine(subDeclaration, _declaredFunctions[subDeclaration]));
                    break;

                case FunctionDeclarationSyntax functionDeclaration:
                    _routines.Add(BindRoutine(functionDeclaration, _declaredFunctions[functionDeclaration]));
                    break;
            }
        }

        return new BoundProgram(GetProgramLocation(program), _globalStatements, _routines);
    }
}

