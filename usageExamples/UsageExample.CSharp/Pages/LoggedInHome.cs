using Scrutiny.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    [PageState]
    public class LoggedInHome
    {
        private readonly GlobalState globalState;

        public LoggedInHome(GlobalState globalState)
        {
            this.globalState = globalState;
            globalState.Logger.WriteLine($"Constructing {nameof(LoggedInHome)}");
        }

        [OnEnter]
        public async Task OnEnter()
        {
            globalState.Logger.WriteLine("Checking on page home logged in");
            var header = await globalState.Page.QuerySelectorAsync("#header");

            Assert.NotNull(header);

            var displayState = await header.EvaluateAsync("e => e.style.display");

            Assert.False(displayState.ToString() == "none");

            var welcomeText = await globalState.Page.InnerTextAsync("#welcomeText");

            Assert.Equal($"Welcome {globalState.Username}", welcomeText);
        }

        [ExitAction]
        public async Task ExitAction()
        {
            globalState.Logger.WriteLine("Exiting!");
            await globalState.Page.ClickAsync("#logout");
        }

        [TransitionTo(nameof(Home))]
        public async Task ClickOnSignIn()
        {
            await globalState.Page.ClickAsync("#logout");
        }

        [TransitionTo(nameof(LoggedInComment))]
        public async Task ClickOnComment()
        {
            await globalState.Page.ClickAsync("#comment");
        }
    }
}
