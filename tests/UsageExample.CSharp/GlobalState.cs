using PlaywrightSharp;
using Xunit.Abstractions;

namespace UsageExample.CSharp
{
    public class GlobalState
    {
        public IPage Page { get; }

        public ITestOutputHelper Logger { get; }

        public GlobalState(IPage page, ITestOutputHelper logger)
        {
            Page = page;
            Logger = logger;
        }
    }
}
