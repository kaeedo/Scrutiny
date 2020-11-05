using System.Threading.Tasks;
using Scrutiny.CSharp;

namespace UsageExample.CSharp.Pages
{
    [PageState]
    public class SignIn
    {
        private readonly GlobalState globalState;

        public SignIn(GlobalState globalState)
        {
            this.globalState = globalState;
        }

        [TransitionTo(nameof(Home))]
        public async Task ClickOnHome()
        {
            await globalState.Page.ClickAsync("#home");
        }
    }
}
