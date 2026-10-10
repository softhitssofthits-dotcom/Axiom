namespace Axiom.Ide.SmokeHarness;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var options = SmokeHarnessOptions.Parse(args);
            var harness = new IdeSmokeHarness(options);
            harness.Run();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Axiom IDE smoke harness failed.");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}

internal sealed record SmokeHarnessOptions(bool KeepWorkspace, string? IdeExecutablePath, bool EventLogOnly, bool InKeyOnly, bool OnMouseOnly, bool GetPutOnly, bool Screen10Only, bool SpriteOnly, bool DebuggerOnly, bool InputCanvasOnly)
{
    public static SmokeHarnessOptions Parse(string[] args)
    {
        var keepWorkspace = args.Any(static argument =>
            string.Equals(argument, "--keep-workspace", StringComparison.OrdinalIgnoreCase));
        var eventLogOnly = args.Any(static argument =>
            string.Equals(argument, "--event-log-only", StringComparison.OrdinalIgnoreCase));
        var inKeyOnly = args.Any(static argument =>
            string.Equals(argument, "--inkey-only", StringComparison.OrdinalIgnoreCase));
        var onMouseOnly = args.Any(static argument =>
            string.Equals(argument, "--on-mouse-only", StringComparison.OrdinalIgnoreCase));
        var getPutOnly = args.Any(static argument =>
            string.Equals(argument, "--get-put-only", StringComparison.OrdinalIgnoreCase));
        var screen10Only = args.Any(static argument =>
            string.Equals(argument, "--screen10-only", StringComparison.OrdinalIgnoreCase));
        var spriteOnly = args.Any(static argument =>
            string.Equals(argument, "--sprite-only", StringComparison.OrdinalIgnoreCase));
        string? ideExecutablePath = null;

        for (var index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], "--ide-exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Expected path after --ide-exe.");
            }

            ideExecutablePath = args[index + 1];
            index++;
        }

        var debuggerOnly = args.Any(static argument =>
            string.Equals(argument, "--debugger-only", StringComparison.OrdinalIgnoreCase));
        var inputCanvasOnly = args.Any(static argument =>
            string.Equals(argument, "--input-canvas-only", StringComparison.OrdinalIgnoreCase));
        return new SmokeHarnessOptions(keepWorkspace, ideExecutablePath, eventLogOnly, inKeyOnly, onMouseOnly, getPutOnly, screen10Only, spriteOnly, debuggerOnly, inputCanvasOnly);
    }
}
