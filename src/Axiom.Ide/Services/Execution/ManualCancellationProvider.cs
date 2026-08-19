using Axiom.Runtime;

namespace Axiom.Ide.Services.Execution;

/// <summary>
/// Провайдер отмены, который может быть triggered пользователем через вызов Cancel().
/// </summary>
public sealed class ManualCancellationProvider : IRuntimeCancellationProvider
{
    private volatile bool _cancelled;

    public bool IsCancellationRequested => _cancelled;

    public void Cancel()
    {
        _cancelled = true;
    }

    public void Reset()
    {
        _cancelled = false;
    }
}
