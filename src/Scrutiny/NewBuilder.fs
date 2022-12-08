namespace Scrutiny

open System.Runtime.CompilerServices
open System.Threading.Tasks


// TODO make constructors private
type Name = Name of string

type LocalState<'b> = LocalState of 'b

type OnEnter<'b> =
    | OnEnterTAsync of ('b -> Task<unit>)
    | OnEnter of ('b -> unit)

type OnExit<'b> =
    | OnExitTAsync of ('b -> Task<unit>)
    | OnExit of ('b -> unit)

type ExitAction<'b> =
    | ExitActionTAsync of ('b -> Task<unit>)
    | ExitAction of ('b -> unit)

type Page2Builder() =
    member this.Zero() = id

    member this.Yield(name: Name) : PageState<'a, 'b> -> PageState<'a, 'b> =
        fun state ->
            let (Name name) = name
            { state with Name = name }

    member this.Yield(localState: LocalState<'b>) : PageState<'a, 'b> -> PageState<'a, 'b> =
        fun state ->
            let (LocalState localState) = localState
            { state with LocalState = localState }

    member this.Yield(onEnter: OnEnter<'b>) : PageState<'a, 'b> -> PageState<'a, 'b> =
        fun state ->
            let fn =
                match onEnter with
                | OnEnterTAsync tfn -> tfn
                | OnEnter fn -> fun localState -> Task.FromResult(fn localState)

            { state with OnEnter = fn }

    member this.Yield(onExit: OnExit<'b>) : PageState<'a, 'b> -> PageState<'a, 'b> =
        fun state ->
            let fn =
                match onExit with
                | OnExitTAsync tfn -> tfn
                | OnExit fn -> fun localState -> Task.FromResult(fn localState)

            { state with OnExit = fn }

    member this.Yield(exitAction: ExitAction<'b>) : PageState<'a, 'b> -> PageState<'a, 'b> =
        fun state ->
            let fn =
                match exitAction with
                | ExitActionTAsync tfn -> tfn
                | ExitAction fn -> fun localState -> Task.FromResult(fn localState)

            { state with ExitActions = fn :: state.ExitActions }



    member this.Combine(f, g) =
        fun (state: PageState<'a, 'b>) -> g (f state)

    member this.Delay f = fun state -> (f ()) state



    member _.Yield _ : PageState<'a, 'b> =
        { PageState.Name = ""
          LocalState = Unchecked.defaultof<'b>
          OnEnter = fun _ -> Task.FromResult()
          OnExit = fun _ -> Task.FromResult()
          Transitions = []
          Actions = []
          ExitActions = [] } // TODO. states can have many exit actions. one is chosen at random anyway.

    // [<CustomOperation("name")>]
    // member _.Name(state, handler) : PageState<'a, 'b> = { state with Name = handler }

    // [<CustomOperation("localState")>]
    // member _.LocalState(state, handler) : PageState<'a, 'b> = { state with LocalState = handler }

    // [<CustomOperation("onEnter")>]
    // member _.OnEnter(state, handler: 'b -> unit) : PageState<'a, 'b> =
    //     let handler = fun localState -> Task.FromResult(handler localState)
    //     { state with OnEnter = handler }
    //
    // [<CustomOperation("onEnter")>]
    // member _.OnEnter(state, handler: 'b -> Task<unit>) : PageState<'a, 'b> = { state with OnEnter = handler }

    // [<CustomOperation("onExit")>]
    // member _.OnExit(state, handler: 'b -> unit) : PageState<'a, 'b> =
    //     let handler = fun localState -> Task.FromResult(handler localState)
    //     { state with OnExit = handler }
    //
    // [<CustomOperation("onExit")>]
    // member _.OnExit(state, handler: 'b -> Task<unit>) : PageState<'a, 'b> = { state with OnExit = handler }

    //-----------------
    // Transitions
    //-----------------

    [<CustomOperation("transition")>]
    member _.Transitions(state, handler) : PageState<'a, 'b> =
        { state with Transitions = ([], handler) :: state.Transitions }

    [<CustomOperation("transitionWith")>]
    member _.Transitions(state, (handler: string list * Transition<'a, 'b>)) : PageState<'a, 'b> =
        { state with Transitions = handler :: state.Transitions }

    //-----------------
    // Actions
    //-----------------

    // Unnamed action without task
    [<CustomOperation("action")>]
    member _.Actions
        (
            state,
            handler: 'b -> unit,
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler = fun localState -> Task.FromResult(handler localState)

        { state with
            Actions =
                (callerInformation, (None, [], handler))
                :: state.Actions }

    // Unnamed action with task
    [<CustomOperation("action")>]
    member _.Actions
        (
            state,
            handler: 'b -> Task<unit>,
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        { state with
            Actions =
                (callerInformation, (None, [], handler))
                :: state.Actions }

    // Unnamed action without task with dependencies
    [<CustomOperation("actionWith")>]
    member _.Actions
        (
            state,
            handler: (string list) * ('b -> unit),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let dependencies, handler = handler
            None, dependencies, (fun localState -> Task.FromResult(handler localState))

        { state with Actions = (callerInformation, handler) :: state.Actions }

    // Unnamed action with task with dependencies
    [<CustomOperation("actionWith")>]
    member _.Actions
        (
            state,
            handler: string list * ('b -> Task<unit>),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        { state with
            Actions =
                (callerInformation, (None, (fst handler), (snd handler)))
                :: state.Actions }

    // Named action without task
    [<CustomOperation("actionN")>]
    member _.Actions
        (
            state,
            handler: string * ('b -> unit),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let name = fst handler
            Some name, [], (fun localState -> Task.FromResult((snd handler) localState))

        { state with Actions = (callerInformation, handler) :: state.Actions }

    // Named action with task
    [<CustomOperation("actionN")>]
    member _.Actions
        (
            state,
            handler: string * ('b -> Task<unit>),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let name = fst handler
            Some name, [], snd handler

        { state with Actions = (callerInformation, handler) :: state.Actions }

    // Named action without task with dependencies
    [<CustomOperation("actionNWith")>]
    member _.Actions
        (
            state,
            handler: string * string list * ('b -> unit),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let name, dependencies, handler = handler
            Some name, dependencies, (fun localState -> Task.FromResult(handler localState))

        { state with Actions = (callerInformation, handler) :: state.Actions }

    // Named action with task with dependencies
    [<CustomOperation("actionNWith")>]
    member _.Actions
        (
            state,
            handler: string * string list * ('b -> Task<unit>),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let name, dependencies, handler = handler
            Some name, dependencies, handler

        { state with Actions = (callerInformation, handler) :: state.Actions }

//-----------------
// Exit actions
//-----------------

// [<CustomOperation("exitAction")>]
// member _.ExitAction(state, handler: 'b -> unit) : PageState<'a, 'b> =
//     let handler = fun localState -> Task.FromResult(handler localState)
//     { state with ExitActions = handler :: state.ExitActions }
//
// [<CustomOperation("exitAction")>]
// member _.ExitAction(state, handler: 'b -> Task<unit>) : PageState<'a, 'b> =
//     { state with ExitActions = handler :: state.ExitActions }
