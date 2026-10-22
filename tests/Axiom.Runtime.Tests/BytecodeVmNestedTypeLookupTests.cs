using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed class BytecodeVmNestedTypeLookupTests
{
    [Theory]
    [InlineData(false, "array", "Item", "11")]
    [InlineData(true, "array", "Item", "11")]
    [InlineData(false, "cycle", "Item", "11")]
    [InlineData(true, "cycle", "Item", "11")]
    [InlineData(false, "qualified", "first::Item", "11")]
    [InlineData(true, "qualified", "first::Item", "11")]
    [InlineData(false, "qualified", "second::Item", "second")]
    [InlineData(true, "qualified", "second::Item", "second")]
    [InlineData(false, "qualified", "SECOND::iTeM", "second")]
    [InlineData(true, "qualified", "SECOND::iTeM", "second")]
    public void ValidatorAndVmResolveNestedTypeWithExpectedFields(bool observed, string graph, string identity, string expected)
    {
        var module = CreateModule(graph, identity);

        var validation = new BytecodeModuleValidator().Validate(module);
        Assert.True(validation.Succeeded, string.Join(" | ", validation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.Empty(validation.Diagnostics);

        var host = new TestRuntimeHost([]);
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        var result = BytecodeVm.Execute(module, host, observed ? snapshots.Add : null);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal([expected], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
    }

    [Theory]
    [InlineData(false, "cycle", "Missing")]
    [InlineData(true, "cycle", "Missing")]
    [InlineData(false, "qualified", "Item")]
    [InlineData(true, "qualified", "Item")]
    [InlineData(false, "qualified", "third::Item")]
    [InlineData(true, "qualified", "third::Item")]
    public void ValidatorAndVmRejectUnknownIdentityAtCreateRecord(bool observed, string graph, string identity)
    {
        var module = CreateModule(graph, identity);

        var validation = new BytecodeModuleValidator().Validate(module);
        Assert.False(validation.Succeeded);
        var validatorDiagnostic = Assert.Single(validation.Diagnostics);
        Assert.Equal("BC4117", validatorDiagnostic.Code);
        Assert.Equal(23, validatorDiagnostic.Location.Line);
        Assert.Equal(9, validatorDiagnostic.Location.Column);

        var host = new TestRuntimeHost([]);
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        var result = BytecodeVm.Execute(module, host, observed ? snapshots.Add : null);

        Assert.False(result.Succeeded);
        var vmDiagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", vmDiagnostic.Code);
        Assert.Equal(23, vmDiagnostic.Location.Line);
        Assert.Equal(9, vmDiagnostic.Location.Column);
        Assert.Empty(host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
    }

    private static BytecodeModule CreateModule(string graph, string identity)
    {
        var first = new UserDefinedTypeSymbol("Item", graph == "qualified" ? "first" : null);
        first.Complete([new TypeFieldSymbol("Value", TypeSymbol.Integer, hasDefaultValue: true, defaultValue: 11)]);
        var root = new UserDefinedTypeSymbol("Root");

        switch (graph)
        {
            case "array":
                root.Complete([new TypeFieldSymbol("Items", ArrayTypeSymbol.Create(first, rank: 2))]);
                break;
            case "cycle":
                var node = new UserDefinedTypeSymbol("Node");
                root.Complete([new TypeFieldSymbol("Nodes", ArrayTypeSymbol.Create(node))]);
                node.Complete([
                    new TypeFieldSymbol("Parents", ArrayTypeSymbol.Create(root)),
                    new TypeFieldSymbol("Children", ArrayTypeSymbol.Create(node)),
                    new TypeFieldSymbol("Items", ArrayTypeSymbol.Create(first))
                ]);
                break;
            case "qualified":
                var second = new UserDefinedTypeSymbol("Item", "second");
                second.Complete([new TypeFieldSymbol("Value", TypeSymbol.String, hasDefaultValue: true, defaultValue: "second")]);
                root.Complete([
                    new TypeFieldSymbol("FirstItems", ArrayTypeSymbol.Create(first)),
                    new TypeFieldSymbol("SecondItems", ArrayTypeSymbol.Create(second))
                ]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(graph));
        }

        var location = new TextLocation(new TextSpan(100, 4), 23, 9);
        BytecodeInstruction[] instructions = [
            new(Opcode.CreateRecord, [0]),
            new(Opcode.LoadField, [1]),
            new(Opcode.PrintValue),
            new(Opcode.PrintLine),
            new(Opcode.Return)
        ];
        var routine = new BytecodeRoutine(0, "Main", location, TypeSymbol.Void, 0, 0, instructions, isEntryPoint: true);

        return new BytecodeModule(
            new BytecodeHeader(BytecodeFormatVersion.V1, "Axiom language vNext", BytecodeModuleFlags.HasDebugInfo, "axiom-bytecode-v1"),
            new ConstantPool([
                new BytecodeConstant(BytecodeConstantKind.String, identity),
                new BytecodeConstant(BytecodeConstantKind.String, "Value")
            ]),
            [routine],
            [new BytecodeGlobal(0, "roots", ArrayTypeSymbol.Create(root), false, GlobalInitializationStrategy.DefaultValue)],
            [],
            new BytecodeDebugInfo([new SourceInstructionMap(0, 0, location)], []),
            0);
    }
}
