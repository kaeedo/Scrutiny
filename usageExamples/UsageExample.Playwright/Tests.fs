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
            //launchOptions.SlowMo <- 500f

            let! browser = playwright.Firefox.LaunchAsync(launchOptions)
            let! context = browser.NewContextAsync(BrowserNewContextOptions(IgnoreHTTPSErrors = true))

            let! page = context.NewPageAsync()

            let! _ = page.GotoAsync("http://127.0.0.1:5000/home")

            let config =
                { ScrutinyConfig.Default with
                    Seed = 553931187
                    MapOnly = false
                    ComprehensiveActions = true
                    ComprehensiveStates = true }

            let! result = scrutinize config (GlobalState(page, logger)) ScrutinyStateMachine.home
            Assert.True(result.Steps |> Seq.length >= 5)
            Assert.Equal(5, result.Graph.Length)
        }

    interface IDisposable with
        member this.Dispose() = playwright.Dispose()
