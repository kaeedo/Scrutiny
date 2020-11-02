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
        logger "Finished setting up browser drivers"

        let page =
            async {
                let! browser = playwright.Firefox.LaunchAsync(headless = false) |> Async.AwaitTask
                let! context = browser.NewContextAsync(ignoreHTTPSErrors = true) |> Async.AwaitTask
                let! page = context.NewPageAsync() |> Async.AwaitTask

                let! _ = page.GoToAsync("https://127.0.0.1:5001/home") |> Async.AwaitTask
                return page
            }            
            |> Async.RunSynchronously

        let config =
            { ScrutinyConfig.Default with
                  Seed = 553931187
                  MapOnly = false
                  ComprehensiveActions = true
                  ComprehensiveStates = true
                  ScrutinyResultFilePath = 
                    Path.Join(DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "myResult.html") }

        scrutinize config (GlobalState(page, logger)) ScrutinyStateMachine.home

