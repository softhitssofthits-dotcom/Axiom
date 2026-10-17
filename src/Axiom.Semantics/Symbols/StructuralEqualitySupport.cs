namespace Axiom.Semantics;

/// <summary>
/// Определяет, что значения типа допускают структурное сравнение операторами = и &lt;&gt;.
/// Обход графа полей устойчив к ошибочным циклическим объявлениям и запоминает
/// уже проверенные типы, поэтому повторяющиеся подграфы не обходятся заново.
/// </summary>
public static class StructuralEqualitySupport
{
    public static bool IsSupported(TypeSymbol type)
    {
        return IsSupported(type, cache: null);
    }

    public static bool IsSupported(TypeSymbol type, Dictionary<UserDefinedTypeSymbol, bool>? cache)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (IsScalar(type))
        {
            return true;
        }

        if (type is not UserDefinedTypeSymbol root)
        {
            return false;
        }

        cache ??= [];

        if (cache.TryGetValue(root, out var cached))
        {
            return cached;
        }

        var supported = Visit(root, cache);
        cache[root] = supported;
        return supported;
    }

    private static bool IsScalar(TypeSymbol type)
    {
        return type == TypeSymbol.Integer
            || type == TypeSymbol.Long
            || type == TypeSymbol.Double
            || type == TypeSymbol.Decimal
            || type == TypeSymbol.String
            || type == TypeSymbol.Boolean
            || type == TypeSymbol.Date
            || type == TypeSymbol.Time
            || type == TypeSymbol.DateTime;
    }

    private static bool Visit(UserDefinedTypeSymbol root, Dictionary<UserDefinedTypeSymbol, bool> cache)
    {
        var visiting = new HashSet<UserDefinedTypeSymbol>();
        var stack = new List<Frame> { new(root) };
        visiting.Add(root);

        while (stack.Count > 0)
        {
            var frame = stack[^1];

            if (frame.FieldIndex >= frame.Type.Fields.Count)
            {
                stack.RemoveAt(stack.Count - 1);
                visiting.Remove(frame.Type);
                cache[frame.Type] = true;
                continue;
            }

            var fieldType = frame.Type.Fields[frame.FieldIndex++].Type;

            if (IsScalar(fieldType))
            {
                continue;
            }

            if (fieldType is not UserDefinedTypeSymbol nested)
            {
                return false;
            }

            if (cache.TryGetValue(nested, out var nestedSupported))
            {
                if (!nestedSupported)
                {
                    return false;
                }

                continue;
            }

            // Повторный вход в тип, который ещё обходится, означает цикл полей.
            // Такое объявление уже отклонено отдельной диагностикой, поэтому
            // сравнение значений этого типа не поддерживается.
            if (!visiting.Add(nested))
            {
                return false;
            }

            stack.Add(new Frame(nested));
        }

        return true;
    }

    private sealed class Frame(UserDefinedTypeSymbol type)
    {
        public UserDefinedTypeSymbol Type { get; } = type;

        public int FieldIndex { get; set; }
    }
}
