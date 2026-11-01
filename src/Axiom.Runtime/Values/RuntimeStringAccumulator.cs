using System.Text;

namespace Axiom.Runtime;

internal sealed class RuntimeStringAccumulator
{
    private readonly StringBuilder _builder;
    private string? _cachedText;

    public RuntimeStringAccumulator(string initialText)
    {
        ArgumentNullException.ThrowIfNull(initialText);
        _builder = new StringBuilder(initialText);
    }

    /// <summary>
    /// Кэш обёртки значения: добавления переиспользуют один экземпляр RuntimeValue.
    /// Любая передача на хранение материализует независимую строку через CopyForStorage,
    /// поэтому общий доступ к кэшу не создаёт синонимов аккумулятора.
    /// </summary>
    internal RuntimeValue? CachedWrapper { get; set; }

    public void Append(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        RuntimeStringLimits.Append(_builder, text);
        _cachedText = null;
    }

    public override string ToString()
    {
        return _cachedText ??= _builder.ToString();
    }
}
