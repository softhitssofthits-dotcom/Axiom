using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;

namespace Axiom.Ide.Services.Input;

/// <summary>
/// Управляет жизненным циклом консольной сессии: начало, завершение, отмена.
/// Выделен из ProgramConsoleService для разделения ответственности.
/// </summary>
public sealed class SessionManager
{
    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private volatile bool _isCancellationRequested;
    private volatile bool _isSessionActive;

    public SessionManager(
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null)
    {
        _traceService = traceService ?? NullIdeTraceService.Instance;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
    }

    /// <summary>
    /// Сессия активна (между BeginSession и EndSession).
    /// </summary>
    public bool IsSessionActive => _isSessionActive;

    /// <summary>
    /// Запрошена ли отмена выполнения.
    /// </summary>
    public bool IsCancellationRequested
    {
        get => _isCancellationRequested;
        set => _isCancellationRequested = value;
    }

    /// <summary>
    /// Начать новую сессию. Вызывает onReset для сброса состояния всех сервисов.
    /// </summary>
    public void BeginSession(Action onReset)
    {
        ArgumentNullException.ThrowIfNull(onReset);

        onReset();
        _isSessionActive = true;
        _isCancellationRequested = false;
        _traceService.Write("console.begin", "Console session started.");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "output",
            EventName = "console.session-begin",
            Source = nameof(SessionManager),
            OutputLength = 0
        });
    }

    /// <summary>
    /// Завершить текущую сессию.
    /// </summary>
    public void EndSession()
    {
        _isSessionActive = false;
    }

    /// <summary>
    /// Сбросить состояние (без изменения флага сессии).
    /// </summary>
    public void Reset(Action onReset)
    {
        ArgumentNullException.ThrowIfNull(onReset);

        onReset();
        _isCancellationRequested = false;
    }
}
