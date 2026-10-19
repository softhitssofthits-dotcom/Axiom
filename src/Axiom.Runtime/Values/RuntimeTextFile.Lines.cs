using System.Text;

namespace Axiom.Runtime;

public sealed partial class RuntimeTextFile
{
    private byte[]? _lineReadBuffer;
    private char[]? _lineCharacters;
    private long _lineReadBufferStart = -1;
    private int _lineReadBufferLength;

    private string ReadLineCore(bool skip = false)
    {
        EnsureUtf16Alignment();
        var start = _position;
        var bytes = _lineReadBuffer ??= new byte[4096];
        var characters = _lineCharacters ??= new char[4098];
        var decoder = _strictEncoding.GetDecoder();
        var result = skip ? null : new StringBuilder();
        var unitSize = IsUtf16 ? 2 : 1;
        long consumed = 0;
        try
        {
            while (true)
            {
                _checkCancellation?.Invoke();
                var absolutePosition = start + consumed;
                if (absolutePosition < _lineReadBufferStart || absolutePosition >= _lineReadBufferStart + _lineReadBufferLength)
                {
                    _stream.Position = absolutePosition;
                    _lineReadBufferStart = absolutePosition;
                    // Не оставлять прежний блок действительным, если новое чтение откажет.
                    _lineReadBufferLength = 0;
                    _lineReadBufferLength = _stream.ReadAtLeast(bytes, bytes.Length, throwOnEndOfStream: false);
                }
                var bufferOffset = (int)(absolutePosition - _lineReadBufferStart);
                var count = _lineReadBufferLength - bufferOffset;
                if (count % unitSize != 0)
                    throw new InvalidOperationException("UTF-16-файл заканчивается неполной двухбайтовой единицей.");
                var end = count;
                var terminator = -1;
                for (var offset = 0; offset < count; offset += unitSize)
                {
                    var index = bufferOffset + offset;
                    var value = unitSize == 1 ? bytes[index]
                        : _strictEncoding.CodePage == 1200 ? bytes[index] | (bytes[index + 1] << 8)
                        : (bytes[index] << 8) | bytes[index + 1];
                    if (value is not '\r' and not '\n') continue;
                    end = offset;
                    terminator = value;
                    break;
                }

                var written = decoder.GetChars(bytes.AsSpan(bufferOffset, end), characters, flush: terminator >= 0 || count == 0);
                if (result is not null) RuntimeStringLimits.Append(result, characters.AsSpan(0, written));
                consumed += end;
                if (terminator >= 0)
                {
                    consumed += unitSize;
                    _stream.Position = start + consumed;
                    if (terminator == '\r')
                    {
                        var next = IsUtf16 ? ReadUtf16Unit() : _stream.ReadByte();
                        if (next == '\n') consumed += unitSize;
                    }
                }
                if (terminator >= 0 || count == 0)
                {
                    _position = start + consumed;
                    _stream.Position = _position;
                    return result?.ToString() ?? string.Empty;
                }
            }
        }
        catch (DecoderFallbackException error)
        {
            RestoreReadPosition(start);
            throw CreateInvalidTextReadException(start, error);
        }
        catch
        {
            RestoreReadPosition(start);
            throw;
        }
    }
}
