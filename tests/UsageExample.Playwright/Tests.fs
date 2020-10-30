module PlaywrightTests

open System
open Xunit
open FSharp.Control.Tasks.V2
open PlaywrightSharp
open Scrutiny
open Scrutiny.Scrutiny
open UsageExample.Playwright
open System.IO

[<Fact(Timeout = Playwright.DefaultTimeout)>]
let ``Run Scrutiny Test`` () =
    Playwright.InstallAsync() |> Async.AwaitTask |> Async.RunSynchronously
    use playwright = Playwright.CreateAsync() |> Async.AwaitTask |> Async.RunSynchronously

    let page = 
        task {
            let! browser = playwright.Firefox.LaunchAsync(headless = false, slowMo = 1000)
            let! context = browser.NewContextAsync(ignoreHTTPSErrors = true)
            let! page = context.NewPageAsync()

            let! _ = page.GoToAsync("https://127.0.0.1:5001/home")
            return page
        }
        |> Async.AwaitTask 
        |> Async.RunSynchronously
    
    let config =
        { ScrutinyConfig.Default with
              Seed = 553931187
              MapOnly = false
              ComprehensiveActions = true
              ComprehensiveStates = true
              ScrutinyResultFilePath = DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "/myResult.html" }

    scrutinize config (GlobalState(page)) ScrutinyStateMachine.home