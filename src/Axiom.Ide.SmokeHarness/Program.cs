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

internal sealed record SmokeHarnessOptions(bool KeepWorkspace, string? IdeExecutablePath)
{
    public static SmokeHarnessOptions Parse(string[] args)
    {
        var keepWorkspace = args.Any(static argument =>
            string.Equals(argument, "--keep-workspace", StringComparison.OrdinalIgnoreCase));
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

        return new SmokeHarnessOptions(keepWorkspace, ideExecutablePath);
    }
}
