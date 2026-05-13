using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundStatement BindIfStatement(IfStatementSyntax statement)
    {
        var condition = BindCondition(statement.Condition);
        var statements = BindStatementsInChildScope(statement.Statements, DeclarationContexts.IfBlock);
        var elseStatements = BindElseBranch(statement.ElseIfClauses, statement.ElseClause);

        return new BoundIfStatement(GetLocation(statement), condition, statements, elseStatements);
    }

    private IReadOnlyList<BoundStatement> BindElseBranch(
        IReadOnlyList<ElseIfClauseSyntax> elseIfClauses,
        ElseClauseSyntax? elseClause)
    {
        if (elseIfClauses.Count > 0)
        {
            return [BindElseIfChain(elseIfClauses, 0, elseClause)];
        }

        return elseClause is null
            ? Array.Empty<BoundStatement>()
            : BindStatementsInChildScope(elseClause.Statements, DeclarationContexts.ElseBranch);
    }

    private BoundIfStatement BindElseIfChain(
        IReadOnlyList<ElseIfClauseSyntax> elseIfClauses,
        int index,
        ElseClauseSyntax? elseClause)
    {
        var clause = elseIfClauses[index];
        var condition = BindCondition(clause.Condition);
        var statements = BindStatementsInChildScope(clause.Statements, DeclarationContexts.ElseIfBranch);
        var elseStatements = index + 1 < elseIfClauses.Count
            ? new BoundStatement[] { BindElseIfChain(elseIfClauses, index + 1, elseClause) }
            : elseClause is null
                ? Array.Empty<BoundStatement>()
                : BindStatementsInChildScope(elseClause.Statements, DeclarationContexts.ElseBranch);

        return new BoundIfStatement(clause.ElseIfKeyword.Location, condition, statements, elseStatements);
    }

    private BoundStatement BindWhileStatement(WhileStatementSyntax statement)
    {
        var condition = BindCondition(statement.Condition);
        var statements = BindStatementsInChildScope(statement.Statements, DeclarationContexts.WhileBlock);
        return new BoundWhileStatement(GetLocation(statement), condition, statements);
    }

    private BoundStatement BindTryStatement(TryStatementSyntax statement)
    {
        var statements = BindStatementsInChildScope(statement.Statements, DeclarationContexts.TryBlock);
        var (catchVariable, catchStatements) = BindCatchClause(statement.CatchClause);
        var finallyStatements = statement.FinallyClause is null
            ? Array.Empty<BoundStatement>()
            : BindStatementsInChildScope(statement.FinallyClause.Statements, DeclarationContexts.FinallyBlock);

        return new BoundTryStatement(GetLocation(statement), statements, catchVariable, catchStatements, finallyStatements);
    }

    private BoundStatement BindErrorStatement(ErrorStatementSyntax statement)
    {
        if (statement.MessageExpression is null)
        {
            var expression = BindExpression(statement.CodeExpression);
            if (expression.Type == TypeSymbol.RuntimeError)
            {
                return new BoundErrorStatement(GetLocation(statement), expression, messageExpression: null, BoundErrorStatementMode.ExistingError);
            }

            if (expression.Type != TypeSymbol.String && expression.Type != TypeSymbol.Error)
            {
                ReportTypeMismatch(expression.Type, TypeSymbol.String, GetLocation(statement.CodeExpression));
                return new BoundErrorStatement(GetLocation(statement), new BoundErrorExpression(GetLocation(statement.CodeExpression)), messageExpression: null, BoundErrorStatementMode.UserCode);
            }

            return new BoundErrorStatement(GetLocation(statement), expression, messageExpression: null, BoundErrorStatementMode.UserCode);
        }

        var codeExpression = BindExpression(statement.CodeExpression, TypeSymbol.String);
        var messageExpression = BindExpression(statement.MessageExpression, TypeSymbol.String);

        return new BoundErrorStatement(GetLocation(statement), codeExpression, messageExpression, BoundErrorStatementMode.UserCode);
    }

    private (VariableSymbol? variable, IReadOnlyList<BoundStatement> statements) BindCatchClause(CatchClauseSyntax? clause)
    {
        if (clause is null)
        {
            return (null, Array.Empty<BoundStatement>());
        }

        EnterScope(DeclarationContexts.CatchBlock);

        try
        {
            var variable = new VariableSymbol(
                clause.Identifier.Text,
                TypeSymbol.RuntimeError,
                isReadOnly: true,
                isGlobal: false);

            TryDeclareScopedVariable(variable, clause.Identifier.Location);
            return (variable, BindStatements(clause.Statements));
        }
        finally
        {
            LeaveScope();
        }
    }

    private BoundStatement BindDoLoopStatement(DoLoopStatementSyntax statement)
    {
        var leadingCondition = BindDoLoopCondition(statement.LeadingCondition);
        var trailingCondition = BindDoLoopCondition(statement.TrailingCondition);
        var condition = leadingCondition.condition ?? trailingCondition.condition;
        var conditionKind = leadingCondition.kind != BoundDoLoopConditionKind.None
            ? leadingCondition.kind
            : trailingCondition.kind;
        var evaluateConditionAfterBody = trailingCondition.kind != BoundDoLoopConditionKind.None;

        _doLoopDepth++;
        IReadOnlyList<BoundStatement> statements;

        try
        {
            statements = BindStatementsInChildScope(statement.Statements, DeclarationContexts.DoBlock);
        }
        finally
        {
            _doLoopDepth--;
        }

        return new BoundDoLoopStatement(GetLocation(statement), condition, conditionKind, evaluateConditionAfterBody, statements);
    }

    private (BoundExpression? condition, BoundDoLoopConditionKind kind) BindDoLoopCondition(DoLoopConditionClauseSyntax? clause)
    {
        if (clause is null)
        {
            return (null, BoundDoLoopConditionKind.None);
        }

        var condition = BindCondition(clause.Condition);
        var kind = clause.Keyword.Kind switch
        {
            SyntaxKind.WhileKeyword => BoundDoLoopConditionKind.While,
            SyntaxKind.UntilKeyword => BoundDoLoopConditionKind.Until,
            _ => throw new ArgumentOutOfRangeException(nameof(clause), clause.Keyword.Kind, "Unsupported DO LOOP condition keyword.")
        };

        return (condition, kind);
    }

    private BoundStatement BindForStatement(ForStatementSyntax statement)
    {
        var startExpression = BindExpression(statement.StartExpression);
        var reusesExistingVariable = _scope.TryLookupVariable(statement.Identifier.Text, out var existingVariable)
            && existingVariable is not null
            && !_implicitForVariables.Contains(existingVariable);

        var provisionalType = reusesExistingVariable
            ? existingVariable!.Type
            : startExpression.Type == TypeSymbol.Double ? TypeSymbol.Double : TypeSymbol.Integer;

        EnterScope(DeclarationContexts.ForLoop);

        try
        {
            if (!reusesExistingVariable)
            {
                var provisionalVariable = new VariableSymbol(
                    statement.Identifier.Text,
                    provisionalType,
                    isReadOnly: false,
                    isGlobal: false);

                _scope.TryDeclareVariable(provisionalVariable);
            }

            var boundEndExpression = BindExpression(statement.EndExpression);
            var boundStepExpression = statement.StepClause is null
                ? null
                : BindExpression(statement.StepClause.Expression);

            var variableType = reusesExistingVariable
                ? existingVariable!.Type
                : InferForControlVariableType(startExpression, boundEndExpression, boundStepExpression);

            if (variableType == TypeSymbol.Error || !IsValidForControlVariableType(variableType))
            {
                ReportInvalidForControlVariable(statement.Identifier.Text, variableType, statement.Identifier.Location);
            }

            if (reusesExistingVariable && existingVariable!.IsReadOnly)
            {
                ReportAssignToConst(existingVariable.Name, statement.Identifier.Location);
            }

            LeaveScope();
            EnterScope(DeclarationContexts.ForLoop);

            VariableSymbol variable;
            var declaresVariable = !reusesExistingVariable;

            if (reusesExistingVariable)
            {
                variable = existingVariable!;
            }
            else
            {
                variable = new VariableSymbol(
                    statement.Identifier.Text,
                    variableType == TypeSymbol.Error ? TypeSymbol.Integer : variableType,
                    isReadOnly: false,
                    isGlobal: false);

                if (TryDeclareScopedVariable(variable, statement.Identifier.Location))
                {
                    _implicitForVariables.Add(variable);
                }
            }

            var expectedType = IsValidForControlVariableType(variable.Type) ? variable.Type : TypeSymbol.Error;
            startExpression = expectedType == TypeSymbol.Error
                ? BindExpression(statement.StartExpression)
                : BindExpression(statement.StartExpression, expectedType);
            var endExpression = expectedType == TypeSymbol.Error
                ? BindExpression(statement.EndExpression)
                : BindExpression(statement.EndExpression, expectedType);
            var stepExpression = statement.StepClause is null
                ? CreateDefaultStepExpression(statement.ForKeyword.Location, expectedType == TypeSymbol.Error ? TypeSymbol.Integer : expectedType)
                : expectedType == TypeSymbol.Error
                    ? BindExpression(statement.StepClause.Expression)
                    : BindExpression(statement.StepClause.Expression, expectedType);
            _forDepth++;
            IReadOnlyList<BoundStatement> statements;

            try
            {
                statements = BindStatements(statement.Statements);
            }
            finally
            {
                _forDepth--;
            }

            return new BoundForStatement(GetLocation(statement), variable, declaresVariable, startExpression, endExpression, stepExpression, statements);
        }
        finally
        {
            LeaveScope();
        }
    }

    private BoundStatement BindExitForStatement(ExitForStatementSyntax statement)
    {
        if (_forDepth <= 0)
        {
            ReportInvalidExitForContext(statement.ExitKeyword.Location);
        }

        return new BoundExitForStatement(GetLocation(statement));
    }

    private BoundStatement BindExitDoStatement(ExitDoStatementSyntax statement)
    {
        if (_doLoopDepth <= 0)
        {
            ReportInvalidExitDoContext(statement.ExitKeyword.Location);
        }

        return new BoundExitDoStatement(GetLocation(statement));
    }

    private BoundStatement BindReturnStatement(ReturnStatementSyntax statement)
    {
        if (_currentFunction is null)
        {
            ReportInvalidReturnContext(statement.ReturnKeyword.Location);
            return new BoundReturnStatement(statement.ReturnKeyword.Location, statement.Expression is null ? null : BindExpression(statement.Expression));
        }

        if (_currentFunction.IsSub)
        {
            if (statement.Expression is not null)
            {
                ReportReturnValueInSub(_currentFunction.Name, statement.ReturnKeyword.Location);
            }

            return new BoundReturnStatement(statement.ReturnKeyword.Location, statement.Expression is null ? null : BindExpression(statement.Expression));
        }

        if (statement.Expression is null)
        {
            ReportMissingReturnValue(_currentFunction.Name, statement.ReturnKeyword.Location);
            return new BoundReturnStatement(statement.ReturnKeyword.Location, null);
        }

        return new BoundReturnStatement(statement.ReturnKeyword.Location, BindExpression(statement.Expression, _currentFunction.ReturnType));
    }
}
