using System.Text;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public enum RuntimeTextFileMode
{
    Input,
    Output,
    Append
}

public sealed class RuntimeTextFile : IDisposable
{
    private readonly Encoding _encoding;
    private readonly Encoding _strictEncoding;
    private readonly FileStream _stream;
    private readonly List<byte> _writeBuffer = new();
    private long? _bufferedWriteStartPosition;
    // Распознанный BOM не является частью текста: пользовательские позиции
    // отсчитываются от его конца, включая явные UTF-16 OUTPUT/APPEND.
    private readonly long _contentStart;
    private long _position;
    private long _length;
    private bool _disposed;

    public RuntimeTextFile(string path, RuntimeTextFileMode mode, Encoding encoding, string? explicitEncodingName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(encoding);

        Path = path;
        Mode = mode;
        _encoding = explicitEncodingName is null ? encoding : BuiltinTextEncodingHelpers.Resolve(explicitEncodingName);
        _strictEncoding = CreateStrictEncoding(_encoding);

        FileStream? openedStream = null;
        try
        {
            switch (mode)
            {
                case RuntimeTextFileMode.Input:
                {
                    if (explicitEncodingName is not null)
                    {
                        _stream = openedStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        _contentStart = BuiltinTextEncodingHelpers.ValidateStream(_stream, _strictEncoding);
                        _stream.Position = _contentStart;
                        break;
                    }
                    // Единый контракт чтения с READFILE: строгий UTF-8 с BOM,
                    // при невалидном UTF-8 — Windows-1251.
                    var (inputEncoding, contentStart) = DetectInputEncoding(path, encoding);
                    _encoding = inputEncoding;
                    _strictEncoding = ReferenceEquals(inputEncoding, encoding)
                        ? _strictEncoding
                        : inputEncoding;
                    _stream = openedStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _contentStart = contentStart;
                    _stream.Seek(_contentStart, SeekOrigin.Begin);
                    break;
                }

                case RuntimeTextFileMode.Output:
                    _stream = openedStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                    if (explicitEncodingName is not null)
                    {
                        var preamble = _encoding.GetPreamble();
                        _stream.Write(preamble);
                        _contentStart = preamble.Length;
                    }
                    break;

                case RuntimeTextFileMode.Append:
                    _stream = openedStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    if (explicitEncodingName is not null)
                    {
                        if (_stream.Length > 0)
                            _contentStart = BuiltinTextEncodingHelpers.ValidateStream(_stream, _strictEncoding);
                        else
                        {
                            var preamble = _encoding.GetPreamble();
                            _stream.Write(preamble);
                            _contentStart = preamble.Length;
                        }
                    }
                    _stream.Seek(0, SeekOrigin.End);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported file mode.");
            }

            _position = _stream.Position;
            _length = _stream.Length;
        }
        catch
        {
            openedStream?.Dispose();
            throw;
        }
    }

    private static Encoding CreateStrictEncoding(Encoding encoding)
    {
        return Encoding.GetEncoding(
            encoding.CodePage,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }

    private static (Encoding Encoding, long ContentStart) DetectInputEncoding(string path, Encoding defaultEncoding)
    {
        using var probe = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

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
        if (IsStrictUtf8(probe))
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

    private static bool IsStrictUtf8(Stream stream)
    {
        var decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetDecoder();
        var buffer = new byte[4096];
        var chars = new char[4096];
        var completed = false;

        try
        {
            while (!completed)
            {
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

    public string Path { get; }

    public RuntimeTextFileMode Mode { get; }

    public bool IsOpen => !_disposed;

    public long Position
    {
        get
        {
            EnsureNotDisposed();
            return _position - _contentStart;
        }
    }

    public long Length
    {
        get
        {
            EnsureNotDisposed();
            return _length - _contentStart;
        }
    }

    public long LineNumber
    {
        get
        {
            EnsureMode(RuntimeTextFileMode.Input);
            return CountLinesBeforePosition(_position);
        }
    }

    public bool IsEndOfFile
    {
        get
        {
            EnsureMode(RuntimeTextFileMode.Input);
            return _position >= _length;
        }
    }

    public string ReadLine()
    {
        EnsureMode(RuntimeTextFileMode.Input);
        return ReadLineCore();
    }

    public IReadOnlyList<string> ReadLines(int count)
    {
        EnsureMode(RuntimeTextFileMode.Input);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
        {
            return Array.Empty<string>();
        }

        var startPosition = _position;
        // Запрошенный предел может быть большим при пустом или коротком файле.
        // Начальная ёмкость не должна зависеть от всего предела чтения.
        var lines = new List<string>(Math.Min(count, 256));

        try
        {
            while (lines.Count < count && _position < _length)
            {
                lines.Add(ReadLineCore());
            }

            return lines;
        }
        catch
        {
            RestoreReadPosition(startPosition);
            throw;
        }
    }

    public string ReadToEnd()
    {
        EnsureMode(RuntimeTextFileMode.Input);
        return ReadDecodedText(maxCharacters: null);
    }

    public string ReadText(int count)
    {
        EnsureMode(RuntimeTextFileMode.Input);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return ReadDecodedText(count);
    }

    public void SkipLine()
    {
        EnsureMode(RuntimeTextFileMode.Input);
        ReadLineCore();
    }

    public void Write(string text, bool appendLineBreak)
    {
        EnsureNotDisposed();

        if (Mode is not RuntimeTextFileMode.Output and not RuntimeTextFileMode.Append)
        {
            throw new InvalidOperationException($"File '{Path}' is not open for writing.");
        }

        var chunk = appendLineBreak ? text + "\n" : text;
        // Каждый вызов кодируется отдельно: FLUSH не должен менять текст
        // или объединять изолированные половины UTF-16 из разных вызовов.
        var bytes = _encoding.GetBytes(chunk);
        var byteCount = bytes.Length;

        if (byteCount == 0)
        {
            return;
        }

        if (_bufferedWriteStartPosition is null)
        {
            _bufferedWriteStartPosition = _position;
        }

        _writeBuffer.AddRange(bytes);
        _position += byteCount;
        _length = Math.Max(_length, _position);
    }

    public void Flush()
    {
        EnsureNotDisposed();

        if (Mode is not RuntimeTextFileMode.Output and not RuntimeTextFileMode.Append)
        {
            throw new InvalidOperationException($"File '{Path}' is not open for writing.");
        }

        FlushWriteBuffer();
    }

    public void Seek(long position)
    {
        EnsureNotDisposed();

        if (Mode == RuntimeTextFileMode.Append)
        {
            throw new InvalidOperationException($"File '{Path}' is open in APPEND mode and does not support explicit positioning.");
        }

        if (position < 0 || position > _length - _contentStart)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position must stay within the file bounds.");
        }

        if (Mode is RuntimeTextFileMode.Output)
        {
            FlushWriteBuffer();
        }

        position += _contentStart;
        _stream.Seek(position, SeekOrigin.Begin);
        _position = position;
    }

    public void Rewind()
    {
        Seek(0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            FlushWriteBuffer();
        }
        finally
        {
            _disposed = true;
            _stream.Dispose();
        }
    }

    private void EnsureMode(RuntimeTextFileMode expectedMode)
    {
        EnsureNotDisposed();

        if (Mode != expectedMode)
        {
            throw new InvalidOperationException($"File '{Path}' is not open in mode {expectedMode}.");
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RuntimeTextFile), $"File '{Path}' is already closed.");
        }
    }

    private void FlushWriteBuffer()
    {
        EnsureNotDisposed();

        if (_writeBuffer.Count == 0 || _bufferedWriteStartPosition is null)
        {
            return;
        }

        var bytes = _writeBuffer.ToArray();
        _stream.Seek(_bufferedWriteStartPosition.Value, SeekOrigin.Begin);
        _stream.Write(bytes, 0, bytes.Length);
        _stream.Flush();
        _stream.SetLength(_length);
        _stream.Seek(_position, SeekOrigin.Begin);

        _writeBuffer.Clear();
        _bufferedWriteStartPosition = null;
    }

    private string ReadLineCore()
    {
        if (IsUtf16) return ReadUtf16LineCore();
        var startPosition = _position;
        var bytes = new List<byte>();

        try
        {
            while (true)
            {
                var next = _stream.ReadByte();

                if (next < 0)
                {
                    break;
                }

                _position++;

                if (next == '\n')
                {
                    break;
                }

                if (next == '\r')
                {
                    var maybeLf = _stream.ReadByte();

                    if (maybeLf == '\n')
                    {
                        _position++;
                    }
                    else if (maybeLf >= 0)
                    {
                        _stream.Seek(-1, SeekOrigin.Current);
                    }

                    break;
                }

                bytes.Add((byte)next);
            }

            return DecodeStrict(bytes.ToArray(), startPosition);
        }
        catch
        {
            RestoreReadPosition(startPosition);
            throw;
        }
    }

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

    private string DecodeStrict(byte[] bytes, long startPosition)
    {
        try
        {
            return _strictEncoding.GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw CreateInvalidTextReadException(startPosition, ex);
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

    private long CountLinesBeforePosition(long position)
    {
        EnsureUtf16Alignment();
        var savedPosition = _stream.Position;

        try
        {
            _stream.Seek(_contentStart, SeekOrigin.Begin);

            var lineNumber = 1L;
            var previousWasCarriageReturn = false;

            for (long index = _contentStart; index < position; index += IsUtf16 ? 2 : 1)
            {
                var next = IsUtf16 ? ReadUtf16Unit() : _stream.ReadByte();

                if (next < 0)
                {
                    break;
                }

                if (next == '\r')
                {
                    lineNumber++;
                    previousWasCarriageReturn = true;
                    continue;
                }

                if (next == '\n')
                {
                    if (!previousWasCarriageReturn)
                    {
                        lineNumber++;
                    }

                    previousWasCarriageReturn = false;
                    continue;
                }

                previousWasCarriageReturn = false;
            }

            return lineNumber;
        }
        finally
        {
            _stream.Seek(savedPosition, SeekOrigin.Begin);
        }
    }

    private bool IsUtf16 => _strictEncoding.CodePage is 1200 or 1201;

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

    private string ReadUtf16LineCore()
    {
        EnsureUtf16Alignment();
        var start = _position;
        var bytes = new List<byte>();
        _stream.Position = start;
        try
        {
            while (true)
            {
                var unit = ReadUtf16Unit();
                if (unit < 0) break;
                _position += 2;
                if (unit == '\n') break;
                if (unit == '\r')
                {
                    var next = ReadUtf16Unit();
                    if (next == '\n') _position += 2;
                    else if (next >= 0) _stream.Seek(-2, SeekOrigin.Current);
                    break;
                }
                if (_strictEncoding.CodePage == 1200)
                {
                    bytes.Add((byte)unit);
                    bytes.Add((byte)(unit >> 8));
                }
                else
                {
                    bytes.Add((byte)(unit >> 8));
                    bytes.Add((byte)unit);
                }
            }
            return DecodeStrict(bytes.ToArray(), start);
        }
        catch
        {
            RestoreReadPosition(start);
            throw;
        }
    }
}
