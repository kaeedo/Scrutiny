using Xunit;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using UsageExample.CSharp.Pages;

namespace UsageExample.CSharp
{
    public class Test
    {
        [Fact]
        public void Scrutinize()
        {
            var options = new FirefoxOptions();
            options.AddAdditionalCapability("acceptInsecureCerts", true, true);

            using (var driver = new FirefoxDriver(".", options))
            {
                driver.Navigate().GoToUrl("https://localhost:5001");

                new Home(driver, null).OnEnter();
            }
        }
    }
}
