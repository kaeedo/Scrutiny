namespace UsageExample

open OpenQA.Selenium.Firefox

open Scrutiny
open Scrutiny.Operators
open Scrutiny.Scrutiny

open canopy.classic
open canopy.runner.classic

module rec Entry =
    let signIn = fun _ ->
        page {
            name "Sign In"
            entryCheck (fun _ ->
                printfn "Checking on page sign in"
                "#header" == "Sign In"
            )

            transition ((fun () -> click "#home") ==> home)

            exitFunction (fun _ ->
                printfn "Exiting sign in"
            )
        }

    let comment = fun _ ->
         page {
            name "Comment"
            entryCheck (fun _ ->
                printfn "Checking on page comment"
                "#header" == "Comments"
            )

            transition ((fun () -> click "#home") ==> home)
            transition ((fun () -> click "#signin") ==> signIn)
            
            exitFunction (fun _ ->
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
            clickFlow {
                entryFunction (fun _ ->
                    printfn "opening url"
                    url "https://localhost:5001/home"

                    home
                )
            } |> scrutinize

        switchTo ff
        pin canopy.types.direction.Right

        run()
        quit ff

        0
