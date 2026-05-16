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
            SleepStatementSyntax sleepStatement => BindSleepStatement(sleepStatement),
            ScreenStatementSyntax screenStatement => BindScreenStatement(screenStatement),
            ViewStatementSyntax viewStatement => BindViewStatement(viewStatement),
            ClsStatementSyntax clsStatement => new BoundClsStatement(GetLocation(clsStatement)),
            ColorStatementSyntax colorStatement => BindColorStatement(colorStatement),
            LocateStatementSyntax locateStatement => BindLocateStatement(locateStatement),
            PSetStatementSyntax psetStatement => BindPSetStatement(psetStatement),
            LineStatementSyntax lineStatement => BindLineStatement(lineStatement),
            CircleStatementSyntax circleStatement => BindCircleStatement(circleStatement),
            PaintStatementSyntax paintStatement => BindPaintStatement(paintStatement),
            DrawStatementSyntax drawStatement => BindDrawStatement(drawStatement),
            OnKeyStatementSyntax onKeyStatement => BindOnKeyStatement(onKeyStatement),
            OnMouseStatementSyntax onMouseStatement => BindOnMouseStatement(onMouseStatement),
            OpenStatementSyntax openStatement => BindOpenStatement(openStatement),
            CloseStatementSyntax closeStatement => BindCloseStatement(closeStatement),
            ErrorStatementSyntax errorStatement => BindErrorStatement(errorStatement),
            IfStatementSyntax ifStatement => BindIfStatement(ifStatement),
            TryStatementSyntax tryStatement => BindTryStatement(tryStatement),
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
