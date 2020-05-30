module ReporterTests

open System
open Expecto
open Scrutiny
open Swensen.Unquote
open FsCheck
open Scrutiny.Scrutiny
open Scrutiny.Operators
open System.Text.Json

open Expecto.Logging
open Expecto.Logging.Message

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
    let logger = Log.create "MyTests"
    testList "Reporter Tests" [
        Tests.test "Should set graph" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start ag

            let final = reporter.Finish ()

            test <@ final.Graph = ag @>
            test <@ final.Error = None @>
            test <@ final.PerformedTransitions.Length = 0 @>
        }

        Tests.test "Should add transitions" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start ag
            reporter.PushTransition <| (TestPages.loggedInComment(), TestPages.signIn())
            reporter.PushTransition <| (TestPages.loggedInHome(), TestPages.comment())
            reporter.PushTransition <| (TestPages.comment(), TestPages.home())

            let final = reporter.Finish ()

            test <@ final.Error = None @>
            test <@ final.PerformedTransitions.Length = 3 @>
        }

        Tests.test "Should set error" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start ag
            reporter.PushTransition <| (TestPages.loggedInComment(), TestPages.signIn())
            reporter.PushTransition <| (TestPages.loggedInHome(), TestPages.comment())
            reporter.PushTransition <| (TestPages.comment(), TestPages.home())
            reporter.OnError (State (TestPages.loggedInComment().Name, Exception("Error")))

            let final = reporter.Finish ()

            test <@ final.Error.IsSome @>
            test <@ final.PerformedTransitions.Length = 3 @>
        }

        Tests.test "Should write results to file with state error" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start ag
            reporter.PushTransition <| (TestPages.loggedInComment(), TestPages.signIn())
            reporter.PushTransition <| (TestPages.loggedInHome(), TestPages.comment())
            reporter.PushTransition <| (TestPages.comment(), TestPages.home())
            reporter.OnError (State (TestPages.comment().Name, Exception("Error")))

            let final = reporter.Finish ()

            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())

            let a = JsonSerializer.Serialize(final, options)

            logger.info(eventX a)

            test <@ final.Error.IsSome @>
            test <@ final.PerformedTransitions.Length = 3 @>
        }

        Tests.ftest "Should write results to file with transition error" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.home ()) ()

            reporter.Start ag
            reporter.PushTransition <| (TestPages.home(), TestPages.signIn())
            reporter.PushTransition <| (TestPages.signIn(), TestPages.loggedInHome())
            reporter.PushTransition <| (TestPages.loggedInHome(), TestPages.loggedInComment())
            reporter.PushTransition <| (TestPages.loggedInComment(), TestPages.loggedInHome())
            reporter.PushTransition <| (TestPages.loggedInHome(), TestPages.home())

            reporter.OnError (Transition (TestPages.loggedInHome().Name, TestPages.home().Name, Exception("Error")))

            let final = reporter.Finish ()

            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())

            let a = JsonSerializer.Serialize(final, options)

            //logger.info(eventX a)
            System.IO.File.WriteAllText("C:\\users\\kait\\desktop\\herp.json", a)

            test <@ final.Error.IsSome @>
            test <@ final.PerformedTransitions.Length = 5 @>
        }

        // Write test for specific error transition
    ]
