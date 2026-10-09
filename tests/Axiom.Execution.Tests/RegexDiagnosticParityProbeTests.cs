using System.Text.Json;

namespace Axiom.Execution.Tests;

public sealed class RegexDiagnosticParityProbeTests
{
    [Fact]
    public void InvalidPatternDiagnosticsHaveSameCodeAndLocationInBothEngines()
    {
        var observations = new List<object>();
        var differences = new List<string>();
        foreach (var form in RegexIndependentContractTests.Forms().Where(row => (ExecutionMode)row[0] == ExecutionMode.Interpreter))
        {
            var name = (string)form[1];
            var modes = (bool)form[2];
            var count = (bool)form[3];
            var args = new List<string> { "\"a\"", "\"[\"" };
            if (name is "REGEXEXTRACT" or "REGEXEXTRACTALL") args.Add("1");
            if (name == "REGEXREPLACE") args.Add("\"x\"");
            if (modes) args.Add("\"i\"");
            if (count) args.Add("1");
            var source = $"PRINT {name}({string.Join(", ", args)})";
            var interpreter = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
            var vm = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
            Assert.False(interpreter.Succeeded);
            Assert.False(vm.Succeeded);
            Assert.False(vm.UsedInterpreterFallback);
            var first = Assert.Single(interpreter.Diagnostics);
            var second = Assert.Single(vm.Diagnostics);
            Assert.Equal("MB3007", first.Code);
            Assert.Equal(first.Code, second.Code);
            Assert.Equal(first.Location.Span, second.Location.Span);
            Assert.Equal(1, first.Location.Line);
            Assert.Equal(7, first.Location.Column);
            observations.Add(new { source, interpreter = new { first.Code, first.Location.Line, first.Location.Column }, vm = new { second.Code, second.Location.Line, second.Location.Column } });
            if (first.Location.Line != second.Location.Line || first.Location.Column != second.Location.Column)
                differences.Add($"{name} modes={modes} count={count}: Interpreter {first.Location.Line}:{first.Location.Column}, VM {second.Location.Line}:{second.Location.Column}");
        }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "regex-1132-diagnostic-locations.json"), JsonSerializer.Serialize(observations, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(differences.Count == 0, string.Join("\n", differences));
    }
}
