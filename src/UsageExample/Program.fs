namespace UsageExample

open Expecto
open OpenQA.Selenium.Firefox

open Scrutiny
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
    let signInPage = "Sign In"
    let commentPage = "Comment"
    let homePage = "Home"

    let navigateToPage (link: string) (nextStateName: string) =
        fun () ->
            click link
            nextStateName

    let signIn =
        page {
            name signInPage
            entryCheck (fun _ ->
                printfn "Checking on page sign in"
                on "https://localhost:5001/signin"
            )

            transitions [ navigateToPage "home" homePage; navigateToPage "comment" commentPage ]

            exitFunction (fun _ ->
                printfn "Exiting sign in"
            )
        }

    let comment =
        page {
            name commentPage
            entryCheck (fun _ ->
                printfn "Checking on page comment"
                on "https://localhost:5001/comment"
            )

            transitions [ navigateToPage "signin" signInPage; navigateToPage "home" homePage ]

            exitFunction (fun _ ->
                printfn "Exiting comment"
            )
        }

    let home =
        page {
            name homePage
            entryCheck (fun _ ->
                printfn "Checking on page home"
                on "https://localhost:5001/home"
            )

            transitions [ navigateToPage "signin" signInPage; navigateToPage "comment" commentPage ]

            exitFunction (fun _ ->
                printfn "Exiting home"
            )
        }

    [<Tests>]
    let allTests =
        testCase "Simple with builder" <| fun () ->
            use ff = new Browser()
            scrutinize {
                //pages [ fun () -> signIn(); comment; home ]
                entryFunction (fun _ ->
                    printfn "opening url"
                    url "https://localhost:5001/home"

                    home
                )
            }

    [<EntryPoint>]
    let main argv =
        let config = { defaultConfig with ``parallel`` = false }

        runTestsWithArgs config argv allTests
