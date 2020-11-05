using OpenQA.Selenium;
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
        public void ClickOnHome()
        {
            System.Threading.Thread.Sleep(5000);
            globalState.Driver.FindElement(By.Id("home")).Click();
        }
    }
}
