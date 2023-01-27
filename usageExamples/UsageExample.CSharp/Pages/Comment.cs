using System.Threading.Tasks;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages;

[PageState]
public class Comment
{
    private readonly GlobalState _globalState;

    public Comment(GlobalState globalState)
    {
        _globalState = globalState;
        globalState.Logger.WriteLine($"Constructing {nameof(Comment)}");
    }

    [OnEnter]
    public async Task OnEnter()
    {
        _globalState.Logger.WriteLine("Checking on page comment");
        var headerText = await _globalState.Page.InnerTextAsync("id=header");

        Assert.Equal("Comments", headerText);
    }

    [Action]
    public void FindText()
    {
        var text = _globalState.Page.GetByText("Sign in to comment");

        Assert.NotNull(text);
    }

    [Action(IsExit = true)]
    [DependantAction(nameof(FindText))]
    public async Task ExitAction()
    {
        _globalState.Logger.WriteLine("Exiting!");
        await _globalState.Page.CloseAsync();
    }

    [OnExit]
    public void OnExit()
    {
        _globalState.Logger.WriteLine("Exiting comment");
    }

    [TransitionTo(nameof(SignIn))]
    public async Task ClickOnSignIn()
    {
        await _globalState.Page.ClickAsync("id=signin");
    }

    [TransitionTo(nameof(Home))]
    public async Task ClickOnHome()
    {
        await _globalState.Page.ClickAsync("id=home");
    }
}
