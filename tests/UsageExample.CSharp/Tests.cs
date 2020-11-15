using PlaywrightSharp;
using Scrutiny.CSharp;
using System;
using System.Threading.Tasks;
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
            Playwright.InstallAsync().GetAwaiter().GetResult();
            Environment.SetEnvironmentVariable("PWDEBUG", "1");
            Environment.SetEnvironmentVariable("DEBUG", "pw:api");

            playwright = Playwright.CreateAsync().GetAwaiter().GetResult();

            outputHelper.WriteLine("Finished setting up browser drivers");
            this.outputHelper = outputHelper;
        }

        [Fact]
        public async Task WithAttrs()
        {
            var browser = await playwright.Firefox.LaunchAsync(headless: false);
            var context = await browser.NewContextAsync(ignoreHTTPSErrors: true);
            var page = await context.NewPageAsync();

            await page.GoToAsync("https://127.0.0.1:5001/home");

            var config = new Configuration
            {
                Seed = 553931187,
                MapOnly = false,
                ComprehensiveActions = true,
                ComprehensiveStates = true
            };

            var gs = new GlobalState(page, outputHelper);
            Scrutinize.Start<Home>(gs, config);
        }

        public void Dispose()
        {
            playwright?.Dispose();
        }
    }
}