using System.Text.Json.Serialization;

namespace Axiom.Ide.Services.EventLog;

public sealed class IdeEventLogEntry
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("sequence")]
    public long Sequence { get; init; }

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; init; }

    [JsonPropertyName("timestampLocal")]
    public DateTimeOffset TimestampLocal { get; init; }

    [JsonPropertyName("level")]
    public IdeEventLevel Level { get; init; } = IdeEventLevel.Info;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("eventName")]
    public string EventName { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("threadId")]
    public int ThreadId { get; init; }

    [JsonPropertyName("documentPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentPath { get; init; }

    [JsonPropertyName("documentName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentName { get; init; }

    [JsonPropertyName("line")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Line { get; init; }

    [JsonPropertyName("column")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Column { get; init; }

    [JsonPropertyName("commandName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CommandName { get; init; }

    [JsonPropertyName("menuName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MenuName { get; init; }

    [JsonPropertyName("debugLine")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DebugLine { get; init; }

    [JsonPropertyName("opcode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Opcode { get; init; }

    [JsonPropertyName("callDepth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CallDepth { get; init; }

    [JsonPropertyName("canStepBack")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CanStepBack { get; init; }

    [JsonPropertyName("outputLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OutputLength { get; init; }

    [JsonPropertyName("textLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TextLength { get; init; }

    [JsonPropertyName("textPreview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TextPreview { get; init; }

    [JsonPropertyName("statusBar")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IdeEventStatusBarSnapshot? StatusBar { get; init; }

    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string?>? Properties { get; init; }
}

