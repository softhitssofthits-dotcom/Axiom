using System.Text;

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
    private readonly StringBuilder _writeBuffer = new();
    private long? _bufferedWriteStartPosition;
    private long _position;
    private long _length;
    private bool _disposed;

    public RuntimeTextFile(string path, RuntimeTextFileMode mode, Encoding encoding)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(encoding);

        Path = path;
        Mode = mode;
        _encoding = encoding;
        _strictEncoding = Encoding.GetEncoding(
            encoding.CodePage,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);

        switch (mode)
        {
            case RuntimeTextFileMode.Input:
                _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                break;

            case RuntimeTextFileMode.Output:
                _stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                break;

            case RuntimeTextFileMode.Append:
                _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                _stream.Seek(0, SeekOrigin.End);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported file mode.");
        }

        _position = _stream.Position;
        _length = _stream.Length;
    }

    public string Path { get; }

    public RuntimeTextFileMode Mode { get; }

    public bool IsOpen => !_disposed;

    public long Position
    {
        get
        {
            EnsureNotDisposed();
            return _position;
        }
    }

    public long Length
    {
        get
        {
            EnsureNotDisposed();
            return _length;
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
        var lines = new List<string>(count);

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

        var chunk = appendLineBreak ? text + Environment.NewLine : text;
        var byteCount = _encoding.GetByteCount(chunk);

        if (byteCount == 0)
        {
            return;
        }

        if (_bufferedWriteStartPosition is null)
        {
            _bufferedWriteStartPosition = _position;
        }

        _writeBuffer.Append(chunk);
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

        if (position < 0 || position > _length)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position must stay within the file bounds.");
        }

        if (Mode is RuntimeTextFileMode.Output)
        {
            FlushWriteBuffer();
        }

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

        FlushWriteBuffer();
        _disposed = true;
        _stream.Dispose();
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

        if (_writeBuffer.Length == 0 || _bufferedWriteStartPosition is null)
        {
            return;
        }

        var bytes = _encoding.GetBytes(_writeBuffer.ToString());
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

        _stream.Seek(startPosition, SeekOrigin.Begin);

        try
        {
            var builder = new StringBuilder(maxCharacters ?? 256);
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
                        ? Math.Min(charBuffer.Length, Math.Max(remainingCharacters.Value, 1))
                        : charBuffer.Length;

                    int bytesUsed;
                    int charsUsed;

                    try
                    {
                        decoder.Convert(
                            byteBuffer,
                            bufferOffset,
                            bytesRead - bufferOffset,
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
                            remainingCharacters -= charsUsed;
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
                ? Math.Min(charBuffer.Length, Math.Max(remainingCharacters.Value, 1))
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
                        remainingCharacters -= charsUsed;
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

    private InvalidOperationException CreateInvalidTextReadException(long position, Exception innerException)
    {
        return new InvalidOperationException(
            $"Текст с байтовой позиции {position} не удаётся прочитать как UTF-8. Убедитесь, что позиция указывает на корректную границу символа.",
            innerException);
    }

    private void RestoreReadPosition(long position)
    {
        _stream.Seek(position, SeekOrigin.Begin);
        _position = position;
    }

    private long CountLinesBeforePosition(long position)
    {
        var savedPosition = _stream.Position;

        try
        {
            _stream.Seek(0, SeekOrigin.Begin);

            var lineNumber = 1L;
            var previousWasCarriageReturn = false;

            for (long index = 0; index < position; index++)
            {
                var next = _stream.ReadByte();

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
}
