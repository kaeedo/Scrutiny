using Scrutiny.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    [PageState]
    public class Home
    {
        private readonly GlobalState globalState;

        public Home(GlobalState globalState)
        {
            this.globalState = globalState;
            globalState.Logger.WriteLine($"Constructing {nameof(Home)}");
        }

        [OnEnter]
        public async Task OnEnter()
        {
            globalState.Logger.WriteLine("Checking on page home");
            var headerText = await globalState.Page.InnerTextAsync("#header");

            Assert.Equal("Home", headerText);
        }

        [OnExit]
        public void OnExit()
        {
            globalState.Logger.WriteLine("Exiting home");
        }

        [ExitAction]
        public async Task ExitAction()
        {
            globalState.Logger.WriteLine("Exiting!");
            await globalState.Page.CloseAsync();
        }

        [TransitionTo(nameof(SignIn))]
        public async Task ClickOnSignIn()
        {
            await globalState.Page.ClickAsync("#signin");
        }

        [TransitionTo(nameof(Comment))]
        public async Task ClickOnComment()
        {
            await globalState.Page.ClickAsync("#comment");
        }
    }
}
