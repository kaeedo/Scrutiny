namespace UsageExample.Playwright

open System
open Scrutiny
open Scrutiny.Operators
open Scrutiny.Scrutiny

open PlaywrightSharp
open Xunit

type GlobalState(page: IPage, logger: string -> unit) =
    member val Logger = logger with get
    member val Page = page with get
    member val IsSignedIn = false with get, set
    member val Username = "MyUsername" with get, set
    member val Number = 42

    member x.GetInputValueAsync (selector: string) =
        async {
            let! element = x.Page.QuerySelectorAsync(selector) |> Async.AwaitTask
            let! value = element.EvaluateAsync("e => e.value") |> Async.AwaitTask
            return value.ToString()
        }

type LoggedInComment() =
    member val Comment = String.Empty with get, set

module rec ScrutinyStateMachine =
    let signIn =
        fun (globalState: GlobalState) ->
            page {
                name "Sign In"
                onEnter (fun _ ->
                    globalState.Logger "Checking on page sign in"
                    async {
                        globalState.Logger "Sign in: Looking for header text"
                        let! headerText = globalState.Page.GetInnerTextAsync("#header") |> Async.AwaitTask
                        Assert.Equal("Sign In", headerText)
                    }
                    |> Async.RunSynchronously
                )

                transition ((fun _ ->
                    async {
                        globalState.Logger "Sign in: Clicking on home"
                        do! globalState.Page.ClickAsync("#home") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                ) ==> home)

                transition
                    ((fun _ ->
                        async {
                            globalState.Logger "Sign in: Filling in username"
                            globalState.Username <- "kaeedo"
                            do! globalState.Page.FillAsync("#username", globalState.Username) |> Async.AwaitTask

                            globalState.Logger "Sign in: Filling in number"
                            do! globalState.Page.FillAsync("#number", globalState.Number.ToString()) |> Async.AwaitTask

                            globalState.IsSignedIn <- true

                            globalState.Logger "Sign in: clicking text=sign in"
                            do! globalState.Page.ClickAsync("css=button >> text=Sign In") |> Async.AwaitTask
                        }
                        |> Async.RunSynchronously
                    ) ==> loggedInHome)

                action (fun _ ->
                    async {
                        globalState.Logger "Sign in: filling username"
                        do! globalState.Page.FillAsync("#username", "MyUsername") |> Async.AwaitTask

                        globalState.Logger "Sign in: getting username"
                        
                        let! username = globalState.GetInputValueAsync("#username")
                        Assert.Equal("MyUsername", username)
                    }
                    |> Async.RunSynchronously
                )
                action (fun _ ->
                    async {
                        globalState.Logger "Sign in: filling number"
                        do! globalState.Page.FillAsync("#number", "42") |> Async.AwaitTask

                        globalState.Logger "Sign in: getting number"
                        let! number = globalState.GetInputValueAsync("#number")
                        Assert.Equal("42", number)
                    }
                    |> Async.RunSynchronously
                )

                action (fun _ ->
                    async {
                        let! username = globalState.GetInputValueAsync("#username")
                        let! number = globalState.GetInputValueAsync("#number")

                        let signInButtonSelector = "css=button >> text=Sign In"

                        if String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(number) then
                            do! globalState.Page.ClickAsync(signInButtonSelector) |> Async.AwaitTask
                        else
                            do! globalState.Page.FillAsync("#username", String.Empty) |> Async.AwaitTask
                            do! globalState.Page.ClickAsync(signInButtonSelector) |> Async.AwaitTask
                        
                        let! errorMessage = globalState.Page.QuerySelectorAsync("#ErrorMessage") |> Async.AwaitTask
                        Assert.NotNull(errorMessage)
                        let! displayState = errorMessage.EvaluateAsync("e => e.style.display") |> Async.AwaitTask

                        Assert.False(displayState.ToString() = "none")
                    }
                    |> Async.RunSynchronously
                )

                onExit (fun _ -> globalState.Logger "Exiting sign in")
            }

    let loggedInComment =
        fun (globalState: GlobalState) ->

            page {
                name "Logged In Comment"

                localState (LoggedInComment())

                transition ((fun _ ->
                    async {
                        do! globalState.Page.ClickAsync("#home") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                ) ==> loggedInHome)

                action (fun ls ->
                    async {
                        do! globalState.Page.ClickAsync("#openModal") |> Async.AwaitTask

                        ls.Comment <- "This is my super comment"

                        do! globalState.Page.FillAsync("#comment", ls.Comment) |> Async.AwaitTask
                        do! globalState.Page.ClickAsync("#modalFooterSave") |> Async.AwaitTask

                        let! comments = globalState.Page.QuerySelectorAllAsync("#commentsUl>li") |> Async.AwaitTask
                        let comments = comments |> List.ofSeq

                        let writtenComment =
                            comments
                            |> List.tryFind(fun c ->
                                async {
                                    let! text = c.GetInnerTextAsync() |> Async.AwaitTask
                                    return text = sprintf "%s wrote:\n%s" globalState.Username ls.Comment
                                }
                                |> Async.RunSynchronously
                            )

                        Assert.True(writtenComment.IsSome)
                    }
                    |> Async.RunSynchronously
                )

                onEnter (fun _ ->
                    globalState.Logger "Checking comment is logged in"
                    async {
                        let! openModal = globalState.Page.QuerySelectorAsync("#openModal") |> Async.AwaitTask
                        Assert.NotNull(openModal)
                        let! displayState = openModal.EvaluateAsync("e => e.style.display") |> Async.AwaitTask

                        Assert.False(displayState.ToString() = "none")
                    }
                    |> Async.RunSynchronously
                )

                onExit (fun _ -> globalState.Logger "Exiting comment logged in")
            }

    let loggedInHome =
        fun (globalState: GlobalState) ->
            page {
                name "Logged in Home"

                transition ((fun _ ->
                    async {
                        do! globalState.Page.ClickAsync("#comment") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                ) ==> loggedInComment)
                transition ((fun _ ->
                    async {
                        do! globalState.Page.ClickAsync("#logout") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                ) ==> home)

                onEnter (fun _ ->
                    globalState.Logger "Checking on page home logged in"

                    async {
                        let! welcomeText = globalState.Page.QuerySelectorAsync("#welcomeText") |> Async.AwaitTask
                        Assert.NotNull(welcomeText)
                        let! displayState = welcomeText.EvaluateAsync("e => e.style.display") |> Async.AwaitTask

                        Assert.False(displayState.ToString() = "none")

                        let! welcomeText = globalState.Page.GetInnerTextAsync("#welcomeText") |> Async.AwaitTask

                        Assert.Equal(sprintf "Welcome %s" globalState.Username, welcomeText);
                    }
                    |> Async.RunSynchronously
                )

                exitAction (fun _ ->
                    globalState.Logger "Exiting!"
                    async {
                        do! globalState.Page.ClickAsync("#logout") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                )
            }

    let comment =
        fun (globalState: GlobalState) ->
            page {
                name "Comment"
                onEnter (fun _ ->
                    globalState.Logger "Checking on page comment"

                    async {
                        let! headerText = globalState.Page.GetInnerTextAsync("#header") |> Async.AwaitTask
                        Assert.Equal("Comments", headerText)
                    }
                    |> Async.RunSynchronously
                )

                transition ((fun _ ->
                    async {
                        do! globalState.Page.ClickAsync("#home") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                ) ==> home)
                transition ((fun _ ->
                    async {
                        do! globalState.Page.ClickAsync("#signin") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                ) ==> signIn)

                onExit (fun _ -> globalState.Logger "Exiting comment")
            }

    let home =
        fun (globalState: GlobalState) ->
            page {
                name "Home"
                onEnter (fun _ ->
                    globalState.Logger "Checking on page home"

                    async {
                        let! headerText = globalState.Page.GetInnerTextAsync("#header") |> Async.AwaitTask
                        Assert.Equal("Home", headerText)
                    }
                    |> Async.RunSynchronously
                )

                transition ((fun _ ->
                    async {
                        do! globalState.Page.ClickAsync("#comment") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                ) ==> comment)
                transition ((fun _ ->
                    async {
                        do! globalState.Page.ClickAsync("#signin") |> Async.AwaitTask
                    }
                    |> Async.RunSynchronously
                ) ==> signIn)

                onExit (fun _ ->
                    globalState.Logger "Exiting home"
                )
            }
