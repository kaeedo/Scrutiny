using System;
using System.Linq;
using System.Threading.Tasks;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages
{
    [PageState]
    public class LoggedInComment
    {
        private readonly GlobalState globalState;
        private string localComment = string.Empty;

        public LoggedInComment(GlobalState globalState)
        {
            this.globalState = globalState;
            globalState.Logger.WriteLine($"Constructing {nameof(LoggedInComment)}");
        }

        [OnEnter]
        public async Task OnEnter()
        {
            globalState.Logger.WriteLine("Checking comment is logged in");
            var modal = await globalState.Page.QuerySelectorAsync("#openModal");

            Assert.NotNull(modal);

            var displayState = await modal.EvaluateAsync("e => e.style.display");

            Assert.False(displayState.ToString() == "none");
        }

        [Action]
        public async Task WriteComment()
        {
            await globalState.Page.ClickAsync("#openModal");
            localComment = "This is my super comment";

            await globalState.Page.FillAsync("#comment", localComment);

            await globalState.Page.ClickAsync("#modalFooterSave");
        }

        [OnExit]
        public async Task OnExit()
        {
            var comments = await globalState.Page.QuerySelectorAllAsync("#commentsUl > li");
            var commentTexts = await Task.WhenAll(comments.Select(async c => await c.GetInnerTextAsync()));
            var hasNewComment = commentTexts.Any(c => c == $"{globalState.Username} wrote:\n{localComment}");

            Assert.True(hasNewComment);
            globalState.Logger.WriteLine("Exiting comment logged in");
        }

        [TransitionTo(nameof(LoggedInHome))]
        public async Task ClickOnHome()
        {
            await globalState.Page.ClickAsync("#home");
        }
    }
}
