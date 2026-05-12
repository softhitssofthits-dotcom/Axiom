using System.Text;

namespace Axiom.Runtime;

internal static class RuntimeStringReplaceMany
{
    public static string Replace(string text, IReadOnlyList<string> oldItems, IReadOnlyList<string> newItems)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(oldItems);
        ArgumentNullException.ThrowIfNull(newItems);

        if (text.Length == 0 || oldItems.Count == 0)
        {
            return text;
        }

        var buckets = CreateBuckets(oldItems);
        StringBuilder? builder = null;
        var segmentStart = 0;
        var index = 0;

        while (index < text.Length)
        {
            if (TryFindMatch(text, index, oldItems, buckets, out var ruleIndex, out var matchedLength))
            {
                builder ??= new StringBuilder(text.Length);
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

        builder.Append(text, segmentStart, text.Length - segmentStart);
        return builder.ToString();
    }

    private static Dictionary<char, List<int>> CreateBuckets(IReadOnlyList<string> oldItems)
    {
        var buckets = new Dictionary<char, List<int>>();

        for (var index = 0; index < oldItems.Count; index++)
        {
            var oldText = oldItems[index];
            var firstChar = oldText[0];

            if (!buckets.TryGetValue(firstChar, out var bucket))
            {
                bucket = [];
                buckets.Add(firstChar, bucket);
            }

            bucket.Add(index);
        }

        return buckets;
    }

    private static bool TryFindMatch(
        string text,
        int textIndex,
        IReadOnlyList<string> oldItems,
        IReadOnlyDictionary<char, List<int>> buckets,
        out int ruleIndex,
        out int matchedLength)
    {
        ruleIndex = -1;
        matchedLength = 0;

        if (!buckets.TryGetValue(text[textIndex], out var candidates))
        {
            return false;
        }

        foreach (var candidateIndex in candidates)
        {
            var oldText = oldItems[candidateIndex];
            if (oldText.Length <= matchedLength || textIndex + oldText.Length > text.Length)
            {
                continue;
            }

            if (string.CompareOrdinal(text, textIndex, oldText, 0, oldText.Length) != 0)
            {
                continue;
            }

            ruleIndex = candidateIndex;
            matchedLength = oldText.Length;
        }

        return ruleIndex >= 0;
    }
}
