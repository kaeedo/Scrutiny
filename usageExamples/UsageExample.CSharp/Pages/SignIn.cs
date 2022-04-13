using System.Threading.Tasks;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    [PageState]
    public class SignIn
    {
        private readonly GlobalState globalState;

        public SignIn(GlobalState globalState)
        {
            this.globalState = globalState;
            globalState.Logger.WriteLine($"Constructing {nameof(SignIn)}");
        }

        [OnEnter]
        public async Task OnEnter()
        {
            globalState.Logger.WriteLine("Checking on page sign in");
        
            var headerText = await globalState.Page.InnerTextAsync("#header");
        
            Assert.Equal("Sign In", headerText);
        }

        [OnExit]
        public void OnExit()
        {
            globalState.Logger.WriteLine("Exiting sign in");
        }

        [TransitionTo(nameof(Home))]
        public async Task ClickOnHome()
        {
            await globalState.Page.ClickAsync("#home");
        }

        [TransitionTo(nameof(LoggedInHome))]
        public async Task LogInAndTransitionToHome()
        {
            globalState.Username = "kaeedo";
            await globalState.Page.FillAsync("#username", globalState.Username);
            await globalState.Page.FillAsync("#number", globalState.Number.ToString());

            globalState.IsSignedIn = true;

            await globalState.Page.ClickAsync("css=button >> text=Sign In");
        }

        [Action]
        public async Task FillInUsername()
        {
            globalState.Logger.WriteLine("Sign in: filling username");
            await globalState.Page.FillAsync("#username", "MyUsername");

            var username = await globalState.GetInputValueAsync("#username");

            Assert.Equal("MyUsername", username);
        }

        [Action]
        public async Task FillInNumber()
        {
            globalState.Logger.WriteLine("Sign in: filling number");
            await globalState.Page.FillAsync("#number", "42");

            var number = await globalState.GetInputValueAsync("#number");

            Assert.Equal("42", number);
        }

        [ExitAction]
        public async Task ExitAction()
        {
            globalState.Logger.WriteLine("Exiting!");
            await globalState.Page.CloseAsync();
        }

        [Action]
        public async Task ForceInvalidForm()
        {
            var username = await globalState.GetInputValueAsync("#username");
            var number = await globalState.GetInputValueAsync("#number");

            var signInButtonSelector = "css=button >> text=Sign In";

            if(string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(number))
            {
                await globalState.Page.ClickAsync(signInButtonSelector);
            }
            else
            {
                await globalState.Page.FillAsync("#username", string.Empty);
                await globalState.Page.ClickAsync(signInButtonSelector);
            }

            var errorMessage = await globalState.Page.QuerySelectorAsync("#ErrorMessage");

            Assert.NotNull(errorMessage);

            var displayState = await errorMessage.EvaluateAsync<string>("e => e.style.display");

            Assert.NotEqual("none", displayState);
        }
    }
}