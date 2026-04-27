using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundRoutine BindRoutine(MemberSyntax declaration, FunctionSymbol symbol)
    {
        return BindRoutine(declaration, symbol, _globalScope, _globalScope);
    }

    private BoundRoutine BindRoutine(
        MemberSyntax declaration,
        FunctionSymbol symbol,
        BoundScope functionParentScope,
        BoundScope variableParentScope)
    {
        var parentScope = _scope;
        var parentFunction = _currentFunction;

        _scope = new BoundScope(parentScope, variableParentScope, functionParentScope);
        _currentFunction = symbol;

        foreach (var parameter in symbol.Parameters)
        {
            if (!TryDeclareScopedVariable(
                parameter,
                GetParameterLocation(declaration, parameter.Ordinal),
                DeclarationContexts.RoutineParameters))
            {
                continue;
            }
        }

        var routineStatements = GetRoutineStatements(declaration);
        DeclareLocalFunctions(routineStatements);
        var statements = BindRoutineStatements(routineStatements);

        _scope = parentScope;
        _currentFunction = parentFunction;

        return new BoundRoutine(GetLocation(declaration), symbol, statements);
    }

    private void DeclareLocalFunctions(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case LocalSubDeclarationStatementSyntax localSub:
                    DeclareScopedFunction(localSub.Declaration, TypeSymbol.Void);
                    break;

                case LocalFunctionDeclarationStatementSyntax localFunction:
                    DeclareScopedFunction(localFunction.Declaration, ResolveFunctionReturnType(localFunction.Declaration));
                    break;
            }
        }
    }

    private IReadOnlyList<BoundStatement> BindRoutineStatements(IReadOnlyList<StatementSyntax> statements)
    {
        var result = new List<BoundStatement>(statements.Count);

        foreach (var statement in statements)
        {
            switch (statement)
            {
                case LocalSubDeclarationStatementSyntax localSub:
                    _routines.Add(BindRoutine(localSub.Declaration, _declaredFunctions[localSub.Declaration], _scope, _scope));
                    continue;

                case LocalFunctionDeclarationStatementSyntax localFunction:
                    _routines.Add(BindRoutine(localFunction.Declaration, _declaredFunctions[localFunction.Declaration], _scope, _scope));
                    continue;
            }

            result.Add(BindStatement(statement));
        }

        return result;
    }

    private void DeclareScopedFunction(MemberSyntax declaration, TypeSymbol returnType)
    {
        var symbol = declaration switch
        {
            SubDeclarationSyntax subDeclaration => CreateFunctionSymbol(subDeclaration.Identifier.Text, subDeclaration.Parameters, returnType, _currentFunction),
            FunctionDeclarationSyntax functionDeclaration => CreateFunctionSymbol(functionDeclaration.Identifier.Text, functionDeclaration.Parameters, returnType, _currentFunction),
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
        };

        _declaredFunctions[declaration] = symbol;

        var location = declaration switch
        {
            SubDeclarationSyntax subDeclaration => subDeclaration.Identifier.Location,
            FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Identifier.Location,
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
        };

        if (_scope.TryLookupFunctionInCurrentScope(symbol.Name, out _))
        {
            ReportDuplicateDeclaration(symbol.Name, location);
            return;
        }

        if (_scope.TryLookupFunctionInParents(symbol.Name, out _))
        {
            ReportShadowedDeclaration(symbol.Name, location, "теле процедуры или функции");
        }

        _scope.TryDeclareFunction(symbol);
    }

    private static IReadOnlyList<StatementSyntax> GetRoutineStatements(MemberSyntax declaration)
    {
        return declaration switch
        {
            SubDeclarationSyntax subDeclaration => subDeclaration.Statements,
            FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Statements,
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
        };
    }
}
