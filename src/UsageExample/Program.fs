namespace UsageExample

open OpenQA.Selenium.Firefox

open System
open Scrutiny
open Scrutiny.Operators
open Scrutiny.Scrutiny

open canopy.classic
open canopy.runner.classic

module rec Entry =
    let signIn = fun _ ->
        page {
            name "Sign In"
            entryCheck (fun () ->
                printfn "Checking on page sign in"
                "#header" == "Sign In"
            )

            transition ((fun () -> click "#home") ==> home)
            transition ((fun () ->
                "#username" << "MyUsername"
                "#number" << "42"
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

    let loggedInComment = fun _ ->
        page {
            name "Logged In Comment"

            entryCheck (fun () ->
                printfn "Checking comment is logged in"
                displayed "#openModal"
            )

            exitFunction (fun () ->
                printfn "Exiting comment logged in"
            )
        }

    let loggedInHome = fun _ ->
        page {
            name "Logged in Home"

            transition ((fun () -> click "#comment") ==> loggedInComment)

            entryCheck (fun () ->
                printfn "Checking on page home logged in"
                displayed "#welcomeText"
            )
        }

    let comment = fun _ ->
        page {
            name "Comment"
            entryCheck (fun () ->
                printfn "Checking on page comment"
                "#header" == "Comments"
            )

            (*action (fun () ->
                if not <| isLoggedIn() 
                then ()
                else
                    click "#openModal"
                    "#comment" << "I'm very happy about this comment"
            )*)

            transition ((fun () -> click "#home") ==> home)
            transition ((fun () -> click "#signin") ==> signIn)
            
            exitFunction (fun () ->
                printfn "Exiting comment"
            )
        }

    let home = fun _ ->
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
            home |> scrutinize

        switchTo ff
        pin canopy.types.direction.Right

        run()
        quit ff
        
        0
