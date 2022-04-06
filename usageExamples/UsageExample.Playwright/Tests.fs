namespace UsageExample.Playwright

open Xunit
open PlaywrightSharp
open Scrutiny
open Scrutiny.Scrutiny
open UsageExample.Playwright
open System.IO
open Xunit.Abstractions

type PlaywrightTests(outputHelper: ITestOutputHelper) =
    let logger msg = outputHelper.WriteLine(msg)

    [<Fact(Timeout = Playwright.DefaultTimeout)>]
    let ``Run Scrutiny Test`` () =
        //System.Environment.SetEnvironmentVariable("DEBUG", "pw:api")
        logger "Setting up browser drivers. This might take awhile"
        Playwright.InstallAsync() |> Async.AwaitTask |> Async.RunSynchronously
        System.Environment.SetEnvironmentVariable("PWDEBUG", "1")
        System.Environment.SetEnvironmentVariable("DEBUG", "pw:api")

        use playwright = Playwright.CreateAsync() |> Async.AwaitTask |> Async.RunSynchronously

        let isHeadless = System.Environment.GetEnvironmentVariable("CI") = "true"

        logger "Finished setting up browser drivers"

        let page =
            let task =
                task {
                    let! browser = playwright.Firefox.LaunchAsync(headless = isHeadless)
                    let! context = browser.NewContextAsync(ignoreHTTPSErrors = true)
                    let! page = context.NewPageAsync() 

                    let! _ = page.GoToAsync("https://127.0.0.1:5001/home") 
                    return page
                }
            task.GetAwaiter().GetResult()

        let config =
            { ScrutinyConfig.Default with
                  Seed = 553931187
                  MapOnly = false
                  ComprehensiveActions = true
                  ComprehensiveStates = true
                  Logger = logger
                  ScrutinyResultFilePath = Path.Join(Directory.GetCurrentDirectory(), "myResult.html") }

        let result = scrutinize config (GlobalState(page, logger)) ScrutinyStateMachine.home

        Assert.Equal(9, result.Steps |> Seq.length);
        Assert.Equal(5, result.Graph.Length);