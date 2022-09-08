namespace UsageExample.Playwright

open System
open Microsoft.Playwright
open Xunit
open Scrutiny
open UsageExample.Playwright
open Xunit.Abstractions

type PlaywrightTests(outputHelper: ITestOutputHelper) =
    do
        Microsoft.Playwright.Program.Main([| "install" |])
        |> ignore

    let logger msg = outputHelper.WriteLine(msg)
    let playwright = Playwright.CreateAsync().GetAwaiter().GetResult()

    [<Fact>]
    member this.``Run Scrutiny Test``() =
        task {
            let isHeadless = Environment.GetEnvironmentVariable("CI") = "true"

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
                    ComprehensiveStates = true }

            let! result = scrutinize config (GlobalState(page, logger)) ScrutinyStateMachine.home

            Assert.Equal(9, result.Steps |> Seq.length)
            Assert.Equal(5, result.Graph.Length)
        }

    interface IDisposable with
        member this.Dispose() = playwright.Dispose()
