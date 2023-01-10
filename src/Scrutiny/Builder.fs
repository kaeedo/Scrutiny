namespace Scrutiny

open System
open System.Runtime.CompilerServices
open System.Threading.Tasks

[<RequireQualifiedAccess>]
type TransitionProperties<'a> =
    | DependantActions of string list
    | ViaFn of (unit -> Task<unit>)
    | Destination of ('a -> PageState<'a>)

type TransitionBuilder() =
    member _.Yield(()) = ()

    member _.Delay(f: unit -> TransitionProperties<'a> list) = f ()
    member _.Delay(f: unit -> TransitionProperties<'a>) = [ f () ]

    member _.Combine(newProp: TransitionProperties<'a>, previousProp: TransitionProperties<'a> list) =
        newProp :: previousProp

    member x.Run(props: TransitionProperties<'a> list) =
        props
        |> List.fold
            (fun tp prop ->
                match prop with
                | TransitionProperties.DependantActions actions -> { tp with Transition.DependantActions = actions }
                | TransitionProperties.ViaFn viaFnTAsync -> { tp with ViaFn = viaFnTAsync }
                | TransitionProperties.Destination destinationState -> { tp with Destination = destinationState })
            { Transition.DependantActions = []
              ViaFn = fun _ -> Task.FromResult()
              Destination = fun _ -> Unchecked.defaultof<PageState<'a>> }

    member x.Run(prop: TransitionProperties<'a>) = x.Run([ prop ])

    member x.For(prop: TransitionProperties<'a>, f: unit -> TransitionProperties<'a> list) = x.Combine(prop, f ())

    member x.For(prop: TransitionProperties<'a>, f: unit -> TransitionProperties<'a>) = [ prop; f () ]

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
type ActionProperties =
    | Name of string
    | DependantActions of string list
    | Fn of CallerInformation * (unit -> Task<unit>)
    | IsExit

type ActionBuilder() =
    member _.Yield(()) = ()
    member _.Delay(f: unit -> ActionProperties list) = f ()
    member _.Delay(f: unit -> ActionProperties) = [ f () ]

    member _.Combine(newProp: ActionProperties, previousProp: ActionProperties list) = newProp :: previousProp

    member x.Run(props: ActionProperties list) =
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

    member x.Run(prop: ActionProperties) = x.Run([ prop ])

    member x.For(prop: ActionProperties, f: unit -> ActionProperties list) = x.Combine(prop, f ())

    member x.For(prop: ActionProperties, f: unit -> ActionProperties) = [ prop; f () ]

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
            action: unit -> unit,
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName String.Empty
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath String.Empty }

        let action = fun _ -> Task.FromResult(action ())

        ActionProperties.Fn(callerInformation, action)

    [<CustomOperation("fn")>]
    member _.Fn
        (
            _,
            action: unit -> Task<unit>,
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
type PageStateProperties<'a> =
    | Name of string
    | Transition of Transition<'a>
    | Action of StateAction
    | OnEnter of (unit -> Task<unit>)
    | OnExit of (unit -> Task<unit>)

type Page2Builder() =
    member _.Yield(()) = ()

    member _.Yield(transition: Transition<'a>) =
        PageStateProperties.Transition transition

    member _.Yield(action: StateAction) = PageStateProperties.Action action

    member _.Delay(f: unit -> PageStateProperties<'a> list) = f ()
    member _.Delay(f: unit -> PageStateProperties<'a>) = [ f () ]

    member _.Combine(newProp: PageStateProperties<'a>, previousProp: PageStateProperties<'a> list) =
        newProp :: previousProp

    member x.Run(props: PageStateProperties<'a> list) =
        props
        |> List.fold
            (fun ps prop ->
                match prop with
                | PageStateProperties.Name name -> { ps with PageState.Name = name }
                | PageStateProperties.Transition transition -> { ps with Transitions = transition :: ps.Transitions }
                | PageStateProperties.Action action -> { ps with Actions = action :: ps.Actions }
                | PageStateProperties.OnEnter onEnterFn -> { ps with OnEnter = onEnterFn }
                | PageStateProperties.OnExit onExitFn -> { ps with OnExit = onExitFn })
            { PageState.Name = String.Empty
              OnEnter = fun _ -> Task.FromResult()
              OnExit = fun _ -> Task.FromResult()
              Transitions = []
              Actions = [] }

    member x.Run(prop: PageStateProperties<'a>) = x.Run([ prop ])

    member x.For(prop: PageStateProperties<'a>, f: unit -> PageStateProperties<'a> list) = x.Combine(prop, f ())

    member x.For(prop: PageStateProperties<'a>, f: unit -> PageStateProperties<'a>) = [ prop; f () ]

    [<CustomOperation("name")>]
    member _.Name(_, name: string) = PageStateProperties.Name name

    [<CustomOperation("onEnter")>]
    member _.OnEnter(_, onEnterFn: unit -> unit) =
        PageStateProperties.OnEnter(fun localState -> Task.FromResult(onEnterFn localState))

    [<CustomOperation("onEnter")>]
    member _.OnEnter(_, onEnterFn: unit -> Task<unit>) = PageStateProperties.OnEnter onEnterFn

    [<CustomOperation("onExit")>]
    member _.OnExit(_, onExitFn: unit -> unit) =
        PageStateProperties.OnExit(fun localState -> Task.FromResult(onExitFn localState))

    [<CustomOperation("onExit")>]
    member _.OnExit(_, onExitFn: unit -> Task<unit>) = PageStateProperties.OnExit onExitFn
