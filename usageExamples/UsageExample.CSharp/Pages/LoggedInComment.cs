using System.Linq;
using System.Threading.Tasks;
using Scrutiny.CSharp;
using Xunit;

namespace UsageExample.CSharp.Pages;

[PageState]
public class LoggedInComment
{
    private readonly GlobalState _globalState;
    private string _localComment = string.Empty;

    public LoggedInComment(GlobalState globalState)
    {
        _globalState = globalState;
        globalState.Logger.WriteLine($"Constructing {nameof(LoggedInComment)}");
    }

    [OnEnter]
    public async Task OnEnter()
    {
        _globalState.Logger.WriteLine("Checking comment is logged in");
        var modal = await _globalState.Page.QuerySelectorAsync("id=openModal");

        Assert.NotNull(modal);

        var displayState = await modal.EvaluateAsync("e => e.style.display");

        Assert.False(displayState.ToString() == "none");
    }

    [Action]
    public async Task WriteComment()
    {
        await _globalState.Page.ClickAsync("id=openModal");
        _localComment = "This is my super comment";

        await _globalState.Page.FillAsync("id=comment", _localComment);

        await _globalState.Page.ClickAsync("id=modalFooterSave");
    }

    [OnExit]
    public async Task OnExit()
    {
        var comments = await _globalState.Page.QuerySelectorAllAsync("#commentsUl > li");
        var commentTexts = await Task.WhenAll(comments.Select(async c => await c.InnerTextAsync()));
        var hasNewComment = commentTexts.Any(c => c == $"{_globalState.Username} wrote:\n{_localComment}");

        Assert.True(hasNewComment);
        _globalState.Logger.WriteLine("Exiting comment logged in");
    }


    [Action(IsExit = true)]
    public async Task ExitAction()
    {
        _globalState.Logger.WriteLine("Exiting!");
        await _globalState.Page.CloseAsync();
    }

    [TransitionTo(nameof(LoggedInHome))]
    public async Task ClickOnHome()
    {
        await _globalState.Page.ClickAsync("id=home");
    }
}
