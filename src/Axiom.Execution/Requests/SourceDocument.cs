namespace Axiom.Execution;

/// <summary>
/// Дополнительный исходный документ многофайловой программы. Имя файла
/// без расширения задаёт имя модуля.
/// </summary>
public sealed class SourceDocument
{
    public SourceDocument(string name, string text)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Document name must be provided.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(text);

        Name = name;
        Text = text;
    }

    public string Name { get; }

    public string Text { get; }

    public string ModuleName => Path.GetFileNameWithoutExtension(Name);
}
