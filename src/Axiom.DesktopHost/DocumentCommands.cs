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
        if (method == "document.format")
            return new { snapshot.DocumentId, snapshot.DocumentRevision,
                sourceText = formatter.FormatDocument(snapshot.SourceText, style) };
        if (method == "document.editTransition")
        {
            var plan = new BlockEditPlanner(formatter).Plan(snapshot.SourceText,
                payload.GetProperty("previousLineNumber").GetInt32(),
                payload.GetProperty("currentLineNumber").GetInt32(),
                payload.GetProperty("caretOffset").GetInt32(), style);
            return new { snapshot.DocumentId, snapshot.DocumentRevision,
                edits = plan.HasChanges ? new[] { new TextEdit(plan.Start, plan.Length, plan.NewText) } : [],
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
}
