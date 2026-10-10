using Axiom.Core;

namespace Axiom.Semantics;

/// <summary>Предупреждения редактора о переменных, значение которых не читается.</summary>
public static class VariableUsageAnalysis
{
    public static IReadOnlyList<Diagnostic> Analyze(BoundProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        var declarations = new Dictionary<VariableSymbol, TextLocation>();
        var read = new HashSet<VariableSymbol>();
        foreach (var statement in program.GlobalStatements) VisitStatement(statement);
        foreach (var routine in program.Routines)
            foreach (var statement in routine.Statements) VisitStatement(statement);
        return declarations.Where(entry => !read.Contains(entry.Key))
            .OrderBy(static entry => entry.Value.Span.Start)
            .Select(static entry => new Diagnostic(DiagnosticCodes.UnusedVariable,
                $"Переменная '{entry.Key.Name}' объявлена, но её значение нигде не используется.",
                entry.Value, DiagnosticSeverity.Warning)).ToArray();

        void VisitStatement(BoundStatement statement)
        {
            switch (statement)
            {
                case BoundVariableDeclarationStatement declaration:
                    declarations.TryAdd(declaration.Variable, declaration.Location);
                    break;
                case BoundVariableDeclarationsStatement grouped:
                    foreach (var declaration in grouped.Declarations) VisitStatement(declaration);
                    return;
                case BoundAssignmentStatement assignment:
                    // Запись в переменную не считается чтением. При записи в
                    // элемент массива или поле объект и индексы используются.
                    if (assignment.Target is not BoundVariableExpression)
                        VisitExpression(assignment.Target);
                    VisitExpression(assignment.Expression);
                    return;
                case BoundInputStatement:
                    // INPUT записывает значение, но не читает его.
                    break;
                case BoundForStatement loop:
                    read.Add(loop.Variable); // Счётчик читается самим циклом.
                    break;
                case BoundOpenStatement open:
                    read.Add(open.FileVariable);
                    break;
                case BoundCloseStatement close:
                    read.Add(close.FileVariable);
                    break;
                case BoundSelectCaseStatement selection:
                    foreach (var clause in selection.Clauses)
                        foreach (var selector in clause.Selectors)
                            switch (selector)
                            {
                                case BoundCaseValueSelector value: VisitExpression(value.Expression); break;
                                case BoundCaseRangeSelector range:
                                    VisitExpression(range.LowerBound); VisitExpression(range.UpperBound); break;
                                case BoundCaseComparisonSelector comparison: VisitExpression(comparison.Expression); break;
                            }
                    break;
            }
            foreach (var expression in BoundExpressionTraversal.Immediate(statement)) VisitExpression(expression);
            foreach (var nested in BoundExpressionTraversal.Nested(statement)) VisitStatement(nested);
        }

        void VisitExpression(BoundExpression expression)
        {
            if (expression is BoundVariableExpression variable) read.Add(variable.Variable);
            foreach (var child in BoundExpressionTraversal.Immediate(expression)) VisitExpression(child);
        }
    }
}
