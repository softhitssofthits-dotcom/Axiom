using System.Text.RegularExpressions;
using Axiom.Core;

namespace Axiom.Runtime;

// Английский текст значения ERROR формируется независимо от текста диагностики IDE.
internal static partial class RuntimeErrorMessageFormatter
{
    public static string Format(string code, string message)
    {
        var chainStart = message.IndexOf("\nЦепочка вызовов:", StringComparison.Ordinal);
        var body = chainStart < 0 ? message : message[..chainStart].TrimEnd('\r');
        var chain = chainStart < 0 ? "" : message[chainStart..];
        var english = TranslateBody(code, body);
        return english + TranslateChain(chain);
    }

    private static string TranslateBody(string code, string message)
    {
        var input = InputConversion().Match(message);
        if (code == DiagnosticCodes.InputConversion && input.Success)
            return $"Cannot convert input '{input.Groups[1].Value}' to type {input.Groups[2].Value}.";
        if (!Cyrillic().IsMatch(QuotedContext().Replace(message, ""))) return message;
        var parts = Regex.Split(message, "('[^']*')");
        for (var index = 0; index < parts.Length; index += 2)
        {
            parts[index] = parts[index].Replace("Функция ", "Function ", StringComparison.Ordinal)
                .Replace(": аргумент ", ": argument ", StringComparison.Ordinal);
            foreach (var (original, english) in Details)
                parts[index] = parts[index].Replace(original, english, StringComparison.Ordinal);
        }
        var translated = string.Concat(parts);
        if (!Cyrillic().IsMatch(QuotedContext().Replace(translated, ""))) return translated;

        var description = code switch
        {
            DiagnosticCodes.DivisionByZero => "Division by zero.",
            DiagnosticCodes.ArrayBounds => "Array index is out of bounds.",
            DiagnosticCodes.InputConversion => "Input cannot be converted to the required type.",
            DiagnosticCodes.StepZero => "FOR STEP cannot be zero.",
            DiagnosticCodes.InvalidRuntimeState => "Invalid runtime state.",
            DiagnosticCodes.RuntimeMissingReturnValue => "Function completed without returning a value.",
            DiagnosticCodes.BuiltinArgument => "Invalid built-in argument.",
            DiagnosticCodes.NumericOverflow => "Numeric overflow.",
            DiagnosticCodes.EmptyFunctionValue => "Function value is empty.",
            DiagnosticCodes.MissingGraphicsMode => "Required graphics mode is not active.",
            DiagnosticCodes.CallStackOverflow => "Call stack overflow.",
            DiagnosticCodes.NumericDomain => "Argument is outside the numeric domain.",
            _ => "Runtime operation failed."
        };
        // Непереведённый текст внешнего отказа не попадает в языковое значение.
        // Сохраняем имена/пути из сообщения; полная диагностика остаётся у IDE.
        var operation = Operation().Match(translated);
        var context = QuotedContext().Matches(message).Select(match => match.Value).Distinct().ToArray();
        return (operation.Success ? operation.Groups[1].Value + ": " : "") + description +
            (context.Length == 0 ? "" : " Context: " + string.Join(", ", context) + ".");
    }

    private static string TranslateChain(string chain) => chain
        .Replace("Цепочка вызовов:", "Call chain:", StringComparison.Ordinal)
        .Replace("локальная ", "local ", StringComparison.Ordinal)
        .Replace("локальной ", "local ", StringComparison.Ordinal)
        .Replace("процедура '", "procedure '", StringComparison.Ordinal)
        .Replace("функция '", "function '", StringComparison.Ordinal)
        .Replace("процедуры '", "procedure '", StringComparison.Ordinal)
        .Replace("функции '", "function '", StringComparison.Ordinal)
        .Replace(", вызов в ", ", called at ", StringComparison.Ordinal)
        .Replace(", объявлена внутри ", ", declared inside ", StringComparison.Ordinal);

    private static readonly (string Original, string English)[] Details =
    [
        ("Деление на ноль.", "Division by zero."),
        ("Индексы ", "Indices "),
        (" выходят за границы массива ", " are outside the bounds of array "),
        ("Невозможно преобразовать ввод ", "Cannot convert input "),
        (" к типу ", " to type "),
        ("Ожидалось ", "Expected "),
        (" значений INPUT, но получено ", " INPUT values, received "),
        (" требует активного графического режима SCREEN или CANVAS.", " requires active SCREEN or CANVAS graphics mode."),
        (" требует активного графического режима SCREEN.", " requires active SCREEN graphics mode."),
        (" требует активного графического режима CANVAS.", " requires active CANVAS graphics mode."),
        ("Оператор ", "Operator "),
        (" не определён для отрицательного основания и нецелой степени.", " is undefined for a negative base and a noninteger exponent."),
        (" выходит за диапазон ", " is outside the range of "),
        ("Значение функционального типа не содержит функции.", "Function value is empty."),
        ("не должен быть больше аргумента", "must not be greater than argument"),
        ("должен быть больше или равен 0.", "must be greater than or equal to 0."),
        ("должен быть больше 0.", "must be greater than 0."),
        ("не может быть пустой строкой.", "must not be empty."),
        ("не должен быть пустым.", "must not be empty."),
        ("должен быть неотрицательным.", "must be nonnegative."),
        ("должен быть положительным.", "must be positive."),
        ("должен быть целым числом.", "must be an integer."),
        ("должен быть простым именем без пути.", "must be a simple name without a path."),
        ("должен быть простым префиксом имени без пути.", "must be a simple name prefix without a path."),
        ("не содержит родительскую папку.", "has no parent directory."),
        ("не указывает на диск или корень файловой системы.", "does not identify a drive or file system root."),
        ("указывает на файл или папку, которые не существуют.", "identifies a file or directory that does not exist."),
        ("указывает на файл, который не существует.", "identifies a file that does not exist."),
        ("не содержит массива времени выполнения.", "does not contain a runtime array."),
        ("содержит массив, который ещё не выделен.", "contains an array that has not been allocated."),
        ("должен быть открыт для чтения.", "must be open for reading."),
        ("должен быть открыт для записи.", "must be open for writing."),
        ("Папка назначения не может совпадать с исходной папкой или находиться внутри исходной папки.", "The destination directory must not be the source directory or a descendant of the source directory."),
        ("ERROR: код пользовательской ошибки не должен быть пустым.", "ERROR: user error code must not be empty.")
    ];

    [GeneratedRegex("^Невозможно преобразовать ввод '(.*)' к типу ([A-Z]+)\\.$", RegexOptions.Singleline)]
    private static partial Regex InputConversion();
    [GeneratedRegex("[А-Яа-яЁё]")]
    private static partial Regex Cyrillic();
    [GeneratedRegex("'[^']*'")]
    private static partial Regex QuotedContext();
    [GeneratedRegex("^(?:Function )?([A-Z][A-Z0-9]*(?: [A-Z][A-Z0-9]*)*):")]
    private static partial Regex Operation();
}
