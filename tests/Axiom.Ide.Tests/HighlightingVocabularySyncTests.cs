using System.Text;
using System.Text.RegularExpressions;
using Axiom.Formatting;
using Axiom.Ide.Editor.Highlighting;
using Axiom.Semantics;

namespace Axiom.Ide.Tests;

/// <summary>
/// Синхронизация трёх копий списка встроенных функций (светлая и тёмная схемы
/// подсветки + словарь форматирования) с реестром BuiltinFunctions. До аудита
/// 2026-10-06 (AXD-016) все три копии велись вручную без проверки полноты.
/// </summary>
public sealed class HighlightingVocabularySyncTests
{
    [Theory]
    [InlineData("Axiom.Ide.Editor.Highlighting.Axiom.xshd")]
    [InlineData("Axiom.Ide.Editor.Highlighting.Axiom.Dark.xshd")]
    public void Highlighting_BuiltInSectionMatchesBuiltinFunctionsRegistry(string resourceName)
    {
        var words = ReadBuiltInWords(resourceName);
        var registry = BuiltinFunctions.All
            .Select(static function => function.Name.ToUpperInvariant())
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(words);
        Assert.Equal(registry, words);
    }

    [Fact]
    public void Formatting_BuiltInVocabularyMatchesBuiltinFunctionsRegistry()
    {
        var registry = BuiltinFunctions.All
            .Select(static function => function.Name.ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(AxiomFormattingVocabulary.BuiltIns);
        Assert.Equal(registry, AxiomFormattingVocabulary.BuiltIns.ToHashSet(StringComparer.Ordinal));
    }

    [Fact]
    public void Highlighting_BothThemesExposeIdenticalBuiltInSections()
    {
        var light = ReadBuiltInWords("Axiom.Ide.Editor.Highlighting.Axiom.xshd");
        var dark = ReadBuiltInWords("Axiom.Ide.Editor.Highlighting.Axiom.Dark.xshd");

        Assert.Equal(light, dark);
    }

    private static List<string> ReadBuiltInWords(string resourceName)
    {
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        var section = Regex.Match(xshd, @"<Keywords color=""BuiltIn"">\s*(.*?)\s*</Keywords>", RegexOptions.Singleline);
        Assert.True(section.Success, $"BuiltIn section not found in {resourceName}.");

        return Regex.Matches(section.Groups[1].Value, @"<Word>([^<]+)</Word>")
            .Select(static match => match.Groups[1].Value)
            .ToList();
    }
}
