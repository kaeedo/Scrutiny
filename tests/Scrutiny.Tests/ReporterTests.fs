module ReporterTests

open System
open Expecto
open Scrutiny
open Swensen.Unquote
open FsCheck
open Scrutiny.Scrutiny
open Scrutiny.Operators

module rec TestPages =
    let page1 = fun _ ->
        page {
            name "Page1"
            transition (ignore ==> page2)
        }

    let page2 = fun _ ->
        page {
            name "Page2"
            transition (ignore ==> page1)
            transition (ignore ==> page3)
        }

    let page3 = fun _ ->
        page {
            name "Page3"
            transition (ignore ==> page4)
            transition (ignore ==> page5)
        }

    let page4 = fun _ ->
        page {
            name "Page4"
            transition (ignore ==> page3)
            transition (ignore ==> page5)
        }

    let page5 = fun _ ->
        page {
            name "Page5"
            transition (ignore ==> page2)
            transition (ignore ==> page3)
        }

[<Tests>]
let reporterTests =
    testList "Reporter Tests" [
        Tests.test "Should set graph" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.page1 ()) ()

            reporter.Start ag

            let final = reporter.Finish ()

            test <@ final.Graph = ag @>
            test <@ final.Error = String.Empty @>
            test <@ final.Transitions.Length = 0 @>
        }

        Tests.test "Should add transitions" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.page1 ()) ()

            reporter.Start ag
            reporter.PushTransition <| TestPages.page4()
            reporter.PushTransition <| TestPages.page5()
            reporter.PushTransition <| TestPages.page2()

            let final = reporter.Finish ()

            test <@ final.Error = String.Empty @>
            test <@ final.Transitions.Length = 3 @>
        }

        Tests.test "Should set error" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.page1 ()) ()

            reporter.Start ag
            reporter.PushTransition <| TestPages.page4()
            reporter.PushTransition <| TestPages.page5()
            reporter.PushTransition <| TestPages.page2()
            reporter.OnError "Error"

            let final = reporter.Finish ()

            test <@ final.Error = "Error" @>
            test <@ final.Transitions.Length = 3 @>
        }

        Tests.ftest "Should write results to file" {
            let reporter: IReporter<unit, obj> = Reporter<unit, obj>(ScrutinyConfig.Default.ScrutinyResultFilePath) :> IReporter<unit, obj>
            let ag = Navigator.constructAdjacencyGraph (TestPages.page1 ()) ()

            reporter.Start ag
            reporter.PushTransition <| TestPages.page4()
            reporter.PushTransition <| TestPages.page5()
            reporter.PushTransition <| TestPages.page2()
            reporter.OnError "Error"

            let final = reporter.Finish ()

            test <@ final.Error = "Error" @>
            test <@ final.Transitions.Length = 3 @>
        }
    ]
