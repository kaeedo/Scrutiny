namespace UsageExample

open Expecto
open OpenQA.Selenium.Firefox

open System
open canopy.classic

type Browser() =
    do
        let options = new FirefoxOptions()
        options.AddAdditionalCapability("acceptInsecureCerts", true, true)

        let firefox = new FirefoxDriver(options)
        
        switchTo firefox
        
    interface IDisposable with
        member this.Dispose() =
            quit()

module Entry =

    [<Tests>]
    let allTests =
        testList "all-tests" [
            testCase "Simple test" <| fun () ->
                use ff = new Browser()

                url "https://localhost:5001/home"

                "#welcome" == "Welcome"
        ]

    [<EntryPoint>]
    let main argv =
        let config = { defaultConfig with ``parallel`` = false }

        runTestsWithArgs config argv allTests
