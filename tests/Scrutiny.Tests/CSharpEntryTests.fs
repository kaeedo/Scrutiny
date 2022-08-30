module CSharpEntryTests

open Scrutiny
open Scrutiny.CSharp
open Expecto
open Swensen.Unquote
open System.Threading.Tasks

[<PageState>]
type ValidPageState(gs: obj) =
    [<OnEnter>]
    member _.OnEnter() = ()

    [<TransitionTo("AnotherValidPageState")>]
    member _.MoveToAnother() = ()

[<PageState>]
type AnotherValidPageState(gs: obj) =
    [<OnExit>]
    member _.OnEnter() = ()

    [<TransitionTo("ValidPageState")>]
    member _.MoveToValid() = ()

[<PageState>]
type AsyncPageState(gs: obj) =
    [<Action>]
    member _.DoSomething() =
        Task.FromResult(gs.ToString() |> ignore)

    [<TransitionTo("AnotherValidPageState")>]
    member _.MoveToAnotherValid() = Task.FromResult(())

    [<TransitionTo("ValidPageState")>]
    member _.MoveToValid() = Task.FromResult(())

[<Tests>]
let csharpEntryTests =
    testList
        "C# Entry Tests"
        [ Tests.test "Should construct page state definitions" {
              let definitions =
                  ScrutinyCSharp.buildPageStateDefinitions (obj ()) typeof<ValidPageState>

              let transitions =
                  definitions
                  |> List.collect (fun d -> d.Transitions)

              test <@ definitions.Length = 3 @>
              test <@ transitions.Length = 4 @>
          } ]
