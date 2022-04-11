namespace UsageExample.Playwright

open Microsoft.Playwright
open Xunit
open Scrutiny
open Scrutiny.Scrutiny
open UsageExample.Playwright
open System.IO
open Xunit.Abstractions

type PlaywrightTests(outputHelper: ITestOutputHelper) =
    let logger msg = outputHelper.WriteLine(msg)

    [<Fact>]
    let ``Run Scrutiny Test`` () =
        task {
            Microsoft.Playwright.Program.Main([|"install"|]) |> ignore

            use! playwright = Playwright.CreateAsync()

            let isHeadless = System.Environment.GetEnvironmentVariable("CI") = "true"

            let launchOptions = BrowserTypeLaunchOptions()
            launchOptions.Headless <- isHeadless

            let! browser = playwright.Firefox.LaunchAsync(launchOptions)
            let! context = browser.NewContextAsync(BrowserNewContextOptions(IgnoreHTTPSErrors = true))
            let! page = context.NewPageAsync() 

            let! _ = page.GotoAsync("https://127.0.0.1:5001/home") 

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
            Assert.Equal(5, result.Graph.Length)
        }