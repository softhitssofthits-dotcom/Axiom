using System.Text;
using System.Text.RegularExpressions;

internal static class DiagnosticsTableAudit
{
    private static readonly Regex CodeRegex = new(
        @"""MB\d{4}""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ManualCodeRegex = new(
        @"<code>(MB\d{4})</code>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static int Run(string root, string outputPath)
    {
        var sourcePath = Path.Combine(root, "src", "Axiom.Core", "DiagnosticCodes.cs");
        var manualPath = Path.Combine(root, "docs", "manual", "diagnostics.html");

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Source file not found: {sourcePath}");
            return 1;
        }

        if (!File.Exists(manualPath))
        {
            Console.Error.WriteLine($"Manual file not found: {manualPath}");
            return 1;
        }

        var sourceCodes = ExtractSourceCodes(File.ReadAllText(sourcePath, Encoding.UTF8));
        var manualCodes = ExtractManualCodes(File.ReadAllText(manualPath, Encoding.UTF8));

        // MB4xxx — служебные коды инфраструктуры выполнения, не входящие в пользовательский контракт.
        var serviceCodes = sourceCodes.Where(static c => c.StartsWith("MB4", StringComparison.Ordinal)).ToArray();
        var userSourceCodes = sourceCodes.Except(serviceCodes).ToHashSet(StringComparer.Ordinal);

        var inSourceOnly = userSourceCodes.Except(manualCodes).OrderBy(static c => c).ToArray();
        var inManualOnly = manualCodes.Except(userSourceCodes).OrderBy(static c => c).ToArray();

        var sb = new StringBuilder();
        sb.AppendLine("# Diagnostics Table Audit");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Source: {sourcePath}");
        sb.AppendLine($"Manual: {manualPath}");
        sb.AppendLine();
        sb.AppendLine($"Source codes: {sourceCodes.Count} ({serviceCodes.Length} service MB4xxx excluded)");
        sb.AppendLine($"Manual codes: {manualCodes.Count}");
        sb.AppendLine();

        if (inSourceOnly.Length == 0 && inManualOnly.Length == 0)
        {
            sb.AppendLine("RESULT: PASS — all codes are synchronized.");
        }
        else
        {
            sb.AppendLine("RESULT: ISSUES FOUND");
            sb.AppendLine();

            if (inSourceOnly.Length > 0)
            {
                sb.AppendLine($"## In source but NOT in manual ({inSourceOnly.Length}):");
                foreach (var code in inSourceOnly)
                {
                    sb.AppendLine($"- {code}");
                }

                sb.AppendLine();
            }

            if (inManualOnly.Length > 0)
            {
                sb.AppendLine($"## In manual but NOT in source ({inManualOnly.Length}):");
                foreach (var code in inManualOnly)
                {
                    sb.AppendLine($"- {code}");
                }

                sb.AppendLine();
            }
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        Console.Write(sb.ToString());
        return inSourceOnly.Length == 0 && inManualOnly.Length == 0 ? 0 : 1;
    }

    private static HashSet<string> ExtractSourceCodes(string sourceText)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in CodeRegex.Matches(sourceText))
        {
            codes.Add(match.Value.Trim('"'));
        }

        return codes;
    }

    private static HashSet<string> ExtractManualCodes(string manualText)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in ManualCodeRegex.Matches(manualText))
        {
            codes.Add(match.Groups[1].Value);
        }

        return codes;
    }
}
