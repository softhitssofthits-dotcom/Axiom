namespace Axiom.Runtime;

public sealed partial class RuntimeTextFile
{
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

    private long CountLinesBeforePosition(long position)
    {
        EnsureUtf16Alignment();
        var savedPosition = _stream.Position;

        try
        {
            var scanStart = position < _lineScanPosition ? _contentStart : _lineScanPosition;
            _stream.Seek(scanStart, SeekOrigin.Begin);
            var lineNumber = position < _lineScanPosition ? 1L : _lineScanNumber;
            var previousWasCarriageReturn = position >= _lineScanPosition && _lineScanPreviousWasCarriageReturn;

            for (long index = scanStart; index < position; index += IsUtf16 ? 2 : 1)
            {
                if ((index - scanStart) % 4096 == 0) _checkCancellation?.Invoke();
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

            _lineScanPosition = position;
            _lineScanNumber = lineNumber;
            _lineScanPreviousWasCarriageReturn = previousWasCarriageReturn;
            return lineNumber;
        }
        finally
        {
            _stream.Seek(savedPosition, SeekOrigin.Begin);
        }
    }
}
