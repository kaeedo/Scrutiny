namespace UsageExample.Playwright

open System
open System.Threading.Tasks
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

                onExit (fun _ -> globalState.Logger "Exiting sign in")

                transition {
                    via (fun _ ->
                        task {
                            globalState.Logger "Sign in transition: Clicking on home"

                            do! globalState.Page.ClickAsync("id=home")
                        })

                    destination home
                }

                transition {
                    dependantActions
                        [ "Fill out username"
                          "Fill out number" ]

                    via (fun _ ->
                        task {
                            globalState.IsSignedIn <- true

                            globalState.Logger "Sign in transition: clicking text=sign in"
                            do! globalState.Page.ClickAsync("css=button >> text=Sign In")
                        })

                    destination loggedInHome
                }

                action {
                    name "Fill out username"

                    fn (fun _ ->
                        task {
                            globalState.Logger "Sign in action: filling username"
                            globalState.Username <- "kaeedo"
                            do! globalState.Page.FillAsync("id=username", globalState.Username)

                            globalState.Logger "Sign in action: getting username"

                            let! username = globalState.GetInputValueAsync("id=username")
                            Assert.Equal(globalState.Username, username)
                        })
                }

                action {
                    name "Fill out number"

                    fn (fun _ ->
                        task {
                            globalState.Logger "Sign in action: filling number"
                            do! globalState.Page.FillAsync("id=number", "42")

                            globalState.Logger "Sign in action: getting number"
                            let! number = globalState.GetInputValueAsync("id=number")
                            Assert.Equal("42", number)
                        })
                }

                action {
                    fn (fun _ ->
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
                }
            }

    let loggedInComment =
        fun (globalState: GlobalState) ->
            let ls = LoggedInComment()

            page {
                name "Logged In Comment"

                onEnter (fun _ ->
                    task {
                        globalState.Logger "Checking comment is logged in"

                        let! openModal = globalState.Page.QuerySelectorAsync("id=openModal")
                        Assert.NotNull(openModal)
                        let! displayState = openModal.EvaluateAsync("e => e.style.display")

                        Assert.False(displayState.ToString() = "none")
                    })

                onExit (fun _ ->
                    task {
                        let! comments = globalState.Page.QuerySelectorAllAsync("#commentsUl>li")
                        let comments = comments |> List.ofSeq

                        let! writtenComments = Task.WhenAll(comments |> List.map (fun c -> c.InnerTextAsync()))

                        let writtenComment =
                            writtenComments
                            |> Array.tryFind (fun wc ->
                                wc.Contains(globalState.Username)
                                && wc.Contains(ls.Comment))

                        Assert.True(writtenComment.IsSome)
                        globalState.Logger "Exiting comment logged in"
                    })

                transition {
                    via (fun _ -> task { do! globalState.Page.ClickAsync("id=home") })
                    destination loggedInHome
                }

                action {
                    fn (fun _ ->
                        task {
                            do! globalState.Page.ClickAsync("id=openModal")

                            ls.Comment <- "This is my super comment"

                            do! globalState.Page.FillAsync("id=comment", ls.Comment)
                            do! globalState.Page.ClickAsync("id=modalFooterSave")
                        })
                }
            }

    let loggedInHome =
        fun (globalState: GlobalState) ->
            page {
                name "Logged in Home"

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

                transition {
                    via (fun _ -> task { do! globalState.Page.ClickAsync("id=comment") })
                    destination loggedInComment
                }

                transition {
                    via (fun _ -> task { do! globalState.Page.ClickAsync("id=logout") })
                    destination home
                }


            (*exitAction (fun _ ->
                    task {
                        globalState.Logger "Exiting!"
                        let! welcomeText = globalState.Page.QuerySelectorAsync("id=welcomeText")
                        Assert.NotNull(welcomeText)
                        do! globalState.Page.ClickAsync("id=logout")
                    })*)
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

                onExit (fun _ -> globalState.Logger "Exiting comment")

                transition {
                    via (fun _ -> task { do! globalState.Page.ClickAsync("id=home") })
                    destination home
                }

                transition {
                    via (fun _ -> task { do! globalState.Page.ClickAsync("id=signin") })
                    destination signIn
                }


            (*exitAction (fun _ -> task { do! globalState.Page.CloseAsync() })*)
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

                onExit (fun _ -> globalState.Logger "Exiting home")

                transition {
                    via (fun _ -> task { do! globalState.Page.ClickAsync("id=comment") })
                    destination comment
                }

                transition {
                    via (fun _ -> task { do! globalState.Page.ClickAsync("id=signin") })
                    destination signIn
                }
            }
