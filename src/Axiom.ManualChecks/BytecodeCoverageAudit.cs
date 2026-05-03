using System.Text;
using System.Text.RegularExpressions;
using Axiom.Runtime;
using Axiom.Semantics;

internal static class BytecodeCoverageAudit
{
    private static readonly Regex TitleRegex = new(
        "title:\\s*\"(?<title>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static int Run(string root, string outputPath)
    {
        var declaredFamilies = GetDeclaredBuiltinFamilies();
        var declaredNames = declaredFamilies
            .Select(static family => family.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var indexedFunctions = GetIndexedFunctionTitles(root);
        var sampleDirectories = GetSampleDirectoryNames(root);

        var missingVmSupport = new List<string>();
        var missingManualIndex = new List<string>();
        var missingSamples = new List<string>();

        foreach (var family in declaredFamilies)
        {
            var unsupportedVariants = family.Variants
                .Where(static variant => !BytecodeVm.SupportsBuiltin(variant.Id, variant.ParameterCount))
                .OrderBy(static variant => variant.ParameterCount)
                .ThenBy(static variant => variant.Id)
                .ToArray();

            if (unsupportedVariants.Length > 0)
            {
                missingVmSupport.Add(
                    $"{family.Name}: {string.Join(", ", unsupportedVariants.Select(FormatVariant))}");
            }

            if (!indexedFunctions.Contains(family.Name))
            {
                missingManualIndex.Add(family.Name);
            }

            if (!sampleDirectories.Contains(family.Name.ToLowerInvariant()))
            {
                missingSamples.Add(family.Name);
            }
        }

        var manualOnly = indexedFunctions
            .Where(declaredNames.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var extraManualFunctions = indexedFunctions
            .Except(declaredNames, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var extraSampleDirectories = sampleDirectories
            .Except(declaredFamilies.Select(static family => family.Name.ToLowerInvariant()), StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var report = new StringBuilder();
        var reportTime = DateTimeOffset.Now;

        report.AppendLine("Axiom Bytecode Coverage Audit");
        report.AppendLine($"Generated: {reportTime:yyyy-MM-dd HH:mm:ss zzz}");
        report.AppendLine($"Repository: {root}");
        report.AppendLine($"Declared builtin families: {declaredFamilies.Count}");
        report.AppendLine($"Manual index function entries: {indexedFunctions.Count}");
        report.AppendLine($"Sample directories under samples/functions: {sampleDirectories.Count}");
        report.AppendLine($"Manual entries matched to declared builtins: {manualOnly.Count}");
        report.AppendLine();

        AppendSection(
            report,
            "Missing VM support by declared variant",
            missingVmSupport,
            "Все семантические варианты built-in имеют исполняемый путь в BytecodeVm.");
        AppendSection(
            report,
            "Missing manual index entries",
            missingManualIndex,
            "Все объявленные built-in найдены в docs/manual/scripts/search-index.js.");
        AppendSection(
            report,
            "Missing sample directories",
            missingSamples,
            "Для всех объявленных built-in найдены тематические каталоги в samples/functions/.");
        AppendSection(
            report,
            "Manual index entries without declared builtins",
            extraManualFunctions,
            "Лишних function-entry в manual-индексе не найдено.");
        AppendSection(
            report,
            "Sample directories without declared builtins",
            extraSampleDirectories,
            "Лишних каталогов в samples/functions/ не найдено.");

        var hasIssues =
            missingVmSupport.Count > 0 ||
            missingManualIndex.Count > 0 ||
            missingSamples.Count > 0 ||
            extraManualFunctions.Length > 0 ||
            extraSampleDirectories.Length > 0;

        report.AppendLine("Summary");
        report.AppendLine("=======");
        report.AppendLine(hasIssues ? "Overall result: FAIL" : "Overall result: PASS");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, report.ToString(), Encoding.UTF8);

        Console.WriteLine(outputPath);
        Console.WriteLine();
        Console.WriteLine(report.ToString());

        return hasIssues ? 1 : 0;
    }

    private static List<BuiltinFamily> GetDeclaredBuiltinFamilies()
    {
        return Enum.GetValues<BuiltinFunctionId>()
            .Where(static id => BuiltinFunctions.TryGetDescriptor(id, out _))
            .Select(static id =>
            {
                BuiltinFunctions.TryGetDescriptor(id, out var descriptor);
                return descriptor;
            })
            .GroupBy(static descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new BuiltinFamily(
                group.Key,
                group.Select(static descriptor => new BuiltinVariant(
                        descriptor.Id,
                        descriptor.Parameters.Count))
                    .OrderBy(static variant => variant.ParameterCount)
                    .ThenBy(static variant => variant.Id)
                    .ToArray()))
            .OrderBy(static family => family.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<string> GetIndexedFunctionTitles(string root)
    {
        var path = Path.Combine(root, "docs", "manual", "scripts", "search-index.js");
        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            if (!line.Contains("kind: \"Функция\"", StringComparison.Ordinal))
            {
                continue;
            }

            var match = TitleRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var title = match.Groups["title"].Value.Trim();
            if (title.Length > 0)
            {
                titles.Add(title);
            }
        }

        return titles;
    }

    private static HashSet<string> GetSampleDirectoryNames(string root)
    {
        var path = Path.Combine(root, "samples", "functions");

        return Directory.Exists(path)
            ? Directory.GetDirectories(path)
                .Select(Path.GetFileName)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .Select(static name => name!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static void AppendSection(
        StringBuilder report,
        string title,
        IReadOnlyList<string> items,
        string emptyMessage)
    {
        report.AppendLine(title);
        report.AppendLine(new string('=', title.Length));

        if (items.Count == 0)
        {
            report.AppendLine(emptyMessage);
        }
        else
        {
            foreach (var item in items)
            {
                report.AppendLine($"- {item}");
            }
        }

        report.AppendLine();
    }

    private static string FormatVariant(BuiltinVariant variant)
    {
        return $"{variant.Id} (arity {variant.ParameterCount})";
    }

    private sealed record BuiltinFamily(string Name, IReadOnlyList<BuiltinVariant> Variants);

    private sealed record BuiltinVariant(BuiltinFunctionId Id, int ParameterCount);
}
