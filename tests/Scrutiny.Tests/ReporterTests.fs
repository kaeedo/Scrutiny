module ReporterTests

open System
open Expecto
open Scrutiny
open Swensen.Unquote
open Scrutiny.Scrutiny
open Scrutiny.Operators
open System.Text.Json

open System.Text.Json.Serialization

module rec TestPages =
    let home = fun _ ->
        page {
            name "Home"
            transition (ignore ==> comment)
            transition (ignore ==> signIn)
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

            reporter.Start ag

            let final = reporter.Finish ()

            let pt = final.PerformedTransitions

            test <@ final.Graph = ag @>
            test <@ pt |> List.exists (fun f -> f.Error.IsSome) |> not @>
            test <@ pt.Length = 0 @>
        }

        Tests.test "Should add transitions" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start ag
            reporter.PushTransition <| (TestPages.loggedInComment(), TestPages.signIn())
            reporter.PushTransition <| (TestPages.loggedInHome(), TestPages.comment())
            reporter.PushTransition <| (TestPages.comment(), TestPages.home())

            let final = reporter.Finish ()

            let pt = final.PerformedTransitions

            test <@ pt |> List.exists (fun f -> f.Error.IsSome) |> not @>
            test <@ pt.Length = 3 @>
        }

        Tests.test "Should set error" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            let serializableException =
                { SerializableException.Type = "Exception"
                  Message = "Error message"
                  StackTrace = "Happened here"
                  InnerException = None }

            reporter.Start ag
            reporter.PushTransition <| (TestPages.loggedInComment(), TestPages.signIn())
            reporter.PushTransition <| (TestPages.loggedInHome(), TestPages.comment())
            reporter.PushTransition <| (TestPages.comment(), TestPages.home())
            reporter.OnError (State (TestPages.loggedInComment().Name, serializableException))

            let final = reporter.Finish ()

            let pt = final.PerformedTransitions

            test <@ (pt |> List.last).Error.IsSome @>
            test <@ pt.Length = 3 @>
        }

        Tests.test "Should write results to file with state error" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            let serializableException =
                { SerializableException.Type = "Exception"
                  Message = "Error message"
                  StackTrace = "Happened here"
                  InnerException = None }

            reporter.Start ag
            reporter.PushTransition <| (TestPages.loggedInComment(), TestPages.signIn())
            reporter.PushTransition <| (TestPages.loggedInHome(), TestPages.comment())
            reporter.PushTransition <| (TestPages.comment(), TestPages.home())
            reporter.OnError (State (TestPages.comment().Name, serializableException))

            let final = reporter.Finish ()

            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())

            let pt = final.PerformedTransitions

            test <@ pt.[0..pt.Length - 2] |> List.forall (fun f -> f.Error.IsNone)  @>
            test <@ (pt |> List.last).Error.IsSome @>
            test <@ pt.Length = 2 @>
        }
    ]
