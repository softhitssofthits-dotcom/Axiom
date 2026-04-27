using Axiom.Core;

namespace Axiom.Semantics;

public sealed class BoundArrayBound
{
    public BoundArrayBound(BoundExpression lowerBound, BoundExpression upperBound)
    {
        ArgumentNullException.ThrowIfNull(lowerBound);
        ArgumentNullException.ThrowIfNull(upperBound);

        LowerBound = lowerBound;
        UpperBound = upperBound;
    }

    public BoundExpression LowerBound { get; }

    public BoundExpression UpperBound { get; }
}

public sealed class BoundVariableDeclarationStatement : BoundStatement
{
    public BoundVariableDeclarationStatement(
        TextLocation location,
        VariableSymbol variable,
        BoundExpression? initializer,
        IReadOnlyList<BoundArrayBound> arrayBounds)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentNullException.ThrowIfNull(arrayBounds);

        Variable = variable;
        Initializer = initializer;
        ArrayBounds = arrayBounds;
    }

    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;

    public VariableSymbol Variable { get; }

    public BoundExpression? Initializer { get; }

    public IReadOnlyList<BoundArrayBound> ArrayBounds { get; }

    public BoundExpression? LowerBound => ArrayBounds.Count == 0 ? null : ArrayBounds[0].LowerBound;

    public BoundExpression? UpperBound => ArrayBounds.Count == 0 ? null : ArrayBounds[0].UpperBound;
}

public sealed class BoundVariableDeclarationsStatement : BoundStatement
{
    public BoundVariableDeclarationsStatement(TextLocation location, IReadOnlyList<BoundVariableDeclarationStatement> declarations)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        Declarations = declarations;
    }

    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationsStatement;

    public IReadOnlyList<BoundVariableDeclarationStatement> Declarations { get; }
}

public sealed class BoundAssignmentStatement : BoundStatement
{
    public BoundAssignmentStatement(TextLocation location, BoundExpression target, BoundExpression expression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(expression);

        Target = target;
        Expression = expression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;

    public BoundExpression Target { get; }

    public BoundExpression Expression { get; }
}

public sealed class BoundEraseStatement : BoundStatement
{
    public BoundEraseStatement(TextLocation location, BoundExpression target, string displayName)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        Target = target;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.EraseStatement;

    public BoundExpression Target { get; }

    public string DisplayName { get; }

    public VariableSymbol? Variable => (Target as BoundVariableExpression)?.Variable;
}

public sealed class BoundRedimStatement : BoundStatement
{
    public BoundRedimStatement(
        TextLocation location,
        BoundExpression target,
        string displayName,
        bool preserve,
        IReadOnlyList<BoundArrayBound> arrayBounds)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(arrayBounds);

        Target = target;
        DisplayName = displayName;
        Preserve = preserve;
        ArrayBounds = arrayBounds;
    }

    public override BoundNodeKind Kind => BoundNodeKind.RedimStatement;

    public BoundExpression Target { get; }

    public string DisplayName { get; }

    public VariableSymbol? Variable => (Target as BoundVariableExpression)?.Variable;

    public bool Preserve { get; }

    public IReadOnlyList<BoundArrayBound> ArrayBounds { get; }

    public BoundExpression LowerBound => ArrayBounds[0].LowerBound;

    public BoundExpression UpperBound => ArrayBounds[0].UpperBound;
}

public enum PrintSeparatorKind
{
    Semicolon,
    Comma
}

public sealed class BoundPrintItem
{
    public BoundPrintItem(BoundExpression expression, PrintSeparatorKind? separatorKind)
    {
        ArgumentNullException.ThrowIfNull(expression);

        Expression = expression;
        SeparatorKind = separatorKind;
    }

    public BoundExpression Expression { get; }

    public PrintSeparatorKind? SeparatorKind { get; }
}

public sealed class BoundPrintStatement : BoundStatement
{
    public BoundPrintStatement(TextLocation location, IReadOnlyList<BoundPrintItem> items)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PrintStatement;

    public IReadOnlyList<BoundPrintItem> Items { get; }
}

public sealed class BoundPrintUsingStatement : BoundStatement
{
    public BoundPrintUsingStatement(TextLocation location, BoundExpression formatExpression, IReadOnlyList<BoundPrintItem> items)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(formatExpression);
        ArgumentNullException.ThrowIfNull(items);

        FormatExpression = formatExpression;
        Items = items;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PrintUsingStatement;

    public BoundExpression FormatExpression { get; }

    public IReadOnlyList<BoundPrintItem> Items { get; }
}

public sealed class BoundInputStatement : BoundStatement
{
    public BoundInputStatement(TextLocation location, IReadOnlyList<VariableSymbol> variables, string? promptText)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(variables);
        Variables = variables;
        PromptText = promptText;
    }

    public override BoundNodeKind Kind => BoundNodeKind.InputStatement;

    public IReadOnlyList<VariableSymbol> Variables { get; }

    public string? PromptText { get; }
}

public enum BoundFileOpenMode
{
    Input,
    Output,
    Append
}

public sealed class BoundOpenStatement : BoundStatement
{
    public BoundOpenStatement(TextLocation location, BoundExpression pathExpression, VariableSymbol fileVariable, BoundFileOpenMode mode)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(pathExpression);
        ArgumentNullException.ThrowIfNull(fileVariable);

        PathExpression = pathExpression;
        FileVariable = fileVariable;
        Mode = mode;
    }

    public override BoundNodeKind Kind => BoundNodeKind.OpenStatement;

    public BoundExpression PathExpression { get; }

    public VariableSymbol FileVariable { get; }

    public BoundFileOpenMode Mode { get; }
}

public sealed class BoundCloseStatement : BoundStatement
{
    public BoundCloseStatement(TextLocation location, VariableSymbol fileVariable)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(fileVariable);
        FileVariable = fileVariable;
    }

    public override BoundNodeKind Kind => BoundNodeKind.CloseStatement;

    public VariableSymbol FileVariable { get; }
}

public sealed class BoundIfStatement : BoundStatement
{
    public BoundIfStatement(
        TextLocation location,
        BoundExpression condition,
        IReadOnlyList<BoundStatement> statements,
        IReadOnlyList<BoundStatement> elseStatements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(elseStatements);

        Condition = condition;
        Statements = statements;
        ElseStatements = elseStatements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;

    public BoundExpression Condition { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }

    public IReadOnlyList<BoundStatement> ElseStatements { get; }
}

public sealed class BoundWhileStatement : BoundStatement
{
    public BoundWhileStatement(TextLocation location, BoundExpression condition, IReadOnlyList<BoundStatement> statements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(statements);

        Condition = condition;
        Statements = statements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;

    public BoundExpression Condition { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }
}

public enum BoundDoLoopConditionKind
{
    None,
    While,
    Until
}

public sealed class BoundDoLoopStatement : BoundStatement
{
    public BoundDoLoopStatement(
        TextLocation location,
        BoundExpression? condition,
        BoundDoLoopConditionKind conditionKind,
        bool evaluateConditionAfterBody,
        IReadOnlyList<BoundStatement> statements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(statements);

        Condition = condition;
        ConditionKind = conditionKind;
        EvaluateConditionAfterBody = evaluateConditionAfterBody;
        Statements = statements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DoLoopStatement;

    public BoundExpression? Condition { get; }

    public BoundDoLoopConditionKind ConditionKind { get; }

    public bool EvaluateConditionAfterBody { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }
}

public sealed class BoundForStatement : BoundStatement
{
    public BoundForStatement(
        TextLocation location,
        VariableSymbol variable,
        BoundExpression startExpression,
        BoundExpression endExpression,
        BoundExpression stepExpression,
        IReadOnlyList<BoundStatement> statements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentNullException.ThrowIfNull(startExpression);
        ArgumentNullException.ThrowIfNull(endExpression);
        ArgumentNullException.ThrowIfNull(stepExpression);
        ArgumentNullException.ThrowIfNull(statements);

        Variable = variable;
        StartExpression = startExpression;
        EndExpression = endExpression;
        StepExpression = stepExpression;
        Statements = statements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ForStatement;

    public VariableSymbol Variable { get; }

    public BoundExpression StartExpression { get; }

    public BoundExpression EndExpression { get; }

    public BoundExpression StepExpression { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }
}

public sealed class BoundExitForStatement : BoundStatement
{
    public BoundExitForStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ExitForStatement;
}

public sealed class BoundExitDoStatement : BoundStatement
{
    public BoundExitDoStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ExitDoStatement;
}

public sealed class BoundReturnStatement : BoundStatement
{
    public BoundReturnStatement(TextLocation location, BoundExpression? expression)
        : base(location)
    {
        Expression = expression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;

    public BoundExpression? Expression { get; }
}

public sealed class BoundExpressionStatement : BoundStatement
{
    public BoundExpressionStatement(TextLocation location, BoundExpression expression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression = expression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;

    public BoundExpression Expression { get; }
}
