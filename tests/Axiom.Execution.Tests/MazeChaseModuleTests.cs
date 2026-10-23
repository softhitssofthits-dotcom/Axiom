using Axiom.Core;
using Axiom.Bytecode;

namespace Axiom.Execution.Tests;

public sealed class MazeChaseModuleTests
{
    private const string Sample = "samples/graphics/game/maze_chase_project/";

    [Fact]
    public void ProjectEntryAndAllImportsCompile()
    {
        var main = TestProgramLoader.LoadText(Sample + "main.axm");
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(main,
            sourceName: "main.axm", additionalDocuments: Documents()));
        Assert.Equal(CompilationStatus.Success, result.Status);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(9, result.CompiledProgram!.SourceDocuments.Count);
    }

    [Fact]
    public void WorldOwnsPelletsAndTunnelBoundaries()
    {
        var result = Run("IMPORT maze_config\nIMPORT maze_world\nInitMaze()\nPRINT MazeError()\nPRINT MazeColumns()\nPRINT PlayerStartColumn();\",\";PlayerStartRow()\nPRINT GhostStartColumn();\",\";GhostStartRow()\nPRINT CellAt(0,1)\nPRINT IsTunnelPassageRow(18)\nPRINT WrapTunnelColumn(0,18)\nPRINT WrapTunnelColumn(29,18)\nDIM before AS INTEGER=PelletsRemaining()\nPRINT TakePellet(5,2)\nPRINT TakePellet(5,3)\nPRINT TakePellet(5,3)\nPRINT before-PelletsRemaining()");
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["", "28", "14,27", "15,21", "#", "TRUE", "28", "1", "50", "10", "0", "2"], result.Output);
    }

    [Fact]
    public void PrivateWorldDataIsNotExported()
    {
        var result = Compile("IMPORT maze_world\nPRINT maze[1]");
        Assert.Contains(result.Diagnostics, d => d.Code == "MB2053");
    }

    [Fact]
    public void ImportsDoNotReexportConfiguration()
    {
        var result = Compile("IMPORT maze_world\nPRINT DirX(1)");
        Assert.Contains(result.Diagnostics, d => d.Code == "MB2005");
        Assert.Equal(["-1"], Run("IMPORT maze_world\nIMPORT maze_config\nPRINT DirX(DIR_LEFT)").Output);
    }

    [Fact]
    public void ByRefWalkerCrossesTunnelAndReevaluatesDirectionAtEachCell()
    {
        var result = Run("IMPORT maze_config\nIMPORT maze_world\nIMPORT maze_motion\nInitMaze()\nDIM actor AS Walker\nactor.tileCol=1\nactor.tileRow=18\nactor.dir=DIR_LEFT\nactor.wantedDir=DIR_LEFT\nDIM calls AS INTEGER=0\nStepWalker(actor,32,TRUE,Choose)\nPRINT actor.tileCol\nPRINT actor.tileRow\nPRINT actor.travel\nPRINT calls\nFUNCTION Choose() AS INTEGER\ncalls=calls+1\nRETURN DIR_LEFT\nEND FUNCTION");
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["27", "18", "0", "2"], result.Output);
    }

    [Fact]
    public void PlayerSnapshotCannotMutateItsOwner()
    {
        var result = Run("IMPORT maze_config\nIMPORT maze_world\nIMPORT maze_player\nInitMaze()\nResetPlayer()\nDIM copy AS Walker=GetPlayer()\ncopy.tileCol=99\nPRINT GetPlayer().tileCol");
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["14"], result.Output);
    }

    [Theory]
    [InlineData(28, 18, 2, 2, 0, 32, "2,18,0,2")]
    [InlineData(2, 5, 2, 3, 0, 4, "2,5,4,2")]
    [InlineData(2, 5, 2, 4, 4, 4, "2,5,8,2")]
    [InlineData(2, 5, 3, 3, 0, 4, "2,5,0,0")]
    public void MovementRetainsWallsPendingTurnsAndRightTunnel(int column, int row, int direction, int wanted, int travel, int speed, string expected)
    {
        var source = $"IMPORT maze_config\nIMPORT maze_world\nIMPORT maze_motion\nInitMaze()\nDIM actor AS Walker\nactor.tileCol={column}\nactor.tileRow={row}\nactor.dir={direction}\nactor.wantedDir={wanted}\nactor.travel={travel}\nStepWalker(actor,{speed},FALSE,NoRoute)\nPRINT actor.tileCol;\",\";actor.tileRow;\",\";actor.travel;\",\";actor.dir\nFUNCTION NoRoute() AS INTEGER\nRETURN 0\nEND FUNCTION";
        var result = Run(source);
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal([expected], result.Output);
    }

    [Fact]
    public void GhostStepsMatchOriginalAlgorithmIncludingCellBoundaries()
    {
        var original = TestProgramLoader.LoadText("samples/graphics/game/maze_chase.axm");
        var declarations = original[..original.IndexOf("\nLoadResources()", StringComparison.Ordinal)];
        var routines = original[original.IndexOf("' #region Игровой шаг", StringComparison.Ordinal)..];
        // Выполняется сам исходник, а не переписанная в C# копия алгоритма BFS.
        var control = declarations + "\nInitPelletAlignment()\nInitMaze()\nResetActors()\nFOR tick=1 TO 120\nStepWalker(ghost,GHOST_SPEED,TRUE)\nPRINT ghost.tileCol;\",\";ghost.tileRow;\",\";ghost.travel;\",\";ghost.dir\nNEXT\n" + routines;
        var expected = new AxiomExecutionPipeline().Run(new ExecutionRequest(control));
        Assert.Equal(ExecutionStatus.Success, expected.Status);
        var actual = Run("IMPORT maze_config\nIMPORT maze_world\nIMPORT maze_calibration\nIMPORT maze_player\nIMPORT maze_ghost\nInitPelletAlignment()\nInitMaze()\nResetPlayer()\nResetGhost()\nFOR tick=1 TO 120\nStepGhost(GetPlayer())\nDIM actor AS Walker=GetGhost()\nPRINT actor.tileCol;\",\";actor.tileRow;\",\";actor.travel;\",\";actor.dir\nNEXT");
        Assert.Equal(ExecutionStatus.Success, actual.Status);
        Assert.Equal(120, actual.Output.Count);
        Assert.Equal(expected.Output, actual.Output);
    }

    [Theory]
    [InlineData("missing", 31, false)]
    [InlineData("valid", 20, false)]
    [InlineData("partial", 31, true)]
    [InlineData("duplicate", 31, true)]
    [InlineData("out-of-range", 31, true)]
    public void CalibrationIsAppliedOnlyAfterFullValidation(string mode, int expectedY, bool hasError)
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "maze-modules", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var text = TestProgramLoader.LoadText(Sample + "maze_chase_pellet_calibration.txt")
                .Replace("pelletRowOffset(5) = -33", "pelletRowOffset(5) = -44", StringComparison.Ordinal);
            if (mode == "partial") text = "pelletRowOffset(5) = -44";
            if (mode == "duplicate") text += "\npelletRowOffset(5) = -44";
            if (mode == "out-of-range") text = text.Replace("pelletRowOffset(5) = -44", "pelletRowOffset(99) = -44", StringComparison.Ordinal);
            if (mode != "missing") File.WriteAllText(Path.Combine(directory, "maze_chase_pellet_calibration.txt"), text);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
                "IMPORT maze_calibration\nInitPelletAlignment()\nInitCalibrationFile()\nLoadPelletAlignment()\nPRINT PelletPixelY(5)\nPRINT CalibrationError()<>\"\"",
                sourceName: "probe.axm", additionalDocuments: Documents(), resourceBaseDirectory: directory));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal([expectedY.ToString(), hasError ? "TRUE" : "FALSE"], result.Output);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void SerializedProjectPreservesWalkerAndFunctionCallback()
    {
        const string source = "IMPORT maze_config\nIMPORT maze_world\nIMPORT maze_motion\nInitMaze()\nDIM actor AS Walker\nactor.tileCol=1\nactor.tileRow=18\nactor.dir=DIR_LEFT\nactor.wantedDir=DIR_LEFT\nStepWalker(actor,32,TRUE,Choose)\nPRINT actor.tileCol\nFUNCTION Choose() AS INTEGER\nRETURN DIR_LEFT\nEND FUNCTION";
        var pipeline = new AxiomExecutionPipeline();
        var compilation = Compile(source);
        Assert.True(compilation.Succeeded);
        var program = compilation.CompiledProgram!;
        var restored = BytecodeModuleSerializer.DeserializeModuleFromJson(BytecodeModuleSerializer.SerializeToJson(program.BytecodeModule!));
        Assert.Empty(new BytecodeModuleValidator().Validate(restored).Diagnostics);
        var result = pipeline.RunCompiled(new CompiledProgram(source, program.Syntax, program.BoundProgram, bytecodeModule: restored), new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["27"], result.Output);
    }

    [Fact]
    public void InvalidMazeIsRejectedBeforeActorInitialization()
    {
        var documents = Documents().Select(d => d.ModuleName == "maze_world" ? new SourceDocument(d.Name,
            d.Text.Replace("maze[1] = \"############################\"", "maze[1] = \"#\"", StringComparison.Ordinal)) : d).ToArray();
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("IMPORT maze_world\nInitMaze()\nPRINT MazeError()<>\"\"\nPRINT PelletsRemaining()",
            sourceName: "probe.axm", additionalDocuments: documents));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["TRUE", "0"], result.Output);
    }

    private static CompilationResult Compile(string source) => new AxiomExecutionPipeline().Compile(
        new CompilationRequest(source, sourceName: "probe.axm", additionalDocuments: Documents()));

    private static ExecutionResult Run(string source) => new AxiomExecutionPipeline().Run(
        new ExecutionRequest(source, sourceName: "probe.axm", additionalDocuments: Documents()));

    private static SourceDocument[] Documents() => new[] { "maze_config", "maze_world", "maze_calibration", "maze_motion", "maze_player", "maze_ghost", "maze_game", "maze_render" }
        .Select(name => new SourceDocument(name + ".axm", TestProgramLoader.LoadText(Sample + name + ".axm"))).ToArray();
}
