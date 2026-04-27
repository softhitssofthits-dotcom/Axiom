using Axiom.Core;

namespace Axiom.Bytecode;

public sealed class LoweredProgram
{
    public const string EntryPointName = "<global>";

    public LoweredProgram(TextLocation location, LoweredRoutine entryPoint, IReadOnlyList<LoweredRoutine> routines)
    {
        ArgumentNullException.ThrowIfNull(entryPoint);
        ArgumentNullException.ThrowIfNull(routines);

        Location = location;
        EntryPoint = entryPoint;
        Routines = routines;
    }

    public TextLocation Location { get; }

    public LoweredRoutine EntryPoint { get; }

    public IReadOnlyList<LoweredRoutine> Routines { get; }

    public IEnumerable<LoweredRoutine> AllRoutines
    {
        get
        {
            yield return EntryPoint;

            foreach (var routine in Routines)
            {
                yield return routine;
            }
        }
    }
}
