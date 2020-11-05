using Scrutiny.CSharp;
using OpenQA.Selenium;
using System;
using NUnit.Framework;

namespace UsageExample.CSharp.Pages
{
    [PageState]
    public class Home
    {
        private readonly GlobalState globalState;

        public Home(GlobalState globalState)
        {
            this.globalState = globalState;
        }

        [OnEnter]
        public void OnEnter()
        {
            Console.WriteLine("Checking on page home");
            var headerText = globalState.Driver.FindElement(By.Id("header"));

            Assert.AreEqual("Home", headerText.Text);
        }

        [OnExit]
        public void OnExit()
        {
            Console.WriteLine("Exiting home");
        }

        [TransitionTo(nameof(SignIn))]
        public void ClickOnSignIn()
        {
            System.Threading.Thread.Sleep(5000);
            globalState.Driver.FindElement(By.Id("signin")).Click();
        }

        [TransitionTo(nameof(Comment))]
        public void ClickOnComment()
        {
            System.Threading.Thread.Sleep(5000);
            globalState.Driver.FindElement(By.Id("comment")).Click();
        }
    }
}
