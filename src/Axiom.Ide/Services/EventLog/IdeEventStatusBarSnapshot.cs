using System.Text.Json.Serialization;

namespace Axiom.Ide.Services.EventLog;

public sealed class IdeEventStatusBarSnapshot
{
    [JsonPropertyName("statusText")]
    public string StatusText { get; init; } = string.Empty;

    [JsonPropertyName("lineColumnText")]
    public string LineColumnText { get; init; } = string.Empty;

    [JsonPropertyName("pathText")]
    public string PathText { get; init; } = string.Empty;

    [JsonPropertyName("compilationTimingText")]
    public string CompilationTimingText { get; init; } = string.Empty;

    [JsonPropertyName("preparationTimingText")]
    public string PreparationTimingText { get; init; } = string.Empty;

    [JsonPropertyName("executionTimingText")]
    public string ExecutionTimingText { get; init; } = string.Empty;

    [JsonPropertyName("totalTimingText")]
    public string TotalTimingText { get; init; } = string.Empty;

    [JsonPropertyName("cacheTimingText")]
    public string CacheTimingText { get; init; } = string.Empty;

    [JsonPropertyName("engineTimingText")]
    public string EngineTimingText { get; init; } = string.Empty;

    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; init; }

    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; init; }

    [JsonPropertyName("isCompilationProgressVisible")]
    public bool IsCompilationProgressVisible { get; init; }

    [JsonPropertyName("compilationProgressPercent")]
    public int CompilationProgressPercent { get; init; }
}

