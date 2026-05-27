namespace Axiom.Ide.Services.Input;

public interface IScreenModeSizeProvider
{
    (int Width, int Height) GetSize(int mode);
}
