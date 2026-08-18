using Axiom.Ide.Editor.Highlighting;
using Axiom.Ide.Editor.Navigation;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Persistence;
using Axiom.Semantics;
using Axiom.Syntax;
using System.Reflection;
using System.Text;

namespace Axiom.Ide.Tests;

public sealed class EditorFeatureTests
{
    [Fact]
    public void ScreenRenderer_UsesDirect3D11SwapChainDependency()
    {
        var project = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Axiom.Ide.csproj"));
        var renderer = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Graphics\Direct3D11SwapChainRenderer.cs"));

        Assert.Contains("Vortice.Direct3D11", project, StringComparison.Ordinal);
        Assert.Contains("D3D11CreateDeviceAndSwapChain", renderer, StringComparison.Ordinal);
        Assert.Contains("IDXGISwapChain", renderer, StringComparison.Ordinal);
        Assert.Contains("PresentSyncInterval", renderer, StringComparison.Ordinal);
        Assert.Contains("Present(PresentSyncInterval, PresentFlags.None)", renderer, StringComparison.Ordinal);
        Assert.Contains("UpdateSubresource", renderer, StringComparison.Ordinal);
        Assert.Contains("PresentFrame(ScreenFramebufferSnapshot", renderer, StringComparison.Ordinal);
        Assert.Contains("ScaleFramePixels", renderer, StringComparison.Ordinal);
    }

    [Fact]
    public void ScreenRenderer_UsesNativeWin32Direct3DWindow()
    {
        var nativeWindow = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Graphics\NativeGraphicsScreenWindow.cs"), Encoding.UTF8);
        var service = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Services\Input\ProgramConsoleService.cs"), Encoding.UTF8);

        Assert.Contains("CreateWindowEx", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("WndProc", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("WmKeyDown", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("WmChar", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("WmSize", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("WmPaint", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("HandleResize", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("WmMouseMove", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("Direct3D11SwapChainRenderer", nativeWindow, StringComparison.Ordinal);
        Assert.Contains("NativeGraphicsScreenWindow", service, StringComparison.Ordinal);
        Assert.DoesNotContain("new GraphicsScreenWindow", service, StringComparison.Ordinal);
        Assert.Contains("GraphicsFrameCoalescingInterval", service, StringComparison.Ordinal);
        Assert.Contains("DispatcherTimer", service, StringComparison.Ordinal);
        Assert.Contains("GraphicsFrameSummaryInterval", service, StringComparison.Ordinal);
        Assert.Contains("screen.frame.summary", service, StringComparison.Ordinal);
        Assert.Contains("screen.input.summary", service, StringComparison.Ordinal);
        Assert.DoesNotContain("screen.key.enqueued", service, StringComparison.Ordinal);
        Assert.DoesNotContain("screen.key.read", service, StringComparison.Ordinal);
        Assert.DoesNotContain("screen.mouse.enqueued", service, StringComparison.Ordinal);
        Assert.DoesNotContain("screen.mouse.read", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_DefinitionLoadsCentralizedNamedColors()
    {
        var definition = AxiomHighlighting.Definition;

        Assert.Equal("Axiom", definition.Name);
        Assert.NotNull(definition.GetNamedColor("Keyword"));
        Assert.NotNull(definition.GetNamedColor("String"));
        Assert.NotNull(definition.GetNamedColor("Number"));
        Assert.NotNull(definition.GetNamedColor("Comment"));
        Assert.NotNull(definition.GetNamedColor("TypeName"));
        Assert.NotNull(definition.GetNamedColor("BuiltIn"));
    }

    [Fact]
    public void Highlighting_DarkDefinitionLoadsCentralizedNamedColors()
    {
        var definition = AxiomHighlighting.GetDefinition(IdeTheme.Dark);

        Assert.Equal("Axiom", definition.Name);
        Assert.NotNull(definition.GetNamedColor("Keyword"));
        Assert.NotNull(definition.GetNamedColor("String"));
        Assert.NotNull(definition.GetNamedColor("Number"));
        Assert.NotNull(definition.GetNamedColor("Comment"));
        Assert.NotNull(definition.GetNamedColor("TypeName"));
        Assert.NotNull(definition.GetNamedColor("BuiltIn"));
    }

    [Fact]
    public void Highlighting_KeywordResourceContainsElseIf()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>ELSEIF</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_KeywordResourceContainsExit()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>EXIT</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_KeywordResourceContainsDoAndLoop()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>DO</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>LOOP</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>UNTIL</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_NumberRuleContainsHexLiteralPattern()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains(@"<Rule color=""Number"">\b\d+(\.\d+)?\b|&amp;H[0-9A-F]+\b</Rule>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_DarkNumberRuleContainsHexLiteralPattern()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.Dark.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains(@"<Rule color=""Number"">\b\d+(\.\d+)?\b|&amp;H[0-9A-F]+\b</Rule>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_KeywordResourceContainsSelectCaseAndIs()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>SELECT</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>CASE</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>IS</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_KeywordResourceContainsTryCatchFinally()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>TRY</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>CATCH</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>FINALLY</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>ERROR</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_KeywordResourceContainsLogicalOperators()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>AND</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>OR</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>XOR</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>EQV</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>IMP</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>MOD</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_TypeResourceContainsLong()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>LONG</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_TypeResourceContainsDecimal()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>DECIMAL</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_BuiltInResourceContainsMathFunctions()
    {
        const string resourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
        using var stream = typeof(AxiomHighlighting).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xshd = reader.ReadToEnd();

        Assert.Contains("<Word>ABS</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>ACOS</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>CLAMP</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>ROUND</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>FIX</Word>", xshd, StringComparison.Ordinal);
        Assert.Contains("<Word>SETSIGN</Word>", xshd, StringComparison.Ordinal);
    }

    [Fact]
    public void Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes()
    {
        var expectedKeywords = Enum.GetValues<SyntaxKind>()
            .Where(static kind => kind.ToString().EndsWith("Keyword", StringComparison.Ordinal))
            .Select(static kind => kind.ToString()[..^"Keyword".Length].ToUpperInvariant())
            .Except(["BOOLEAN", "DECIMAL", "DOUBLE", "FILE", "IMAGE", "INTEGER", "LONG", "SPRITE", "STRING", "TILEMAP", "TILESET"], StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var missingKeywords = expectedKeywords
            .Where(keyword => !AxiomHighlightingVocabulary.Keywords.Contains(keyword))
            .ToArray();

        var expectedTypes = new[] { "BOOLEAN", "DECIMAL", "DOUBLE", "FILE", "IMAGE", "INTEGER", "LONG", "SPRITE", "STRING", "TILEMAP", "TILESET" };
        var missingTypes = expectedTypes
            .Where(typeName => !AxiomHighlightingVocabulary.TypeNames.Contains(typeName))
            .ToArray();

        Assert.Empty(missingKeywords);
        Assert.Empty(missingTypes);
        Assert.Contains("TRUE", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("FALSE", AxiomHighlightingVocabulary.Keywords);
    }

    [Fact]
    public void Highlighting_VocabularyContainsImageWords()
    {
        Assert.Contains("DRAWIMAGE", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("IMAGE", AxiomHighlightingVocabulary.TypeNames);
        Assert.Contains("LOADIMAGE", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("IMAGEWIDTH", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("IMAGEHEIGHT", AxiomHighlightingVocabulary.BuiltIns);
    }

    [Fact]
    public void Highlighting_VocabularyContainsSpriteWords()
    {
        Assert.Contains("DRAWSPRITE", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("SPRITE", AxiomHighlightingVocabulary.TypeNames);
        Assert.Contains("LOADSPRITE", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("SPRITEWIDTH", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("SPRITEHEIGHT", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("SPRITECOUNT", AxiomHighlightingVocabulary.BuiltIns);
    }

    [Fact]
    public void Highlighting_VocabularyContainsCanvasWords()
    {
        Assert.Contains("CANVAS", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("BEGINFRAME", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("PRESENT", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("VIEWPORT", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("DRAWRECT", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("FILLRECT", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("RGB", AxiomHighlightingVocabulary.BuiltIns);
    }

    [Fact]
    public void Highlighting_VocabularyContainsTilesetWords()
    {
        Assert.Contains("DRAWTILE", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("TILESET", AxiomHighlightingVocabulary.TypeNames);
        Assert.Contains("LOADTILESET", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("TILECOUNT", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("TILEWIDTH", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("TILEHEIGHT", AxiomHighlightingVocabulary.BuiltIns);
    }

    [Fact]
    public void Highlighting_VocabularyContainsTilemapWords()
    {
        Assert.Contains("DRAWTILEMAP", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("DRAWTILELAYER", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("SETMAPTILE", AxiomHighlightingVocabulary.Keywords);
        Assert.Contains("TILEMAP", AxiomHighlightingVocabulary.TypeNames);
        Assert.Contains("LOADTILEMAP", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPWIDTH", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPHEIGHT", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPPIXELWIDTH", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPPIXELHEIGHT", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPTILEWIDTH", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPTILEHEIGHT", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPLAYERCOUNT", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPTILE", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPTILESET", AxiomHighlightingVocabulary.BuiltIns);
        Assert.Contains("MAPTILEFLAGS", AxiomHighlightingVocabulary.BuiltIns);
    }

    [Fact]
    public void Highlighting_VocabularyCoversAllBuiltInFunctions()
    {
        var missingBuiltIns = BuiltinFunctions.All
            .Select(static function => function.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => !AxiomHighlightingVocabulary.BuiltIns.Contains(name))
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Empty(missingBuiltIns);
    }

    [Fact]
    public void DiagnosticNavigationTargetFactory_ReturnsTargetForValidLocation()
    {
        const string sourceText = "PRINT 1\r\nPRINT 2";
        var diagnostic = new ProgramDiagnosticItem("MB1001", "Syntax", "Example", 2, 7, 1);

        var target = DiagnosticNavigationTargetFactory.Create(sourceText, diagnostic);

        Assert.NotNull(target);
        Assert.Equal(15, target!.Offset);
        Assert.Equal(1, target.Length);
        Assert.Equal(2, target.Line);
        Assert.Equal(7, target.Column);
    }

    [Fact]
    public void DiagnosticNavigationTargetFactory_ReturnsNullWhenLocationIsMissing()
    {
        var diagnostic = new ProgramDiagnosticItem("MB3005", "Runtime", "Internal", null, null, null);

        var target = DiagnosticNavigationTargetFactory.Create("PRINT 1", diagnostic);

        Assert.Null(target);
    }

    [Fact]
    public void DiagnosticNavigationTargetFactory_ReturnsNullForOutOfRangeLocation()
    {
        var diagnostic = new ProgramDiagnosticItem("MB1001", "Syntax", "Example", 5, 1, 1);

        var target = DiagnosticNavigationTargetFactory.Create("PRINT 1", diagnostic);

        Assert.Null(target);
    }

    [Fact]
    public void MainWindowXaml_WiresEditorContextMenuAndFindCommands()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.DoesNotContain("editor:SearchPanelBehavior.InstallSearchPanel=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Icon=\"/Axiom.Ide;component/Assets/axiom.ico\"", xaml, StringComparison.Ordinal);
        Assert.Contains("LineNumbersForeground=\"{DynamicResource LineNumberForegroundBrush}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsReadOnly=\"{Binding IsReadOnly}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Loaded=\"OnEditorLoaded\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Unloaded=\"OnEditorUnloaded\"", xaml, StringComparison.Ordinal);
        Assert.Contains("GotKeyboardFocus=\"OnEditorGotKeyboardFocus\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ApplicationCommands.Find", xaml, StringComparison.Ordinal);
        Assert.Contains("search:SearchCommands.FindNext", xaml, StringComparison.Ordinal);
        Assert.Contains("search:SearchCommands.FindPrevious", xaml, StringComparison.Ordinal);
        Assert.Contains("<avalonedit:TextEditor.ContextMenu>", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void IdeProject_DefinesApplicationIcon()
    {
        var project = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Axiom.Ide.csproj"), Encoding.UTF8);

        Assert.Contains("<ApplicationIcon>Assets\\axiom.ico</ApplicationIcon>", project, StringComparison.Ordinal);
        Assert.Contains("<Resource Include=\"Assets\\axiom.ico\" />", project, StringComparison.Ordinal);
        Assert.True(File.Exists(GetRepositoryFilePath(@"src\Axiom.Ide\Assets\axiom.ico")));
        Assert.True(File.Exists(GetRepositoryFilePath(@"src\Axiom.Ide\Assets\axiom-icon.svg")));
    }

    [Fact]
    public void InformationDialog_UsesApplicationIcon()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Services\Prompts\InformationDialog.xaml"), Encoding.UTF8);

        Assert.Contains("Title=\"{DynamicResource Prompts.DefaultTitle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Icon=\"/Axiom.Ide;component/Assets/axiom.ico\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"{DynamicResource Prompts.Ok}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Viewbox Width=\"48\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"InformationDialogIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"M70 188 122 62h20l52 126h-27l-10-27H107l-10 27H70Z\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Source=\"/Axiom.Ide;component/Assets/axiom.ico\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowCode_ClearsFindReplaceStatusWhenDialogCloses()
    {
        var shellCode = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);
        var searchCode = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.Search.cs"), Encoding.UTF8);

        Assert.Contains("private bool _hasFindReplaceStatusMessage;", shellCode, StringComparison.Ordinal);
        Assert.Contains("ClearFindReplaceStatusBarText();", searchCode, StringComparison.Ordinal);
        Assert.Contains("UpdateFindReplaceStatusBarText(\"Совпадений не найдено.\");", searchCode, StringComparison.Ordinal);
        Assert.Contains("IdeLanguageResourceService.GetCurrentString(\"Status.Ready\", \"Ready.\")", searchCode, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_UsesIconsInEditorContextMenu()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("<avalonedit:TextEditor.ContextMenu>", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellContextMenuStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Edit.Undo}\" Command=\"ApplicationCommands.Undo\" Style=\"{StaticResource DropDownMenuItemStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Edit.Copy}\" Command=\"ApplicationCommands.Copy\" Style=\"{StaticResource DropDownMenuItemStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Search.Find}\" Command=\"ApplicationCommands.Find\" Style=\"{StaticResource DropDownMenuItemStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE7A7;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE8C8;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE721;", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_DefinesClassicAndModernFormatMenu()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Header=\"{DynamicResource Menu.Format}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Format.Classic}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsClassicFormattingEnabled, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding UseClassicFormattingCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Format.Modern}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsModernFormattingEnabled, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding UseModernFormattingCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Format.SmartIdentifier}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsSmartIdentifierEnabled, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ToggleSmartIdentifierCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("LostKeyboardFocus=\"OnEditorLostKeyboardFocus\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_WiresOpenRecentMenuToRecentFilesCollection()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Header=\"{DynamicResource Menu.File.Recent}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding RecentFiles}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OpenRecentCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"CommandParameter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Value=\"{Binding}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_WiresFileManagerTreeExpandAndOpen()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Header=\"{DynamicResource Menu.View.FileManager}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("RefreshFilesPaneCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("CloseFilesPaneCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Files.Close}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeSelectedItemChanged", xaml, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeItemExpanded", xaml, StringComparison.Ordinal);
        Assert.Contains("HierarchicalDataTemplate DataType=\"{x:Type files:FileTreeNode}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToggleButton x:Name=\"Expander\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TreeExpanderBackgroundBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("TreeExpanderBorderBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("TreeExpanderForegroundBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("TargetName=\"ExpanderGlyph\" Property=\"Text\" Value=\"-\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Value=\"&#xE7C3;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Value=\"&#xE8B7;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseRightButtonDown", xaml, StringComparison.Ordinal);
        Assert.Contains("NavigateFilesPaneNodeCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("OpenFilesPaneNodeExternallyCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("ShowFilesPaneNodePropertiesCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Files.Context.ShowHidden}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding NewFilesPaneFileCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding NewFilesPaneDirectoryCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding CutFilesPaneNodeCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding CopyFilesPaneNodeCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding PasteIntoFilesPaneCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding RenameFilesPaneNodeCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding MoveFilesPaneNodeToRecycleBinCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Files.Context.Delete}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeRenameTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeRenameTextBoxIsVisibleChanged", xaml, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeRenameTextBoxLostKeyboardFocus", xaml, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeRenameTextBoxPreviewKeyDown", xaml, StringComparison.Ordinal);
        Assert.Contains("Binding=\"{Binding IsRenaming}\" Value=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Foreground\" Value=\"White\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Background\" Value=\"{DynamicResource TreeSelectionBrush}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_WiresBottomPaneFocusTargets()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("IsChecked=\"{Binding BottomPane.IsOutputVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding BottomPane.IsDiagnosticsVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding CloseOutputPaneCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding CloseDiagnosticsPaneCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ResizeDirection=\"Rows\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ResizeBehavior=\"PreviousAndNext\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DataContext.BottomPane.IsVisible", xaml, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding BottomPane.IsVisible, Converter={StaticResource BooleanToVisibilityConverter}}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"BottomPaneTabs\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectionChanged=\"OnBottomPaneSelectionChanged\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"OutputTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsDataGrid\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectionChanged=\"OnDiagnosticsSelectionChanged\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_UsesFocusedOutputContextMenu()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("x:Name=\"OutputTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ContextMenuOpening=\"OnOutputTextBoxContextMenuOpening\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewMouseRightButtonDown=\"OnOutputTextBoxPreviewMouseRightButtonDown\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewMouseRightButtonUp=\"OnOutputTextBoxPreviewMouseRightButtonUp\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ContextMenu Style=\"{StaticResource ShellContextMenuStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"OutputContextMenu\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"OutputCopyMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"OutputClearMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"OutputSelectAllMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Bottom.Output.Clear}\"", xaml, StringComparison.Ordinal);

        var menuStart = xaml.IndexOf("AutomationProperties.AutomationId=\"OutputContextMenu\"", StringComparison.Ordinal);
        Assert.NotEqual(-1, menuStart);
        var menuEnd = xaml.IndexOf("</ContextMenu>", menuStart, StringComparison.Ordinal);
        Assert.NotEqual(-1, menuEnd);
        var outputMenu = xaml[menuStart..menuEnd];

        Assert.Contains("Style=\"{StaticResource DropDownMenuItemStyle}\"", outputMenu, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource MenuItemGlyphStyle}\"", outputMenu, StringComparison.Ordinal);
        Assert.Contains("Menu.Edit.Copy", outputMenu, StringComparison.Ordinal);
        Assert.Contains("Menu.Edit.SelectAll", outputMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("Menu.Edit.Cut", outputMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("Menu.Edit.Paste", outputMenu, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowCodeBehind_HandlesOutputContextMenuActions()
    {
        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("OnOutputTextBoxPreviewMouseRightButtonDown", code, StringComparison.Ordinal);
        Assert.Contains("OnOutputTextBoxPreviewMouseRightButtonUp", code, StringComparison.Ordinal);
        Assert.Contains("OnOutputTextBoxContextMenuOpening", code, StringComparison.Ordinal);
        Assert.Contains("OnOutputCopyMenuItemClick", code, StringComparison.Ordinal);
        Assert.Contains("OnOutputClearMenuItemClick", code, StringComparison.Ordinal);
        Assert.Contains("OnOutputSelectAllMenuItemClick", code, StringComparison.Ordinal);
        Assert.Contains("UpdateOutputContextMenuState", code, StringComparison.Ordinal);
        Assert.Contains("_consoleService.ClearOutput();", code, StringComparison.Ordinal);
        Assert.Contains("_outputSelectionStart", code, StringComparison.Ordinal);
        Assert.Contains("_outputSelectionLength", code, StringComparison.Ordinal);
        Assert.Contains("OutputTextBox.Select(_outputSelectionStart, _outputSelectionLength);", code, StringComparison.Ordinal);
        Assert.Contains("_preserveOutputSelectionOnRightClick", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_ExposesAutomationHooksForSmokeHarness()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("AutomationProperties.AutomationId=\"MainWindow\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"MainMenu\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FileMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FileCloseTabMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FormatMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"SearchMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"SearchFindMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"RunMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"CompileProgramMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"RunProgramMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ViewMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ViewFileManagerMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ViewOutputMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ViewDiagnosticsMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ViewStatusBarMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ShellStatusBar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarTextBlock\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarCompilationProgressHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarCompilationProgressBar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarCompilationProgressTextBlock\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarTimingHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarTimingTextBlock\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarPreparationTimingTextBlock\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarExecutionTimingTextBlock\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarTotalTimingTextBlock\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarCacheTimingTextBlock\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesPaneBorder\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FilesTreeView\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeView\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeContextMenu\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeNavigateMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeOpenExternalMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeShowHiddenFilesMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeNewFileMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeNewDirectoryMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeCutMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeCopyMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreePasteMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeRenameMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreeDeleteMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FilesTreePropertiesMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE8A7;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE890;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE8C6;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE8C8;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE77F;", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"DocumentTabs\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"BottomPaneTabs\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"OutputTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"DiagnosticsDataGrid\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"HelpMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"HelpOpenManualMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"HelpAboutMenuItem\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_WiresHelpMenuToManualAndAboutCommands()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Header=\"{DynamicResource Menu.Help}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Help.Manual}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding OpenManualCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Help.About}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding AboutCommand}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"{DynamicResource Menu.Help.About}\" IsEnabled=\"False\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_ExpandsEditorWhenFilesPaneIsHidden()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.DoesNotContain("Grid.Row=\"0\" Grid.Column=\"2\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding FilesPane.IsVisible, Converter={StaticResource BooleanToVisibilityConverter}}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Grid.Column\" Value=\"2\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Grid.ColumnSpan\" Value=\"1\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<DataTrigger Binding=\"{Binding FilesPane.IsVisible}\" Value=\"False\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Grid.Column\" Value=\"0\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Grid.ColumnSpan\" Value=\"3\" />", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_FileManagerPane_DoesNotOverlapBottomPaneRows()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("<Border Grid.Row=\"0\" Grid.Column=\"0\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Border Grid.Row=\"0\" Grid.RowSpan=\"3\" Grid.Column=\"0\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_FilesPaneSplitter_StopsAtBottomPaneBoundary()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("<GridSplitter Grid.Row=\"0\" Grid.Column=\"1\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<GridSplitter Grid.Row=\"0\" Grid.RowSpan=\"3\" Grid.Column=\"1\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void AppXaml_DoesNotUseStartupUriWhenStartupIsHandledInCode()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\App.xaml"), Encoding.UTF8);

        Assert.DoesNotContain("StartupUri=", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowCodeBehind_GuardsFindCommandsBeforeDocumentTabsAreReady()
    {
        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);
        var filesPaneCode = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.FilesPane.cs"), Encoding.UTF8);
        var visualTreeCode = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.VisualTree.cs"), Encoding.UTF8);

        Assert.Contains("if (DocumentTabs is null)", code, StringComparison.Ordinal);
        Assert.Contains("return null;", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowCodeBehind_TracksCaretAndBottomPaneFocus()
    {
        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);
        var filesPaneCode = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.FilesPane.cs"), Encoding.UTF8);
        var searchCode = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.Search.cs"), Encoding.UTF8);
        var visualTreeCode = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.VisualTree.cs"), Encoding.UTF8);

        Assert.Contains("OnEditorCaretPositionChanged", code, StringComparison.Ordinal);
        Assert.Contains("UpdateCaretStatusFromActiveEditor", code, StringComparison.Ordinal);
        Assert.Contains("OnBottomPanePropertyChanged", code, StringComparison.Ordinal);
        Assert.Contains("OnDiagnosticsSelectionChanged", code, StringComparison.Ordinal);
        Assert.Contains("AvalonEditDiagnosticNavigation.NavigateToDiagnostic", code, StringComparison.Ordinal);
        Assert.Contains("SetDiagnosticLineHighlight(editor, false);", code, StringComparison.Ordinal);
        Assert.Contains("SetDiagnosticLineHighlight(editor, true);", code, StringComparison.Ordinal);
        Assert.Contains("FocusRequestedBottomPaneTarget", code, StringComparison.Ordinal);
        Assert.Contains("DiagnosticsDataGrid.Focus()", code, StringComparison.Ordinal);
        Assert.Contains("RemoveDecorativeLineMargins", code, StringComparison.Ordinal);
        Assert.Contains("DottedLineMargin", code, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeItemExpanded", filesPaneCode, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeSelectedItemChanged", filesPaneCode, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeItemPreviewMouseRightButtonDown", filesPaneCode, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeRenameTextBoxIsVisibleChanged", filesPaneCode, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeRenameTextBoxLostKeyboardFocus", filesPaneCode, StringComparison.Ordinal);
        Assert.Contains("OnFilesTreeRenameTextBoxPreviewKeyDown", filesPaneCode, StringComparison.Ordinal);
        Assert.Contains("CommitFilesPaneNodeRename", filesPaneCode, StringComparison.Ordinal);
        Assert.Contains("CancelFilesPaneNodeRename", filesPaneCode, StringComparison.Ordinal);
        Assert.Contains("GetFilesTreeRenameSelectionLength", visualTreeCode, StringComparison.Ordinal);
        Assert.Contains("FocusFilesTreeItem", visualTreeCode, StringComparison.Ordinal);
        Assert.Contains("_suppressFilesTreeNodeActivation", code, StringComparison.Ordinal);
        Assert.Contains("OnEditorLostKeyboardFocus", code, StringComparison.Ordinal);
        Assert.Contains("OnEditorPreviewKeyDown", code, StringComparison.Ordinal);
        Assert.Contains("OnEditorPreviewMouseLeftButtonUp", code, StringComparison.Ordinal);
        Assert.Contains("PreviewKeyDown += OnEditorPreviewKeyDown", code, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseLeftButtonUp += OnEditorPreviewMouseLeftButtonUp", code, StringComparison.Ordinal);
        Assert.Contains("AvalonEditTextBinding.EnsureAttached(editor);", code, StringComparison.Ordinal);
        Assert.Contains("TryFormatEditorLine", code, StringComparison.Ordinal);
        Assert.Contains("AxiomBlockEditingService", code, StringComparison.Ordinal);
        Assert.Contains("TryApplyBlockEditing", code, StringComparison.Ordinal);
        Assert.Contains("TryHandleLineTransition", code, StringComparison.Ordinal);
        Assert.DoesNotContain("TryApplyBlockEditing(editor, previousLine, currentLine);", code, StringComparison.Ordinal);
        Assert.Contains("SelectedFormattingStyle", code, StringComparison.Ordinal);
        Assert.Contains("_codeFormattingService.FormatLine", code, StringComparison.Ordinal);
        Assert.Contains("var editor = sender as TextEditor ?? activeEditor;", code, StringComparison.Ordinal);
        Assert.Contains("e.Key != Key.Return", code, StringComparison.Ordinal);
        Assert.Contains("TryApplyBlockEditingAfterEnter", code, StringComparison.Ordinal);
        Assert.Contains("TraceEditorEvent(\"editor.enter.line-transition\"", code, StringComparison.Ordinal);
        Assert.Contains("TraceEditorEvent(\"editor.caret.line-transition\"", code, StringComparison.Ordinal);
        Assert.Contains("\"editor.line-format.applied\",", code, StringComparison.Ordinal);
        Assert.Contains("\"editor.identifier-normalized\",", code, StringComparison.Ordinal);
        Assert.Contains("DocumentFormattingPolicy.CanApplyFormatting(viewModel.Documents.ActiveDocument)", code, StringComparison.Ordinal);
        Assert.Contains("HandleEditorInteractionException(\"editor.caret.line-transition.exception\"", code, StringComparison.Ordinal);
        Assert.Contains("AxiomSmartIdentifierService", code, StringComparison.Ordinal);
        Assert.Contains("_findReplaceDialog.ShowDialog();", searchCode, StringComparison.Ordinal);
        Assert.Contains("_findReplaceDialog.PrepareForShow(selectSearchPattern);", searchCode, StringComparison.Ordinal);
        Assert.Contains("TraceService.Write(\"ui.theme.apply\"", code, StringComparison.Ordinal);
        Assert.Contains("ApplyCurrentLineHighlightStyle", code, StringComparison.Ordinal);
        Assert.Contains("UpdateCurrentLineHighlightVisibility", code, StringComparison.Ordinal);
        Assert.Contains("EditorCurrentLineHighlightBrush", code, StringComparison.Ordinal);
        Assert.Contains("EditorCurrentLineBorderPen", code, StringComparison.Ordinal);
        Assert.Contains("StoppedRunLineHighlightBrush", code, StringComparison.Ordinal);
        Assert.Contains("StoppedRunLineBorderPen", code, StringComparison.Ordinal);
        Assert.Contains("DebugExecutionSpanRenderer", code, StringComparison.Ordinal);
        Assert.Contains("StoppedRunExecutionSpanBrush", code, StringComparison.Ordinal);
        Assert.Contains("StoppedRunExecutionSpanBorderPen", code, StringComparison.Ordinal);
        Assert.Contains("SourceLocation.Span", code, StringComparison.Ordinal);
        Assert.Contains("NavigateToStoppedRunLine", code, StringComparison.Ordinal);
        Assert.Contains("nameof(MainWindowViewModel.StoppedRunLine)", code, StringComparison.Ordinal);
        Assert.Contains("StopDebuggingCommand", code, StringComparison.Ordinal);
        Assert.Contains("RestartDebuggingCommand", code, StringComparison.Ordinal);
        Assert.Contains("ModifierKeys.Shift", code, StringComparison.Ordinal);
        Assert.Contains("editor.IsKeyboardFocusWithin", code, StringComparison.Ordinal);
        Assert.Contains("OnStatusBarPathTextBlockMouseLeftButtonDown", code, StringComparison.Ordinal);
    }

    [Fact]
    public void FindReplaceDialogXaml_ContainsExpectedControls()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Editor\FindReplaceDialog.xaml"), Encoding.UTF8);
        var styles = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);

        Assert.Contains("x:Name=\"ModeTabControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FindTabItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ReplaceTabItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SearchPatternTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ReplacePatternTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ReverseDirectionCheckBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MatchCaseCheckBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"WholeWordsCheckBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"UseRegexCheckBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FindNextButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ReplaceButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ReplaceAllButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"CloseButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectionChanged=\"OnModeTabSelectionChanged\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<RowDefinition Height=\"Auto\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogTabControlStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("BasedOn=\"{StaticResource ShellTabItemStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FindReplaceTextBoxStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FindReplaceOptionsBorderStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FindReplaceActionButtonStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FindReplaceLastActionButtonStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FindReplaceDialogWindowStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"26\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"28\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<GroupBox", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"FindReplaceDialogWindowStyle\" TargetType=\"Window\" BasedOn=\"{StaticResource DialogWindowStyle}\">", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Width\" Value=\"640\" />", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"MaxHeight\" Value=\"420\" />", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void FindReplaceDialogXaml_ExposesAutomationHooksForSmokeHarness()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Editor\FindReplaceDialog.xaml"), Encoding.UTF8);

        Assert.Contains("AutomationProperties.AutomationId=\"FindReplaceDialog\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FindTabItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ReplaceTabItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"SearchPatternTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ReplacePatternTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ReverseDirectionCheckBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"UseRegexCheckBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FindNextButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ReplaceButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ReplaceAllButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"FindReplaceCloseButton\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TextInputDialogXaml_ExposesAutomationHooksForSmokeHarness()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Services\Prompts\TextInputDialog.xaml"), Encoding.UTF8);

        Assert.Contains("Title=\"{DynamicResource Prompts.DefaultTitle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"TextInputDialog\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"TextInputPromptTextBlock\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"TextInputTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"TextInputOkButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"TextInputCancelButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"{DynamicResource Prompts.Ok}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_KeepsStatusBarDockedAndGridAsLastChild()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("<DockPanel LastChildFill=\"True\">", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.View.StatusBar}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding StatusBar.IsVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding StatusBar.IsVisible, Converter={StaticResource BooleanToVisibilityConverter}}\"", xaml, StringComparison.Ordinal);

        var statusBarIndex = xaml.IndexOf("<StatusBar DockPanel.Dock=\"Bottom\"", StringComparison.Ordinal);
        var gridIndex = xaml.IndexOf("<!-- Main content area -->", StringComparison.Ordinal);

        Assert.True(statusBarIndex >= 0);
        Assert.True(gridIndex > statusBarIndex);
    }

    [Fact]
    public void MainWindowXaml_KeepsQuickAccessToolbarWithoutBottomBorder()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);
        var styles = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);

        var toolbarCommentIndex = xaml.IndexOf("<!-- Quick access toolbar -->", StringComparison.Ordinal);
        var statusBarCommentIndex = xaml.IndexOf("<!-- Status Bar -->", StringComparison.Ordinal);
        Assert.True(toolbarCommentIndex >= 0);
        Assert.True(statusBarCommentIndex > toolbarCommentIndex);

        var toolbarBlock = xaml[toolbarCommentIndex..statusBarCommentIndex];
        Assert.Contains("Style=\"{StaticResource QuickAccessToolbarBorderStyle}\"", toolbarBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("BorderThickness=\"0,0,0,1\"", toolbarBlock, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"QuickAccessToolbarBorderStyle\" TargetType=\"Border\">", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Background\" Value=\"{DynamicResource ToolbarBackgroundBrush}\" />", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"BorderThickness\" Value=\"0\" />", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_UsesCopyableStatusBarPathText()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);
        var styles = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);
        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("<TextBlock Text=\"{Binding StatusBar.PathText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StatusBarPathTextBlockStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{Binding StatusBar.PathText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MouseLeftButtonDown=\"OnStatusBarPathTextBlockMouseLeftButtonDown\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarPathContextMenu\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusBarPathCopyMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StatusBarPathSelectAllMenuItem", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("CommandTarget=\"{Binding PlacementTarget, RelativeSource={RelativeSource AncestorType=ContextMenu}}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnStatusBarPathCopyMenuItemClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StatusBarPathTextBlockStyle\" TargetType=\"TextBlock\">", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Foreground\" Value=\"{DynamicResource StatusBarForegroundBrush}\" />", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Cursor\" Value=\"Hand\" />", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"TextTrimming\" Value=\"CharacterEllipsis\" />", styles, StringComparison.Ordinal);
        Assert.Contains("OnStatusBarPathTextBlockMouseLeftButtonDown", code, StringComparison.Ordinal);
        Assert.Contains("OnStatusBarPathCopyMenuItemClick", code, StringComparison.Ordinal);
        Assert.Contains("Clipboard.SetText(text);", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_UsesCopyableDiagnosticsGrid()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);
        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("x:Name=\"DiagnosticsDataGrid\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MouseDoubleClick=\"OnDiagnosticsDataGridMouseDoubleClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"DiagnosticsContextMenu\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"DiagnosticsCopyMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnDiagnosticsCopyMenuItemClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.Edit.Copy}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OnDiagnosticsDataGridMouseDoubleClick", code, StringComparison.Ordinal);
        Assert.Contains("OnDiagnosticsCopyMenuItemClick", code, StringComparison.Ordinal);
        Assert.Contains("BuildDiagnosticClipboardText", code, StringComparison.Ordinal);
        Assert.Contains("diagnostic.Stage", code, StringComparison.Ordinal);
        Assert.Contains("diagnostic.Code", code, StringComparison.Ordinal);
        Assert.Contains("diagnostic.Message", code, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellStylesXaml_UsesClassicMenuGutterAndIndentedSeparators()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);

        Assert.Contains("<Style x:Key=\"MainMenuItemStyle\" TargetType=\"MenuItem\" BasedOn=\"{StaticResource ShellMenuItemStyle}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DropDownMenuItemStyle\" TargetType=\"MenuItem\" BasedOn=\"{StaticResource ShellMenuItemStyle}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Padding\" Value=\"7,4,7,4\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Padding\" Value=\"6,4\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("MinHeight=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"MenuGutterSeparatorMargin\">29,2,0,2</Thickness>", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate x:Key=\"ShellPopupMenuHostTemplate\" TargetType=\"ContentControl\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate x:Key=\"ShellPopupMenuScrollViewerTemplate\" TargetType=\"ScrollViewer\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellPopupMenuScrollViewerStyle\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource MenuGutterSeparatorMargin}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"Separator\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Margin\" Value=\"0\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("Background=\"{DynamicResource MenuSeparatorBrush}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellPopupMenuScrollViewerStyle}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_UsesQuickAccessIconToolbarUnderMenu()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("<!-- Quick access toolbar -->", xaml, StringComparison.Ordinal);
        Assert.Contains("QuickAccessButtonStyle", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE8A5;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE8B7;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE74E;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE7B8;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE768;", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding RunCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding RunOrContinueCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding StepBackCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding StepIntoCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding StepOverCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding StepOutCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StartDebuggingToolbarIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepBackToolbarIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepIntoToolbarIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepOverToolbarIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepOutToolbarIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("M6 10V9h9v1H6zm4-4h5v1h-5V6zm5-3v1H6V3h9zm-9 9v1h9v-1H6z", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding RestartDebuggingCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding StopDebuggingCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ClearAllSourceBreakpointsCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Toolbar.StartDebugging}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Toolbar.StepBack}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Toolbar.StepInto}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Toolbar.StepOver}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Toolbar.StepOut}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Toolbar.RestartDebugging}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Toolbar.StopDebugging}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{DynamicResource Toolbar.ClearAllBreakpoints}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE71A;", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE721;", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_UsesMenuIconsAndStyledTabStrip()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);
        var styles = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);

        Assert.Contains("MenuItem.Icon", xaml, StringComparison.Ordinal);
        Assert.Contains("DropDownMenuItemStyle", xaml, StringComparison.Ordinal);
        Assert.Contains("RunAccentBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("ShellTabItemStyle", xaml, StringComparison.Ordinal);
        Assert.Contains("<Condition Property=\"IsMouseOver\" Value=\"True\" />", styles, StringComparison.Ordinal);
        Assert.Contains("<Condition Property=\"IsSelected\" Value=\"False\" />", styles, StringComparison.Ordinal);
        Assert.Contains("<MultiTrigger>", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_ExposesRussianThemeMenu()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Header=\"{DynamicResource Menu.View.Theme}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.View.Theme.Light}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.View.Theme.Dark}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding UseLightThemeCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding UseDarkThemeCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsDarkThemeEnabled, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.View.Language}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding UseRussianLanguageCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding UseEnglishLanguageCommand}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_ExposesIdeEventLogViewerCommand()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Header=\"{DynamicResource Menu.View.EventLog}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource Menu.View.EventLog.Viewer}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowIdeEventLogViewerCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ViewEventLogViewerMenuItem\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void IdeEventLogViewerXaml_ExposesRefreshCopyAndDetailsControls()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Services\EventLog\IdeEventLogViewerWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Content=\"{DynamicResource EventLog.Viewer.RefreshButton}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"{DynamicResource EventLog.Viewer.CopySelectedButton}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{DynamicResource EventLog.Viewer.SelectedDetailsLabel}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogWindowStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FindReplaceTextBoxStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogComboBoxStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogButtonStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellContextMenuStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DiagnosticsGridStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.ColumnSpan=\"11\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SequenceColumn\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DetailsColumn\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ResizeDirection=\"Rows\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"IdeEventLogViewerDetailsGridSplitter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"IdeEventLogViewerDetailsPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"{DynamicResource EventLog.Viewer.CopyCellMenuItem}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"{DynamicResource EventLog.Viewer.CopySelectedTextMenuItem}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("OnCopySelectedDetailsTextClick", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE8C8;\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"{DynamicResource EventLog.Viewer.SelectAllMenuItem}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Click=\"OnSelectAllDetailsClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseRightButtonDown=\"OnEntriesDataGridPreviewMouseRightButtonDown\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectionChanged=\"OnEntrySelectionChanged\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"IdeEventLogViewerRefreshButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"IdeEventLogViewerCopySelectedButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"IdeEventLogViewerSelectedDetailsTextBox\"", xaml, StringComparison.Ordinal);

        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Services\EventLog\IdeEventLogViewerWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("IdeLanguageResourceService.LanguageApplied += OnLanguageApplied;", code, StringComparison.Ordinal);
        Assert.Contains("SequenceColumn.Header = IdeLanguageResourceService.GetCurrentString(\"EventLog.Viewer.SequenceHeader\", \"Seq\");", code, StringComparison.Ordinal);
        Assert.Contains("DetailsColumn.Header = IdeLanguageResourceService.GetCurrentString(\"EventLog.Viewer.DetailsHeader\", \"Details\");", code, StringComparison.Ordinal);
        Assert.DoesNotContain("OnSelectAllDetailsClick", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_WiresTabCloseButtonsToCloseTabCommand()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("CloseTabCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("CommandParameter=\"{Binding}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TabCloseButtonStyle", xaml, StringComparison.Ordinal);
        Assert.Contains("&#xE711;", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellStylesXaml_CustomizesLineNumberGutter()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);

        Assert.Contains("TargetType=\"{x:Type editing:TextArea}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{StaticResource LineNumberMarginMinWidth}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Background=\"{DynamicResource LineNumberMarginBackgroundBrush}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Padding=\"8,0,6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Path=LeftMargins", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowCode_WiresBreakpointMarginToSourceBreakpointLines()
    {
        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("BreakpointMargin", code, StringComparison.Ordinal);
        Assert.Contains("_breakpointMarginsByEditor", code, StringComparison.Ordinal);
        Assert.Contains("TextArea.LeftMargins.Insert(0, margin)", code, StringComparison.Ordinal);
        Assert.Contains("nameof(MainWindowViewModel.SourceBreakpointLines)", code, StringComparison.Ordinal);
        Assert.Contains("RefreshBreakpointMargins", code, StringComparison.Ordinal);
        Assert.Contains("BreakpointMarkerBrush", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_ExposesStopDebuggingCommand()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Menu.Debug.StopDebugging", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding StopDebuggingCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"Shift+F5\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StopDebuggingMenuItem\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_LabelsBreakpointCommandWithShortcut()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Menu.Debug.ToggleBreakpoint", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"F9\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ToggleBreakpointMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"EditorToggleBreakpointMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ToggleBreakpointMenuIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"EditorToggleBreakpointMenuIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Fill=\"{DynamicResource BreakpointMarkerBrush}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"&#xE7BA;\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_ExposesStartDebuggingAndClearAllBreakpoints()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);
        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("Menu.Debug", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"DebugMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Menu.Debug.StartDebugging", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding RunOrContinueCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"F5\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StartDebuggingMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepBackMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepBackMenuIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepIntoMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepIntoMenuIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepOverMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepOverMenuIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepOutMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StepOutMenuIcon\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Menu.Debug.Continue", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("AutomationProperties.AutomationId=\"ContinueProgramMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StartDebuggingMenuIcon\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("AutomationProperties.AutomationId=\"ContinueDebuggingMenuIcon\"", xaml, StringComparison.Ordinal);
        Assert.Contains("M6 10V9h9v1H6zm4-4h5v1h-5V6zm5-3v1H6V3h9zm-9 9v1h9v-1H6z", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding RunCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"Ctrl+F5\"", xaml, StringComparison.Ordinal);
        var runProgramIndex = xaml.IndexOf("AutomationProperties.AutomationId=\"RunProgramMenuItem\"", StringComparison.Ordinal);
        var debugStartIndex = xaml.IndexOf("AutomationProperties.AutomationId=\"StartDebuggingMenuItem\"", StringComparison.Ordinal);
        var executionModeIndex = xaml.IndexOf("AutomationProperties.AutomationId=\"ExecutionModeMenuItem\"", StringComparison.Ordinal);
        var separatorAfterRunIndex = xaml.IndexOf("<Separator />", runProgramIndex, StringComparison.Ordinal);
        Assert.True(
            runProgramIndex >= 0
            && separatorAfterRunIndex > runProgramIndex
            && separatorAfterRunIndex < executionModeIndex
            && debugStartIndex > executionModeIndex,
            "Run Program should stay in Run menu while debugging commands live in Debug menu.");
        Assert.Contains("Menu.Debug.StepOver", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"F10\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Menu.Debug.StepBack", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding StepBackCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"Ctrl+F10\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Menu.Debug.StepInto", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"F11\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Menu.Debug.StepOut", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"Shift+F11\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Menu.Debug.ClearAllBreakpoints", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ClearAllSourceBreakpointsCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnClearAllBreakpointsMenuItemClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ClearAllBreakpointsMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"EditorClearAllBreakpointsMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OnClearAllBreakpointsMenuItemClick", code, StringComparison.Ordinal);
        Assert.Contains("DataContext.RunCommand", code, StringComparison.Ordinal);
        Assert.Contains("DataContext.StepBackCommand", code, StringComparison.Ordinal);
        Assert.Contains("Key.F10, ModifierKeys.Control", code, StringComparison.Ordinal);
        Assert.Contains("DataContext.StepOverCommand", code, StringComparison.Ordinal);
        Assert.Contains("Key.F10, ModifierKeys.None", code, StringComparison.Ordinal);
        Assert.Contains("DataContext.StepIntoCommand", code, StringComparison.Ordinal);
        Assert.Contains("Key.F11, ModifierKeys.None", code, StringComparison.Ordinal);
        Assert.Contains("DataContext.StepOutCommand", code, StringComparison.Ordinal);
        Assert.Contains("Key.F11, ModifierKeys.Shift", code, StringComparison.Ordinal);
        Assert.Contains("DataContext.ClearAllSourceBreakpointsCommand", code, StringComparison.Ordinal);
        Assert.Contains("Key.F9, ModifierKeys.Control | ModifierKeys.Shift", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowXaml_ExposesRestartDebuggingCommand()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml"), Encoding.UTF8);
        var code = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\MainWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("Menu.Debug.RestartDebugging", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding RestartDebuggingCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"Ctrl+Shift+F5\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"RestartDebuggingMenuItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DataContext.RestartDebuggingCommand", code, StringComparison.Ordinal);
        Assert.Contains("ModifierKeys.Control | ModifierKeys.Shift", code, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellStylesXaml_EnablesFilesTreeScrollbars()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);

        Assert.Contains("x:Key=\"FilesTreeStyle\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"ScrollViewer.VerticalScrollBarVisibility\" Value=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"ScrollViewer.HorizontalScrollBarVisibility\" Value=\"Auto\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellStylesXaml_UsesCustomTemplatesForMenuAndContextMenu()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);

        Assert.Contains("<ControlTemplate x:Key=\"ShellTopLevelItemTemplate\" TargetType=\"{x:Type MenuItem}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate x:Key=\"ShellTopLevelHeaderTemplate\" TargetType=\"{x:Type MenuItem}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate x:Key=\"ShellSubmenuItemTemplate\" TargetType=\"{x:Type MenuItem}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate x:Key=\"ShellSubmenuHeaderTemplate\" TargetType=\"{x:Type MenuItem}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellMenuItemStyle\" TargetType=\"{x:Type MenuItem}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"MainMenuItemStyle\" TargetType=\"MenuItem\" BasedOn=\"{StaticResource ShellMenuItemStyle}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DropDownMenuItemStyle\" TargetType=\"MenuItem\" BasedOn=\"{StaticResource ShellMenuItemStyle}\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Popup x:Name=\"PART_Popup\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate x:Key=\"ShellPopupMenuHostTemplate\" TargetType=\"ContentControl\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate x:Key=\"ShellPopupMenuScrollViewerTemplate\" TargetType=\"ScrollViewer\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellPopupMenuScrollViewerStyle\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"MenuGutterSeparatorMargin\">29,2,0,2</Thickness>", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellContextMenuStyle\" TargetType=\"ContextMenu\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"ContextMenu\" BasedOn=\"{StaticResource ShellContextMenuStyle}\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"MenuItem\" BasedOn=\"{StaticResource DropDownMenuItemStyle}\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"OverridesDefaultStyle\" Value=\"True\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate TargetType=\"ContextMenu\">", xaml, StringComparison.Ordinal);
        Assert.Contains("MenuHighlightBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("MenuHighlightBorderBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"Separator\">", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource MenuGutterSeparatorMargin}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Fill=\"{DynamicResource MenuSeparatorBrush}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Background=\"{DynamicResource MenuSeparatorBrush}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellStylesXaml_ThemesComboBoxAndTextBoxContextMenus()
    {
        var xaml = File.ReadAllText(GetRepositoryFilePath(@"src\Axiom.Ide\Resources\ShellStyles.xaml"), Encoding.UTF8);

        Assert.Contains("<Style x:Key=\"DialogComboBoxStyle\" TargetType=\"ComboBox\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate TargetType=\"ComboBox\">", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ContentSite\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TextElement.Foreground=\"{DynamicResource TextPrimaryBrush}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Popup x:Name=\"PART_Popup\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"ComboBoxItem\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<ContextMenu x:Key=\"TextBoxEditingContextMenu\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellContextMenuStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"ContextMenu\" Value=\"{StaticResource TextBoxEditingContextMenu}\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"IsInactiveSelectionHighlightEnabled\" Value=\"True\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE8C6;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE8C8;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE77F;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE8B3;\"", xaml, StringComparison.Ordinal);
    }

    private static string GetRepositoryFilePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Repository file not found: {relativePath}");
    }

}

