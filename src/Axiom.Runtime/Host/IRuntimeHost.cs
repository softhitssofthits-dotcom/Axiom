namespace Axiom.Runtime;

public interface IRuntimeHost : IInputProvider, IKeyboardInputProvider, IMouseInputProvider, IOutputWriter, IRuntimeClock, IRuntimeRandom, IGraphicsHost, IRuntimeCancellationProvider;
