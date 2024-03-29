namespace Scrutiny

open System
open System.Threading.Tasks
open Scrutiny
open Scrutiny.Utilities

[<AutoOpen>]
module Scrutiny =
    let private handleError exn errorLocation (reporter: IReporter<_>) (config: ScrutinyConfig) current =
        let message =
            $"System under test failed scrutiny.
            The error occurred in state: '%s{current.Name}'
        The error that occurred is of type: '%A{exn}%s{Environment.NewLine}'
        You can view the current report at '%s{config.ScrutinyResultFilePath}'
        To re-run this exact test, specify the seed in the config with the value: '%i{config.Seed}'.
        "

        let exn = ScrutinyException(message, exn)

        reporter.OnError errorLocation

        raise <| exn

    let private buildActionName (ci: CallerInformation) (actionName: string) =
        match ci.LineNumber > 0 with
        | true -> $"Action: %s{actionName}, Member: %s{ci.MemberName}, Line #: %i{ci.LineNumber}, File: %s{ci.FilePath}"
        | false -> $"Action: %s{actionName}, %s{ci.FilePath}.%s{ci.MemberName}"

    let private simpleTraverse (tasks: (unit -> Task<unit>) list) : unit -> Task<unit> =
        match tasks with
        | [] -> fun () -> Task.FromResult(())
        | x ->
            x
            |> List.reduce (fun accumulator element ->
                fun () ->
                    task {
                        do! accumulator ()
                        return! element ()
                    })

    let rec private runTheActions
        (reporter: IReporter<'a>)
        (config: ScrutinyConfig)
        (state: PageState<'a>)
        (actions: StateAction list)
        =
        actions
        |> List.filter (fun a -> not a.IsExit)
        |> List.map (fun a ->
            fun () ->
                task {
                    let runDependantActions =
                        a.DependantActions
                        |> List.map (fun da ->
                            let action =
                                state.Actions
                                |> List.tryFind (fun sa -> sa.Name = da)

                            match action with
                            | Some a -> runTheActions reporter config state [ a ]
                            | None ->
                                raise
                                <| ScrutinyException(
                                    $"Couldn't find action with name '{da}' among the actions of state '{state.Name}'",
                                    null
                                ))
                        |> simpleTraverse

                    do! runDependantActions ()

                    reporter.PushAction(buildActionName a.CallerInformation a.Name)
                    return! (a.ActionFn())
                })
        |> simpleTraverse

    let private runActions (reporter: IReporter<'a>) (config: ScrutinyConfig) (state: PageState<'a>) =
        if config.ComprehensiveActions then
            state.Actions
            |> runTheActions reporter config state
            |> fun fn -> fn ()
        else
            let random = Random(config.Seed)

            let amount =
                random.Next(
                    if state.Actions.Length > 1 then
                        state.Actions.Length
                    else
                        1
                )

            state.Actions
            |> List.sortBy (fun _ -> random.Next())
            |> List.take amount
            |> runTheActions reporter config state
            |> fun fn -> fn ()

    let private convertException (e: exn) : SerializableException =
        let rec convertInner (currentException: exn) =
            let inner =
                if currentException.InnerException = null then
                    None
                else
                    Some
                    <| convertInner currentException.InnerException

            { SerializableException.Type = currentException.GetType().ToString()
              Message = currentException.Message
              StackTrace = currentException.StackTrace
              InnerException = inner }

        convertInner e

    let private performStateActions (reporter: IReporter<_>) config current =
        task {
            try
                do! current.OnEnter()

                if current.Actions |> List.isEmpty |> not then
                    do! runActions reporter config current

                do! current.OnExit()
            with exn ->
                handleError exn (State(current.Name, convertException exn)) reporter config current
        }

    let private transitionToNextState (reporter: IReporter<_>) config globalState (current, next) =
        task {
            try
                let transition =
                    current.Transitions
                    |> Seq.find (fun t ->
                        let state = t.Destination globalState
                        state.Name = next.Name)

                reporter.PushTransition next

                let dependantActions =
                    transition.DependantActions
                    |> List.map (fun da ->
                        let action =
                            current.Actions
                            |> List.tryFind (fun sa -> sa.Name = da)

                        match action with
                        | Some a -> a
                        | None ->
                            raise
                            <| ScrutinyException(
                                $"Couldn't find action with name '{da}' among the actions of state '{current.Name}'",
                                null
                            ))
                    |> runTheActions reporter config current

                do! dependantActions ()

                do! transition.ViaFn()
            with exn ->
                handleError exn (Transition(current.Name, next.Name, convertException exn)) reporter config current
        }

    let private findExit (config: ScrutinyConfig) (allStates: AdjacencyGraph<PageState<'a>>) =
        let random = Random(config.Seed)

        let exitActions (node: PageState<'a>) =
            node.Actions |> List.filter (fun a -> a.IsExit)

        let exitNode =
            allStates
            |> Seq.filter (fun (node, _) -> (exitActions node) |> (not << Seq.isEmpty))
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.tryHead

        exitNode

    let private navigateStateMachine reporter config allStates globalState startState =
        let random = Random(config.Seed)

        let findPath = Navigator.shortestPathFunction allStates

        let rec travelDirectly (currentPath: PageState<'a> list) =
            task {
                if currentPath.Length = 1 then
                    do! performStateActions reporter config currentPath.Head
                    return currentPath.Head
                else
                    let head = currentPath.Head
                    let tail = currentPath.Tail

                    let (current, next) =
                        currentPath
                        |> Seq.pairwise
                        |> Seq.find (fun (current, _) -> current = head)

                    do! performStateActions reporter config current

                    do!
                        (current, next)
                        |> transitionToNextState reporter config globalState

                    return! travelDirectly tail
            }

        let rec clickAround (visitMap: Map<PageState<'a>, int>) startState destinationState =
            task {
                let path = findPath startState destinationState

                let! endingState = travelDirectly path

                let updatedVisitMap =
                    path
                    |> List.fold
                        (fun visitMap pageState ->
                            visitMap
                            |> Map.change pageState (Option.map (fun v -> v + 1)))
                        visitMap

                if updatedVisitMap |> Map.forall (fun k v -> v >= 1) then
                    return endingState
                else
                    let mapForDestination = updatedVisitMap |> Map.remove endingState
                    return! clickAround updatedVisitMap endingState (mapForDestination |> Map.weightedRandomItem random)
            }

        task {
            let pageStateVisitMap =
                allStates
                |> List.map (fun (g, _) -> g, 0)
                |> Map.ofList

            let! finalNode = clickAround pageStateVisitMap startState (pageStateVisitMap |> Map.randomItem random)

            match findExit config allStates with
            | None -> return ()
            | Some (exitNode, _) ->
                let path = findPath finalNode exitNode
                let! exitNode = travelDirectly path

                let exitFn =
                    exitNode.Actions
                    |> Seq.filter (fun a -> a.IsExit)
                    |> Seq.sortBy (fun _ -> random.Next())
                    |> Seq.tryHead
                    |> Option.map (fun ea ->
                        task {
                            try
                                let runDependantActions =
                                    ea.DependantActions
                                    |> List.map (fun da ->
                                        let action =
                                            exitNode.Actions
                                            |> List.tryFind (fun sa -> sa.Name = da)

                                        match action with
                                        | Some a -> runTheActions reporter config exitNode [ a ]
                                        | None ->
                                            raise
                                            <| ScrutinyException(
                                                $"Couldn't find action with name '{da}' among the actions of state '{exitNode.Name}'",
                                                null
                                            ))
                                    |> simpleTraverse

                                do! runDependantActions ()

                                do! ea.ActionFn()
                            with exn ->
                                handleError exn (State(exitNode.Name, convertException exn)) reporter config exitNode
                        })

                match exitFn with
                | None -> return ()
                | Some ef -> return! ef
        }


    let private baseScrutinize<'a>
        (reporter: IReporter<'a>)
        (config: ScrutinyConfig)
        (globalState: 'a)
        (startFn: 'a -> PageState<'a>)
        =
        task {
            config.Logger
            <| $"Scrutinizing system under test with seed: %i{config.Seed}"

            let startState = startFn globalState

            let allStates = Navigator.constructAdjacencyGraph startState globalState

            reporter.Start(allStates, startState)

            try
                if not config.MapOnly then
                    do! navigateStateMachine reporter config allStates globalState startState
            finally
                config.Logger $"Scrutiny Result written to: {config.ScrutinyResultFilePath}"
                reporter.GenerateMap()

            return reporter.Finish()
        }

    let scrutinize<'a, 'b> config =
        let reporter = Reporter<'a>(config.ScrutinyResultFilePath) :> IReporter<'a>

        baseScrutinize<'a> reporter config

    let page = PageBuilder()
    let transition = TransitionBuilder()
    let action = ActionBuilder()

    /// Alias for page, just in case you want a different term for your page states
    let state = page

    let scrutinizeWithDefaultConfig<'a, 'b> = scrutinize<'a, 'b> ScrutinyConfig.Default
