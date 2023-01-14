using System.Threading.Tasks;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages;

[PageState]
public class Home
{
    private readonly GlobalState _globalState;

    public Home(GlobalState globalState)
    {
        _globalState = globalState;
        globalState.Logger.WriteLine($"Constructing {nameof(Home)}");
    }

    [OnEnter]
    public async Task OnEnter()
    {
        _globalState.Logger.WriteLine("Checking on page home");
        var headerText = await _globalState.Page.InnerTextAsync("id=header");

        Assert.Equal("Home", headerText);
    }

    [OnExit]
    public void OnExit()
    {
        _globalState.Logger.WriteLine("Exiting home");
    }

    [Action(IsExit = true)]
    public async Task ExitAction()
    {
        _globalState.Logger.WriteLine("Exiting!");
        await _globalState.Page.CloseAsync();
    }

    [TransitionTo(nameof(SignIn))]
    public async Task ClickOnSignIn()
    {
        await _globalState.Page.ClickAsync("id=signin");
    }

    [TransitionTo(nameof(Comment))]
    public async Task ClickOnComment()
    {
        await _globalState.Page.ClickAsync("id=comment");
    }
}
