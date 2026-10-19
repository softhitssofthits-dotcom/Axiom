using System.Text;

namespace Axiom.Runtime;

/// <summary>Предел одного строкового значения, измеренный в единицах UTF-16.</summary>
internal static class RuntimeStringLimits
{
    internal const int MaximumLength = 16_777_216;

    internal static int Check(long length)
    {
        if (length < 0 || length > MaximumLength)
            throw new StringResourceLimitException($"Длина строки не должна превышать {MaximumLength} единиц UTF-16.");
        return (int)length;
    }

    internal static string Concat(string left, string right)
    {
        Check((long)left.Length + right.Length);
        return string.Concat(left, right);
    }

    internal static void Append(StringBuilder builder, string text)
    {
        Check((long)builder.Length + text.Length);
        builder.Append(text);
    }

    internal static void Append(StringBuilder builder, ReadOnlySpan<char> text)
    {
        Check((long)builder.Length + text.Length);
        builder.Append(text);
    }

    internal static void CheckEncodedLength(long length)
    {
        if (length > 4L * MaximumLength + 4)
            throw new StringResourceLimitException("Закодированный текст превышает допустимый размер строкового результата.");
    }

    internal static byte[] ReadBytes(string path, Action? checkCancellation = null)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        CheckEncodedLength(stream.Length);
        var bytes = new byte[(int)stream.Length];
        var offset = 0;
        while (offset < bytes.Length)
        {
            checkCancellation?.Invoke();
            var read = stream.Read(bytes, offset, Math.Min(65536, bytes.Length - offset));
            if (read == 0) throw new EndOfStreamException();
            offset += read;
        }
        return bytes;
    }

    internal static string Decode(Encoding encoding, byte[] bytes, int start = 0)
    {
        Check(encoding.GetCharCount(bytes, start, bytes.Length - start));
        return encoding.GetString(bytes, start, bytes.Length - start);
    }
}

internal sealed class StringResourceLimitException(string message) : Exception(message);
