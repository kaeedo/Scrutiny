namespace Scrutiny

open System
open System.Runtime.CompilerServices
open System.Threading.Tasks

type TransitionBuilder() =
    member _.Yield(()) =
        { Transition.DependantActions = []
          ViaFn = fun _ -> Task.FromResult()
          Destination = fun _ -> Unchecked.defaultof<PageState<'a>> }

    [<CustomOperation("dependantActions")>]
    member _.DependantActions(previous, actions) =
        { previous with Transition.DependantActions = actions }

    [<CustomOperation("via")>]
    member _.Via(previous, viaFn) =
        let viaFn = fun () -> Task.FromResult(viaFn ())
        { previous with ViaFn = viaFn }

    [<CustomOperation("via")>]
    member _.Via(previous, viaFnTAsync) = { previous with ViaFn = viaFnTAsync }

    [<CustomOperation("destination")>]
    member _.Destination(previous, destinationState) =
        { previous with Destination = destinationState }

type ActionBuilder() =
    member _.Yield(()) =
        { StateAction.CallerInformation =
            { CallerInformation.MemberName = String.Empty
              LineNumber = 0
              FilePath = String.Empty }
          Name = String.Empty
          DependantActions = []
          IsExit = false
          ActionFn = fun _ -> Task.FromResult() }

    [<CustomOperation("name")>]
    member _.Name(previous, name) =
        { previous with StateAction.Name = name }

    [<CustomOperation("dependantActions")>]
    member _.DependantActions(previous, actions) =
        { previous with StateAction.DependantActions = actions }

    [<CustomOperation("isExit")>]
    member _.IsExit(previous) = { previous with IsExit = true }

    [<CustomOperation("fn")>]
    member _.Fn
        (
            previous,
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

        { previous with
            ActionFn = action
            CallerInformation = callerInformation }

    [<CustomOperation("fn")>]
    member _.Fn
        (
            previous,
            action: unit -> Task<unit>,
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName String.Empty
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath String.Empty }

        { previous with
            ActionFn = action
            CallerInformation = callerInformation }


// https://github.com/sleepyfran/sharp-point
// https://sleepyfran.github.io/blog/posts/fsharp/ce-in-fsharp/

[<RequireQualifiedAccess>]
type Extra =
    | Name of string
    | OnEnter of (unit -> Task<unit>)
    | OnExit of (unit -> Task<unit>)

[<RequireQualifiedAccess>]
type PageStateProperties<'a> =
    | Extras of Extra list
    | Transition of Transition<'a>
    | Action of StateAction

type PageBuilder() =
    member _.Yield(()) = PageStateProperties.Extras []

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
                | PageStateProperties.Extras extras ->
                    extras
                    |> List.fold
                        (fun state current ->
                            match current with
                            | Extra.Name n -> { state with PageState.Name = n }
                            | Extra.OnEnter oe -> { state with PageState.OnEnter = oe }
                            | Extra.OnExit oe -> { state with PageState.OnExit = oe })
                        ps
                | PageStateProperties.Transition transition -> { ps with Transitions = transition :: ps.Transitions }
                | PageStateProperties.Action action -> { ps with Actions = action :: ps.Actions })
            { PageState.Name = String.Empty
              OnEnter = fun _ -> Task.FromResult()
              OnExit = fun _ -> Task.FromResult()
              Transitions = []
              Actions = [] }

    member x.Run(prop: PageStateProperties<'a>) = x.Run([ prop ])

    member x.For(prop: PageStateProperties<'a>, f: unit -> PageStateProperties<'a> list) = x.Combine(prop, f ())

    member x.For(prop: PageStateProperties<'a>, f: unit -> PageStateProperties<'a>) = [ prop; f () ]

    [<CustomOperation("name")>]
    member _.Name(previous, name: string) =
        match previous with
        | PageStateProperties.Extras e -> Extra.Name name :: e |> PageStateProperties.Extras
        | psp -> psp

    [<CustomOperation("onEnter")>]
    member _.OnEnter(previous, onEnterFn: unit -> unit) =
        match previous with
        | PageStateProperties.Extras e ->
            Extra.OnEnter(fun () -> Task.FromResult(onEnterFn ()))
            :: e
            |> PageStateProperties.Extras
        | psp -> psp

    [<CustomOperation("onEnter")>]
    member _.OnEnter(previous, onEnterFn: unit -> Task<unit>) =
        match previous with
        | PageStateProperties.Extras e ->
            Extra.OnEnter onEnterFn :: e
            |> PageStateProperties.Extras
        | psp -> psp

    [<CustomOperation("onExit")>]
    member _.OnExit(previous, onExitFn: unit -> unit) =
        match previous with
        | PageStateProperties.Extras e ->
            Extra.OnExit(fun () -> Task.FromResult(onExitFn ()))
            :: e
            |> PageStateProperties.Extras
        | psp -> psp

    [<CustomOperation("onExit")>]
    member _.OnExit(previous, onExitFn: unit -> Task<unit>) =
        match previous with
        | PageStateProperties.Extras e ->
            Extra.OnExit onExitFn :: e
            |> PageStateProperties.Extras
        | psp -> psp
