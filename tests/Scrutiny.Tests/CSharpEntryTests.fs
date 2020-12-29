module CSharpEntryTests

open Scrutiny
open Scrutiny.CSharp
open Expecto
open Swensen.Unquote
open System.Threading.Tasks

[<PageState>]
type ValidPageState(gs: obj) =
    [<OnEnter>] 
    member __.OnEnter() = ()

    [<TransitionTo("AnotherValidPageState")>] 
    member __.MoveToAnother() = ()

[<PageState>]
type AnotherValidPageState(gs: obj) =
    [<OnExit>] 
    member __.OnEnter() = ()

    [<TransitionTo("ValidPageState")>] 
    member __.MoveToValid() = ()

[<PageState>]
type AsyncPageState(gs: obj) =
    [<Action>]
    member __.DoSOmething() = 
        Task.FromResult(gs.ToString() |> ignore)

    [<TransitionTo("AnotherValidPageState")>]
    member __.MoveToAnotherValid() = Task.FromResult(())

    [<TransitionTo("ValidPageState")>]
    member __.MoveToValid() = Task.FromResult(())

[<Tests>]
let csharpEntryTests = 
    testList "C# Entry Tests" [
        Tests.test "Should construct page state definitions" {
            let definitions = ScrutinyCSharp.buildPageStateDefinitions (obj()) typeof<ValidPageState>
            let transitions = 
                definitions 
                |> List.collect (fun d ->
                    d.Transitions
                )

            test <@ definitions.Length = 3 @>
            test <@ transitions.Length = 4 @>
        }
    ]
