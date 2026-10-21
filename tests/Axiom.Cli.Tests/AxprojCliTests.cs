using System.Text.Json;
namespace Axiom.Cli.Tests;
public sealed class AxprojCliTests
{
    [Fact] public void RunEntryAndCheckUnimportedFile(){
        var folder=Path.GetFullPath(Path.Combine(".artifacts","tests","cli-project",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(folder);
        try{
            File.WriteAllText(Path.Combine(folder,"main.axm"),"PRINT 42");File.WriteAllText(Path.Combine(folder,"spare.axm"),"PRINT missing");
            var project=Path.Combine(folder,"game.axproj");File.WriteAllText(project,JsonSerializer.Serialize(new {formatVersion=1,entry="main.axm"}));
            var output=new StringWriter();var error=new StringWriter();
            Assert.Equal(0,CliApp.Run(["run",project],output,error,new StringReader("")));Assert.Equal("42",output.ToString().Trim());
            output.GetStringBuilder().Clear();error.GetStringBuilder().Clear();
            Assert.Equal(1,CliApp.Run(["check","--json",project],output,error));
            using var result=JsonDocument.Parse(output.ToString());Assert.False(result.RootElement.GetProperty("ok").GetBoolean());
            Assert.Contains("spare.axm",output.ToString());
        }finally{Directory.Delete(folder,true);}
    }
    [Fact] public void InvalidProjectKeepsMachineReadableError(){
        var folder=Path.GetFullPath(Path.Combine(".artifacts","tests","cli-project",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(folder);
        try{var file=Path.Combine(folder,"bad.axproj");File.WriteAllText(file,"{");var output=new StringWriter();
            Assert.Equal(2,CliApp.Run(["check",file,"--json"],output,new StringWriter()));using var json=JsonDocument.Parse(output.ToString());Assert.False(json.RootElement.GetProperty("ok").GetBoolean());
        }finally{Directory.Delete(folder,true);}
    }
}
