using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private IReadOnlyList<BoundStatement> BindStatements(IReadOnlyList<StatementSyntax> statements)
    {
        var result = new List<BoundStatement>(statements.Count);

        foreach (var statement in statements)
        {
            result.Add(BindStatement(statement));
        }

        return result;
    }

    private BoundStatement BindStatement(StatementSyntax statement)
    {
        return statement switch
        {
            DimStatementSyntax dimStatement => BindDimStatement(dimStatement),
            ConstStatementSyntax constStatement => BindConstStatement(constStatement),
            AssignmentStatementSyntax assignmentStatement => BindAssignmentStatement(assignmentStatement),
            EraseStatementSyntax eraseStatement => BindEraseStatement(eraseStatement),
            RedimStatementSyntax redimStatement => BindRedimStatement(redimStatement),
            PrintStatementSyntax printStatement => BindPrintStatement(printStatement),
            PrintUsingStatementSyntax printUsingStatement => BindPrintUsingStatement(printUsingStatement),
            InputStatementSyntax inputStatement => BindInputStatement(inputStatement),
            OpenStatementSyntax openStatement => BindOpenStatement(openStatement),
            CloseStatementSyntax closeStatement => BindCloseStatement(closeStatement),
            IfStatementSyntax ifStatement => BindIfStatement(ifStatement),
            SelectCaseStatementSyntax selectCaseStatement => BindSelectCaseStatement(selectCaseStatement),
            DoLoopStatementSyntax doLoopStatement => BindDoLoopStatement(doLoopStatement),
            WhileStatementSyntax whileStatement => BindWhileStatement(whileStatement),
            ForStatementSyntax forStatement => BindForStatement(forStatement),
            ExitForStatementSyntax exitForStatement => BindExitForStatement(exitForStatement),
            ExitDoStatementSyntax exitDoStatement => BindExitDoStatement(exitDoStatement),
            ReturnStatementSyntax returnStatement => BindReturnStatement(returnStatement),
            ExpressionStatementSyntax expressionStatement => new BoundExpressionStatement(GetLocation(expressionStatement), BindExpression(expressionStatement.Expression)),
            _ => throw new ArgumentOutOfRangeException(nameof(statement), statement, "Unsupported statement.")
        };
    }
}
