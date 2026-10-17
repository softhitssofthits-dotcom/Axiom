using Axiom.Runtime;
using Axiom.Semantics;
namespace Axiom.Execution.Tests;
public sealed class GraphicsAuditRepairTests
{
    [Theory]
    [InlineData("GET (0,0)-(0,0),b")]
    [InlineData("PUT (0,0),b,AND")]
    [InlineData("PUT (0,0),b,PSET")]
    [InlineData("PAINT (0,0),6,9")]
    public void GraphicsAllocationFailureIsCatchable(string command) =>
        Check("SCREEN 1\nDIM b(0 TO 0,0 TO 0) AS INTEGER\n" + Catch(command),
            new ProbeHost { FailAllocation = true }, ["MB3005", "42"]);

    [Theory]
    [InlineData("DRAWRECT -1000000000,1000000000,1,1,1")]
    [InlineData("FILLRECT 1000000000,-1000000000,1,1,1")]
    [InlineData("DRAWIMAGE im,0,0,2147483647,2147483647")]
    [InlineData("DRAWSPRITE sp,0,0,0,2147483647,2147483647")]
    public void ValidModernBoundaryArgumentsStillReachTheHost(string command)
    {
        var host = new ProbeHost();
        Check("CANVAS 8,8\nDIM im AS IMAGE=LOADIMAGE(\"stub\")\nDIM sp AS SPRITE=LOADSPRITE(\"stub\",1,1)\n" + command + "\nPRINT 42", host, ["42"]);
        Assert.Single(host.Calls);
    }
    [Theory]
    [InlineData("CANVAS 8,8")]
    [InlineData("SCREEN 1")]
    public void SurfaceCreationFailureIsCatchable(string command) =>
        Check(Catch(command), new ProbeHost { FailSurface = true }, ["MB3005", "42"]);
    [Theory]
    [InlineData("PSET", 5, 0)]
    [InlineData("PRESET", 250, 0)]
    [InlineData("AND", 5, 1)]
    [InlineData("OR", 7, 1)]
    [InlineData("XOR", 2, 1)]
    public void PutUsesOneBulkWriteAndReadsOnlyForLogicalActions(string action, int expected, int reads)
    {
        var host = new ProbeHost { UseBulk = true };
        Check($"SCREEN 1\nDIM b(10 TO 11,20 TO 21) AS INTEGER\nb(10,20)=5\nb(11,20)=5\nb(10,21)=5\nb(11,21)=5\nPUT (0,0),b,{action}", host, []);
        Assert.Equal(reads, host.Captures);
        Assert.Equal(1, host.BulkCalls);
        Assert.All(host.BulkPixels!, pixel => Assert.Equal(expected, pixel));
        Assert.Empty(host.Calls);
    }
    [Theory]
    [InlineData("DIM b(2147483647 TO 2147483647,0 TO 0) AS INTEGER\nGET (0,0)-(0,0),b")]
    [InlineData("DIM b(0 TO 0,2147483647 TO 2147483647) AS INTEGER\nGET (0,0)-(0,0),b")]
    [InlineData("DIM b(2147483647 TO 2147483647,0 TO 0) AS INTEGER\nPUT (0,0),b,PSET")]
    [InlineData("DIM b(0 TO 0,2147483647 TO 2147483647) AS INTEGER\nPUT (0,0),b,PSET")]
    public void TransferWithMaximumBoundsFinishes(string command) => Check("SCREEN 1\n"+command+"\nPRINT 42",new ProbeHost(),["42"]);

    [Theory]
    [InlineData("PSET (2147483648,0),1",2)]
    [InlineData("DRAW \"BM2147483648,0\"",2)]
    [InlineData("WINDOW (0,0)-(0.000000001,1)\nPSET (1,0),1",0)]
    public void PointOverflowIsCatchable(string setup,int selector) => Check("SCREEN 1\n"+setup+"\n"+Catch($"PRINT POINT({selector})"),new ProbeHost(),["MB3008","42"]);

    [Theory]
    [InlineData("DRAWIMAGE im,0,0,2147483648,1")]
    [InlineData("DRAWSPRITE sp,0,0,0,1,2147483648")]
    [InlineData("FILLRECT 1000000001,0,1,1,1")]
    [InlineData("DRAWRECT 0,-1000000001,1,1,1")]
    [InlineData("DRAWIMAGE im,1000000001,0")]
    [InlineData("DRAWSPRITE sp,0,0,-1000000001")]
    [InlineData("CANVAS 16385,1")]
    [InlineData("CANVAS 4097,4096")]
    [InlineData("CANVAS 9223372036854775807,9223372036854775807")]
    public void InvalidModernArgumentsAreCatchable(string command) => Check("CANVAS 8,8\nDIM im AS IMAGE=LOADIMAGE(\"stub\")\nDIM sp AS SPRITE=LOADSPRITE(\"stub\",1,1)\n"+Catch(command),new ProbeHost(),["MB3007","42"]);

    [Theory]
    [InlineData(0)] [InlineData(63)] [InlineData(64)] [InlineData(65)] [InlineData(128)]
    public void DeferredEventsDoNotStarveNewPresses(int count) => Check("CANVAS 32,32\nDIM hits AS INTEGER\nON MOUSE(1),Hit\nFOR i=1 TO 10\nDIM marker AS INTEGER=i\nNEXT\nPRINT hits\nSUB Hit()\nhits=hits+1\nEND SUB",new MouseProbeHost(count),["1"]);

    private static string Catch(string command) => "TRY\n"+command+"\nCATCH e\nPRINT e.Code\nEND TRY\nPRINT 42";
    private static void Check(string source,IInputProvider host,string[] expected)
    {
        var pipeline=new AxiomExecutionPipeline();
        var compiled=pipeline.Compile(new CompilationRequest(source,initialGraphicsContext:InitialGraphicsContext.Unknown));
        Assert.True(compiled.Succeeded,string.Join("\n",compiled.Diagnostics.Select(d=>d.Message)));
        var run=pipeline.RunCompiled(compiled.CompiledProgram!,new ExecutionRequest(source,inputProvider:host));
        Assert.True(run.Succeeded,string.Join("\n",run.Diagnostics.Select(d=>d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(expected,run.Output);
    }
    private class ProbeHost : IInputProvider,IGraphicsHost
    {
        public bool UseBulk { get; init; }
        public bool FailSurface { get; init; }
        public bool FailAllocation { get; init; }
        public int Captures { get; private set; }
        public int BulkCalls { get; private set; }
        public int[]? BulkPixels { get; private set; }
        public int[]? CaptureAbsolutePixels(long x,long y,int width,int height)
        { if(FailAllocation) throw new OutOfMemoryException(); Captures++; return Enumerable.Repeat(7,width*height).ToArray(); }
        public bool TryWriteAbsolutePixels(long x,long y,int width,int height,int[] pixels)
        { if(FailAllocation) throw new OutOfMemoryException(); if(!UseBulk) return false; BulkCalls++; BulkPixels=pixels;return true; }
        public void Paint(GraphicsPaintRequest request) { if(FailAllocation) throw new OutOfMemoryException(); }
        public List<string> Calls { get; }=[];
        public bool IsScreenActive { get; private set; }
        public bool IsCanvasActive { get; private set; }
        public bool IsScreenOutputActive=>false;
        public int ScreenWidth=>32;
        public int ScreenHeight=>32;
        public long CurrentGraphicsX { get;private set; }
        public long CurrentGraphicsY { get;private set; }
        public bool WindowEnabled {get;private set;}
        public bool WindowUsesScreenCoordinates {get;private set;}
        public double WindowLeft {get;private set;}
        public double WindowTop {get;private set;}
        public double WindowRight {get;private set;}
        public double WindowBottom {get;private set;}
        public void ActivateScreen(int mode) { if(FailSurface) throw new GraphicsSurfaceException("Device unavailable."); IsScreenActive=mode!=0; IsCanvasActive=false; }
        public void ActivateScreen(int mode,int a,int v)=>ActivateScreen(mode);
        public void ActivateCanvas(long width,long height) { if(FailSurface) throw new GraphicsSurfaceException("Device unavailable."); IsCanvasActive=true;IsScreenActive=false; }
        public void SetGraphicsCurrentPosition(long x,long y) { CurrentGraphicsX=x;CurrentGraphicsY=y; }
        public void SetPixel(long x,long y,int color)=>SetGraphicsCurrentPosition(x,y);
        public void SetAbsolutePixel(long x,long y,int color)=>Calls.Add($"pixel {x},{y},{color}");
        public void FillRect(long x,long y,long w,long h,int c)=>Calls.Add($"rect {x},{y},{w},{h}");
        public void DrawRect(long x,long y,long w,long h,int c)=>Calls.Add($"rect {x},{y},{w},{h}");
        public RuntimeImageValue? LoadImage(string path)=>new(1,1,new uint[]{0xffffffff});
        public void DrawImage(RuntimeImageValue image,long x,long y,int w,int h)=>Calls.Add($"image {x},{y},{w},{h}");
        public void DrawSprite(RuntimeSpriteValue sprite,int f,long x,long y,int w,int h)=>Calls.Add($"sprite {x},{y},{w},{h}");
        public bool SetWindow(double l,double t,double r,double b,bool screen)
        { WindowEnabled=true;WindowLeft=l;WindowTop=t;WindowRight=r;WindowBottom=b;WindowUsesScreenCoordinates=screen;return true; }
        public string ReadInput(InputRequest request)=>throw new InvalidOperationException();
    }

    private sealed class MouseProbeHost : ProbeHost,IMouseInputProvider
    {
        private readonly Queue<MouseInputEvent> _events=[];
        public MouseProbeHost(int count) {for(var i=0;i<count;i++)_events.Enqueue(new(MouseInputEventKind.Wheel,120));_events.Enqueue(new(MouseInputEventKind.Press,1));}
        public int Remaining=>_events.Count;
        public int ReadCalls{get;private set;}
        public long GetMouseX()=>1;
        public long GetMouseY()=>1;
        public bool IsMouseButtonDown(int button)=>false;
        public int ReadMouseButtonPress()=>0;
        public MouseInputEvent ReadMouseEvent() {ReadCalls++;return _events.Count>0?_events.Dequeue():default;}
    }
}
