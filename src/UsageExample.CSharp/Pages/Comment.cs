using System;
using System.Collections.Generic;
using OpenQA.Selenium.Remote;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    public class Comment : PageState<GlobalState, object>
    {
        private readonly RemoteWebDriver _driver;
        private readonly GlobalState _globalState;

        public Comment(RemoteWebDriver driver, GlobalState globalState) : base("Comment")
        {
            _driver = driver;
            _globalState = globalState;
        }

        public override void OnEnter()
        {
            Console.WriteLine("Checking on page comment");
            var header = _driver.FindElementById("header");

            Assert.True(header.Text == "Comments");
        }

        public override IEnumerable<Action> Actions(object _)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Func<PageState<GlobalState, object>>> Transitions()
        {
            Func<Home> goToHome = () =>
            {
                _driver.FindElementById("home").Click();
                return new Home(_driver, _globalState);
            };

            Func<SignIn> goToSignIn = () =>
            {
                _driver.FindElementById("signin").Click();
                return new SignIn(_driver, _globalState);
            };

            return new List<Func<PageState<GlobalState, object>>>
            {
                goToHome,
                goToSignIn
            };
        }

        public override void OnExit()
        {
            Console.WriteLine("Exiting Home");
        }
    }
}
