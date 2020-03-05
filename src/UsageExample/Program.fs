namespace UsageExample

open OpenQA.Selenium.Firefox

open System
open Scrutiny
open Scrutiny.Operators
open Scrutiny.Scrutiny

open canopy.classic
open canopy.runner.classic

type GlobalState() =
    member val IsSignedIn = false with get, set
    member val Username = "MyUsername" with get
    member val Number = 42 with get

module rec Entry =
    let signIn = fun (globalState: GlobalState) ->
        page {
            name "Sign In"
            entryCheck (fun () ->
                printfn "Checking on page sign in"
                "#header" == "Sign In"
            )

            transition ((fun () -> click "#home") ==> home)
            transition ((fun () ->
                "#username" << globalState.Username
                "#number" << globalState.Number.ToString()

                globalState.IsSignedIn <- true

                click "Sign In"
            ) ==> loggedInHome)

            action (fun () ->
                "#username" << "MyUsername"
                "#username" == "MyUsername"
            )
            action (fun () -> 
                "#number" << "42"
                "#number" == "42"
            )
            action (fun () -> 
                let username = read "#username"
                let number = read "#number"
                if String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(number)
                then click "Sign In"
                else
                    "#username" << ""
                    click "Sign In"

                displayed "#ErrorMessage"
            )

            exitFunction (fun () ->
                printfn "Exiting sign in"
            )
        }

    let loggedInComment = fun (globalState: GlobalState) ->
        page {
            name "Logged In Comment"

            action (fun () ->
                click "#openModal"
                "#comment" << "This is my super comment"
                click "#modalFooterSave"

                "#commentsUl>li" *= sprintf "%s wrote:%sThis is my super comment" globalState.Username Environment.NewLine
            )

            entryCheck (fun () ->
                printfn "Checking comment is logged in"
                displayed "#openModal"
            )

            exitFunction (fun () ->
                printfn "Exiting comment logged in"
            )
        }

    let loggedInHome = fun (globalState: GlobalState) ->
        page {
            name "Logged in Home"

            transition ((fun () -> click "#comment") ==> loggedInComment)

            entryCheck (fun () ->
                printfn "Checking on page home logged in"
                displayed "#welcomeText"
            )
        }

    let comment = fun (globalState: GlobalState) ->
        page {
            name "Comment"
            entryCheck (fun () ->
                printfn "Checking on page comment"
                "#header" == "Comments"
            )

            transition ((fun () -> click "#home") ==> home)
            transition ((fun () -> click "#signin") ==> signIn)
            
            exitFunction (fun () ->
                printfn "Exiting comment"
            )
        }

    let home = fun (globalState: GlobalState) ->
        page {
            name "Home"
            entryCheck (fun _ ->
                printfn "Checking on page home"
                "#header" == "Home"
            )

            transition ((fun () -> click "#comment") ==> comment)
            transition ((fun () -> click "#signin") ==> signIn)

            exitFunction (fun _ ->
                printfn "Exiting home"
            )
        }

    [<EntryPoint>]
    let main argv =
        let options = new FirefoxOptions()
        do options.AddAdditionalCapability("acceptInsecureCerts", true, true)

        let ff = new FirefoxDriver(options)

        "Scrutiny" &&& fun _ ->
            printfn "opening url"
            url "https://localhost:5001/home"
            home |> scrutinize (new GlobalState()) 

        switchTo ff
        pin canopy.types.direction.Right

        run()
        quit ff
        
        0
