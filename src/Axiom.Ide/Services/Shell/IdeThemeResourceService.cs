using System.Windows;
using Axiom.Ide.Services.Persistence;

namespace Axiom.Ide.Services.Shell;

public static class IdeThemeResourceService
{
    private const string LightThemeResource = "Resources/ShellColors.xaml";
    private const string DarkThemeResource = "Resources/ShellColors.Dark.xaml";

    public static void Apply(IdeTheme theme)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var source = theme == IdeTheme.Dark ? DarkThemeResource : LightThemeResource;
        var replacement = new ResourceDictionary
        {
            Source = new Uri(source, UriKind.Relative)
        };

        for (var index = 0; index < dictionaries.Count; index++)
        {
            var currentSource = dictionaries[index].Source?.OriginalString;

            if (string.Equals(currentSource, LightThemeResource, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentSource, DarkThemeResource, StringComparison.OrdinalIgnoreCase))
            {
                dictionaries[index] = replacement;
                return;
            }
        }

        dictionaries.Add(replacement);
    }
}
