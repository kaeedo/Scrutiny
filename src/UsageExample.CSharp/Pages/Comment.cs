using OpenQA.Selenium.Remote;
using Scrutiny.CSharp;

namespace UsageExample.CSharp.Pages
{
    public class Comment : PageState<GlobalState>
    {
        private readonly RemoteWebDriver _driver;
        private readonly GlobalState _globalState;

        public Comment(RemoteWebDriver driver, GlobalState globalState) : base("Comment")
        {
            _driver = driver;
            _globalState = globalState;
        }
    }
}
