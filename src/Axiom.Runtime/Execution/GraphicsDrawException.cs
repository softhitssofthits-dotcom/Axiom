namespace Axiom.Runtime;

internal sealed class GraphicsDrawException : Exception
{
    public GraphicsDrawException(string message, int position)
        : base($"{message} (позиция {position}).")
    {
        Position = position;
    }

    public int Position { get; }
}
