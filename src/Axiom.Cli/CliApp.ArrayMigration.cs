using System.Text.Json;
using Axiom.Execution;

namespace Axiom.Cli;

internal static partial class CliApp
{
    private static int RunArrayMigration(string[] args, TextWriter stdout, TextWriter stderr, TextReader stdin)
    {
        var json = false;
        var fromStdin = false;
        string? path = null;
        foreach (var argument in args.Skip(1))
        {
            if (argument == "--json" && !json) json = true;
            else if (argument == "--stdin" && !fromStdin) fromStdin = true;
            else if (argument.StartsWith("--", StringComparison.Ordinal)) return Usage(stderr);
            else if (path is null) path = argument;
            else return Usage(stderr);
        }
        if (fromStdin == (path is not null)) return Usage(stderr);
        // Запись намеренно отсутствует: даже --write отклоняется до чтения файла.
        var source = fromStdin ? ReadStdin(stdin, stderr) : ReadSource(path!, stderr);
        if (source is null) return 2;
        IReadOnlyList<SourceDocument> dependencies = [];
        if (!fromStdin && !TryLoadDependencies(source, path, stderr, out dependencies, out var failure))
        {
            if (json) stdout.Write(JsonSerializer.Serialize(new
            {
                canApply = false, originalSource = source, source, edits = Array.Empty<object>(),
                issues = new[] { new { code = failure.Code, message = failure.Message, document = failure.Location.Document } }
            }));
            return 2;
        }
        var result = ArraySyntaxMigrationService.Migrate(source, path is null ? null : Path.GetFullPath(path), dependencies);
        if (json)
            stdout.Write(JsonSerializer.Serialize(new
            {
                canApply = result.CanApply,
                originalSource = result.OriginalSource,
                source = result.TransformedSource,
                edits = result.Edits.Select(edit => new
                {
                    start = edit.Span.Start, length = edit.Span.Length,
                    replacement = edit.Replacement, kind = edit.Kind, document = edit.Document
                }),
                issues = result.Diagnostics.Select(issue => new
                {
                    code = issue.Code, message = issue.Message, document = issue.Document ?? issue.Location.Document,
                    start = issue.Location.Span.Start, length = issue.Location.Span.Length,
                    line = issue.Location.Line, column = issue.Location.Column
                })
            }));
        else if (result.CanApply) stdout.Write(result.TransformedSource);
        foreach (var issue in result.Diagnostics)
            stderr.WriteLine($"{issue.Document ?? path ?? "<buffer>"}({issue.Location.Line},{issue.Location.Column}): ошибка {issue.Code}: {issue.Message}");
        return result.CanApply ? 0 : 1;
    }
}
