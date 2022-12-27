namespace Scrutiny

open System
open System.Runtime.CompilerServices
open System.Threading.Tasks

[<RequireQualifiedAccess>]
type internal TransitionProperties<'a, 'b> =
    | DependantActions of string list
    | ViaFn of ('b -> Task<unit>)
    | Destination of ('a -> PageState<'a, 'b>)

type internal TransitionBuilder() =
    member inline _.Yield(()) = ()

    member inline _.Delay(f: unit -> TransitionProperties<'a, 'b> list) = f ()
    member inline _.Delay(f: unit -> TransitionProperties<'a, 'b>) = [ f () ]

    member inline _.Combine(newProp: TransitionProperties<'a, 'b>, previousProp: TransitionProperties<'a, 'b> list) =
        newProp :: previousProp

    member inline x.Run(props: TransitionProperties<'a, 'b> list) =
        props
        |> List.fold
            (fun tp prop ->
                match prop with
                | TransitionProperties.DependantActions actions -> { tp with DependantActions = actions }
                | TransitionProperties.ViaFn viaFnTAsync -> { tp with ViaFn = viaFnTAsync }
                | TransitionProperties.Destination destinationState -> { tp with Destination = destinationState })
            { Transition.DependantActions = []
              ViaFn = fun _ -> Task.FromResult()
              Destination = fun _ -> Unchecked.defaultof<PageState<'a, 'b>> }

    member inline x.Run(prop: TransitionProperties<'a, 'b>) = x.Run([ prop ])

    member inline x.For(prop: TransitionProperties<'a, 'b>, f: unit -> TransitionProperties<'a, 'b> list) =
        x.Combine(prop, f ())

    member inline x.For(prop: TransitionProperties<'a, 'b>, f: unit -> TransitionProperties<'a, 'b>) = [ prop; f () ]

    [<CustomOperation("dependantActions")>]
    member inline _.DependantActions(_, actions) =
        TransitionProperties.DependantActions actions

    [<CustomOperation("via")>]
    member inline _.Via(_, viaFn) =
        let viaFn = fun localState -> Task.FromResult(viaFn localState)
        TransitionProperties.ViaFn viaFn

    [<CustomOperation("via")>]
    member inline _.Via(_, viaFnTAsync) = TransitionProperties.ViaFn viaFnTAsync

    [<CustomOperation("destination")>]
    member inline _.Destination(_, destinationState) =
        TransitionProperties.Destination destinationState

[<RequireQualifiedAccess>]
type internal ActionProperties<'b> =
    | Name of string
    | DependantActions of string list
    | Action of CallerInformation * ('b -> Task<unit>)

type internal ActionBuilder() =
    member inline _.Yield(()) = ()
    member inline _.Delay(f: unit -> ActionProperties<'b> list) = f ()
    member inline _.Delay(f: unit -> ActionProperties<'b>) = [ f () ]

    member inline _.Combine(newProp: ActionProperties<'b>, previousProp: ActionProperties<'b> list) =
        newProp :: previousProp

    member inline x.Run(props: ActionProperties<'b> list) =
        props
        |> List.fold
            (fun ap prop ->
                match prop with
                | ActionProperties.Name name -> { ap with Action.Name = name }
                | ActionProperties.DependantActions da -> { ap with DependantActions = da }
                | ActionProperties.Action (ci, action) ->
                    { ap with
                        CallerInformation = ci
                        ActionFn = action })
            { Action.CallerInformation =
                { CallerInformation.MemberName = String.Empty
                  LineNumber = 0
                  FilePath = String.Empty }
              Name = String.Empty
              DependantActions = []
              ActionFn = fun _ -> Task.FromResult() }

    member inline x.Run(prop: ActionProperties<'b>) = x.Run([ prop ])

    member inline x.For(prop: ActionProperties<'b>, f: unit -> ActionProperties<'b> list) = x.Combine(prop, f ())

    member inline x.For(prop: ActionProperties<'b>, f: unit -> ActionProperties<'b>) = [ prop; f () ]

    [<CustomOperation("name")>]
    member inline _.Name(_, name) = ActionProperties.Name name

    [<CustomOperation("dependantActions")>]
    member inline _.DependantActions(_, actions) =
        ActionProperties.DependantActions actions

    [<CustomOperation("action")>]
    member inline _.Action
        (
            _,
            action: 'b -> unit,
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName String.Empty
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath String.Empty }

        let action = fun localState -> Task.FromResult(action localState)

        ActionProperties.Action(callerInformation, action)

    [<CustomOperation("action")>]
    member inline _.Action
        (
            _,
            action: 'b -> Task<unit>,
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName String.Empty
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath String.Empty }

        ActionProperties.Action(callerInformation, action)

// https://github.com/sleepyfran/sharp-point
// https://sleepyfran.github.io/blog/posts/fsharp/ce-in-fsharp/
[<RequireQualifiedAccess>]
type internal PageStateProperties<'a, 'b> =
    | Name of string
    | Transition of Transition<'a, 'b>

type internal Page2Builder() =
    member inline _.Yield(()) = ()

    member inline _.Yield(transition: Transition<'a, 'b>) =
        PageStateProperties.Transition transition

    member inline _.Delay(f: unit -> PageStateProperties<'a, 'b> list) = f ()
    member inline _.Delay(f: unit -> PageStateProperties<'a, 'b>) = [ f () ]

    member inline _.Combine(newProp: PageStateProperties<'a, 'b>, previousProp: PageStateProperties<'a, 'b> list) =
        newProp :: previousProp

    member inline x.Run(props: PageStateProperties<'a, 'b> list) =
        props
        |> List.fold
            (fun ps prop ->
                match prop with
                | PageStateProperties.Name name -> { ps with Name = name }
                | PageStateProperties.Transition transition -> { ps with Transitions = transition :: ps.Transitions })
            { PageState.Name = String.Empty
              LocalState = Unchecked.defaultof<'b>
              OnEnter = fun _ -> Task.FromResult()
              OnExit = fun _ -> Task.FromResult()
              Transitions = []
              Actions = []
              ExitActions = [] }

    member inline x.Run(prop: PageStateProperties<'a, 'b>) = x.Run([ prop ])

    [<CustomOperation("name")>]
    member inline _.Name((), name: string) = PageStateProperties.Name name

    member inline x.For(prop: PageStateProperties<'a, 'b>, f: unit -> PageStateProperties<'a, 'b> list) =
        x.Combine(prop, f ())

    member inline x.For(prop: PageStateProperties<'a, 'b>, f: unit -> PageStateProperties<'a, 'b>) = [ prop; f () ]



(*
    //-----------------
    // Transitions
    //-----------------

    [<CustomOperation("transition")>]
    member _.Transitions(state, handler) : PageState<'a, 'b> =
        { state with Transitions = handler :: state.Transitions } // TODO string list for required actions


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
        *)
