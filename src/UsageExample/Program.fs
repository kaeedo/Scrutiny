namespace UsageExample

open Expecto
open OpenQA.Selenium.Firefox

open Scrutiny
open Scrutiny.Operators
open Scrutiny.Scrutiny

open System
open canopy.classic

type Browser() =
    let options = new FirefoxOptions()
    do options.AddAdditionalCapability("acceptInsecureCerts", true, true)

    let ff = new FirefoxDriver(options)

    do switchTo ff

    interface IDisposable with
        member this.Dispose() =
            quit ff

module Entry =
    let signIn =
        page {
            name "Sign In"
            entryCheck (fun _ ->
                printfn "Checking on page sign in"
                on "https://localhost:5001/signin"
            )
            
            navigationLink ("clickHome", fun () -> click "home")

            exitFunction (fun _ ->
                printfn "Exiting sign in"
            )
        }

    let comment =
        page {
            name "Comment"
            entryCheck (fun _ ->
                printfn "Checking on page comment"
                on "https://localhost:5001/comment"
            )

            navigationLink ("clickHome", fun () -> click "home")
            navigationLink ("clickSignin", fun () -> click "signin")

            exitFunction (fun _ ->
                printfn "Exiting comment"
            )
        }

    let home =
        page {
            name "Home"
            entryCheck (fun _ ->
                printfn "Checking on page home"
                on "https://localhost:5001/home"
            )

            navigationLink ("clickComment", fun () -> click "comment")
            navigationLink ("clickSignin", fun () -> click "signin")

            exitFunction (fun _ ->
                printfn "Exiting home"
            )
        }

    [<Tests>]
    let allTests =
        testCase "Simple with builder" <| fun () ->
            use ff = new Browser()

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

    [<EntryPoint>]
    let main argv =
        let config = { defaultConfig with ``parallel`` = false }

        runTestsWithArgs config argv allTests
