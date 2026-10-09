using System.Text;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinTextEncodingHelpers
{
    public static Encoding Resolve(string name)
    {
        return name.Trim().ToUpperInvariant() switch
        {
            "UTF-8" => new UTF8Encoding(false, true),
            "UTF-16LE" => new UnicodeEncoding(false, true, true),
            "UTF-16BE" => new UnicodeEncoding(true, true, true),
            "WINDOWS-1251" => StrictWindows1251(),
            _ => throw new ArgumentException($"Неизвестная кодировка '{name}'. Допустимы UTF-8, WINDOWS-1251, UTF-16LE, UTF-16BE.")
        };
    }

    private static Encoding StrictWindows1251()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1251, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    public static string Read(string path, string name)
    {
        var encoding = Resolve(name);
        var bytes = File.ReadAllBytes(path);
        var start = ContentStart(bytes, encoding);
        return encoding.GetString(bytes, start, bytes.Length - start);
    }

    public static void Write(string path, string text, string name, bool append)
    {
        var encoding = Resolve(name);
        // Кодирование завершено до создания/обнуления файла: ошибка не портит его.
        var payload = encoding.GetBytes(text);
        var preamble = encoding.GetPreamble();
        using var stream = new FileStream(path, append ? FileMode.OpenOrCreate : FileMode.Create,
            FileAccess.ReadWrite, FileShare.Read);
        if (append && stream.Length > 0)
        {
            ValidateStream(stream, encoding);
            stream.Seek(0, SeekOrigin.End);
        }
        else
        {
            stream.Write(preamble);
        }
        stream.Write(payload);
    }

    public static long ValidateStream(FileStream stream, Encoding encoding)
    {
        stream.Position = 0;
        var header = new byte[3];
        var read = stream.ReadAtLeast(header, (int)Math.Min(3, stream.Length), throwOnEndOfStream: false);
        var start = ContentStart(header.AsSpan(0, read), encoding);
        stream.Position = start;
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096, leaveOpen: true);
        var characters = new char[4096];
        while (reader.Read(characters, 0, characters.Length) != 0) { }
        return start;
    }

    private static int ContentStart(ReadOnlySpan<byte> bytes, Encoding encoding)
    {
        // В WINDOWS-1251 эти же байты могут быть обычными буквами («яю»).
        // Явный выбор однобайтовой кодировки запрещает угадывать Unicode BOM.
        if (encoding.CodePage == 1251) return 0;
        var codePage = bytes.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 65001
            : bytes.StartsWith(new byte[] { 0xFF, 0xFE }) ? 1200
            : bytes.StartsWith(new byte[] { 0xFE, 0xFF }) ? 1201 : 0;
        if (codePage == 0) return 0;
        if (codePage != encoding.CodePage)
            throw new ArgumentException("BOM файла не соответствует выбранной кодировке.");
        return codePage == 65001 ? 3 : 2;
    }
}
