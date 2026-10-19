using System.Text;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class RuntimeTextFile
{
    private static Encoding CreateStrictEncoding(Encoding encoding)
    {
        return Encoding.GetEncoding(
            encoding.CodePage,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }

    private static (Encoding Encoding, long ContentStart) DetectInputEncoding(FileStream probe, Encoding defaultEncoding, Action? checkCancellation)
    {

        var header = new byte[3];
        var contentStart = 0;
        var headerLength = ReadUpTo(probe, header);

        if (headerLength == header.Length
            && header[0] == 0xEF
            && header[1] == 0xBB
            && header[2] == 0xBF)
        {
            contentStart = header.Length;
        }

        // Без BOM первые три байта также являются текстом и должны
        // участвовать в проверке, включая короткие файлы.
        probe.Seek(contentStart, SeekOrigin.Begin);
        if (IsStrictUtf8(probe, checkCancellation))
        {
            return (defaultEncoding, contentStart);
        }

        return (BuiltinFileSystemHelpers.Windows1251TextEncoding, 0);
    }

    private static int ReadUpTo(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }

    private static bool IsStrictUtf8(Stream stream, Action? checkCancellation)
    {
        var decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetDecoder();
        var buffer = new byte[4096];
        var chars = new char[4096];
        var completed = false;

        try
        {
            while (!completed)
            {
                checkCancellation?.Invoke();
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var flush = bytesRead == 0;
                decoder.Convert(
                    buffer,
                    0,
                    bytesRead,
                    chars,
                    0,
                    chars.Length,
                    flush,
                    out _,
                    out _,
                    out _);
                completed = flush;
            }

            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private void EnsureUtf16Alignment()
    {
        if (IsUtf16 && (_position - _contentStart) % 2 != 0)
            throw new InvalidOperationException("Позиция UTF-16-файла должна указывать на границу двухбайтовой единицы.");
    }

    private int ReadUtf16Unit()
    {
        var first = _stream.ReadByte();
        if (first < 0) return -1;
        var second = _stream.ReadByte();
        if (second < 0) throw new InvalidOperationException("UTF-16-файл заканчивается неполной двухбайтовой единицей.");
        return _strictEncoding.CodePage == 1200 ? first | (second << 8) : (first << 8) | second;
    }
}
