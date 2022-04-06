using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace UsageExample.CSharp
{
    public class GlobalState
    {
        public IPage Page { get; }

        public ITestOutputHelper Logger { get; }

        public string Username { get; set; }
        public bool IsSignedIn { get; set; }
        public int Number { get; } = 42;

        public GlobalState(IPage page, ITestOutputHelper logger)
        {
            Page = page;
            Logger = logger;
            Username = "MyUsername";
            IsSignedIn = false;
        }

        public async Task<string> GetInputValueAsync(string selector)
        {
            var element = await this.Page.QuerySelectorAsync(selector);
            var value = await element.EvaluateAsync("e => e.value");

            return value.ToString();
        }
    }
}
