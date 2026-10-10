namespace Axiom.Execution.Tests;

public sealed class StringCaseIndependentContractTests
{
    [Fact]
    public void TrimmingUsesWhitespaceOrAnExplicitUtf16Set()
    {
        Check("""
PRINT TRIM(CHRW(160) + "Я" + CHRW(8195))
PRINT TRIM(CHRW(8203) + "Я" + CHRW(8203)) = CHRW(8203) + "Я" + CHRW(8203)
PRINT TRIM("абЯба", "аб")
PRINT TRIM("абЯба", "")
PRINT TRIM("", "аб") = ""
PRINT TRIM("😀😁а", "😀") = RIGHT("😁", 1) + "а"
PRINT LTRIM("😀а😀", "😀")
PRINT RTRIM("😀а😀", "😀")
PRINT REVERSE("é") = "́e"
PRINT REVERSE(LEFT("😀", 1) + "а" + RIGHT("😀", 1)) = RIGHT("😀", 1) + "а" + LEFT("😀", 1)
""", ["Я", "TRUE", "Я", "абЯба", "TRUE", "TRUE", "а😀", "😀а", "TRUE", "TRUE"]);
    }

    [Fact]
    public void CharacterCodesDistinguishCp866UnicodeAndMalformedInput()
    {
        Check("""
PRINT ASC(CHR(0))
PRINT ASC(CHR(255))
PRINT ASCW(CHRW(255))
PRINT ASCW(CHRW(256))
PRINT ASCW(CHRW(1114111))
PRINT ASC("😀я")
PRINT ASCW("😀я")
PRINT ASCW(LEFT("😀", 1))
PRINT ASCW(RIGHT("😀", 1))
PRINT ASC(LEFT("😀", 1))
PRINT ASCW("a" + LEFT("😀", 1))
PRINT CHRW(ASCW("é")) = "é"
PRINT CHR(ASC("é")) = "é"
PRINT REVERSE("") = ""
PRINT LCASE("") = ""
PRINT UCASE("") = ""
""", ["0", "255", "255", "256", "1114111", "128512", "128512", "65533", "65533", "65533", "97", "TRUE", "FALSE", "TRUE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void CaseConversionIsInvariantAcrossCultures()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "tr-TR", "ru-RU" })
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
                Check("""
PRINT LCASE("IЯABC")
PRINT UCASE("iяabc")
PRINT LCASE(LEFT("😀", 1)) = LEFT("😀", 1)
PRINT UCASE(RIGHT("😀", 1)) = RIGHT("😀", 1)
""", ["iяabc", "IЯABC", "TRUE", "TRUE"]);
            }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void InvalidCharacterCodesAndEmptyAscAreCatchableAndHaveMatchingLocations()
    {
        foreach (var call in new[] { "ASC(\"\")", "ASCW(\"\")", "CHR(-1)", "CHRW(-1)", "CHR(55296)", "CHRW(55296)", "CHR(57343)", "CHRW(57343)", "CHR(1114112)", "CHRW(1114112)", "CHR(2147483647)", "CHRW(2147483647)" })
        {
            var second = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call));
            Assert.False(second.Succeeded);
            var b = Assert.Single(second.Diagnostics);
            Assert.Equal("MB3007", b.Code);

                Check($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        }
    }

    [Fact]
    public void AllCp866CodesMatchIndependentEncodingTable()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var encoding = System.Text.Encoding.GetEncoding(866);
        var lines = new List<string>();
        var expected = new List<string>();
        for (var code = 0; code <= 255; code++)
        {
            lines.Add($"PRINT ASCW(CHR({code}))");
            expected.Add(char.ConvertToUtf32(encoding.GetString(new[] { (byte)code }), 0).ToString(System.Globalization.CultureInfo.InvariantCulture));
            lines.Add($"PRINT ASC(CHR({code}))");
            expected.Add(code.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        Check(string.Join("\n", lines), expected.ToArray());
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }
}
