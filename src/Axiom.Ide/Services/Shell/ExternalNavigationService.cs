using System.Diagnostics;

namespace Axiom.Ide.Services.Shell;

public interface IExternalNavigationService
{
    void OpenPath(string path);
}

public sealed class ExternalNavigationService : IExternalNavigationService
{
    public void OpenPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
