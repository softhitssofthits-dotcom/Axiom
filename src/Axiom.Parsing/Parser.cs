using Axiom.Core;
using Axiom.Lexing;
using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private readonly IReadOnlyList<SyntaxToken> _tokens;
    private int _position;

    public Parser(string text, string? document = null)
    {
        var lexedTokens = Lexer.Lex(text, out var lexerDiagnostics, document);

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
        return Parse(text, out diagnostics, document: null);
    }

    public static ProgramSyntax Parse(string text, out DiagnosticBag diagnostics, string? document)
    {
        var parser = new Parser(text, document);
        var program = parser.ParseProgram();
        diagnostics = parser.Diagnostics;
        return program;
    }

    public ProgramSyntax ParseProgram()
    {
        var members = new List<MemberSyntax>();
        var importsClosed = false;

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

            if (Current.Kind == SyntaxKind.ImportKeyword)
            {
                if (importsClosed)
                {
                    ReportUnexpectedToken(Current, "оператор IMPORT должен находиться в начале файла до других объявлений");
                }

                members.Add(ParseImportStatement());
                SkipNewLines();
                continue;
            }

            importsClosed = true;
            var start = _position;
            members.Add(ParseMember(allowVisibility: true));

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
