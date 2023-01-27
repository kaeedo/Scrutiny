using System.Threading.Tasks;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages;

[PageState]
public class LoggedInHome
{
    private readonly GlobalState _globalState;

    public LoggedInHome(GlobalState globalState)
    {
        _globalState = globalState;
        globalState.Logger.WriteLine($"Constructing {nameof(LoggedInHome)}");
    }

    [OnEnter]
    public async Task OnEnter()
    {
        _globalState.Logger.WriteLine("Checking on page home logged in");
        var header = await _globalState.Page.QuerySelectorAsync("id=header");

        Assert.NotNull(header);

        var displayState = await header.EvaluateAsync("e => e.style.display");

        Assert.False(displayState.ToString() == "none");

        var welcomeText = await _globalState.Page.InnerTextAsync("id=welcomeText");

        Assert.Equal($"Welcome {_globalState.Username}", welcomeText);
    }

    [Action(IsExit = true)]
    public async Task ExitAction()
    {
        _globalState.Logger.WriteLine("Exiting!");
        await _globalState.Page.ClickAsync("id=logout");
    }

    [TransitionTo(nameof(Home))]
    public async Task ClickOnSignIn()
    {
        await _globalState.Page.ClickAsync("id=logout");
    }

    [TransitionTo(nameof(LoggedInComment))]
    public async Task ClickOnComment()
    {
        await _globalState.Page.ClickAsync("id=comment");
    }
}
