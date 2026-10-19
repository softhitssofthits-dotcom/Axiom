using System.Text;

namespace Axiom.Runtime;

internal static class RuntimeStringReplaceMany
{
    public static string Replace(string text, IReadOnlyList<string> oldItems, IReadOnlyList<string> newItems, Action? checkCancellation = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(oldItems);
        ArgumentNullException.ThrowIfNull(newItems);

        if (text.Length == 0 || oldItems.Count == 0)
        {
            return text;
        }

        var root = CreateTrie(oldItems, text.Length, checkCancellation);
        StringBuilder? builder = null;
        var segmentStart = 0;
        var index = 0;

        var iterations = 0;
        while (index < text.Length)
        {
            if ((iterations++ & 1023) == 0) checkCancellation?.Invoke();
            if (TryFindMatch(text, index, root, checkCancellation, out var ruleIndex, out var matchedLength))
            {
                builder ??= new StringBuilder(text.Length);
                RuntimeStringLimits.Check((long)builder.Length + index - segmentStart + newItems[ruleIndex].Length);
                builder.Append(text, segmentStart, index - segmentStart);
                builder.Append(newItems[ruleIndex]);
                index += matchedLength;
                segmentStart = index;
                continue;
            }

            index++;
        }

        if (builder is null)
        {
            return text;
        }

        RuntimeStringLimits.Check((long)builder.Length + text.Length - segmentStart);
        builder.Append(text, segmentStart, text.Length - segmentStart);
        return builder.ToString();
    }

    private sealed class RuleNode
    {
        public Dictionary<char, RuleEdge> Children { get; } = [];
        public int RuleIndex { get; set; } = -1;
    }

    private sealed record RuleEdge(string Text, int Start, int Length, RuleNode Target);

    private static RuleNode CreateTrie(IReadOnlyList<string> oldItems, int maximumMatchLength, Action? checkCancellation)
    {
        var root = new RuleNode();
        for (var index = 0; index < oldItems.Count; index++)
        {
            checkCancellation?.Invoke();
            var pattern = oldItems[index];
            if (pattern.Length > maximumMatchLength) continue;
            var node = root;
            var position = 0;
            while (position < pattern.Length)
            {
                var character = pattern[position];
                if (!node.Children.TryGetValue(character, out var edge))
                {
                    node.Children.Add(character, new RuleEdge(pattern, position, pattern.Length - position, new RuleNode { RuleIndex = index }));
                    break;
                }
                var shared = CommonPrefix(pattern, position, edge, checkCancellation);
                position += shared;
                if (shared == edge.Length)
                {
                    node = edge.Target;
                    continue;
                }
                // Ребро хранит срез исходного правила: число узлов зависит
                // от числа правил, а не от количества символов их префиксов.
                var branch = new RuleNode();
                branch.Children.Add(edge.Text[edge.Start + shared], edge with { Start = edge.Start + shared, Length = edge.Length - shared });
                node.Children[character] = edge with { Length = shared, Target = branch };
                node = branch;
                if (position < pattern.Length)
                {
                    node.Children.Add(pattern[position], new RuleEdge(pattern, position, pattern.Length - position, new RuleNode { RuleIndex = index }));
                    break;
                }
            }
            if (position == pattern.Length && node.RuleIndex < 0) node.RuleIndex = index;
        }
        return root;
    }

    private static int CommonPrefix(string text, int start, RuleEdge edge, Action? checkCancellation)
    {
        var maximum = Math.Min(text.Length - start, edge.Length);
        var length = 0;
        while (length < maximum)
        {
            if ((length & 1023) == 0) checkCancellation?.Invoke();
            if (text[start + length] != edge.Text[edge.Start + length]) break;
            length++;
        }
        return length;
    }

    private static bool TryFindMatch(
        string text,
        int textIndex,
        RuleNode root,
        Action? checkCancellation,
        out int ruleIndex,
        out int matchedLength)
    {
        ruleIndex = -1;
        matchedLength = 0;

        var node = root;
        var index = textIndex;
        while (index < text.Length)
        {
            if (!node.Children.TryGetValue(text[index], out var edge)) break;
            var shared = CommonPrefix(text, index, edge, checkCancellation);
            if (shared != edge.Length) break;
            index += shared;
            node = edge.Target;
            if (node.RuleIndex >= 0)
            {
                ruleIndex = node.RuleIndex;
                matchedLength = index - textIndex;
            }
        }

        return ruleIndex >= 0;
    }
}
