using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Scrutiny.CSharp;
using UsageExample.CSharp.Pages;
using Xunit;
using Xunit.Abstractions;

namespace UsageExample.CSharp;

public class Tests : IDisposable
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IPlaywright _playwright;

    public Tests(ITestOutputHelper outputHelper)
    {
        outputHelper.WriteLine("Setting up browser drivers. This might take awhile");
        Program.Main(new[] {"install"});

        _playwright = Playwright.CreateAsync().GetAwaiter().GetResult();

        outputHelper.WriteLine("Finished setting up browser drivers");
        _outputHelper = outputHelper;
    }

    public void Dispose()
    {
        _playwright?.Dispose();
    }

    [Fact]
    public async Task WithAttrs()
    {
        var isHeadless = Environment.GetEnvironmentVariable("CI") == "true";

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = isHeadless
        };

        var browser = await _playwright.Firefox.LaunchAsync(launchOptions);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions {IgnoreHTTPSErrors = true});
        var page = await context.NewPageAsync();

        await page.GotoAsync("http://127.0.0.1:5000/home");

        var config = new Configuration
        {
            Seed = 553931187,
            MapOnly = false,
            ComprehensiveActions = true,
            ComprehensiveStates = true
        };

        var gs = new GlobalState(page, _outputHelper);
        var result = await Scrutinize.Start<Home>(gs, config);

        Assert.Equal(14, result.Steps.Count());
        Assert.Equal(5, result.Graph.Count());
    }
}
