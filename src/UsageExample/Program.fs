namespace UsageExample

open OpenQA.Selenium.Firefox

open Scrutiny
open Scrutiny.Operators
open Scrutiny.Scrutiny

open canopy.classic
open canopy.runner.classic

module Entry =
    let signIn =
        page {
            name "Sign In"
            entryCheck (fun _ ->
                printfn "Checking on page sign in"
                "#header" == "Sign In"
            )

            navigationLink ("clickHome", fun () -> click "#home")

            exitFunction (fun _ ->
                printfn "Exiting sign in"
            )
        }

    let comment =
        page {
            name "Comment"
            entryCheck (fun _ ->
                printfn "Checking on page comment"
                "#header" == "Comments"
            )

            navigationLink ("clickHome", fun () -> click "#home")
            navigationLink ("clickSignin", fun () -> click "#signin")

            exitFunction (fun _ ->
                printfn "Exiting comment"
            )
        }

    let home =
        page {
            name "Home"
            entryCheck (fun _ ->
                printfn "Checking on page home"
                "#header" == "Home"
            )

            navigationLink ("clickComment", fun () -> click "#comment")
            navigationLink ("clickSignin", fun () -> click "#signin")

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
            clickFlow {
                //pages [ fun () -> signIn(); comment; home ]
                entryFunction (fun _ ->
                    printfn "opening url"
                    url "https://localhost:5001/home"

                    home
                )

                navigation ((home, "clickComment") ==> comment)
                navigation ((home, "clickSignin") ==> signIn)

                navigation ((comment, "clickHome") ==> home)
                navigation ((comment, "clickSignin") ==> signIn)

                navigation ((signIn, "clickHome") ==> home)
            } |> scrutinize

        switchTo ff

        run()
        quit ff

        0
