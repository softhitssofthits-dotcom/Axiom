using Axiom.Execution;

namespace Axiom.Ide.Editor.Identifiers;

public sealed class AxiomSmartIdentifierService
{
    private readonly IdentifierNormalizationService _normalizationService;

    public AxiomSmartIdentifierService(IdentifierNormalizationService normalizationService)
    {
        ArgumentNullException.ThrowIfNull(normalizationService);
        _normalizationService = normalizationService;
    }

    public string NormalizeDocument(string sourceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        return _normalizationService.NormalizeDocument(sourceText);
    }

    public string NormalizeLine(string sourceText, int lineStartOffset, int lineLength)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        return _normalizationService.NormalizeLine(sourceText, lineStartOffset, lineLength);
    }
}
