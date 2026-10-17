namespace Axiom.Runtime;

/// <summary>
/// Обращение к представлению, чьё исходное хранилище было заменено или
/// освобождено операторами REDIM либо ERASE. Такой подмассив больше не связан
/// с текущими элементами корня, поэтому чтение и запись запрещены.
/// </summary>
internal sealed class ExpiredArrayViewException : Exception;
