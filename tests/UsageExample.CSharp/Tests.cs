using Microsoft.FSharp.Core;
using PlaywrightSharp;
using Scrutiny;
using Scrutiny.CSharp;
using System;
using System.Collections.Generic;
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

            //new DriverManager().SetUpDriver(new ChromeConfig());

            //var cOptions = new ChromeOptions();
            //cOptions.AddAdditionalCapability("acceptInsecureCerts", true, true);

            //driver = new ChromeDriver(cOptions);
            //driver.Url = "https://localhost:5001";
            this.outputHelper = outputHelper;
        }

        [Fact(Timeout = Playwright.DefaultTimeout)]
        public async Task WithAttrs()
        {
            var browser = await playwright.Firefox.LaunchAsync(headless: false);
            var context = await browser.NewContextAsync(ignoreHTTPSErrors: true);
            var page = await context.NewPageAsync();

            await page.GoToAsync("https://127.0.0.1:5001/home");

            var gs = new GlobalState(page, outputHelper);
            ScrutinyCSharp.start(gs, new Home(gs));
        }

        public void Dispose()
        {
            playwright?.Dispose();
        }
    }
}