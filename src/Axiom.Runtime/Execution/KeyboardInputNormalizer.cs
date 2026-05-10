namespace Axiom.Runtime;

public static class KeyboardInputNormalizer
{
    public const string AnyKeyHandlerName = "ANY";

    private static readonly string[] CanonicalNamedKeys =
    [
        "LEFT",
        "RIGHT",
        "UP",
        "DOWN",
        "ESC",
        "ENTER",
        "BACKSPACE",
        "TAB",
        "DELETE",
        "INSERT",
        "HOME",
        "END",
        "PAGEUP",
        "PAGEDOWN",
        "SPACE",
        "SHIFT",
        "CTRL",
        "ALT",
        "F1",
        "F2",
        "F3",
        "F4",
        "F5",
        "F6",
        "F7",
        "F8",
        "F9",
        "F10",
        "F11",
        "F12"
    ];

    private static readonly IReadOnlyDictionary<string, string> NamedKeyAliases = CreateNamedKeyAliases();

    public static IReadOnlyList<string> CanonicalNamedKeyNames { get; } = CanonicalNamedKeys;

    public static string Normalize(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return string.Empty;
        }

        var trimmed = keyText.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var lookupKey = CreateLookupKey(trimmed);
        if (NamedKeyAliases.TryGetValue(lookupKey, out var canonicalName))
        {
            return canonicalName;
        }

        return trimmed.Length == 1
            ? trimmed
            : trimmed.ToUpperInvariant();
    }

    public static KeyboardInputChord NormalizeChord(string keyText)
    {
        if (string.IsNullOrWhiteSpace(keyText))
        {
            return KeyboardInputChord.Empty;
        }

        var parts = keyText
            .Split('+', StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(static part => part.Length > 0)
            .ToArray();

        if (parts.Length == 0)
        {
            return KeyboardInputChord.Empty;
        }

        if (parts.Length == 1)
        {
            return new KeyboardInputChord(parts[0], parts[0], Array.Empty<string>());
        }

        var requiredKeys = parts[..^1];
        var primaryKey = parts[^1];
        return new KeyboardInputChord(string.Join("+", parts), primaryKey, requiredKeys);
    }

    private static IReadOnlyDictionary<string, string> CreateNamedKeyAliases()
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var canonicalName in CanonicalNamedKeys)
        {
            AddAlias(aliases, canonicalName, canonicalName);
        }

        AddAliases(aliases, "LEFT", "LEFTARROW", "ARROWLEFT");
        AddAliases(aliases, "RIGHT", "RIGHTARROW", "ARROWRIGHT");
        AddAliases(aliases, "UP", "UPARROW", "ARROWUP");
        AddAliases(aliases, "DOWN", "DOWNARROW", "ARROWDOWN");
        AddAliases(aliases, "ESC", "ESCAPE");
        AddAliases(aliases, "ENTER", "RETURN");
        AddAliases(aliases, "BACKSPACE", "BACK", "BKSP");
        AddAliases(aliases, "DELETE", "DEL");
        AddAliases(aliases, "INSERT", "INS");
        AddAliases(aliases, "PAGEUP", "PAGE UP", "PAGE-UP", "PGUP", "PRIOR");
        AddAliases(aliases, "PAGEDOWN", "PAGE DOWN", "PAGE-DOWN", "PGDN", "PGDOWN", "NEXT");
        AddAliases(aliases, "SPACE", "SPACEBAR", "SPACE BAR", "SPACE-BAR");
        AddAliases(aliases, "SHIFT", "SHIFTKEY", "SHIFT KEY", "SHIFT-KEY");
        AddAliases(aliases, "CTRL", "CONTROL", "CONTROLKEY", "CONTROL KEY", "CONTROL-KEY", "CTL");
        AddAliases(aliases, "ALT", "ALTKEY", "ALT KEY", "ALT-KEY", "MENU");
        AddAliases(aliases, AnyKeyHandlerName, "ANYKEY", "ANY KEY", "ANY-KEY");

        return aliases;
    }

    private static void AddAliases(Dictionary<string, string> aliases, string canonicalName, params string[] values)
    {
        foreach (var value in values)
        {
            AddAlias(aliases, canonicalName, value);
        }
    }

    private static void AddAlias(Dictionary<string, string> aliases, string canonicalName, string value)
    {
        aliases[CreateLookupKey(value)] = canonicalName;
    }

    private static string CreateLookupKey(string value)
    {
        return value
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
    }
}

public readonly record struct KeyboardInputChord(string CanonicalText, string PrimaryKey, IReadOnlyList<string> RequiredKeys)
{
    public static KeyboardInputChord Empty { get; } = new(string.Empty, string.Empty, Array.Empty<string>());

    public bool IsEmpty => PrimaryKey.Length == 0;

    public bool IsSingleKey => RequiredKeys.Count == 0;
}
