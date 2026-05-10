namespace Axiom.Runtime;

public interface IKeyboardInputProvider
{
    string ReadKey();

    bool IsKeyDown(string keyText);
}
