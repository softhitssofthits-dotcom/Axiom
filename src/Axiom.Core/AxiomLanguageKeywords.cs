namespace Axiom.Core;

/// <summary>
/// Единственный источник правды для ключевых слов, типов и встроенных функций языка Axiom.
/// Все новые ключевые слова, типы и встроенные функции должны добавляться сюда.
/// Тесты проверяют Lexer и XSHD-подсветку по этому реестру.
/// </summary>
public static class AxiomLanguageKeywords
{
    /// <summary>
    /// Ключевые слова языка (не включая типы и встроенные функции).
    /// </summary>
    public static readonly IReadOnlySet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AND", "ANDALSO", "APPEND", "AS", "BEGINFRAME", "BYREF", "BYVAL",
        "CAMERA", "CANVAS", "CASE", "CATCH", "CIRCLE", "CLOSE", "CLS", "COLOR",
        "CONST", "CONTINUE",
        "DIM", "DO", "DRAW", "DRAWCIRCLE", "DRAWIMAGE", "DRAWLINE", "DRAWRECT",
        "DRAWSPRITE", "DRAWTILE", "DRAWTILELAYER", "DRAWTILEMAP", "DRAWTEXT",
        "EACH",
        "ELSE", "ELSEIF", "END", "ERROR", "EQV", "ERASE", "EXIT",
        "FALSE", "TRUE",
        "FILLCIRCLE", "FILLRECT", "FINALLY", "FOR", "FUNCTION",
        "GET",
        "IF", "IMP", "IMPORT", "IN", "INPUT", "IS",
        "KEY",
        "LINE", "LOCATE", "LOOP",
        "MOD", "MOUSE",
        "NEXT", "NOT",
        "OFF", "ON", "OPEN", "ORELSE", "OR", "OUTPUT",
        "PAINT", "PALETTE", "PCOPY", "PRESERVE", "PRESENT", "PRESET", "PRINT", "PRIVATE", "PUBLIC", "RANDOMIZE", "PSET", "PUT",
        "REDIM", "RESETCAMERA", "RETURN",
        "SCREEN", "SELECT", "SELF", "SETMAPTILE", "SMOOTH", "SLEEP", "STEP", "STOP", "SUB",
        "THEN", "TO", "TRANSPARENT", "TRY", "TYPE",
        "UNTIL", "USING",
        "VIEW", "VIEWPORT",
        "WHILE", "WINDOW",
        "XOR"
    };

    /// <summary>
    /// Имена типов данных.
    /// </summary>
    public static readonly IReadOnlySet<string> TypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BOOLEAN", "DATE", "DATETIME", "DECIMAL", "DOUBLE", "FILE", "IMAGE", "INTEGER", "LONG", "SPRITE", "STRING", "TILEMAP", "TILESET", "TIME", "UTCDATETIME"
    };

    /// <summary>
    /// Все ключевые слова и типы вместе (для проверки лексера).
    /// </summary>
    public static readonly IReadOnlySet<string> AllKeywordsAndTypes;
    
    /// <summary>
    /// Возвращает все слова, которые должны быть в XSHD-файле подсветки (ключевые слова + типы).
    /// Встроенные функции НЕ включены, так как они подсвечиваются отдельной категорией.
    /// </summary>
    public static readonly IReadOnlySet<string> AllHighlightKeywords;

    static AxiomLanguageKeywords()
    {
        var all = new HashSet<string>(Keywords, StringComparer.OrdinalIgnoreCase);
        foreach (var type in TypeNames)
        {
            all.Add(type);
        }
        AllKeywordsAndTypes = all;
        AllHighlightKeywords = all;
    }
}
