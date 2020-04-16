using OpenQA.Selenium.Remote;
using Scrutiny.CSharp;
using System;
using System.Collections.Generic;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    public class Home : PageState<GlobalState, object>
    {
        private readonly RemoteWebDriver _driver;
        private readonly GlobalState _globalState;

        public Home(RemoteWebDriver driver, GlobalState globalState) : base("Home")
        {
            _driver = driver;
            _globalState = globalState;
        }

        public override void OnEnter()
        {
            Console.WriteLine("Checking on page home");
            var header = _driver.FindElementById("header");

            Assert.True(header.Text == "Home");
        }

        public override void OnExit()
        {
            Console.WriteLine("Exiting Home");
        }

        public override IEnumerable<Func<PageState<GlobalState, object>>> Transitions()
        {
            Func<Comment> goToComment = () =>
            {
                _driver.FindElementById("comment").Click();
                return new Comment(_driver, _globalState);
            };

            return new List<Func<PageState<GlobalState, object>>>
            {
                goToComment,
                GoToSignIn
            };
        }

        private SignIn GoToSignIn()
        {
            _driver.FindElementById("signin").Click();
            return new SignIn(_driver, _globalState);
        }
    }
}
