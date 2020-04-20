using System;
using System.Collections.Generic;
using OpenQA.Selenium.Remote;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    public class SignIn : PageState<GlobalState>
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
            Action formValidation = () =>
            {
                var username = _driver.FindElementById("#username").Text;
                var number = _driver.FindElementById("#number").Text;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(number))
                {
                    _driver.FindElementByLinkText("Sign In").Click();
                }
                else
                {
                    _driver.FindElementById("#username").Clear();
                    _driver.FindElementByLinkText("Sign In").Click();
                }

                Assert.True(_driver.FindElementById("ErrorMessage").Displayed);
            };

            return new List<Action>
            {
                () => WriteAndAssert("#username", "MyUsername"),
                () => WriteAndAssert("#number", "42"),
                formValidation
            };
        }

        public override IEnumerable<Func<PageState<GlobalState>>> Transitions()
        {
            Func<Home> goToHome = () =>
            {
                _driver.FindElementById("home").Click();
                return new Home(_driver, _globalState);
            };

            Func<LoggedInHome> goToLoggedInHome = () =>
            {
                _globalState.Username = "kaeedo";

                _driver.FindElementById("#username").SendKeys(_globalState.Username);
                _driver.FindElementById("#number").SendKeys(_globalState.Number.ToString());

                _globalState.IsSignedIn = true;

                _driver.FindElementByLinkText("Sign In").Click();
                return new LoggedInHome(_driver, _globalState);
            };

            return new List<Func<PageState<GlobalState>>>
            {
                goToHome,
                goToLoggedInHome
            };
        }

        public override void OnExit()
        {
            Console.WriteLine("Exiting sign in");
        }

        private void WriteAndAssert(string id, string text)
        {
            var element = _driver.FindElementById(id);

            element.SendKeys(text);

            Assert.True(element.Text == text);
        }
    }
}
