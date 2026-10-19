using System.Text;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public enum RuntimeTextFileMode
{
    Input,
    Output,
    Append
}

public sealed partial class RuntimeTextFile : IDisposable
{
    private readonly Encoding _encoding;
    private readonly Encoding _strictEncoding;
    private readonly FileStream _stream;
    private readonly RuntimeTextWriteBuffer _writeBuffer = new();
    private long _lineScanPosition;
    private long _lineScanNumber = 1;
    private bool _lineScanPreviousWasCarriageReturn;
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
            _lineScanPosition = _contentStart;
        }
        catch
        {
            openedStream?.Dispose();
            throw;
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
            _writeBuffer.Dispose();
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

    private bool IsUtf16 => _strictEncoding.CodePage is 1200 or 1201;

}
