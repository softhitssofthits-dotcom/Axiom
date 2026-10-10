namespace Axiom.Ide.Services.Execution;

public interface IProgramMessageProvider
{
    string Format(string key, string fallback, params object[] arguments);
}

public sealed class DefaultProgramMessageProvider : IProgramMessageProvider
{
    public string Format(string key, string fallback, params object[] arguments) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, fallback, arguments);
}
