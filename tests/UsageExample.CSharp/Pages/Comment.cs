using OpenQA.Selenium;
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
        public void ClickOnSignIn()
        {
            System.Threading.Thread.Sleep(5000);
            globalState.Driver.FindElement(By.Id("signin")).Click();
        }

        [TransitionTo(nameof(Home))]
        public void ClickOnHome()
        {
            System.Threading.Thread.Sleep(5000);
            globalState.Driver.FindElement(By.Id("home")).Click();
        }
    }
}
