using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Axiom.Syntax;

namespace Axiom.Semantics;

/// <summary>
/// Общий обход контейнеров связанного дерева. Один набор правил используется
/// редакторскими проекциями семантического слоя и Host, чтобы новая форма узла
/// не получала разное покрытие в двух независимых отражениях.
/// </summary>
public static class BoundTreeTraversal
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();

    /// <summary>
    /// Скомпилированные геттеры публичных свойств каждого типа контейнера:
    /// правило отбора свойств задаётся тем же отражением один раз на тип,
    /// а чтение значения выполняется как обычный вызов без рефлексии.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Func<object, object?>[]> CompiledGetters = new();

    /// <summary>
    /// Обходит <paramref name="root"/> и возвращает каждый объект не более одного
    /// раза. Родитель всегда выдаётся раньше своего потомка, поэтому потребитель
    /// успевает запомнить уже описанные узлы до повторной встречи.
    /// </summary>
    /// <remarks>
    /// Детьми дерева считаются элементы последовательностей и значения публичных
    /// свойств без индексаторов у узлов Bound* и у синтаксических узлов. Символы,
    /// типы и значения выдаются, но дальше не раскрываются.
    /// </remarks>
    public static IEnumerable<object> Descendants(object root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var pending = new Stack<object>();
        pending.Push(root);
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        while (pending.TryPop(out var value))
        {
            if (!visited.Add(value)) continue;
            yield return value;
            if (value is IEnumerable sequence && value is not string)
            {
                foreach (var child in sequence)
                {
                    if (child is not null) pending.Push(child);
                }

                continue;
            }

            var type = value.GetType();
            if (!IsTreeContainer(type)) continue;
            foreach (var getter in CompiledGetters.GetOrAdd(type, CompilePropertyGetters))
            {
                if (getter(value) is { } child && child is not string && !child.GetType().IsValueType)
                {
                    pending.Push(child);
                }
            }
        }
    }

    private static Func<object, object?>[] CompilePropertyGetters(Type type)
    {
        var properties = Properties.GetOrAdd(
            type,
            static t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(static p => p.GetIndexParameters().Length == 0).ToArray());

        var getters = new Func<object, object?>[properties.Length];
        for (var index = 0; index < properties.Length; index++)
        {
            var property = properties[index];
            var target = Expression.Parameter(typeof(object), "node");
            var body = Expression.Convert(
                Expression.Property(Expression.Convert(target, type), property),
                typeof(object));
            getters[index] = Expression.Lambda<Func<object, object?>>(body, target).Compile();
        }

        return getters;
    }

    private static bool IsTreeContainer(Type type)
    {
        if (typeof(SyntaxNode).IsAssignableFrom(type)) return true;
        return type.Namespace == typeof(BoundNode).Namespace &&
            type.Name.StartsWith("Bound", StringComparison.Ordinal);
    }
}
