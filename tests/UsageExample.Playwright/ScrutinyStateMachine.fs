namespace UsageExample.Playwright

open System
open Scrutiny
open Scrutiny.Operators
open Scrutiny.Scrutiny

open PlaywrightSharp
open Xunit
open FSharp.Control.Tasks.V2

type GlobalState(page: IPage) =
    member val Page = page with get
    member val IsSignedIn = false with get, set
    member val Username = "MyUsername" with get, set
    member val Number = 42

type LoggedInComment() =
    member val Comment = String.Empty with get, set

module rec ScrutinyStateMachine =
    let signIn =
        fun (globalState: GlobalState) ->
            page {
                name "Sign In"
                onEnter (fun _ ->
                    printfn "Checking on page sign in"
                    task {
                        let! headerText = globalState.Page.GetInnerTextAsync("#header")
                        Assert.Equal(headerText, "Sign In")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )

                transition ((fun _ -> 
                    task {
                        do! globalState.Page.ClickAsync("#home")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                ) ==> home)
                transition
                    ((fun _ ->
                        task {
                            globalState.Username <- "kaeedo"
                            do! globalState.Page.FillAsync("#username", globalState.Username)
                            do! globalState.Page.FillAsync("#number", globalState.Number.ToString())

                            globalState.IsSignedIn <- true

                            do! globalState.Page.ClickAsync("text=Sign In")
                        }
                        |> Async.AwaitTask
                        |> Async.RunSynchronously
                    )
                     ==> loggedInHome)

                action (fun _ ->
                    task {
                        do! globalState.Page.FillAsync("#username", "MyUsername")
                        let! username = globalState.Page.GetInnerTextAsync("#username")
                        Assert.Equal(username, "MyUsername")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )
                action (fun _ ->
                    task {
                        do! globalState.Page.FillAsync("#number", "42")
                        let! number = globalState.Page.GetInnerTextAsync("#number")
                        Assert.Equal(number, "42")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )

                action (fun _ ->
                    task {
                        do! globalState.Page.ClickAsync("#home")
                        
                        let! username = globalState.Page.GetInnerTextAsync("#username")
                        let! number = globalState.Page.GetInnerTextAsync("#number")

                        if String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(number) then
                            do! globalState.Page.ClickAsync("text=Sign In")
                        else
                            do! globalState.Page.FillAsync("#username", String.Empty)
                            do! globalState.Page.ClickAsync("text=Sign In")

                        let! errorMessage = globalState.Page.QuerySelectorAsync("ErrorMessage")
                        let! displayState = errorMessage.EvaluateAsync("e => e.style.display")
                        //let! isVisible = errorMessage.WaitForElementStateAsync(ElementState.Visible)
                        
                        Assert.False(displayState.ToString() = "none")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )

                onExit (fun _ -> printfn "Exiting sign in")
            }

    let loggedInComment =
        fun (globalState: GlobalState) ->

            page {
                name "Logged In Comment"

                localState (LoggedInComment())

                transition ((fun _ -> 
                    task {
                        do! globalState.Page.ClickAsync("#home")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                ) ==> loggedInHome)

                action (fun ls ->
                    task {
                        do! globalState.Page.ClickAsync("#openModal")

                        ls.Comment <- "This is my super comment"

                        do! globalState.Page.FillAsync("#comment", ls.Comment)
                        do! globalState.Page.ClickAsync("#modalFooterSave")

                        let! comments = globalState.Page.QuerySelectorAllAsync("#commentsUl>li")
                        let comments = comments |> List.ofSeq

                        let writtenComment =
                            comments
                            |> List.tryFind(fun c ->
                                task {
                                    let! text = c.GetInnerTextAsync()
                                    return text = sprintf "%s wrote:%s%s" globalState.Username Environment.NewLine ls.Comment
                                }
                                |> Async.AwaitTask
                                |> Async.RunSynchronously
                            )

                        Assert.True(writtenComment.IsSome)
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )

                onEnter (fun _ ->
                    printfn "Checking comment is logged in"
                    task {
                        let! openModal = globalState.Page.QuerySelectorAsync("openModal")
                        let! displayState = openModal.EvaluateAsync("e => e.style.display")
                        
                        Assert.False(displayState.ToString() = "none")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )

                onExit (fun _ -> printfn "Exiting comment logged in")
            }

    let loggedInHome =
        fun (globalState: GlobalState) ->
            page {
                name "Logged in Home"

                transition ((fun _ -> 
                    task {
                        do! globalState.Page.ClickAsync("#comment")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                ) ==> loggedInComment)
                transition ((fun _ -> 
                    task {
                        do! globalState.Page.ClickAsync("#logout")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                ) ==> home)

                onEnter (fun _ ->
                    printfn "Checking on page home logged in"           

                    task {
                        let! welcomeText = globalState.Page.QuerySelectorAsync("welcomeText")
                        let! displayState = welcomeText.EvaluateAsync("e => e.style.display")
                        
                        Assert.False(displayState.ToString() = "none")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )

                exitAction (fun _ ->
                    printfn "Exiting!"
                    task {
                        do! globalState.Page.ClickAsync("#logout")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )
            }

    let comment =
        fun (globalState: GlobalState) ->
            page {
                name "Comment"
                onEnter (fun _ ->
                    printfn "Checking on page comment"

                    task {
                        let! headerText = globalState.Page.GetInnerTextAsync("#header")
                        Assert.Equal(headerText, "Comments")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )

                transition ((fun _ -> 
                    task {
                        do! globalState.Page.ClickAsync("#home")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                ) ==> home)
                transition ((fun _ -> 
                    task {
                        do! globalState.Page.ClickAsync("#signin")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                ) ==> signIn)

                onExit (fun _ -> printfn "Exiting comment")
            }

    let home =
        fun (globalState: GlobalState) ->
            page {
                name "Home"
                onEnter (fun _ ->
                    printfn "Checking on page home"

                    task {
                        let! headerText = globalState.Page.GetInnerTextAsync("#header")
                        Assert.Equal(headerText, "Home")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )

                transition ((fun _ -> 
                    task {
                        do! globalState.Page.ClickAsync("#comment")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                ) ==> comment)
                transition ((fun _ -> 
                    task {
                        do! globalState.Page.ClickAsync("#signin")
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                ) ==> signIn)

                onExit (fun _ ->
                    printfn "Exiting home"
                )
            }