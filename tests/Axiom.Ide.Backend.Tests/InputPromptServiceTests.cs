using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Backend.Tests;

public sealed class InputPromptServiceTests
{
    [Fact]
    public void CurrentInputText_InitiallyEmpty()
    {
        var service = new InputPromptService();

        Assert.Equal(string.Empty, service.CurrentInputText);
    }

    [Fact]
    public void CurrentPromptText_InitiallyEmpty()
    {
        var service = new InputPromptService();

        Assert.Equal(string.Empty, service.CurrentPromptText);
    }

    [Fact]
    public void IsWaitingForInput_InitiallyFalse()
    {
        var service = new InputPromptService();

        Assert.False(service.IsWaitingForInput);
    }

    [Fact]
    public void AppendInputCharacter_AddsCharacter()
    {
        var service = new InputPromptService();
        service.IsWaitingForInput = true;

        service.AppendInputCharacter('A');

        Assert.Equal("A", service.CurrentInputText);
    }

    [Fact]
    public void AppendInputCharacter_IgnoresWhenNotWaiting()
    {
        var service = new InputPromptService();

        service.AppendInputCharacter('A');

        Assert.Equal(string.Empty, service.CurrentInputText);
    }

    [Fact]
    public void AppendInputCharacter_IgnoresControlCharacters()
    {
        var service = new InputPromptService();
        service.IsWaitingForInput = true;

        service.AppendInputCharacter('\n');

        Assert.Equal(string.Empty, service.CurrentInputText);
    }

    [Fact]
    public void BackspaceInputCharacter_RemovesLastCharacter()
    {
        var service = new InputPromptService();
        service.IsWaitingForInput = true;
        service.CurrentInputText = "AB";

        service.BackspaceInputCharacter();

        Assert.Equal("A", service.CurrentInputText);
    }

    [Fact]
    public void BackspaceInputCharacter_DoesNothingWhenEmpty()
    {
        var service = new InputPromptService();
        service.IsWaitingForInput = true;

        service.BackspaceInputCharacter();

        Assert.Equal(string.Empty, service.CurrentInputText);
    }

    [Fact]
    public void BackspaceInputCharacter_IgnoresWhenNotWaiting()
    {
        var service = new InputPromptService();
        service.CurrentInputText = "AB";

        service.BackspaceInputCharacter();

        Assert.Equal("AB", service.CurrentInputText);
    }

    [Fact]
    public void ResetInputState_ClearsAllState()
    {
        var service = new InputPromptService();
        service.IsWaitingForInput = true;
        service.CurrentInputText = "hello";
        service.CurrentPromptText = "? ";

        service.ResetInputState();

        Assert.False(service.IsWaitingForInput);
        Assert.Equal(string.Empty, service.CurrentInputText);
        Assert.Equal(string.Empty, service.CurrentPromptText);
    }

    [Fact]
    public void SubmitInput_DoesNothingWhenNoPendingFrame()
    {
        var service = new InputPromptService();
        var submitCalled = false;

        service.SubmitInput((_, _, _) => { submitCalled = true; }, false);

        Assert.False(submitCalled);
    }

    [Fact]
    public void CancelPendingFrame_DoesNothingWhenNoPendingFrame()
    {
        var service = new InputPromptService();

        service.CancelPendingFrame();

        // Should not throw
    }
}
