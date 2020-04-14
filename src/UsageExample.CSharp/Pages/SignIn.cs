using System;
using System.Collections.Generic;
using OpenQA.Selenium.Remote;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    public class SignIn : PageState<GlobalState, object>
    {
        private readonly RemoteWebDriver _driver;
        private readonly GlobalState _globalState;

        public SignIn(RemoteWebDriver driver, GlobalState globalState) : base("Sign In")
        {
            _driver = driver;
            _globalState = globalState;
        }

        public override void OnEnter()
        {
            Console.WriteLine("Checking on page sign in");
            var header = _driver.FindElementById("header");

            Assert.True(header.Text == "Sign In");
        }

        public override IEnumerable<Action> Actions()
        {
            return null;
        }

        public override IEnumerable<Func<PageState<GlobalState, object>>> Transitions()
        {
            Func<Home> goToHome = () =>
            {
                _driver.FindElementById("home").Click();
                return new Home(_driver, _globalState);
            };

            Func<Home> goToSignIn = () =>
            {
                _driver.FindElementById("signin").Click();
                return new Home(_driver, _globalState);
            };

            return new List<Func<PageState<GlobalState, object>>>
            {
                goToHome,
                goToSignIn
            };
        }

        public override void OnExit()
        {
            Console.WriteLine("Exiting sign in");
        }
    }
}
