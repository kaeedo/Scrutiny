using Scrutiny.CSharp;
using OpenQA.Selenium;

namespace UsageExample.CSharp.Pages
{
    public class GlobalState
    {
        public IWebDriver Driver { get; }

        public GlobalState(IWebDriver driver)
        {
            Driver = driver;
        }
    }


    [PageState]
    public class SignIn
    {
        private readonly GlobalState globalState;

        public SignIn(GlobalState globalState)
        {
            this.globalState = globalState;
        }

        [TransitionTo(nameof(Home))]
        public void ClickOnHome()
        {
            System.Threading.Thread.Sleep(5000);
            globalState.Driver.FindElement(By.Id("home")).Click();
        }
    }

    [PageState]
    public class Home
    {
        private readonly GlobalState globalState;

        public Home(GlobalState globalState)
        {
            this.globalState = globalState;
        }

        [TransitionTo(nameof(SignIn))]
        public void ClickOnSignIn()
        {
            System.Threading.Thread.Sleep(5000);
            globalState.Driver.FindElement(By.Id("signin")).Click();
        }
    }
}
