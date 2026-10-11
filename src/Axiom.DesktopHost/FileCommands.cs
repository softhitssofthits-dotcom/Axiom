using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Axiom.DesktopHost;

internal static class FileCommands
{
    public static object Save(JsonElement payload)
    {
        var path = payload.GetProperty("path").GetString();
        var text = payload.GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || text is null)
            throw new ArgumentException("Неверные параметры сохранения файла.");
        var hasExpected = payload.TryGetProperty("expectedHash", out var expectedValue);
        var expected = hasExpected && expectedValue.ValueKind != JsonValueKind.Null ? expectedValue.GetString() : null;
        var exact = payload.TryGetProperty("exact", out var exactValue) && exactValue.GetBoolean();
        var bytes = Encoding.UTF8.GetBytes(text);
        for (;;)
        {
            FileStream stream;
            var created = false;
            try { stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
            catch (FileNotFoundException)
            {
                if (hasExpected && expected is not null) return new { saved = false, conflict = true, missing = true, currentHash = (string?)null };
                try { stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None); created = true; }
                catch (IOException) when (File.Exists(path)) { continue; }
            }
            using (stream)
            {
                using var original = new MemoryStream();
                stream.CopyTo(original);
                var oldBytes = original.ToArray();
                var current = created ? null : Encoding.UTF8.GetString(oldBytes);
                var rawHash = created ? null : Hash(oldBytes);
                var comparedHash = exact || current is null ? rawHash : Hash(Encoding.UTF8.GetBytes(Normalize(current)));
                if (hasExpected && comparedHash != expected) return new { saved = false, conflict = true, missing = created, currentHash = rawHash };
                // Один исключительный дескриптор защищает проверку, запись и изменение длины.
                try
                {
                    stream.Position = 0;
                    stream.Write(bytes);
                    stream.SetLength(bytes.Length);
                    stream.Flush(true);
                }
                catch
                {
                    try { stream.Position = 0; stream.Write(oldBytes); stream.SetLength(oldBytes.Length); stream.Flush(true); }
                    catch { /* Ошибка носителя может не позволить восстановить исходные байты. */ }
                    throw;
                }
                return new { saved = true, conflict = false, missing = false, currentHash = (string?)null };
            }
        }
    }

    private static string Normalize(string text) => (text.StartsWith('\uFEFF') ? text[1..] : text).Replace("\r\n", "\n").Replace('\r', '\n');
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
