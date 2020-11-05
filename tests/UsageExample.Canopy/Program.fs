namespace UsageExample

open OpenQA.Selenium.Firefox

open System
open Scrutiny
open Scrutiny.Operators
open Scrutiny.Scrutiny

open canopy.classic
open canopy.runner.classic
open canopy
open System.IO
open OpenQA.Selenium.Chrome
open WebDriverManager
open WebDriverManager.DriverConfigs.Impl

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
                name "Sign In"
                onEnter (fun _ ->
                    printfn "Checking on page sign in"
                    "#header" == "Sign In")

                transition ((fun _ -> click "#home") ==> home)
                transition
                    ((fun _ ->
                        globalState.Username <- "kaeedo"
                        "#username" << globalState.Username
                        "#number" << globalState.Number.ToString()

                        globalState.IsSignedIn <- true

                        click "Sign In")
                     ==> loggedInHome)

                action (fun _ ->
                    "#username" << "MyUsername"
                    "#username" == "MyUsername")
                action (fun _ ->
                    "#number" << "42"
                    "#number" == "42")

                action (fun _ ->
                    let username = read "#username"
                    let number = read "#number"
                    if String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(number) then
                        click "Sign In"
                    else
                        "#username" << ""
                        click "Sign In"

                    displayed "#ErrorMessage")

                onExit (fun _ -> printfn "Exiting sign in")
            }

    let loggedInComment =
        fun (globalState: GlobalState) ->

            page {
                name "Logged In Comment"

                localState (LoggedInComment())

                transition ((fun _ -> click "#home") ==> loggedInHome)

                action (fun ls ->
                    click "#openModal"
                    ls.Comment <- "This is my super comment"
                    "#comment" << ls.Comment
                    click "#modalFooterSave"

                    "#commentsUl>li" *= sprintf "%s wrote:%s%s" globalState.Username Environment.NewLine ls.Comment)

                onEnter (fun _ ->
                    printfn "Checking comment is logged in"
                    displayed "#openModal")

                onExit (fun _ -> printfn "Exiting comment logged in")
            }

    let loggedInHome =
        fun (globalState: GlobalState) ->
            page {
                name "Logged in Home"

                transition ((fun _ -> click "#comment") ==> loggedInComment)
                transition ((fun _ -> click "#logout") ==> home)

                onEnter (fun _ ->
                    printfn "Checking on page home logged in"
                    displayed "#welcomeText")

                exitAction (fun _ ->
                    printfn "Exiting!"
                    click "#logout")
            }

    let comment =
        fun (globalState: GlobalState) ->
            page {
                name "Comment"
                onEnter (fun _ ->
                    printfn "Checking on page comment"
                    "#header" == "Comments")

                transition ((fun _ -> click "#home") ==> home)
                transition ((fun _ -> click "#signin") ==> signIn)

                onExit (fun _ -> printfn "Exiting comment")
            }

    let home =
        fun (globalState: GlobalState) ->
            page {
                name "Home"
                onEnter (fun _ ->
                    printfn "Checking on page home"
                    "#header" == "Home")

                transition ((fun _ -> click "#comment") ==> comment)
                transition ((fun _ -> click "#signin") ==> signIn)

                onExit (fun _ ->
                    printfn "Exiting home"
                )
            }

    [<EntryPoint>]
    let main argv =
        printfn "Setting up browser drivers. This might take awhile"
        do DriverManager().SetUpDriver(ChromeConfig())
        //do DriverManager().SetUpDriver(FirefoxConfig())
        printfn "Finished setting up browser drivers"

        let options = FirefoxOptions()
        let cOptions = ChromeOptions()
        do cOptions.AddAdditionalCapability("acceptInsecureCerts", true, true)
        do options.AddAdditionalCapability("acceptInsecureCerts", true, true)

        if System.Environment.GetEnvironmentVariable("CI") = "true"
        then
            do cOptions.AddArgument "headless"
            do cOptions.AddArgument "no-sandbox"
            do options.AddArgument "-headless"

        use chrome = new ChromeDriver(cOptions)
        //use ff = new FirefoxDriver(options)

        let config =
            { ScrutinyConfig.Default with
                  Seed = 553931187
                  MapOnly = false
                  ComprehensiveActions = true
                  ComprehensiveStates = true
                  ScrutinyResultFilePath = Path.Join(Directory.GetCurrentDirectory(), "myResult.html") }

        "Scrutiny" &&& fun _ ->
            printfn "opening url"
            url "https://127.0.0.1:5001/home"
            scrutinize config (GlobalState()) home

        switchTo chrome

        onFail (fun _ ->
            quit chrome
            raise (exn "Failed")
        )

        run()
        quit chrome

        0
