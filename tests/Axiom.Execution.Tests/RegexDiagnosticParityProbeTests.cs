using System.Text.Json;

namespace Axiom.Execution.Tests;

public sealed class RegexDiagnosticParityProbeTests
{
    [Fact]
    public void InvalidPatternDiagnosticsHaveExpectedCodeAndLocationOnBytecodeVm()
    {
        var observations = new List<object>();
        foreach (var form in RegexIndependentContractTests.Forms())
        {
            var name = (string)form[0];
            var modes = (bool)form[1];
            var count = (bool)form[2];
            var args = new List<string> { "\"a\"", "\"[\"" };
            if (name is "REGEXEXTRACT" or "REGEXEXTRACTALL") args.Add("1");
            if (name == "REGEXREPLACE") args.Add("\"x\"");
            if (modes) args.Add("\"i\"");
            if (count) args.Add("1");
            var source = $"PRINT {name}({string.Join(", ", args)})";
            var vm = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.False(vm.Succeeded);
            var second = Assert.Single(vm.Diagnostics);
            Assert.Equal("MB3007", second.Code);
            Assert.Equal(1, second.Location.Line);
            Assert.Equal(7, second.Location.Column);
            observations.Add(new { source, vm = new { second.Code, second.Location.Line, second.Location.Column } });
        }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "regex-1132-diagnostic-locations.json"), JsonSerializer.Serialize(observations, new JsonSerializerOptions { WriteIndented = true }));
    }
}
