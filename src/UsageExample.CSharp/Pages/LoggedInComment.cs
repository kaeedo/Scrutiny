using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium.Remote;
using Scrutiny.CSharp;
using UsageExample.CSharp.PageStates;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    public class LoggedInComment : PageState<GlobalState, LoggedInCommentState>
    {
        private readonly RemoteWebDriver _driver;
        private readonly GlobalState _globalState;

        public LoggedInComment(RemoteWebDriver driver, GlobalState globalState) : base("Logged In Comment")
        {
            _driver = driver;
            _globalState = globalState;
        }

        public override void OnEnter()
        {
            Console.WriteLine("Checking comment is logged in");

            Assert.True(_driver.FindElementById("openModal").Displayed);
        }

        public override IEnumerable<Action> Actions()
        {
            return null;
        }

        public override IEnumerable<Func<PageState<GlobalState, LoggedInCommentState>>> Transitions()
        {
            Func<LoggedInHome> goToLoggedInHome = () =>
            {
                _driver.FindElementById("home").Click();
                return new LoggedInHome(_driver, _globalState);
            };

            return new List<Func<PageState<GlobalState, LoggedInCommentState>>>
            {
                goToLoggedInHome
            };
        }

        public override void OnExit()
        {
            Console.WriteLine("Exiting logged in comment");
        }

        private void WriteAndAssertModalText(LoggedInCommentState localState)
        {
            _driver.FindElementById("openModal").Click();
            localState.Comment = "This is my super comment";
            _driver.FindElementById("comment").SendKeys(localState.Comment);
            _driver.FindElementById("modalFooterSave").Click();

            var comments = _driver.FindElementsByCssSelector("#commentsUl>li");
            var expected = $"{_globalState.Username} wrote:{Environment.NewLine}{localState.Comment}";

            Assert.True(comments.Any(c => c.Text == expected));
        }
    }
}
