using Microsoft.FSharp.Core;
using NUnit.Framework;
using Scrutiny;
using System.Collections.Generic;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace UsageExample.CSharp
{
    public class Tests
    {
        PageState<object, object> SignIn;
        PageState<object, object> Home;

        IWebDriver driver;

        [SetUp]
        public void Setup()
        {
            new DriverManager().SetUpDriver(new ChromeConfig());

            var cOptions = new ChromeOptions();
            cOptions.AddAdditionalCapability("acceptInsecureCerts", true, true);

            driver = new ChromeDriver(cOptions);
            driver.Url = "https://localhost:5001";
        }

        [Test]
        public void Test1()
        {
            var noop = FSharpFunc<object, Unit>.FromConverter(n => null);
            var emptyList = Microsoft.FSharp.Collections.ListModule.OfSeq((IEnumerable<FSharpFunc<object, Unit>>)new List<FSharpFunc<object, Unit>>());
            //            FSharpFunc<int, int>.FromConverter(input => input * 2)
            //  {
            //      PageState.Name = ""
            //LocalState = Unchecked.defaultof < 'b>
            //OnEnter = fun _-> ()
            //OnExit = fun _-> ()
            //Transitions = []
            //Actions = []
            //ExitAction = None }
            var transitions = new List<Transition<object, object>>();
            var toState = FSharpFunc<object, PageState<object, object>>.FromConverter(ls => SignIn);

            var transitionFn = FSharpFunc<object, Unit>.FromConverter(ls =>
            {
                driver.FindElement(By.Id("signin")).Click();

                return null;
            });

            var toSignIn = new Transition<object, object>(transitionFn: transitionFn, toState: toState);

            transitions.Add(toSignIn);

            var a = Microsoft.FSharp.Collections.ListModule.OfSeq<Transition<object, object>>(
                    (IEnumerable<Transition<object, object>>)transitions
                );

            Home = new PageState<object, object>(
                name: "Home",
                localState: null,
                transitions: a,
                onEnter: noop,
                onExit: noop,
                exitAction: null,
                actions: emptyList);



            var transitions2 = new List<Transition<object, object>>();
            var toState2 = FSharpFunc<object, PageState<object, object>>.FromConverter(ls => Home);

            var transitionFn2 = FSharpFunc<object, Unit>.FromConverter(ls =>
            {
                driver.FindElement(By.Id("home")).Click();

                return null;
            });

            var toHome = new Transition<object, object>(transitionFn: transitionFn2, toState: toState2);

            transitions2.Add(toHome);

            var b = Microsoft.FSharp.Collections.ListModule.OfSeq(transitions2);

            SignIn = new PageState<object, object>(
                name: "Sign In",
                localState: null,
                transitions: b,
                onEnter: noop,
                onExit: noop,
                exitAction: null,
                actions: emptyList);

            var config = Scrutiny.ScrutinyConfig.Default;

            var scrut = Scrutiny.Scrutiny.scrutinize<object, object>(config);
            var withGs = scrut.Invoke(new object());
            withGs.Invoke(FSharpFunc<object, PageState<object, object>>.FromConverter(_ => Home));

            Assert.Pass();
        }

        [TearDown]
        public void TearDown()
        {
            driver.Close();
        }
    }
}