using System.Threading.Tasks;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages;

[PageState]
public class SignIn
{
    private readonly GlobalState _globalState;

    public SignIn(GlobalState globalState)
    {
        _globalState = globalState;
        globalState.Logger.WriteLine($"Constructing {nameof(SignIn)}");
    }

    [OnEnter]
    public async Task OnEnter()
    {
        _globalState.Logger.WriteLine("Checking on page sign in");

        var headerText = await _globalState.Page.InnerTextAsync("id=header");

        Assert.Equal("Sign In", headerText);
    }

    [OnExit]
    public void OnExit()
    {
        _globalState.Logger.WriteLine("Exiting sign in");
    }

    [TransitionTo(nameof(Home))]
    public async Task ClickOnHome()
    {
        await _globalState.Page.ClickAsync("id=home");
    }

    [TransitionTo(nameof(LoggedInHome))]
    public async Task LogInAndTransitionToHome()
    {
        _globalState.Username = "kaeedo";
        await _globalState.Page.FillAsync("id=username", _globalState.Username);
        await _globalState.Page.FillAsync("id=number", _globalState.Number.ToString());

        _globalState.IsSignedIn = true;

        await _globalState.Page.ClickAsync("css=button >> text=Sign In");
    }

    [Action]
    public async Task FillInUsername()
    {
        _globalState.Logger.WriteLine("Sign in: filling username");
        await _globalState.Page.FillAsync("id=username", "MyUsername");

        var username = await _globalState.GetInputValueAsync("id=username");

        Assert.Equal("MyUsername", username);
    }

    [Action]
    public async Task FillInNumber()
    {
        _globalState.Logger.WriteLine("Sign in: filling number");
        await _globalState.Page.FillAsync("id=number", "42");

        var number = await _globalState.GetInputValueAsync("id=number");

        Assert.Equal("42", number);
    }

    [Action(IsExit = true)]
    public async Task ExitAction()
    {
        _globalState.Logger.WriteLine("Exiting!");
        await _globalState.Page.CloseAsync();
    }

    [Action]
    public async Task ForceInvalidForm()
    {
        var username = await _globalState.GetInputValueAsync("id=username");
        var number = await _globalState.GetInputValueAsync("id=number");

        var signInButtonSelector = "css=button >> text=Sign In";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(number))
        {
            await _globalState.Page.ClickAsync(signInButtonSelector);
        }
        else
        {
            await _globalState.Page.FillAsync("id=username", string.Empty);
            await _globalState.Page.ClickAsync(signInButtonSelector);
        }

        var errorMessage = await _globalState.Page.QuerySelectorAsync("id=ErrorMessage");

        Assert.NotNull(errorMessage);

        var displayState = await errorMessage.EvaluateAsync<string>("e => e.style.display");

        Assert.NotEqual("none", displayState);
    }
}
