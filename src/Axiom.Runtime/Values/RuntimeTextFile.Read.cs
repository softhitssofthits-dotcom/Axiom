using System.Text;

namespace Axiom.Runtime;

public sealed partial class RuntimeTextFile
{
    private string ReadDecodedText(int? maxCharacters)
    {
        var startPosition = _position;

        if (maxCharacters is 0 || startPosition >= _length)
        {
            return string.Empty;
        }

        EnsureUtf16Alignment();
        _stream.Seek(startPosition, SeekOrigin.Begin);

        try
        {
            // Предел чтения не требует выделения памяти для отсутствующего текста.
            var builder = new StringBuilder(Math.Min(maxCharacters ?? 256, 256));
            var decoder = _strictEncoding.GetDecoder();
            var byteBuffer = new byte[256];
            var charBuffer = new char[256];
            var consumedBytes = 0L;
            var remainingCharacters = maxCharacters;

            while (startPosition + consumedBytes < _length)
            {
                _checkCancellation?.Invoke();
                var remainingFileBytes = _length - (startPosition + consumedBytes);
                var bytesToRead = (int)Math.Min(byteBuffer.Length, remainingFileBytes);
                var bytesRead = _stream.Read(byteBuffer, 0, bytesToRead);

                if (bytesRead == 0)
                {
                    break;
                }

                var bufferOffset = 0;

                while (bufferOffset < bytesRead)
                {
                    if (remainingCharacters == 0)
                    {
                        _stream.Seek(bufferOffset - bytesRead, SeekOrigin.Current);
                        _position = startPosition + consumedBytes;
                        return builder.ToString();
                    }

                    var charCapacity = remainingCharacters.HasValue
                        ? Math.Min(charBuffer.Length, Math.Max(remainingCharacters.Value, 2))
                        : charBuffer.Length;
                    // Последний символ может занять два места UTF-16. Подача
                    // по одному байту не позволяет захватить следующий символ.
                    var bytesToConvert = remainingCharacters == 1
                        ? 1
                        : bytesRead - bufferOffset;

                    int bytesUsed;
                    int charsUsed;

                    try
                    {
                        decoder.Convert(
                            byteBuffer,
                            bufferOffset,
                            bytesToConvert,
                            charBuffer,
                            0,
                            charCapacity,
                            flush: false,
                            out bytesUsed,
                            out charsUsed,
                            out _);
                    }
                    catch (DecoderFallbackException ex)
                    {
                        throw CreateInvalidTextReadException(startPosition + consumedBytes, ex);
                    }

                    if (bytesUsed == 0 && charsUsed == 0)
                    {
                        break;
                    }

                    if (charsUsed > 0)
                    {
                        RuntimeStringLimits.Check((long)builder.Length + charsUsed);
                        builder.Append(charBuffer, 0, charsUsed);

                        if (remainingCharacters.HasValue)
                        {
                            remainingCharacters -= CountUnicodeScalars(charBuffer, charsUsed);
                        }
                    }

                    bufferOffset += bytesUsed;
                    consumedBytes += bytesUsed;
                }

                if (bufferOffset < bytesRead)
                {
                    _stream.Seek(bufferOffset - bytesRead, SeekOrigin.Current);
                }
            }

            FlushDecoder(decoder, builder, charBuffer, ref remainingCharacters);
            _position = startPosition + consumedBytes;
            _stream.Seek(_position, SeekOrigin.Begin);
            return builder.ToString();
        }
        catch
        {
            RestoreReadPosition(startPosition);
            throw;
        }
    }

    private void FlushDecoder(Decoder decoder, StringBuilder builder, char[] charBuffer, ref int? remainingCharacters)
    {
        while (true)
        {
            if (remainingCharacters == 0)
            {
                return;
            }

            var charCapacity = remainingCharacters.HasValue
                ? Math.Min(charBuffer.Length, Math.Max(remainingCharacters.Value, 2))
                : charBuffer.Length;

            try
            {
                decoder.Convert(
                    Array.Empty<byte>(),
                    0,
                    0,
                    charBuffer,
                    0,
                    charCapacity,
                    flush: true,
                    out _,
                    out var charsUsed,
                    out var completed);

                if (charsUsed > 0)
                {
                    RuntimeStringLimits.Check((long)builder.Length + charsUsed);
                    builder.Append(charBuffer, 0, charsUsed);

                    if (remainingCharacters.HasValue)
                    {
                        remainingCharacters -= CountUnicodeScalars(charBuffer, charsUsed);
                    }
                }

                if (completed || charsUsed == 0)
                {
                    return;
                }
            }
            catch (DecoderFallbackException ex)
            {
                throw CreateInvalidTextReadException(_position, ex);
            }
        }
    }

    private static int CountUnicodeScalars(char[] buffer, int length)
    {
        var count = 0;
        foreach (var rune in buffer.AsSpan(0, length).EnumerateRunes())
        {
            count++;
        }

        return count;
    }

    private InvalidOperationException CreateInvalidTextReadException(long position, Exception innerException)
    {
        var encodingName = _strictEncoding.CodePage switch
        {
            65001 => "UTF-8",
            1251 => "WINDOWS-1251",
            1200 => "UTF-16LE",
            1201 => "UTF-16BE",
            _ => _strictEncoding.WebName
        };
        return new InvalidOperationException(
            $"Текст с байтовой позиции {position} не удаётся прочитать как {encodingName}. Убедитесь, что позиция указывает на корректную границу символа.",
            innerException);
    }

    private void RestoreReadPosition(long position)
    {
        _stream.Seek(position, SeekOrigin.Begin);
        _position = position;
    }

}
