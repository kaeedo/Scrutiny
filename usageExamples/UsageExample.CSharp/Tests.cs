using Scrutiny.CSharp;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using UsageExample.CSharp.Pages;
using Xunit;
using Xunit.Abstractions;

namespace UsageExample.CSharp
{
    public class Tests : IDisposable
    {
        private readonly IPlaywright playwright;
        private readonly ITestOutputHelper outputHelper;

        public Tests(ITestOutputHelper outputHelper)
        {
            outputHelper.WriteLine("Setting up browser drivers. This might take awhile");
            Microsoft.Playwright.Program.Main(new[] {"install"});

            playwright = Playwright.CreateAsync().GetAwaiter().GetResult();

            outputHelper.WriteLine("Finished setting up browser drivers");
            this.outputHelper = outputHelper;
        }

        //[Fact]
        public async Task WithAttrs()
        {
            var isHeadless = Environment.GetEnvironmentVariable("CI") == "true";

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = isHeadless
            };
            
            var browser = await playwright.Firefox.LaunchAsync(launchOptions);
            var context = await browser.NewContextAsync(new BrowserNewContextOptions {IgnoreHTTPSErrors = true});
            var page = await context.NewPageAsync();

            await page.GotoAsync("https://127.0.0.1:5001/home");

            var config = new Configuration
            {
                Seed = 553931187,
                MapOnly = false,
                ComprehensiveActions = true,
                ComprehensiveStates = true
            };

            var gs = new GlobalState(page, outputHelper);
            var result = Scrutinize.Start<Home>(gs, config);

            Assert.Equal(7, result.Steps.Count());
            Assert.Equal(5, result.Graph.Count());
        }

        public void Dispose()
        {
            playwright?.Dispose();
        }
    }
}