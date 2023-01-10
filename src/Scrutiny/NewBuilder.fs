namespace Scrutiny

open System
open System.Runtime.CompilerServices
open System.Threading.Tasks

[<RequireQualifiedAccess>]
type TransitionProperties<'a, 'b> =
    | DependantActions of string list
    | ViaFn of ('b -> Task<unit>)
    | Destination of ('a -> PageState<'a, 'b>)

type TransitionBuilder() =
    member _.Yield(()) = ()

    member _.Delay(f: unit -> TransitionProperties<'a, 'b> list) = f ()
    member _.Delay(f: unit -> TransitionProperties<'a, 'b>) = [ f () ]

    member _.Combine(newProp: TransitionProperties<'a, 'b>, previousProp: TransitionProperties<'a, 'b> list) =
        newProp :: previousProp

    member x.Run(props: TransitionProperties<'a, 'b> list) =
        props
        |> List.fold
            (fun tp prop ->
                match prop with
                | TransitionProperties.DependantActions actions -> { tp with Transition.DependantActions = actions }
                | TransitionProperties.ViaFn viaFnTAsync -> { tp with ViaFn = viaFnTAsync }
                | TransitionProperties.Destination destinationState -> { tp with Destination = destinationState })
            { Transition.DependantActions = []
              ViaFn = fun _ -> Task.FromResult()
              Destination = fun _ -> Unchecked.defaultof<PageState<'a, 'b>> }

    member x.Run(prop: TransitionProperties<'a, 'b>) = x.Run([ prop ])

    member x.For(prop: TransitionProperties<'a, 'b>, f: unit -> TransitionProperties<'a, 'b> list) =
        x.Combine(prop, f ())

    member x.For(prop: TransitionProperties<'a, 'b>, f: unit -> TransitionProperties<'a, 'b>) = [ prop; f () ]

    [<CustomOperation("dependantActions")>]
    member _.DependantActions(_, actions) =
        TransitionProperties.DependantActions actions

    [<CustomOperation("via")>]
    member _.Via(_, viaFn) =
        let viaFn = fun localState -> Task.FromResult(viaFn localState)
        TransitionProperties.ViaFn viaFn

    [<CustomOperation("via")>]
    member _.Via(_, viaFnTAsync) = TransitionProperties.ViaFn viaFnTAsync

    [<CustomOperation("destination")>]
    member _.Destination(_, destinationState) =
        TransitionProperties.Destination destinationState

[<RequireQualifiedAccess>]
type ActionProperties<'b> =
    | Name of string
    | DependantActions of string list
    | Fn of CallerInformation * ('b -> Task<unit>)
    | IsExit

type ActionBuilder() =
    member _.Yield(()) = ()
    member _.Delay(f: unit -> ActionProperties<'b> list) = f ()
    member _.Delay(f: unit -> ActionProperties<'b>) = [ f () ]

    member _.Combine(newProp: ActionProperties<'b>, previousProp: ActionProperties<'b> list) = newProp :: previousProp

    member x.Run(props: ActionProperties<'b> list) =
        props
        |> List.fold
            (fun ap prop ->
                match prop with
                | ActionProperties.Name name -> { ap with StateAction.Name = name }
                | ActionProperties.DependantActions da -> { ap with DependantActions = da }
                | ActionProperties.IsExit -> { ap with IsExit = true }
                | ActionProperties.Fn (ci, action) ->
                    { ap with
                        CallerInformation = ci
                        ActionFn = action })
            { StateAction.CallerInformation =
                { CallerInformation.MemberName = String.Empty
                  LineNumber = 0
                  FilePath = String.Empty }
              Name = String.Empty
              DependantActions = []
              IsExit = false
              ActionFn = fun _ -> Task.FromResult() }

    member x.Run(prop: ActionProperties<'b>) = x.Run([ prop ])

    member x.For(prop: ActionProperties<'b>, f: unit -> ActionProperties<'b> list) = x.Combine(prop, f ())

    member x.For(prop: ActionProperties<'b>, f: unit -> ActionProperties<'b>) = [ prop; f () ]

    [<CustomOperation("name")>]
    member _.Name(_, name) = ActionProperties.Name name

    [<CustomOperation("dependantActions")>]
    member _.DependantActions(_, actions) =
        ActionProperties.DependantActions actions

    [<CustomOperation("isExit")>]
    member _.IsExit(_) = ActionProperties.IsExit

    [<CustomOperation("fn")>]
    member _.Fn
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

        ActionProperties.Fn(callerInformation, action)

    [<CustomOperation("action")>]
    member _.Action
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

        ActionProperties.Fn(callerInformation, action)

// https://github.com/sleepyfran/sharp-point
// https://sleepyfran.github.io/blog/posts/fsharp/ce-in-fsharp/
[<RequireQualifiedAccess>]
type PageStateProperties<'a, 'b> =
    | Name of string
    | Transition of Transition<'a, 'b>
    | Action of StateAction<'b>
    | LocalState of 'b
    | OnEnter of ('b -> Task<unit>)
    | OnExit of ('b -> Task<unit>)

type Page2Builder() =
    member _.Yield(()) = ()

    member _.Yield(transition: Transition<'a, 'b>) =
        PageStateProperties.Transition transition

    member _.Yield(action: StateAction<'b>) = PageStateProperties.Action action

    member _.Delay(f: unit -> PageStateProperties<'a, 'b> list) = f ()
    member _.Delay(f: unit -> PageStateProperties<'a, 'b>) = [ f () ]

    member _.Combine(newProp: PageStateProperties<'a, 'b>, previousProp: PageStateProperties<'a, 'b> list) =
        newProp :: previousProp

    member x.Run(props: PageStateProperties<'a, 'b> list) =
        props
        |> List.fold
            (fun ps prop ->
                match prop with
                | PageStateProperties.Name name -> { ps with PageState.Name = name }
                | PageStateProperties.Transition transition -> { ps with Transitions = transition :: ps.Transitions }
                | PageStateProperties.Action action -> { ps with Actions = action :: ps.Actions }
                | PageStateProperties.LocalState ls -> { ps with LocalState = ls }
                | PageStateProperties.OnEnter onEnterFn -> { ps with OnEnter = onEnterFn }
                | PageStateProperties.OnExit onExitFn -> { ps with OnExit = onExitFn })
            { PageState.Name = String.Empty
              LocalState = Unchecked.defaultof<'b>
              OnEnter = fun _ -> Task.FromResult()
              OnExit = fun _ -> Task.FromResult()
              Transitions = []
              Actions = [] }

    member x.Run(prop: PageStateProperties<'a, 'b>) = x.Run([ prop ])

    member x.For(prop: PageStateProperties<'a, 'b>, f: unit -> PageStateProperties<'a, 'b> list) = x.Combine(prop, f ())

    member x.For(prop: PageStateProperties<'a, 'b>, f: unit -> PageStateProperties<'a, 'b>) = [ prop; f () ]

    [<CustomOperation("name")>]
    member _.Name(_, name: string) = PageStateProperties.Name name

    [<CustomOperation("onEnter")>]
    member _.OnEnter(_, onEnterFn: 'b -> unit) =
        PageStateProperties.OnEnter(fun localState -> Task.FromResult(onEnterFn localState))

    [<CustomOperation("onEnter")>]
    member _.OnEnter(_, onEnterFn: 'b -> Task<unit>) = PageStateProperties.OnEnter onEnterFn

    [<CustomOperation("onExit")>]
    member _.OnExit(_, onExitFn: 'b -> unit) =
        PageStateProperties.OnExit(fun localState -> Task.FromResult(onExitFn localState))

    [<CustomOperation("onExit")>]
    member _.OnExit(_, onExitFn: 'b -> Task<unit>) = PageStateProperties.OnExit onExitFn

    [<CustomOperation("localState")>]
    member _.LocalState(_, localState: 'b) =
        PageStateProperties.LocalState localState
