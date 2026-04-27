namespace Axiom.Bytecode;

public sealed class BytecodeHeader
{
    public BytecodeHeader(
        BytecodeFormatVersion formatVersion,
        string languageVersion,
        BytecodeModuleFlags flags,
        string compatibilityToken)
    {
        ArgumentNullException.ThrowIfNull(languageVersion);
        ArgumentNullException.ThrowIfNull(compatibilityToken);

        FormatVersion = formatVersion;
        LanguageVersion = languageVersion;
        Flags = flags;
        CompatibilityToken = compatibilityToken;
    }

    public BytecodeFormatVersion FormatVersion { get; }

    public string LanguageVersion { get; }

    public BytecodeModuleFlags Flags { get; }

    public string CompatibilityToken { get; }
}
