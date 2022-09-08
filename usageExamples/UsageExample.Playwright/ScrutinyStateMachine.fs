namespace UsageExample.Playwright

open System
open Microsoft.Playwright
open Scrutiny

open Xunit

type GlobalState(page: IPage, logger: string -> unit) =
    member val Logger = logger
    member val Page = page
    member val IsSignedIn = false with get, set
    member val Username = "MyUsername" with get, set
    member val Number = 42

    member x.GetInputValueAsync(selector: string) =
        task {
            let! element = x.Page.QuerySelectorAsync(selector)
            let! value = element.EvaluateAsync("e => e.value")
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

                    task {
                        globalState.Logger "Sign in: Looking for header text"
                        let! headerText = globalState.Page.InnerTextAsync("id=header")
                        Assert.Equal("Sign In", headerText)
                    })

                transition (
                    (fun _ ->
                        task {
                            globalState.Logger "Sign in transition: Clicking on home"

                            do! globalState.Page.ClickAsync("id=home")
                        })
                    ==> home
                )

                transition (
                    (fun _ ->
                        task {
                            globalState.Logger "Sign in transition: Filling in username"
                            globalState.Username <- "kaeedo"
                            do! globalState.Page.FillAsync("id=username", globalState.Username)

                            globalState.Logger "Sign in transition: Filling in number"
                            do! globalState.Page.FillAsync("id=number", globalState.Number.ToString())

                            globalState.IsSignedIn <- true

                            globalState.Logger "Sign in transition: clicking text=sign in"
                            do! globalState.Page.ClickAsync("css=button >> text=Sign In")
                        })
                    ==> loggedInHome
                )

                action (fun _ ->
                    task {
                        globalState.Logger "Sign in action: filling username"
                        do! globalState.Page.FillAsync("id=username", "MyUsername")

                        globalState.Logger "Sign in action: getting username"

                        let! username = globalState.GetInputValueAsync("id=username")
                        Assert.Equal("MyUsername", username)
                    })

                action (fun _ ->
                    task {
                        globalState.Logger "Sign in action: filling number"
                        do! globalState.Page.FillAsync("id=number", "42")

                        globalState.Logger "Sign in action: getting number"
                        let! number = globalState.GetInputValueAsync("id=number")
                        Assert.Equal("42", number)
                    })

                action (fun _ ->
                    task {
                        let! username = globalState.GetInputValueAsync("id=username")
                        let! number = globalState.GetInputValueAsync("id=number")

                        let signInButtonSelector = "css=button >> text=Sign In"

                        if
                            String.IsNullOrWhiteSpace(username)
                            || String.IsNullOrWhiteSpace(number)
                        then
                            do! globalState.Page.ClickAsync(signInButtonSelector)
                        else
                            do! globalState.Page.FillAsync("id=username", String.Empty)
                            do! globalState.Page.ClickAsync(signInButtonSelector)

                        let! errorMessage = globalState.Page.QuerySelectorAsync("id=ErrorMessage")
                        Assert.NotNull(errorMessage)
                        let! displayState = errorMessage.EvaluateAsync("e => e.style.display")

                        Assert.False(displayState.ToString() = "none")
                    })

                onExit (fun _ -> globalState.Logger "Exiting sign in")
            }

    let loggedInComment =
        fun (globalState: GlobalState) ->

            page {
                name "Logged In Comment"

                localState (LoggedInComment())

                transition (
                    (fun _ -> task { do! globalState.Page.ClickAsync("id=home") })
                    ==> loggedInHome
                )

                action (fun ls ->
                    task {
                        do! globalState.Page.ClickAsync("id=openModal")

                        ls.Comment <- "This is my super comment"

                        do! globalState.Page.FillAsync("id=comment", ls.Comment)
                        do! globalState.Page.ClickAsync("id=modalFooterSave")
                    })

                onEnter (fun _ ->
                    task {
                        globalState.Logger "Checking comment is logged in"

                        let! openModal = globalState.Page.QuerySelectorAsync("id=openModal")
                        Assert.NotNull(openModal)
                        let! displayState = openModal.EvaluateAsync("e => e.style.display")

                        Assert.False(displayState.ToString() = "none")
                    })

                onExit (fun ls ->
                    task {
                        let! comments = globalState.Page.QuerySelectorAllAsync("id=commentsUl>li")
                        let comments = comments |> List.ofSeq

                        let writtenComment =
                            comments
                            |> List.tryFind (fun c ->
                                (task {
                                    let! text = c.InnerTextAsync()
                                    return text = $"%s{globalState.Username} wrote:\n%s{ls.Comment}"
                                })
                                    .GetAwaiter()
                                    .GetResult())

                        Assert.True(writtenComment.IsSome)
                        globalState.Logger "Exiting comment logged in"
                    })
            }

    let loggedInHome =
        fun (globalState: GlobalState) ->
            page {
                name "Logged in Home"

                transition (
                    (fun _ -> task { do! globalState.Page.ClickAsync("id=comment") })
                    ==> loggedInComment
                )

                transition (
                    (fun _ -> task { do! globalState.Page.ClickAsync("id=logout") })
                    ==> home
                )

                onEnter (fun _ ->
                    task {
                        globalState.Logger "Checking on page home logged in"

                        let! welcomeText = globalState.Page.QuerySelectorAsync("id=welcomeText")
                        Assert.NotNull(welcomeText)
                        let! displayState = welcomeText.EvaluateAsync("e => e.style.display")

                        Assert.False(displayState.ToString() = "none")

                        let! welcomeText = globalState.Page.InnerTextAsync("id=welcomeText")

                        Assert.Equal(sprintf "Welcome %s" globalState.Username, welcomeText)
                    })

                exitAction (fun _ ->
                    task {
                        globalState.Logger "Exiting!"
                        let! welcomeText = globalState.Page.QuerySelectorAsync("id=welcomeText")
                        Assert.NotNull(welcomeText)
                        do! globalState.Page.ClickAsync("id=logout")
                    })
            }

    let comment =
        fun (globalState: GlobalState) ->
            page {
                name "Comment"

                onEnter (fun _ ->
                    globalState.Logger "Checking on page comment"

                    task {
                        let! headerText = globalState.Page.InnerTextAsync("id=header")
                        Assert.Equal("Comments", headerText)
                    })

                transition (
                    (fun _ -> task { do! globalState.Page.ClickAsync("id=home") })
                    ==> home
                )

                transition (
                    (fun _ -> task { do! globalState.Page.ClickAsync("id=signin") })
                    ==> signIn
                )

                onExit (fun _ -> globalState.Logger "Exiting comment")

                exitAction (fun _ -> task { do! globalState.Page.CloseAsync() })
            }

    let home =
        fun (globalState: GlobalState) ->
            page {
                name "Home"

                onEnter (fun _ ->
                    globalState.Logger "Checking on page home"

                    task {
                        let! headerText = globalState.Page.InnerTextAsync("id=header")
                        Assert.Equal("Home", headerText)
                    })

                transition (
                    (fun _ -> task { do! globalState.Page.ClickAsync("id=comment") })
                    ==> comment
                )

                transition (
                    (fun _ -> task { do! globalState.Page.ClickAsync("id=signin") })
                    ==> signIn
                )

                onExit (fun _ -> globalState.Logger "Exiting home")
            }
