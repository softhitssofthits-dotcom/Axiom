using Axiom.Core;
using Axiom.Lexing;
using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private readonly IReadOnlyList<SyntaxToken> _tokens;
    private int _position;

    public Parser(string text)
    {
        var lexedTokens = Lexer.Lex(text, out var lexerDiagnostics);

        Diagnostics = new DiagnosticBag();
        Diagnostics.AddRange(lexerDiagnostics);

        _tokens = lexedTokens
            .Where(static token => token.Kind is not TokenKind.CommentToken and not TokenKind.BadToken)
            .Select(static token => new SyntaxToken(
                MapTokenKind(token.Kind),
                token.Text,
                token.Location,
                token.Value))
            .ToArray();
    }

    public DiagnosticBag Diagnostics { get; }

    public static ProgramSyntax Parse(string text, out DiagnosticBag diagnostics)
    {
        var parser = new Parser(text);
        var program = parser.ParseProgram();
        diagnostics = parser.Diagnostics;
        return program;
    }

    public ProgramSyntax ParseProgram()
    {
        var members = new List<MemberSyntax>();

        SkipNewLines();

        while (Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (IsUnexpectedBoundaryToken())
            {
                ReportUnexpectedToken(Current, "Неожиданный маркер конца блока");
                NextToken();
                SkipNewLines();
                continue;
            }

            var start = _position;
            members.Add(ParseMember());

            if (_position == start)
            {
                ReportUnexpectedToken(Current, "Неожиданный токен");
                NextToken();
            }

            SkipNewLines();
        }

        return new ProgramSyntax(members, Match(SyntaxKind.EndOfFileToken, "конец файла"));
    }
}
