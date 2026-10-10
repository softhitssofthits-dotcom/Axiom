using Axiom.Core;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class GraphicsModeAnalysisTests
{
    [Theory]
    [InlineData("CIRCLE (10, 10), 5, 14")]
    [InlineData("LINE (1, 1)-(5, 5), 14")]
    [InlineData("PSET (1, 1), 14")]
    [InlineData("PRESET (1, 1)")]
    [InlineData("PAINT (1, 1), 14")]
    [InlineData("DRAW \"R10\"")]
    [InlineData("COLOR 14")]
    [InlineData("LOCATE 1, 1")]
    [InlineData("PALETTE 1, 2")]
    [InlineData("PCOPY 0, 1")]
    [InlineData("VIEW")]
    [InlineData("VIEW PRINT")]
    [InlineData("WINDOW")]
    [InlineData("BEGINFRAME")]
    [InlineData("PRESENT")]
    [InlineData("CAMERA 0, 0")]
    [InlineData("VIEWPORT")]
    [InlineData("RESETCAMERA")]
    [InlineData("DRAWRECT 1, 1, 10, 10, 14")]
    [InlineData("FILLRECT 1, 1, 10, 10, 14")]
    [InlineData("DRAWLINE 1, 1, 10, 10, 14")]
    [InlineData("DRAWCIRCLE 1, 1, 10, 14")]
    [InlineData("FILLCIRCLE 1, 1, 10, 14")]
    [InlineData("DRAWTEXT \"текст\", 1, 1, 14")]
    [InlineData("CLS 14")]
    [InlineData("PRINT POINT(1, 1)")]
    [InlineData("PRINT PMAP(1, 0)")]
    [InlineData("PRINT TEXTWIDTH(\"текст\")")]
    [InlineData("PRINT TEXTHEIGHT(\"текст\")")]
    [InlineData("PRINT MOUSEX()")]
    [InlineData("PRINT MOUSEY()")]
    [InlineData("PRINT MOUSEBUTTON(1)")]
    [InlineData("PRINT MOUSEPRESS(1)")]
    public void GraphicsOperationAfterTextScreen_IsCompileError(string operation)
    {
        var diagnostics = Bind("SCREEN 0\n" + operation);
        var error = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidGraphicsContext, error.Code);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("текстовый режим", error.Message);
    }

    [Theory]
    [InlineData("SCREEN 0\nSCREEN 1\nCIRCLE (1, 1), 1")]
    [InlineData("SCREEN 0\nCANVAS 20, 20\nDRAWCIRCLE 1, 1, 1, 14")]
    [InlineData("DIM mode AS INTEGER\nINPUT mode\nSCREEN mode\nCIRCLE (1, 1), 1")]
    [InlineData("SCREEN 0\nPRINT \"текст\"\nCLS\nSLEEP 1")]
    [InlineData("SCREEN 0\nIF FALSE THEN\nCIRCLE (1, 1), 1\nEND IF")]
    [InlineData("SCREEN 0\nEnableGraphics()\nCIRCLE (1, 1), 1\nSUB EnableGraphics()\nSCREEN 1\nEND SUB")]
    public void ValidOrUnknownContext_DoesNotProduceFalseError(string source) => Assert.Empty(Bind(source));

    [Theory]
    [InlineData("CONST mode AS INTEGER = 0\nSCREEN mode\nCIRCLE (1, 1), 1")]
    [InlineData("SCREEN 0\nDIM flag AS BOOLEAN\nIF flag THEN\nCIRCLE (1, 1), 1\nEND IF")]
    [InlineData("SCREEN 0\nFOR i = 1 TO 3\nCIRCLE (1, 1), 1\nNEXT")]
    [InlineData("SCREEN 0\nRenderMark()\nSUB RenderMark()\nCIRCLE (1, 1), 1\nEND SUB")]
    [InlineData("SetText()\nCIRCLE (1, 1), 1\nSUB SetText()\nSCREEN 0\nEND SUB")]
    [InlineData("SUB RenderMark()\nSCREEN 0\nCIRCLE (1, 1), 1\nEND SUB")]
    public void TextContextThroughConstantsBlocksAndCalls_IsDetected(string source) =>
        Assert.Contains(Bind(source), static diagnostic => diagnostic.Code == DiagnosticCodes.InvalidGraphicsContext);

    private static DiagnosticBag Bind(string source)
    {
        var syntax = Parser.Parse(source, out var syntaxDiagnostics);
        Assert.Empty(syntaxDiagnostics);
        Binder.Bind(syntax, out var diagnostics);
        return diagnostics;
    }
}
