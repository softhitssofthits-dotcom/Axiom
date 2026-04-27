using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

public interface IProgramConsoleService : IProgramInputPromptService, IReversibleOutputWriter
{
    void BeginSession();

    void Clear();

    void ClearOutput();
}
