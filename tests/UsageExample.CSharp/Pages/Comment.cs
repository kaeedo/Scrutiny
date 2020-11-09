using System.Threading.Tasks;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    [PageState]
    public class Comment
    {
        private readonly GlobalState globalState;

        public Comment(GlobalState globalState)
        {
            this.globalState = globalState;
            globalState.Logger.WriteLine($"Constructing {nameof(Comment)}");
        }

        [OnEnter]
        public async Task OnEnter()
        {
            globalState.Logger.WriteLine("Checking on page comment");
            var headerText = await globalState.Page.GetInnerTextAsync("#header");

            Assert.Equal("Comments", headerText);
        }

        [OnExit]
        public void OnExit()
        {
            globalState.Logger.WriteLine("Exiting comment");
        }

        [TransitionTo(nameof(SignIn))]
        public async Task ClickOnSignIn()
        {
            await globalState.Page.ClickAsync("#signin");
        }

        [TransitionTo(nameof(Home))]
        public async Task ClickOnHome()
        {
            await globalState.Page.ClickAsync("#home");
        }
    }
}
