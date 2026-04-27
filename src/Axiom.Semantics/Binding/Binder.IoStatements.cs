using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundStatement BindPrintStatement(PrintStatementSyntax statement)
    {
        var items = new List<BoundPrintItem>(statement.Items.Count);

        foreach (var item in statement.Items)
        {
            items.Add(new BoundPrintItem(BindExpression(item.Expression), MapPrintSeparator(item.SeparatorToken)));
        }

        return new BoundPrintStatement(GetLocation(statement), items);
    }

    private BoundStatement BindPrintUsingStatement(PrintUsingStatementSyntax statement)
    {
        var formatExpression = BindExpression(statement.FormatExpression, TypeSymbol.String);
        var items = new List<BoundPrintItem>(statement.Items.Count);

        foreach (var item in statement.Items)
        {
            items.Add(new BoundPrintItem(BindExpression(item.Expression), MapPrintSeparator(item.SeparatorToken)));
        }

        return new BoundPrintUsingStatement(GetLocation(statement), formatExpression, items);
    }

    private BoundStatement BindInputStatement(InputStatementSyntax statement)
    {
        var variables = new List<VariableSymbol>(statement.Identifiers.Count);

        foreach (var identifier in statement.Identifiers)
        {
            var variable = LookupVariable(identifier.Text, identifier.Location);

            if (variable.IsReadOnly)
            {
                ReportAssignToConst(variable.Name, identifier.Location);
            }

            variables.Add(variable);
        }

        var promptText = statement.PromptToken?.Value as string;
        return new BoundInputStatement(GetLocation(statement), variables, promptText);
    }

    private BoundStatement BindOpenStatement(OpenStatementSyntax statement)
    {
        var pathExpression = BindExpression(statement.PathExpression, TypeSymbol.String);
        var fileVariable = LookupVariable(statement.FileIdentifier.Text, statement.FileIdentifier.Location);

        if (fileVariable.IsArray)
        {
            ReportTypeMismatch(TypeSymbol.Error, TypeSymbol.File, statement.FileIdentifier.Location);
        }
        else if (fileVariable.Type != TypeSymbol.File && fileVariable.Type != TypeSymbol.Error)
        {
            ReportTypeMismatch(fileVariable.Type, TypeSymbol.File, statement.FileIdentifier.Location);
        }

        var mode = statement.ModeKeyword.Kind switch
        {
            SyntaxKind.InputKeyword => BoundFileOpenMode.Input,
            SyntaxKind.OutputKeyword => BoundFileOpenMode.Output,
            SyntaxKind.AppendKeyword => BoundFileOpenMode.Append,
            _ => BoundFileOpenMode.Input
        };

        return new BoundOpenStatement(GetLocation(statement), pathExpression, fileVariable, mode);
    }

    private BoundStatement BindCloseStatement(CloseStatementSyntax statement)
    {
        var fileVariable = LookupVariable(statement.FileIdentifier.Text, statement.FileIdentifier.Location);

        if (fileVariable.IsArray)
        {
            ReportTypeMismatch(TypeSymbol.Error, TypeSymbol.File, statement.FileIdentifier.Location);
        }
        else if (fileVariable.Type != TypeSymbol.File && fileVariable.Type != TypeSymbol.Error)
        {
            ReportTypeMismatch(fileVariable.Type, TypeSymbol.File, statement.FileIdentifier.Location);
        }

        return new BoundCloseStatement(GetLocation(statement), fileVariable);
    }

    private static PrintSeparatorKind? MapPrintSeparator(SyntaxToken? token)
    {
        return token?.Kind switch
        {
            null => null,
            SyntaxKind.SemicolonToken => PrintSeparatorKind.Semicolon,
            SyntaxKind.CommaToken => PrintSeparatorKind.Comma,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token.Kind, "Unsupported print separator token.")
        };
    }
}
