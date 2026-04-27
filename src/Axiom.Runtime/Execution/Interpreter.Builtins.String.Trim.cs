namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private static string EvaluateTrimWhitespace(string text) => text.Trim();

    private static string EvaluateTrimCharacters(string text, string chars) => TrimByCharacters(text, chars, trimLeft: true, trimRight: true);

    private static string EvaluateLeftTrimWhitespace(string text) => text.TrimStart();

    private static string EvaluateLeftTrimCharacters(string text, string chars) => TrimByCharacters(text, chars, trimLeft: true, trimRight: false);

    private static string EvaluateRightTrimWhitespace(string text) => text.TrimEnd();

    private static string EvaluateRightTrimCharacters(string text, string chars) => TrimByCharacters(text, chars, trimLeft: false, trimRight: true);
}
