using System.Diagnostics;
using System.Text.Json;
using Axiom.Ide.Services.Input;
using Axiom.Runtime;

var console = new ProgramConsoleService();
console.GraphicsHost.UseGpuCanvas = args.Contains("--gpu");
var pixels = Enumerable.Range(0, 32 * 32).Select(i => (i % 5 == 0) ? 0x8000FF00u : 0xFFFFCC00u).ToArray();
var image = new RuntimeImageValue(32, 32, pixels);
var sprite = new RuntimeSpriteValue(image, 16, 16);
console.ActivateCanvas(1024, 768);
var results = new List<object>();
GpuClock gpuClock = console.GraphicsHost.CanvasRenderer?.Invoke(surface => new GpuClock(surface));
using var currentProcess = Process.GetCurrentProcess();
foreach (var scene in new[] { "sprites", "tiles-camera-text" })
{
    var times = new List<double>();
    var gpuTimes = new List<double>();
    TimeSpan cpuStart = default;
    long allocationStart = 0;
    for (var frame = 0; frame < 40; frame++)
    {
        if (frame == 10) { cpuStart = currentProcess.TotalProcessorTime; allocationStart = GC.GetTotalAllocatedBytes(true); }
        var watch = Stopwatch.StartNew();
        if (gpuClock != null) console.GraphicsHost.CanvasRenderer.Enqueue(_ => gpuClock.Begin());
        console.BeginFrame();
        console.ClearFrame(0x102030);
        if (scene == "sprites")
        {
            for (var i = 0; i < 500; i++)
                console.DrawSprite(sprite, i % 4, (i * 37 + frame) % 990, (i * 19) % 730, 32, 32);
        }
        else
        {
            console.SetCanvasCamera(frame, frame / 2);
            for (var y = 0; y < 25; y++)
                for (var x = 0; x < 33; x++)
                    console.DrawImageRegion(image, 0, 0, 16, 16, x * 32, y * 32, 32, 32);
            console.ResetCanvasCamera();
            console.DrawCanvasText("Кадр " + frame + " — игровая сцена", 8, 8, 0xFFFFFF);
        }
        if (gpuClock != null) console.GraphicsHost.CanvasRenderer.Enqueue(_ => gpuClock.End());
        console.PresentFrame();
        console.GraphicsHost.CanvasRenderer?.WaitForPreviousPresentation();
        if (gpuClock != null) {
            var gpuMs = console.GraphicsHost.CanvasRenderer.Invoke(_ => gpuClock.Read());
            if (frame >= 10) gpuTimes.Add(gpuMs);
        }
        if (frame >= 10) times.Add(watch.Elapsed.TotalMilliseconds);
    }
    times.Sort();
    gpuTimes.Sort();
    results.Add(new { scene, medianMs = times[times.Count / 2], maxMs = times[^1], frames = times.Count,
        cpuMsPerFrame = (currentProcess.TotalProcessorTime - cpuStart).TotalMilliseconds / times.Count,
        gpuDrawingMedianMs = gpuTimes.Count > 0 ? gpuTimes[gpuTimes.Count/2] : (double?)null,
        allocatedBytesPerFrame = (GC.GetTotalAllocatedBytes(true) - allocationStart) / times.Count,
        driver = console.GraphicsHost.CanvasGraphicsDriver,
        gpuReadbackBytes = console.GraphicsHost.CanvasRenderer?.Invoke(surface => surface.ReadbackBytes),
        imageUploads = console.GraphicsHost.CanvasRenderer?.Invoke(surface => surface.UploadedImageCount) });
}
if (gpuClock != null) console.GraphicsHost.CanvasRenderer.Invoke(_ => { gpuClock.Dispose(); return true; });
console.DeactivateScreen();
Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
