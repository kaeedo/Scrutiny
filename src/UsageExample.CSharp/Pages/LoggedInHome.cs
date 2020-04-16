using System;
using System.Collections.Generic;
using OpenQA.Selenium.Remote;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    public class LoggedInHome : PageState<GlobalState, object>
    {
        private readonly RemoteWebDriver _driver;
        private readonly GlobalState _globalState;

        public LoggedInHome(RemoteWebDriver driver, GlobalState globalState) : base("Logged in Home")
        {
            _driver = driver;
            _globalState = globalState;
        }

        public override void OnEnter()
        {
            Console.WriteLine("Checking on page home logged in");

            Assert.True(_driver.FindElementById("welcomeText").Displayed);
        }

        public override IEnumerable<Func<PageState<GlobalState, object>>> Transitions()
        {
            Func<Home> goToHome = () =>
            {
                _driver.FindElementById("home").Click();
                return new Home(_driver, _globalState);
            };

            Func<LoggedInComment> goToLoggedInComment = () =>
            {
                _driver.FindElementById("comment").Click();
                return new LoggedInComment(_driver, _globalState);
            };

            return new List<Func<PageState<GlobalState, object>>>
            {
                goToHome,
                goToLoggedInComment
            };
        }

        public override void OnExit()
        {
            Console.WriteLine("Exiting Logged in home");
        }

        public override void ExitAction()
        {
            Console.WriteLine("EXITING!!");
            _driver.FindElementById("logout").Click();
        }
    }
}
