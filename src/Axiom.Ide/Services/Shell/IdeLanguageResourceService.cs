using System.Windows;
using Axiom.Ide.Services.Persistence;

namespace Axiom.Ide.Services.Shell;

public static class IdeLanguageResourceService
{
    private const string RussianLanguageResource = "Resources/ShellStrings.ru.xaml";
    private const string EnglishLanguageResource = "Resources/ShellStrings.en.xaml";

    public static event EventHandler? LanguageApplied;

    public static void Apply(IdeLanguage language)
    {
        var resources = Application.Current?.Resources.MergedDictionaries;

        if (resources is null)
        {
            return;
        }

        var source = language == IdeLanguage.English
            ? EnglishLanguageResource
            : RussianLanguageResource;

        for (var index = 0; index < resources.Count; index++)
        {
            var currentSource = resources[index].Source?.OriginalString;

            if (string.Equals(currentSource, RussianLanguageResource, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentSource, EnglishLanguageResource, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(currentSource, source, StringComparison.OrdinalIgnoreCase))
                {
                    LanguageApplied?.Invoke(null, EventArgs.Empty);
                    return;
                }

                resources[index] = new ResourceDictionary
                {
                    Source = new Uri(source, UriKind.Relative)
                };

                LanguageApplied?.Invoke(null, EventArgs.Empty);
                return;
            }
        }

        resources.Add(new ResourceDictionary
        {
            Source = new Uri(source, UriKind.Relative)
        });
        LanguageApplied?.Invoke(null, EventArgs.Empty);
    }

    public static string GetString(IdeLanguage language, string key, string fallback)
    {
        try
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri($"/Axiom.Ide;component/{GetLanguageResource(language)}", UriKind.Relative));

            return dictionary[key] as string ?? fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    public static string GetCurrentString(string key, string fallback)
    {
        return Application.Current?.TryFindResource(key) as string ?? fallback;
    }

    public static string GetCurrentFormattedString(string key, string fallbackFormat, params object?[] args)
    {
        return string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            GetCurrentString(key, fallbackFormat),
            args);
    }

    private static string GetLanguageResource(IdeLanguage language)
    {
        return language == IdeLanguage.English
            ? EnglishLanguageResource
            : RussianLanguageResource;
    }
}
