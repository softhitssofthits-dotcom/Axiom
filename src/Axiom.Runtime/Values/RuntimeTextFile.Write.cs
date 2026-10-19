namespace Axiom.Runtime;

public sealed partial class RuntimeTextFile
{
    public void Write(string text, bool appendLineBreak)
    {
        EnsureNotDisposed();

        if (Mode is not RuntimeTextFileMode.Output and not RuntimeTextFileMode.Append)
        {
            throw new InvalidOperationException($"File '{Path}' is not open for writing.");
        }

        if (appendLineBreak) RuntimeStringLimits.Check((long)text.Length + 1);
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

        _writeBuffer.Append(bytes);
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

    private void FlushWriteBuffer()
    {
        EnsureNotDisposed();

        if (_writeBuffer.Length == 0 || _bufferedWriteStartPosition is null)
        {
            return;
        }

        _stream.Seek(_bufferedWriteStartPosition.Value, SeekOrigin.Begin);
        _writeBuffer.CopyTo(_stream);
        _stream.Flush();
        _stream.SetLength(_length);
        _stream.Seek(_position, SeekOrigin.Begin);

        _writeBuffer.Clear();
        _bufferedWriteStartPosition = null;
    }
}
