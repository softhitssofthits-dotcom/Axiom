namespace Axiom.Runtime;

public interface IReversibleOutputWriter : IOutputWriter
{
    int OutputCheckpoint { get; }

    void RestoreOutputCheckpoint(int checkpoint);
}
