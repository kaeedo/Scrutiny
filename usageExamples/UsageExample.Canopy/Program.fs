﻿namespace UsageExample


open System
open OpenQA.Selenium.Chrome
open OpenQA.Selenium.Firefox
open Scrutiny

open WebDriverManager
open WebDriverManager.DriverConfigs.Impl
open canopy.classic
open canopy.runner.classic
open System.IO

type GlobalState() =
    member val IsSignedIn = false with get, set
    member val Username = "MyUsername" with get, set
    member val Number = 42

type LoggedInComment() =
    member val Comment = String.Empty with get, set

module rec Entry =
    let signIn =
        fun (globalState: GlobalState) ->
            page {

                onEnter (fun _ ->
                    printfn "Checking on page sign in"
                    "#header" == "Sign In")

                onExit (fun _ -> printfn "Exiting sign in")
                name "Sign In"

                transition {
                    via (fun _ -> click "#home")
                    destination home
                }

                transition {
                    via (fun _ ->
                        globalState.Username <- "kaeedo"
                        "#username" << globalState.Username
                        "#number" << globalState.Number.ToString()

                        globalState.IsSignedIn <- true

                        click "Sign In")

                    destination loggedInHome
                }

                action {
                    fn (fun _ ->
                        "#username" << "MyUsername"
                        "#username" == "MyUsername")
                }

                action {
                    fn (fun _ ->
                        "#number" << "42"
                        "#number" == "42")
                }

                action {
                    fn (fun _ ->
                        let username = read "#username"
                        let number = read "#number"

                        if
                            String.IsNullOrWhiteSpace(username)
                            || String.IsNullOrWhiteSpace(number)
                        then
                            click "Sign In"
                        else
                            "#username" << ""
                            click "Sign In"

                        displayed "#ErrorMessage")
                }
            }

    let loggedInComment =
        fun (globalState: GlobalState) ->
            let ls = LoggedInComment()

            page {

                onEnter (fun _ ->
                    printfn "Checking comment is logged in"
                    displayed "#openModal")

                onExit (fun _ ->
                    "#commentsUl>li"
                    *= sprintf "%s wrote:%s%s" globalState.Username Environment.NewLine ls.Comment

                    printfn "Exiting comment logged in")

                name "Logged In Comment"

                transition {
                    via (fun _ -> click "#home")
                    destination loggedInHome
                }

                action {
                    fn (fun _ ->
                        click "#openModal"
                        ls.Comment <- "This is my super comment"
                        "#comment" << ls.Comment
                        click "#modalFooterSave")
                }
            }

    let loggedInHome =
        fun (globalState: GlobalState) ->
            page {

                onEnter (fun _ ->
                    printfn "Checking on page home logged in"
                    displayed "#welcomeText"

                    "#welcomeText"
                    == sprintf "Welcome %s" globalState.Username)

                name "Logged in Home"

                transition {
                    via (fun _ -> click "#comment")
                    destination loggedInComment
                }

                transition {
                    via (fun _ -> click "#logout")
                    destination home
                }

                action {
                    isExit

                    fn (fun _ ->
                        printfn "Exiting!"
                        click "#logout")
                }
            }

    let comment =
        fun (globalState: GlobalState) ->
            page {

                onEnter (fun _ ->
                    printfn "Checking on page comment"
                    "#header" == "Comments")

                onExit (fun _ -> printfn "Exiting comment")
                name "Comment"

                transition {
                    via (fun _ -> click "#home")
                    destination home
                }

                transition {
                    via (fun _ -> click "#signin")
                    destination signIn
                }
            }

    let home =
        fun (globalState: GlobalState) ->
            page {

                onEnter (fun _ ->
                    printfn "Checking on page home"
                    "#header" == "Home")

                onExit (fun _ -> printfn "Exiting home")
                name "Home"

                transition {
                    via (fun _ -> click "#comment")
                    destination comment
                }

                transition {
                    via (fun _ -> click "#signin")
                    destination signIn
                }
            }

    [<EntryPoint>]
    let main argv =
        printfn "Setting up browser drivers. This might take awhile"

        // DriverManager().SetUpDriver(ChromeConfig())
        // |> ignore

        DriverManager().SetUpDriver(FirefoxConfig())
        |> ignore

        printfn "Finished setting up browser drivers"

        let options = FirefoxOptions()
        let cOptions = ChromeOptions()
        do cOptions.AddAdditionalCapability("acceptInsecureCerts", true, true)
        do options.AddAdditionalCapability("acceptInsecureCerts", true, true)

        if System.Environment.GetEnvironmentVariable("CI") = "true" then
            do cOptions.AddArgument "headless"
            do cOptions.AddArgument "no-sandbox"
            do options.AddArgument "-headless"

        //use browser = new ChromeDriver(cOptions)
        use browser = new FirefoxDriver(options)

        let config =
            { ScrutinyConfig.Default with
                Seed = 553931187
                MapOnly = false
                ComprehensiveActions = true
                ComprehensiveStates = true
                ScrutinyResultFilePath = Path.Join(Directory.GetCurrentDirectory(), "myResult.html") }

        "Scrutiny"
        &&& fun _ ->
                printfn "opening url"
                url "http://127.0.0.1:5000/home"

                let results =
                    (scrutinize config (GlobalState()) home)
                        .GetAwaiter()
                        .GetResult()

                if results.Steps |> Seq.length <> 16 then
                    raise (Exception($"Expected 16 steps, but was {results.Steps |> Seq.length}"))
                else
                    ()

        switchTo browser

        onFail (fun _ ->
            quit browser
            raise (exn "Failed"))

        run ()
        quit browser

        0
