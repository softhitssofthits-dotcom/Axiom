namespace Axiom.Ide.Services.Input;

public readonly record struct ScreenTextScrollState(int FirstVisibleRow, int VisibleRows, int TotalRows)
{
    public int MaxFirstVisibleRow => Math.Max(1, TotalRows - VisibleRows + 1);

    public bool HasOverflow => TotalRows > VisibleRows;

    public int NormalizeFirstVisibleRow(int firstVisibleRow) =>
        Math.Min(MaxFirstVisibleRow, Math.Max(1, firstVisibleRow));
}
