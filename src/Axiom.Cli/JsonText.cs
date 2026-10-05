using System.Text;

namespace Axiom.Cli;

/// <summary>Минимальная эскапировка строк для ручной сборки JSON (паттерн XSharp.Cli).</summary>
internal static class JsonText
{
    public static string Escape(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder(text.Length + 8);
        foreach (var character in text)
        {
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(character))
                    {
                        builder.Append("\\u").Append(((int)character).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        return builder.ToString();
    }
}
