namespace Scrutiny

open System.Runtime.CompilerServices
open System.Threading.Tasks

type PageBuilder() =
    member _.Yield _: PageState<'a, 'b> =
        { PageState.Name = ""
          LocalState = Unchecked.defaultof<'b>
          OnEnter = fun _ -> Task.FromResult()
          OnExit = fun _ -> Task.FromResult()
          Transitions = []
          Actions = []
          ExitActions = [] } // TODO. states can have many exit actions. one is chosen at random anyway.

    [<CustomOperation("name")>]
    member _.Name(state, handler): PageState<'a, 'b> = { state with Name = handler }

    [<CustomOperation("localState")>]
    member _.LocalState(state, handler): PageState<'a, 'b> = { state with LocalState = handler }

    [<CustomOperation("onEnter")>]
    member _.OnEnter(state, handler: 'b -> unit): PageState<'a, 'b> =
        let handler = fun localState -> Task.FromResult(handler localState)
        { state with OnEnter = handler }
        
    [<CustomOperation("onEnter")>]
    member _.OnEnter(state, handler: 'b -> Task<unit>): PageState<'a, 'b> = { state with OnEnter = handler }

    [<CustomOperation("onExit")>]
    member _.OnExit(state, handler: 'b -> unit): PageState<'a, 'b> =
        let handler = fun localState -> Task.FromResult(handler localState)
        { state with OnExit = handler }
    
    [<CustomOperation("onExit")>]
    member _.OnExit(state, handler: 'b -> Task<unit>): PageState<'a, 'b> = { state with OnExit = handler }

    [<CustomOperation("transition")>]
    member _.Transitions(state, handler): PageState<'a, 'b> = { state with Transitions = handler :: state.Transitions }

    [<CustomOperation("action")>]
    member _.Actions(state, handler: 'b -> unit, [<CallerMemberName>]?memberName: string, [<CallerLineNumber>]?lineNumber: int, [<CallerFilePath>]?filePath: string): PageState<'a, 'b> = 
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }
            
        let handler = fun localState -> Task.FromResult(handler localState)
        { state with Actions = (callerInformation, handler) :: state.Actions }
        
    [<CustomOperation("action")>]
    member _.Actions(state, handler: 'b -> Task<unit>, [<CallerMemberName>]?memberName: string, [<CallerLineNumber>]?lineNumber: int, [<CallerFilePath>]?filePath: string): PageState<'a, 'b> = 
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        { state with Actions = (callerInformation, handler) :: state.Actions }
        
    [<CustomOperation("exitAction")>]
    member _.ExitAction(state, handler: 'b -> unit): PageState<'a, 'b> =
        let handler = fun localState -> Task.FromResult(handler localState)
        { state with ExitActions = handler :: state.ExitActions }
        
    [<CustomOperation("exitAction")>]
    member _.ExitAction(state, handler: 'b -> Task<unit>): PageState<'a, 'b> =
        { state with ExitActions = handler :: state.ExitActions }


