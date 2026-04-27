using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace Axiom.Ide.Editor.Breakpoints;

public sealed class BreakpointMargin : FrameworkElement
{
    private const double MarginWidth = 18;
    private HashSet<int> _breakpointLines = [];
    private TextView? _textView;

    public Brush MarkerBrush { get; set; } = Brushes.Firebrick;

    public IReadOnlyCollection<int> BreakpointLines => _breakpointLines;

    public TextView? TextView
    {
        get => _textView;
        set
        {
            if (ReferenceEquals(_textView, value))
            {
                return;
            }

            if (_textView is not null)
            {
                _textView.VisualLinesChanged -= OnTextViewVisualLinesChanged;
                _textView.ScrollOffsetChanged -= OnTextViewScrollOffsetChanged;
            }

            _textView = value;

            if (_textView is not null)
            {
                _textView.VisualLinesChanged += OnTextViewVisualLinesChanged;
                _textView.ScrollOffsetChanged += OnTextViewScrollOffsetChanged;
            }

            InvalidateVisual();
        }
    }

    public void SetBreakpointLines(IEnumerable<int> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        _breakpointLines = lines
            .Where(line => line > 0)
            .ToHashSet();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(MarginWidth, 0);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (_textView?.Document is null || _textView.VisualLines.Count == 0 || _breakpointLines.Count == 0)
        {
            return;
        }

        foreach (var visualLine in _textView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;

            if (!_breakpointLines.Contains(lineNumber))
            {
                continue;
            }

            var y = visualLine.VisualTop - _textView.VerticalOffset;
            var diameter = Math.Clamp(visualLine.Height - 5, 8, 11);
            var radius = diameter / 2;
            var center = new Point(ActualWidth / 2, y + visualLine.Height / 2);
            drawingContext.DrawEllipse(MarkerBrush, null, center, radius, radius);
        }
    }

    private void OnTextViewVisualLinesChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void OnTextViewScrollOffsetChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }
}
