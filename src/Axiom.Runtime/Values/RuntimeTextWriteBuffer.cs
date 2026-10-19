namespace Axiom.Runtime;

/// <summary>Сохраняет видимость FLUSH, ограничивая память для отложенной записи.</summary>
internal sealed class RuntimeTextWriteBuffer : IDisposable
{
    private const int MemoryThreshold = 65_536;
    private Stream _storage = new MemoryStream();

    public long Length => _storage.Length;

    public void Append(byte[] bytes)
    {
        var previousLength = Length;
        if (_storage is MemoryStream && previousLength + bytes.Length > MemoryThreshold)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"axiom-write-{Guid.NewGuid():N}.tmp");
            var disk = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                MemoryThreshold, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
            try
            {
                _storage.Position = 0;
                _storage.CopyTo(disk);
            }
            catch { disk.Dispose(); throw; }
            _storage.Dispose();
            _storage = disk;
        }
        try
        {
            _storage.Position = previousLength;
            _storage.Write(bytes);
        }
        catch
        {
            _storage.SetLength(previousLength);
            throw;
        }
    }

    public void CopyTo(Stream target)
    {
        _storage.Position = 0;
        _storage.CopyTo(target);
    }

    public void Clear()
    {
        _storage.Dispose();
        _storage = new MemoryStream();
    }

    public void Dispose() => _storage.Dispose();
}
