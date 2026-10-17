namespace Axiom.Runtime;

/// <summary>
/// Отказ выделения массива: превышен предел числа элементов, произошло
/// переполнение формы или среде не хватило памяти. Исключение несёт готовый
/// текст диагностики; BytecodeVm сообщает его как MB3013 и не завершает процесс.
/// </summary>
internal sealed class ArrayAllocationFailureException : Exception
{
    public ArrayAllocationFailureException(string message)
        : base(message)
    {
    }
}
