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
            CanvasStatementSyntax canvasStatement => BindCanvasStatement(canvasStatement),
            BeginFrameStatementSyntax beginFrameStatement => new BoundBeginFrameStatement(GetLocation(beginFrameStatement)),
            PresentStatementSyntax presentStatement => new BoundPresentStatement(GetLocation(presentStatement)),
            CameraStatementSyntax cameraStatement => BindCameraStatement(cameraStatement),
            ResetCameraStatementSyntax resetCameraStatement => BindResetCameraStatement(resetCameraStatement),
            ViewStatementSyntax viewStatement => BindViewStatement(viewStatement),
            ViewPrintStatementSyntax viewPrintStatement => BindViewPrintStatement(viewPrintStatement),
            WindowStatementSyntax windowStatement => BindWindowStatement(windowStatement),
            ClsStatementSyntax clsStatement => BindClsStatement(clsStatement),
            ColorStatementSyntax colorStatement => BindColorStatement(colorStatement),
            LocateStatementSyntax locateStatement => BindLocateStatement(locateStatement),
            PaletteStatementSyntax paletteStatement => BindPaletteStatement(paletteStatement),
            PCopyStatementSyntax pcopyStatement => BindPCopyStatement(pcopyStatement),
            PSetStatementSyntax psetStatement => BindPSetStatement(psetStatement),
            GetStatementSyntax getStatement => BindGetStatement(getStatement),
            PutStatementSyntax putStatement => BindPutStatement(putStatement),
            LineStatementSyntax lineStatement => BindLineStatement(lineStatement),
            CircleStatementSyntax circleStatement => BindCircleStatement(circleStatement),
            PaintStatementSyntax paintStatement => BindPaintStatement(paintStatement),
            DrawStatementSyntax drawStatement => BindDrawStatement(drawStatement),
            DrawLineStatementSyntax drawLineStatement => BindDrawLineStatement(drawLineStatement),
            DrawCircleStatementSyntax drawCircleStatement => BindDrawCircleStatement(drawCircleStatement),
            FillCircleStatementSyntax fillCircleStatement => BindFillCircleStatement(fillCircleStatement),
            DrawTextStatementSyntax drawTextStatement => BindDrawTextStatement(drawTextStatement),
            DrawImageStatementSyntax drawImageStatement => BindDrawImageStatement(drawImageStatement),
            DrawRectStatementSyntax drawRectStatement => BindDrawRectStatement(drawRectStatement),
            FillRectStatementSyntax fillRectStatement => BindFillRectStatement(fillRectStatement),
            DrawSpriteStatementSyntax drawSpriteStatement => BindDrawSpriteStatement(drawSpriteStatement),
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
