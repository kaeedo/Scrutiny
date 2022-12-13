namespace Scrutiny

open System.Runtime.CompilerServices
open System.Threading.Tasks

type TransitionFunc<'b> = TransitionFunc of ('b -> Task<unit>)
type ToState<'a, 'b> = ToState of ('a -> PageState<'a, 'b>)

//type RequiredAction = RequiredAction of string

type TransitionBuilder() =
    member this.Zero() = id

    member this.Yield(transitionFunc: TransitionFunc<'b>) : Transition<'a, 'b> -> Transition<'a, 'b> =
        fun state ->
            let (TransitionFunc fn) = transitionFunc
            { state with TransitionFn = fn }

    member this.Yield(toState: ToState<'a, 'b>) : Transition<'a, 'b> -> Transition<'a, 'b> =
        fun state ->
            let (ToState toState) = toState
            { state with ToState = toState }
    //member this.Yield(requiredAction: )

    member this.Combine(f, g) =
        fun (state: Transition<'a, 'b>) -> g (f state)

    member this.Delay f = fun state -> (f ()) state



    member _.Yield _ : Transition<'a, 'b> =
        { Transition.TransitionFn = fun _ -> Task.FromResult()
          ToState = fun _ -> Unchecked.defaultof<PageState<'a, 'b>> }



// TODO make constructors private
type Name = Name of string

type LocalState<'b> = LocalState of 'b

type OnEnter<'b> = OnEnter of ('b -> Task<unit>)

type OnExit<'b> = OnExit of ('b -> Task<unit>)

type ExitAction<'b> = ExitAction of ('b -> Task<unit>)

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
            let (OnEnter tfn) = onEnter

            { state with OnEnter = tfn }

    member this.Yield(onExit: OnExit<'b>) : PageState<'a, 'b> -> PageState<'a, 'b> =
        fun state ->
            let (OnExit fn) = onExit

            { state with OnExit = fn }

    member this.Yield(exitAction: ExitAction<'b>) : PageState<'a, 'b> -> PageState<'a, 'b> =
        fun state ->
            let (ExitAction fn) = exitAction

            { state with ExitActions = fn :: state.ExitActions }

    member this.Yield(transition: Transition<'a, 'b>) : PageState<'a, 'b> -> PageState<'a, 'b> =
        fun state ->
            // TODO string list for required actions
            { state with Transitions = ([], transition) :: state.Transitions }



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
