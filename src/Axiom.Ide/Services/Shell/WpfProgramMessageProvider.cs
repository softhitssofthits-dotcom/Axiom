using Axiom.Ide.Services.Execution;

namespace Axiom.Ide.Services.Shell;

public sealed class WpfProgramMessageProvider : IProgramMessageProvider
{
    public string Format(string key, string fallback, params object[] arguments) =>
        IdeLanguageResourceService.GetCurrentFormattedString(key, fallback, arguments);
}
