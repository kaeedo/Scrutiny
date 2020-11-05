using OpenQA.Selenium;

namespace UsageExample.CSharp
{
    public class GlobalState
    {
        public IWebDriver Driver { get; }

        public GlobalState(IWebDriver driver)
        {
            Driver = driver;
        }
    }
}
