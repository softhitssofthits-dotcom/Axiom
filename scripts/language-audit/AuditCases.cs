using System.Globalization;
using System.Text;

static class AuditCases
{
    public static List<AuditCase> Create(string output)
    {
        var c = new List<AuditCase>();
        void Expr(string id, string expression, string? expected = null, string? error = null) => c.Add(new(id, "PRINT " + expression, expected, error));
        Expr("clng-upper-exclusive", "CLNG(9223372036854775808.0)", error: "MB3008");
        Expr("clng-upper-neighbour", "CLNG(9223372036854774784.0)", "9223372036854774784");
        Expr("clng-lower", "CLNG(-9223372036854775808.0)", "-9223372036854775808");
        Expr("clng-lower-outside", "CLNG(-9223372036854777856.0)", error: "MB3008");
        // VAL отвергает нечисловой/неконечный текст возвратом 0. Эти выражения
        // проверяют преобразование нуля, а не передачу Infinity/NaN в CINT/CLNG.
        // Договор независимо закреплён NumericBoundaryContractTests.Val_RejectsNonFiniteAndPreservesOrdinaryParsing.
        Expr("clng-positive-inf", "CLNG(VAL(" + Q("1e309") + "))", "0");
        Expr("clng-nan", "CLNG(VAL(" + Q("NaN") + "))", "0");
        Expr("cint-nan", "CINT(VAL(\"NaN\"))", "0");
        Expr("cint-upper-fraction", "CINT(2147483647.9)", "2147483647");
        Expr("cint-lower-fraction", "CINT(-2147483648.9)", "-2147483648");
        Expr("cdec-nan", "CDEC(VAL(\"NaN\"))", "0");
        Expr("val-inf", "VAL(" + Q("1e309") + ")", "0");
        Expr("val-nan", "VAL(" + Q("NaN") + ")", "0");
        Expr("val-prefix", "VAL(" + Q("12abc") + ")", "12");
        Expr("val-comma", "VAL(" + Q("1,5") + ")", "1");
        Expr("val-hex", "VAL(" + Q("&HFF") + ")", "0");
        Expr("cdec-overflow", "CDEC(VAL(\"1e30\"))", error: "MB3008");
        Expr("cint-trunc", "CINT(-2.9)", "-2");
        Expr("int-floor", "INT(-2.9)", "-3");
        Expr("pow-domain", "POW(-2, 0.5)", error: "MB3007");
        Expr("power-domain", "(-2) ^ 0.5", error: "MB3012");
        Expr("pow-zero", "POW(0,0)", "1");
        Expr("cbrt-negative", "CBRT(-8)", "-2");
        Expr("abs-min", "ABS(CINT(-2147483648))", error: "MB3008");
        Expr("sqr-domain", "SQR(-1)", error: "MB3007");
        Expr("log-domain", "LOG(0)", error: "MB3007");
        Expr("acosh-domain", "ACOSH(0.5)", error: "MB3007");
        Expr("atanh-domain", "ATANH(1)", error: "MB3007");
        Expr("round-half", "ROUND(2.5)", "3");
        Expr("numeric-promotion", "MIN(1,2.5)", "1");
        Expr("decimal-mixed", "CDEC(0.1) + CDEC(0.2)", "0.3");
        Expr("format-hex-existing", "$\"{255:X}\"", "FF");
        Expr("format-decimal-existing", "$\"{1.25:F2}\"", "1.25");
        foreach (var pair in new[] { ("positive", "1e308", "1e308"), ("cancel", "1e308", "-1e308") })
        {
            c.Add(new("average-" + pair.Item1, $"DIM a[1 TO 2] AS DOUBLE\na[1] = VAL({Q(pair.Item2)})\na[2] = VAL({Q(pair.Item3)})\nPRINT ARRAYAVERAGE(a[])", pair.Item1 == "cancel" ? "0" : "1E+308"));
        }
        c.Add(new("min-bool-single", "DIM a[1 TO 1] AS BOOLEAN\na[1] = TRUE\nPRINT ARRAYMIN(a[])", ErrorCode: "MB2003"));
        c.Add(new("min-bool-double", "DIM a[1 TO 2] AS BOOLEAN\na[1] = TRUE\nPRINT ARRAYMIN(a[])", ErrorCode: "MB2003"));
        c.Add(new("sum-long-overflow", "DIM a[1 TO 2] AS LONG\na[1]=9223372036854775807\na[2]=1\nPRINT ARRAYSUM(a[])", ErrorCode: "MB3008"));
        c.Add(new("sum-catch", "DIM a[1 TO 2] AS INTEGER\na[1]=2147483647\na[2]=1\nTRY\nPRINT ARRAYSUM(a[])\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT 7\nEND TRY", "MB3008\n7"));
        c.Add(new("sum-overflow", "DIM a[1 TO 2] AS INTEGER\na[1]=2147483647\na[2]=1\nPRINT ARRAYSUM(a[])", ErrorCode: "MB3008"));
        c.Add(new("array-rank", "DIM a[-1 TO 0, 2 TO 3] AS INTEGER\na[-1,2]=1\na[-1,3]=2\na[0,2]=3\na[0,3]=4\nPRINT ARRAYSUM(a[]), ARRAYMIN(a[]), ARRAYMAX(a[]), ARRAYAVERAGE(a[])", "10 1 4 2.5"));
        foreach (var name in new[] {"ARRAYSUM", "ARRAYMIN", "ARRAYMAX"})
        {
            c.Add(new("aggregate-scalar-type-" + name, $"DIM a[1 TO 2] AS INTEGER\na[1]=2\na[2]=3\nDIM result AS INTEGER\nresult={name}(a[])\nPRINT result", name == "ARRAYSUM" ? "5" : name == "ARRAYMIN" ? "2" : "3"));
            c.Add(new("aggregate-inferred-type-" + name, $"DIM a[1 TO 2] AS INTEGER\na[1]=2\na[2]=3\nDIM result = {name}(a[])\nPRINT result + 1", name == "ARRAYSUM" ? "6" : name == "ARRAYMIN" ? "3" : "4"));
        }
        c.Add(new("array-clone", "DIM a[1 TO 2] AS INTEGER\na[1]=9\nDIM b[] AS INTEGER\nb[]=ARRAYCLONE(a[])\nb[1]=4\nPRINT a[1], b[1]", "9 4"));
        c.Add(new("array-unallocated", "DIM a[] AS INTEGER\nPRINT ARRAYAVERAGE(a[])", ErrorCode: "MB3005"));
        c.Add(new("array-empty", "DIM a[1 TO 0] AS INTEGER\nPRINT ARRAYMIN(a[])", ErrorCode: "MB3007"));
        Expr("unicode-len", "LEN(" + Q("a😀e\u0301") + ")", "5");
        Expr("unicode-reverse", "REVERSE(" + Q("a😀b") + ")", "b😀a");
        Expr("unicode-reverse-twice-cut", "REVERSE(REVERSE(LEFT(" + Q("😀") + ",1))) = LEFT(" + Q("😀") + ",1)", "TRUE");
        Expr("unicode-asc-cut", "ASCW(LEFT(" + Q("😀") + ",1))", "65533");
        Expr("unicode-instr", "INSTR(" + Q("a😀b😀") + "," + Q("😀") + ")", "2");
        Expr("unicode-count", "SUBSTRCOUNT(" + Q("😀😀") + "," + Q("😀") + ")", "2");
        Expr("unicode-join", "JOIN(SPLIT(" + Q("😀;е;😎") + "," + Q(";") + ")," + Q(";") + ")", "😀;е;😎");
        Expr("unicode-normalization", Q("é") + " = " + Q("e\u0301"), "False");
        Expr("regex-m", "REGEXCOUNT(" + Q("a\nb") + "," + Q("^.") + "," + Q("m") + ")", "2");
        Expr("regex-s", "REGEXISMATCH(" + Q("a\nb") + "," + Q("a.b") + "," + Q("s") + ")", "True");
        Expr("regex-x", "REGEXISMATCH(" + Q("ab") + "," + Q("a b") + "," + Q("x") + ")", "True");
        Expr("regex-repeat-mode", "REGEXISMATCH(" + Q("A") + "," + Q("a") + "," + Q("ii") + ")", "True");
        Expr("regex-invalid-mode", "REGEXCOUNT(" + Q("a") + "," + Q(".") + "," + Q("q") + ")", error: "MB3007");
        Expr("regex-invalid", "REGEXCOUNT(" + Q("a") + "," + Q("[") + ")", error: "MB3007");
        Expr("regex-zero-width", "REGEXCOUNT(" + Q("ab") + "," + Q("") + ")", "3");
        Expr("regex-timeout", "REGEXISMATCH(REPEAT(" + Q("a") + ",10000) + " + Q("!") + "," + Q("^(a+)+$") + ")", error: "MB3007");
        c.Add(new("short-circuit", "PRINT FALSE ANDALSO (1 / 0 > 0)\nPRINT TRUE ORELSE (1 / 0 > 0)", "False\nTrue"));
        c.Add(new("random-seed", "RANDOMIZE 42\nDIM a AS DOUBLE\na=RND()\nRANDOMIZE 42\nPRINT a=RND()", "True"));
        c.Add(new("finally-rethrow", "TRY\nTRY\nERROR \"APP005\"\nCATCH err\nERROR err\nFINALLY\nPRINT 7\nEND TRY\nCATCH outer\nPRINT 8\nEND TRY", "7\n8"));
        c.Add(new("bound-dimension", "DIM a[-1 TO 0,2 TO 3] AS INTEGER\nPRINT LBOUND(a[],2), UBOUND(a[],2)", "2 3"));
        Expr("date-leap", "ISDATE(" + Q("2024-02-29") + ")", "True");
        Expr("date-nonleap", "ISDATE(" + Q("2023-02-29") + ")", "False");
        // Короткие и смешанные файлы: проверка байтов до и после границы заголовка.
        var fixtures = new Dictionary<string, byte[]>
        {
            ["cp-short"] = [0xDF], ["cp-three"] = [0xC0,0xC1,0xC2],
            ["utf-two"] = Encoding.UTF8.GetBytes("я"), ["utf-cross-header"] = Encoding.UTF8.GetBytes("аб"),
            ["invalid-prefix"] = [0xDF, 0x61, 0x62, 0x63],
            ["cp-long"] = Encoding.GetEncoding(1251).GetBytes("Привет, мир"),
            ["bom"] = [0xEF,0xBB,0xBF,0x61], ["empty"] = [],
            ["bom-only"] = [0xEF,0xBB,0xBF], ["utf-emoji"] = Encoding.UTF8.GetBytes("😀")
        };
        foreach (var (name, bytes) in fixtures)
        {
            var path = Path.Combine(output, name + ".txt");
            var source = $"PRINT READFILE({Q(path)})\nDIM f AS FILE\nOPEN {Q(path)} FOR INPUT AS f\nPRINT READTOEND(f)\nCLOSE f";
            string text;
            try { text = new UTF8Encoding(false,true).GetString(bytes).TrimStart('\uFEFF'); }
            catch (DecoderFallbackException) { text = Encoding.GetEncoding(1251).GetString(bytes); }
            c.Add(new("encoding-" + name, source, text + "\n" + text, Prepare: () => File.WriteAllBytes(path, bytes)));
        }
        var written = Path.Combine(output, "newline.txt");
        c.Add(new("file-newline-bytes", $"DIM f AS FILE\nOPEN {Q(written)} FOR OUTPUT AS f\nWRITELINE(f, {Q("я")})\nCLOSE f\nPRINT FILESIZE({Q(written)})", "3", Inspect: () => Convert.ToHexString(File.ReadAllBytes(written))));
        var emoji = Path.Combine(output, "emoji-bom.txt");
        c.Add(new("readtext-emoji-one", $"DIM f AS FILE\nOPEN {Q(emoji)} FOR INPUT AS f\nPRINT READTEXT(f,1)\nPRINT FILEPOS(f)\nCLOSE f", Prepare: () => File.WriteAllBytes(emoji, new byte[] {0xEF,0xBB,0xBF}.Concat(Encoding.UTF8.GetBytes("😀x")).ToArray())));
        c.Add(new("append-cp1251", $"DIM f AS FILE\nOPEN {Q(emoji)} FOR APPEND AS f\nWRITE(f,{Q("я")})\nCLOSE f\nPRINT READFILE({Q(emoji)})", Prepare: () => File.WriteAllBytes(emoji, [0xC0]), Inspect: () => Convert.ToHexString(File.ReadAllBytes(emoji))));
        // Фиксированный seed обеспечивает повторяемость независимых свойств.
        var random = new Random(7012026);
        for (var i = 0; i < 80; i++)
        {
            var a = random.Next(-100000,100000);
            var b = random.Next(-100000,100000);
            Expr("property-minmax-" + i, $"MIN({a},{b}) <= MAX({a},{b})", "True");
            Expr("property-abs-" + i, $"ABS({a}) >= 0", "True");
            Expr("property-clamp-" + i, $"CLAMP({a},-10,10)", Math.Clamp(a,-10,10).ToString(CultureInfo.InvariantCulture));
            var text = i % 2 == 0 ? "Русский😀текст" + a : "e\u0301b" + a;
            Expr("property-reverse-" + i, $"REVERSE(REVERSE({Q(text)})) = {Q(text)}", "True");
        }
        return c.Select(item => item with { Expected = item.Expected switch { "True" => "TRUE", "False" => "FALSE", "False\nTrue" => "FALSE\nTRUE", _ => item.Expected } }).ToList();
    }

    private static string Q(string s) => "\"" + s.Replace("\"", "\"\"").Replace("\r", "\" + CHR(13) + \"").Replace("\n", "\" + CHR(10) + \"") + "\"";
}
