using System.Text.Json;
using System.Text.RegularExpressions;
namespace Axiom.Execution.Tests;
public sealed class RegexCompatibilityMatrixTests
{
    [Fact]
    public void AllFormsPreserveEngineResultsForBoundaryCases()
    {
        var pipeline = new AxiomExecutionPipeline();
string Q(string s) => "\"" + s.Replace("\"", "\"\"").Replace("\r", "\" + CHR(13) + \"").Replace("\n", "\" + CHR(10) + \"") + "\"";
var cases = new (string text,string pattern,int group,string replacement)[] {
    ("", "", 0,"!"), ("", "a",1,"x"), ("ab", "",0,"!"),
    ("aaa", "a",0,"$&"), ("ababa", "aba",0,"x"), ("ababa", "(?=(aba))",1,"$1"),
    ("A12 б34", "([AБаб])(\\d+)",2,"$2:$1"), ("a\nb", "^b",0,"x"),
    ("a\r\nb", "^a$",0,"x"), ("a\nb", "a.b",0,"x"), ("ab", "a b #c",0,"x"),
    ("abc", "([a-z])+",1,"$1"), ("ab", "(?<name>a)(b)",2,"${name}"),
    ("a", "(?<10>a)",10,"${10}"), ("b ab", "(a)?b",1,"$1"), ("a", "(a)",99,"$99"),
    ("a,b,,", "(,)",1,"|"), ("😀", ".",0,"x"), ("е́", "\\w",0,"x"),
    ("١2", "\\d",0,"x"), ("abc", "b",0,"$$$&$+$'$_$`"),
    ("abc", "b",0,"${missing}"), ("abc", "\\A.*\\z",0,"same"),
};
var matrixFailures = new List<object>(); int matrixTotal=0;
foreach (var c in cases)
foreach (var modes in new[] {"", "i", "ms", "x"}) {
    var options = RegexOptions.CultureInvariant;
    foreach (char flag in modes) options |= flag switch {'i'=>RegexOptions.IgnoreCase,'m'=>RegexOptions.Multiline,'s'=>RegexOptions.Singleline,_=>RegexOptions.IgnorePatternWhitespace};
    var reference = new Regex(c.pattern,options,TimeSpan.FromSeconds(5));
    foreach (var name in new[] {"REGEXISMATCH","REGEXMATCH","REGEXEXTRACT","REGEXMATCHES","REGEXEXTRACTALL","REGEXSPLIT","REGEXCOUNT","REGEXREPLACE"})
    foreach (int? limit in name=="REGEXREPLACE" ? new int?[] {null,0,1,2} : new int?[] {null}) {
        var callArgs = new List<string> {Q(c.text),Q(c.pattern)};
        if (name is "REGEXEXTRACT" or "REGEXEXTRACTALL") callArgs.Add(c.group.ToString());
        if (name=="REGEXREPLACE") callArgs.Add(Q(c.replacement));
        if (modes.Length>0) callArgs.Add(Q(modes));
        if (limit.HasValue) callArgs.Add(limit.Value.ToString());
        string call = $"{name}({string.Join(",",callArgs)})";
        string[]? array = name switch {
            "REGEXMATCHES" => reference.Matches(c.text).Select(m=>m.Value).ToArray(),
            "REGEXEXTRACTALL" => reference.Matches(c.text).Select(m=>m.Groups[c.group].Value).ToArray(),
            "REGEXSPLIT" => c.text.Length==0 ? [] : reference.Split(c.text), _=>null};
        string source;
        if (array is not null) {
            source=$"DIM v[] AS STRING\nv[] = {call}\nPRINT ARRAYLENGTH(v[]) = {array.Length}\n";
            for (int j=0;j<array.Length;j++) source += $"PRINT v[{j+1}] = {Q(array[j])}\n";
        } else {
            string expectation = name switch {
                "REGEXISMATCH"=>reference.IsMatch(c.text)?"TRUE":"FALSE",
                "REGEXCOUNT"=>reference.Matches(c.text).Count.ToString(),
                "REGEXMATCH"=>Q(reference.Match(c.text).Value),
                "REGEXEXTRACT"=>Q(reference.Match(c.text).Groups[c.group].Value),
                _=>Q(limit.HasValue?reference.Replace(c.text,c.replacement,limit.Value):reference.Replace(c.text,c.replacement))};
            source=$"PRINT {call} = {expectation}";
        }
        var r=pipeline.Run(new ExecutionRequest(source)); matrixTotal++;
        if (!r.Succeeded || r.Output.Any(s=>s!="TRUE")) matrixFailures.Add(new {source,r.Output,diagnostics=r.Diagnostics.Select(d=>d.Message)});
    }
}
Assert.Equal(1012, matrixTotal); Assert.True(matrixFailures.Count == 0, JsonSerializer.Serialize(matrixFailures));

    }
}
