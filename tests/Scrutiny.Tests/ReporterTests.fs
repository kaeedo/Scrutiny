module ReporterTests

open System
open Expecto
open Scrutiny
open Swensen.Unquote
open Scrutiny.Scrutiny
open Scrutiny.Operators

module rec TestPages =
    let home = 
        fun _ ->
            page {
                name "Home"
                onEnter (ignore)

                transition (ignore ==> comment)
                transition (ignore ==> signIn)

                action (ignore) String.Empty 0 String.Empty
                action (ignore) String.Empty 0 String.Empty
            }

    let comment = fun _ ->
        page {
            name "Comment"
            transition (ignore ==> home)
            transition (ignore ==> signIn)
        }

    let signIn = fun _ ->
        page {
            name "Sign In"
            transition (ignore ==> home)
            transition (ignore ==> loggedInHome)
            action (ignore) String.Empty 0 String.Empty
        }

    let loggedInComment = fun _ ->
        page {
            name "Logged in Comment"
            transition (ignore ==> loggedInHome)
        }

    let loggedInHome = fun _ ->
        page {
            name "Logged in Home"
            transition (ignore ==> home)
            transition (ignore ==> loggedInComment)
        }

[<Tests>]
let reporterTests =
    testList "Reporter Tests" [
        Tests.test "Should set graph" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start (ag, TestPages.home())

            let final = reporter.Finish ()

            let steps = final.Steps

            test <@ final.Graph = ag @>
            test <@ steps |> Seq.exists (fun f -> f.Error.IsSome) |> not @>
            test <@ steps |> Seq.length = 1 @>
        }

        Tests.test "Should add actions" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start (ag, TestPages.home())
            reporter.PushAction("herp derp")

            let final = reporter.Finish ()
            let homeStep = final.Steps |> Seq.find (fun s -> s.PageState.Name = "Home")

            test <@ (homeStep.Actions |> Seq.head) = "herp derp" @>
        }

        Tests.test "Should add transitions" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start (ag, TestPages.home())
            reporter.PushTransition <| (TestPages.signIn())
            reporter.PushTransition <| (TestPages.comment())
            reporter.PushTransition <| (TestPages.home())

            let final = reporter.Finish ()

            let steps = final.Steps

            test <@ steps |> Seq.exists (fun f -> f.Error.IsSome) |> not @>
            test <@ steps |> Seq.length = 4 @>
        }

        Tests.test "Should set error" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            let serializableException =
                { SerializableException.Type = "Exception"
                  Message = "Error message"
                  StackTrace = "Happened here"
                  InnerException = None }

            reporter.Start (ag, TestPages.home())
            reporter.PushTransition <| (TestPages.loggedInComment())
            reporter.PushTransition <| (TestPages.comment())
            reporter.PushTransition <| (TestPages.home())
            reporter.OnError (State (TestPages.home().Name, serializableException))

            let final = reporter.Finish ()

            let steps = final.Steps

            test <@ (steps |> Seq.last).Error.IsSome @>
            test <@ steps |> Seq.length = 4 @> 
        }

        Tests.test "Should write results to file with state error" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            let serializableException =
                { SerializableException.Type = "Exception"
                  Message = "Error message"
                  StackTrace = "Happened here"
                  InnerException = None }

            reporter.Start (ag, TestPages.home())
            reporter.PushTransition <| (TestPages.signIn())
            reporter.PushTransition <| (TestPages.comment())
            reporter.PushTransition <| (TestPages.home())
            reporter.OnError (State (TestPages.comment().Name, serializableException))

            let final = reporter.Finish ()

            let steps = final.Steps |> Seq.toList

            test <@ steps.[0..steps.Length - 2] |> List.forall (fun f -> f.Error.IsNone)  @>
            test <@ (steps |> Seq.last).Error.IsSome @>
            test <@ steps |> Seq.length = 4 @>
        }
    ]