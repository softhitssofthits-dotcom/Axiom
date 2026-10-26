using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private bool LooksLikeTupleUnpacking()
    {
        var depth = 0;
        for (var offset = 0; ; offset++)
        {
            var kind = Peek(offset).Kind;
            if (kind is SyntaxKind.EndOfFileToken or SyntaxKind.NewLineToken) return false;
            if (kind == SyntaxKind.OpenParenToken) depth++;
            if (kind == SyntaxKind.CloseParenToken && --depth == 0)
                return Peek(offset + 1).Kind == SyntaxKind.EqualsToken;
        }
    }

    private TupleTypeSyntax ParseTupleType()
    {
        var open = Match(SyntaxKind.OpenParenToken, "(");
        var names = new List<SyntaxToken?>();
        var types = new List<TypeSyntax>();
        while (Current.Kind is not (SyntaxKind.CloseParenToken or SyntaxKind.EndOfFileToken or SyntaxKind.NewLineToken))
        {
            // Именованная группа завершается AS; безымянные типы начинаются
            // ключевым словом типа либо именем TYPE без последующего AS/запятой.
            if (Current.Kind is SyntaxKind.IdentifierToken or SyntaxKind.LineKeyword &&
                (Peek(1).Kind == SyntaxKind.AsKeyword || IsNamedTupleGroup()))
            {
                var group = new List<SyntaxToken> { MatchIdentifier() };
                while (Current.Kind == SyntaxKind.CommaToken)
                {
                    NextToken(); group.Add(MatchIdentifier());
                }
                Match(SyntaxKind.AsKeyword, "AS");
                var type = ParseTypeSyntax();
                foreach (var name in group) { names.Add(name); types.Add(type); }
            }
            else { names.Add(null); types.Add(ParseTypeSyntax()); }
            if (Current.Kind != SyntaxKind.CommaToken) break;
            NextToken();
            if (Current.Kind == SyntaxKind.CloseParenToken) { names.Add(null); types.Add(new NamedTypeSyntax(MatchNamedTypeToken())); break; }
        }
        return new TupleTypeSyntax(open, names, types, Match(SyntaxKind.CloseParenToken, ")"));
    }

    private bool IsNamedTupleGroup()
    {
        var offset = 0;
        while (Peek(offset).Kind is SyntaxKind.IdentifierToken or SyntaxKind.LineKeyword)
        {
            offset++;
            if (Peek(offset).Kind == SyntaxKind.AsKeyword) return true;
            if (Peek(offset).Kind != SyntaxKind.CommaToken) return false;
            offset++;
        }
        return false;
    }

    private StatementSyntax ParseTupleLeadingStatement(SyntaxToken? dim = null)
    {
        var target = ParseParenthesizedExpression();
        if (target is TupleExpressionSyntax tuple && Current.Kind == SyntaxKind.EqualsToken)
            return new TupleUnpackingStatementSyntax(dim, tuple, NextToken(), ParseExpression());
        return new ExpressionStatementSyntax(target);
    }

    private IReadOnlyList<ParameterSyntax> ParseParameterGroup(List<SyntaxToken> commas)
    {
        var mode = ParseOptionalPassingKeyword();
        var group = new List<(SyntaxToken Name, ArrayBoundsSyntax? Bounds)>();
        while (true)
        {
            var name = MatchIdentifier();
            var bounds = Current.Kind == SyntaxKind.OpenBracketToken ? ParseParameterArrayBounds() : null;
            group.Add((name, bounds));
            if (Current.Kind != SyntaxKind.CommaToken) break;
            commas.Add(NextToken());
        }
        var clause = ParseTypeClause();
        return group.Select(p => new ParameterSyntax(mode, p.Name, clause, p.Bounds)).ToArray();
    }
}
