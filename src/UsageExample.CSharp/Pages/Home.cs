using OpenQA.Selenium.Remote;
using Scrutiny.CSharp;
using System;
using System.Collections.Generic;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    public class Home : PageState<GlobalState>
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

        public override IEnumerable<Action> Actions()
        {
            //transition((fun()->click "#comment") ==> comment)
            //transition((fun()->click "#signin") ==> signIn)
            return null;
        }

        public override IEnumerable<(Action, PageState<GlobalState>)> Transitions()
        {
            return null;
        }
    }
}