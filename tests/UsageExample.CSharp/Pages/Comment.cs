using System.Threading.Tasks;
using Scrutiny.CSharp;

namespace UsageExample.CSharp.Pages
{
    [PageState]
    public class Comment
    {
        private readonly GlobalState globalState;

        public Comment(GlobalState globalState)
        {
            this.globalState = globalState;
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
