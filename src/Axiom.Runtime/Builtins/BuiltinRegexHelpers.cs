using System.Text;
using System.Text.RegularExpressions;
using Axiom.Core;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinRegexHelpers
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static RegexOptions ParseRegexModes(string modes, TextLocation location, string functionName,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        var options = RegexOptions.CultureInvariant;
        foreach (var mode in modes)
        {
            switch (char.ToLowerInvariant(mode))
            {
                case 'i': options |= RegexOptions.IgnoreCase; break;
                case 'm': options |= RegexOptions.Multiline; break;
                case 's': options |= RegexOptions.Singleline; break;
                case 'x': options |= RegexOptions.IgnorePatternWhitespace; break;
                default:
                    reportArgumentError(functionName, "modes", $"Unknown mode '{mode}'. Allowed modes: i, m, s, x.", location);
                    throw new RuntimeFailureException();
            }
        }
        return options;
    }

    public static Regex CreateRegex(string pattern, RegexOptions options, TextLocation location, string functionName,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        try { return new Regex(pattern, options, DefaultTimeout); }
        catch (RegexParseException error)
        {
            reportArgumentError(functionName, "pattern", $"Invalid pattern at offset {error.Offset} ({error.Error}).", location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue IsMatch(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> report, string name, Action checkCancellation) =>
        Search(() => RuntimeValue.FromBoolean(regex.IsMatch(text)), location, report, name, checkCancellation);

    public static RuntimeValue Match(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> report, string name, Action checkCancellation) =>
        Search(() => RuntimeValue.FromString(regex.Match(text).Value), location, report, name, checkCancellation);

    public static RuntimeValue Extract(Regex regex, string text, int group, TextLocation location,
        Action<string, string, string, TextLocation> report, string name, Action checkCancellation) =>
        Search(() => RuntimeValue.FromString(regex.Match(text).Groups[group].Value), location, report, name, checkCancellation);

    public static RuntimeValue Matches(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> report, string name,
        Func<IReadOnlyList<RuntimeValue>, RuntimeValue> createArray, Action checkCancellation) =>
        Search(() =>
        {
            var values = new List<RuntimeValue>();
            foreach (var match in regex.EnumerateMatches(text.AsSpan()))
            {
                checkCancellation();
                values.Add(RuntimeValue.FromString(text.Substring(match.Index, match.Length)));
            }
            return createArray(values);
        }, location, report, name, checkCancellation);

    public static RuntimeValue ExtractAll(Regex regex, string text, int group, TextLocation location,
        Action<string, string, string, TextLocation> report, string name,
        Func<IReadOnlyList<RuntimeValue>, RuntimeValue> createArray, Action checkCancellation) =>
        Search(() =>
        {
            var values = new List<RuntimeValue>();
            for (var match = regex.Match(text); match.Success; match = match.NextMatch())
            {
                checkCancellation();
                values.Add(RuntimeValue.FromString(match.Groups[group].Value));
            }
            return createArray(values);
        }, location, report, name, checkCancellation);

    public static RuntimeValue Split(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> report, string name,
        Func<IReadOnlyList<RuntimeValue>, RuntimeValue> createArray, Action checkCancellation) =>
        Search(() =>
        {
            var values = new List<RuntimeValue>();
            if (text.Length == 0) return createArray(values);
            var groupNumbers = regex.GetGroupNumbers();
            var position = 0;
            for (var match = regex.Match(text); match.Success; match = match.NextMatch())
            {
                checkCancellation();
                values.Add(RuntimeValue.FromString(text.Substring(position, match.Index - position)));
                position = match.Index + match.Length;
                // Regex.Split проверяет плотные слоты захватов, но читает группу по номеру.
                // Сохраняем это поведение и для явно заданных разреженных номеров.
                for (var slot = 1; slot < groupNumbers.Length; slot++)
                {
                    checkCancellation();
                    if (match.Groups[groupNumbers[slot]].Success)
                        values.Add(RuntimeValue.FromString(match.Groups[slot].Value));
                }
            }
            values.Add(RuntimeValue.FromString(text[position..]));
            return createArray(values);
        }, location, report, name, checkCancellation);

    public static string Replace(Regex regex, string text, string replacement, int? count, TextLocation location,
        Action<string, string, string, TextLocation> report, string name, Action checkCancellation) =>
        Search(() =>
        {
            if (replacement.Contains('$'))
            {
                StringBuilder? expanded = null;
                var previousEnd = 0;
                var remainingMatches = count ?? int.MaxValue;
                for (var match = regex.Match(text); match.Success; match = match.NextMatch())
                {
                    checkCancellation();
                    expanded ??= new StringBuilder();
                    RuntimeStringLimits.Append(expanded, text.AsSpan(previousEnd, match.Index - previousEnd));
                    AppendReplacement(expanded, match, replacement, checkCancellation);
                    previousEnd = match.Index + match.Length;
                    if (--remainingMatches == 0) break;
                }
                if (expanded is null) return text;
                RuntimeStringLimits.Append(expanded, text.AsSpan(previousEnd));
                return expanded.ToString();
            }
            StringBuilder? result = null;
            var position = 0;
            var remaining = count ?? int.MaxValue;
            foreach (var match in regex.EnumerateMatches(text.AsSpan()))
            {
                checkCancellation();
                if (result is null && text.AsSpan(match.Index, match.Length).SequenceEqual(replacement.AsSpan()))
                {
                    // До первой фактической правки результат уже содержится в исходной строке.
                }
                else
                {
                    if (result is null)
                    {
                        result = new StringBuilder();
                        RuntimeStringLimits.Append(result, text.AsSpan(0, position));
                    }
                    RuntimeStringLimits.Append(result, text.AsSpan(position, match.Index - position));
                    RuntimeStringLimits.Append(result, replacement);
                }
                position = match.Index + match.Length;
                if (--remaining == 0) break;
            }
            if (result is null) return text;
            RuntimeStringLimits.Append(result, text.AsSpan(position));
            return result.ToString();
        }, location, report, name, checkCancellation);

    private static void AppendReplacement(StringBuilder builder, Match match, string replacement, Action checkCancellation)
    {
        var position = 0;
        while (position < replacement.Length)
        {
            checkCancellation();
            var dollar = replacement.IndexOf('$', position);
            if (dollar < 0)
            {
                RuntimeStringLimits.Append(builder, replacement.AsSpan(position));
                break;
            }
            RuntimeStringLimits.Append(builder, replacement.AsSpan(position, dollar - position));
            var end = dollar + 1;
            if (end < replacement.Length)
            {
                if (replacement[end] == '{')
                {
                    var close = replacement.IndexOf('}', end + 1);
                    if (close >= 0 && replacement.AsSpan(end + 1, close - end - 1).IndexOf('$') < 0) end = close + 1;
                }
                else if (char.IsAsciiDigit(replacement[end]))
                {
                    while (end < replacement.Length && char.IsAsciiDigit(replacement[end])) end++;
                }
                else end++;
            }
            // Значение каждой отдельной подстановки определяет сам .NET.
            // Между подстановками проверяем суммарный размер до копирования.
            var token = replacement[dollar..end];
            var repetitions = 1;
            var nextPosition = end;
            while (nextPosition < replacement.Length && replacement.AsSpan(nextPosition).StartsWith(token, StringComparison.Ordinal))
            {
                if ((repetitions & 1023) == 0) checkCancellation();
                repetitions++;
                nextPosition += token.Length;
            }
            var expanded = match.Result(token);
            RuntimeStringLimits.Check(builder.Length + (long)expanded.Length * repetitions);
            for (var repeat = 0; repeat < repetitions; repeat++)
            {
                if ((repeat & 1023) == 0) checkCancellation();
                builder.Append(expanded);
            }
            position = nextPosition;
        }
    }

    public static int Count(Regex regex, string text, TextLocation location,
        Action<string, string, string, TextLocation> report, string name, Action checkCancellation) =>
        Search(() =>
        {
            var count = 0;
            foreach (var match in regex.EnumerateMatches(text.AsSpan()))
            {
                checkCancellation();
                count++;
            }
            return count;
        }, location, report, name, checkCancellation);

    private static T Search<T>(Func<T> operation, TextLocation location,
        Action<string, string, string, TextLocation> report, string name, Action checkCancellation)
    {
        checkCancellation();
        try
        {
            var result = operation();
            checkCancellation();
            return result;
        }
        catch (RegexMatchTimeoutException)
        {
            checkCancellation();
            report(name, "pattern", "Pattern matching exceeded the 5 second timeout.", location);
            throw new RuntimeFailureException();
        }
        catch (ArgumentException)
        {
            report(name, "replacement", "Invalid replacement expression.", location);
            throw new RuntimeFailureException();
        }
    }
}
