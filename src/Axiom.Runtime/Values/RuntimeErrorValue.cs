using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeErrorValue
{
    public RuntimeErrorValue(string code, string message, int line, int column)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);

        Code = code;
        Message = message;
        Line = line;
        Column = column;
    }

    public string Code { get; }

    public string Message { get; }

    public int Line { get; }

    public int Column { get; }

    public RuntimeValue GetField(TypeFieldSymbol field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return field.Name.ToUpperInvariant() switch
        {
            "CODE" => RuntimeValue.FromString(Code),
            "MESSAGE" => RuntimeValue.FromString(Message),
            "LINE" => RuntimeValue.FromInteger(Line),
            "COLUMN" => RuntimeValue.FromInteger(Column),
            _ => throw new InvalidOperationException($"Runtime ERROR does not contain field '{field.Name}'.")
        };
    }
}
