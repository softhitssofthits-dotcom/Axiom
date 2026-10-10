using System.Text.Json;
using Axiom.Execution;
using Axiom.Formatting;
using Axiom.Ide.Contracts;
using Axiom.Ide.Models;

namespace Axiom.DesktopHost;

internal static class DocumentCommands
{
    public static object Execute(string method, JsonElement payload)
    {
        if (!payload.TryGetProperty("documentRevision", out var revision) || !revision.TryGetInt32(out _))
            throw new ArgumentException("Отсутствует корректная версия документа.");
        var snapshot = payload.Deserialize<DocumentSnapshot>(new JsonSerializerOptions
            { PropertyNameCaseInsensitive = true }) ?? throw new ArgumentException("Отсутствует снимок документа.");
        if (string.IsNullOrWhiteSpace(snapshot.DocumentId) || snapshot.DocumentId.Length > 128 ||
            snapshot.DocumentRevision < 0 || snapshot.SourceText is null)
            throw new ArgumentException("Некорректный снимок документа.");
        var formatter = new AxiomCodeFormattingService();
        var style = CodeFormattingStyle.Classic;
        if (payload.TryGetProperty("formattingStyle", out var styleValue) &&
            (!Enum.TryParse(styleValue.GetString(), out style) || !Enum.IsDefined(style)))
            throw new ArgumentException("Неизвестный стиль форматирования.");
        if (method == "document.normalizeIdentifiers")
            return new { snapshot.DocumentId, snapshot.DocumentRevision,
                sourceText = NormalizeIdentifiers(snapshot.SourceText, payload) };
        if (method == "document.format")
            return new { snapshot.DocumentId, snapshot.DocumentRevision,
                sourceText = NormalizeIdentifiers(formatter.FormatDocument(snapshot.SourceText, style), payload) };
        if (method == "document.editTransition")
        {
            var previous = payload.GetProperty("previousLineNumber").GetInt32();
            var caret = payload.GetProperty("caretOffset").GetInt32();
            var source = FormatPreviousLine(snapshot.SourceText, previous, formatter, style, ref caret);
            var plan = new BlockEditPlanner(formatter).Plan(source, previous,
                payload.GetProperty("currentLineNumber").GetInt32(),
                caret, style);
            var resultText = NormalizeIdentifiers(source[..plan.Start] + plan.NewText + source[(plan.Start + plan.Length)..], payload);
            var prefix = 0;
            while (prefix < snapshot.SourceText.Length && prefix < resultText.Length && snapshot.SourceText[prefix] == resultText[prefix]) prefix++;
            var suffix = 0;
            while (suffix < snapshot.SourceText.Length - prefix && suffix < resultText.Length - prefix && snapshot.SourceText[^(suffix + 1)] == resultText[^(suffix + 1)]) suffix++;
            return new { snapshot.DocumentId, snapshot.DocumentRevision,
                edits = resultText != snapshot.SourceText ? new[] { new TextEdit(prefix, snapshot.SourceText.Length - prefix - suffix, resultText.Substring(prefix, resultText.Length - prefix - suffix)) } : [],
                plan.NewCaretOffset };
        }
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(snapshot.SourceText));
        var diagnostics = result.Diagnostics.Concat(result.BackendDiagnostics)
            .DistinctBy(d => (d.Code, d.Location.Span, d.Message))
            .Select(d => {
                var item = ProgramDiagnosticItem.FromDiagnostic(d);
                return new { item.Code, item.Stage, item.Message, item.Line, item.Column,
                    item.Length, item.SpanStart, item.SpanLength,
                    snapshot.DocumentId, snapshot.DocumentRevision, snapshot.SourceFilePath,
                    severity = d.Severity.ToString().ToLowerInvariant() };
            }).ToArray();
        return new { snapshot.DocumentId, snapshot.DocumentRevision,
            status = result.Status.ToString(), diagnostics };
    }

    private static string NormalizeIdentifiers(string source, JsonElement payload)
    {
        if (!payload.TryGetProperty("smartIdentifierEnabled", out var enabled)) return source;
        if (enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new ArgumentException("Некорректное значение настройки умного идентификатора.");
        return enabled.GetBoolean() ? new IdentifierNormalizationService().NormalizeDocument(source) : source;
    }

    private static string FormatPreviousLine(string source, int lineNumber, ICodeFormattingService formatter, CodeFormattingStyle style, ref int caret)
    {
        var start = 0;
        for (var line = 1; line < lineNumber; line++)
        {
            var end = source.IndexOfAny(['\r', '\n'], start);
            if (end < 0) return source;
            start = end + (source[end] == '\r' && end + 1 < source.Length && source[end + 1] == '\n' ? 2 : 1);
        }
        if (lineNumber < 1) return source;
        var lineEnd = source.IndexOfAny(['\r', '\n'], start);
        if (lineEnd < 0) lineEnd = source.Length;
        var text = source[start..lineEnd];
        if (string.IsNullOrWhiteSpace(text) || !formatter.IsLineSyntaxValid(text)) return source;
        var formatted = formatter.FormatLine(text, style);
        if (caret >= lineEnd) caret += formatted.Length - text.Length;
        else if (caret > start) caret = start + Math.Min(caret - start, formatted.Length);
        return source[..start] + formatted + source[lineEnd..];
    }
}
