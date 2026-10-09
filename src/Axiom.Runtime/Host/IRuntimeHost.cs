namespace Axiom.Runtime;

public interface IRuntimeHost : IInputProvider, IKeyboardInputProvider, IMouseInputProvider, IOutputWriter, IRuntimeClock, IRuntimeRandom, IGraphicsHost, IRuntimeCancellationProvider
{
    string? ResourceBaseDirectory => null;

    string CurrentDirectory => ResourceBaseDirectory ?? Environment.CurrentDirectory;

    // Прямые встраиваемые хосты сохраняют прежнее поведение; адаптер пайплайна
    // хранит рабочую папку отдельно для каждого запуска.
    void ChangeCurrentDirectory(string path) => Directory.SetCurrentDirectory(path);
}
