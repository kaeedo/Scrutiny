namespace Scrutiny

open System
open System.Threading.Tasks
open Scrutiny
open Scrutiny.Utilities

[<AutoOpen>]
module Scrutiny =
    let private handleError exn errorLocation (reporter: IReporter<_, _>) config current =
        let message =
            $"System under test failed scrutiny.
        To re-run this exact test, specify the seed in the config with the value: '%i{config.Seed}'.
        The error occurred in state: '%s{current.Name}'
        The error that occurred is of type: '%A{exn}%s{Environment.NewLine}'"

        let exn = ScrutinyException(message, exn)

        reporter.OnError errorLocation

        raise <| exn

    let private printPath logger path =
        logger
        <| sprintf
            "path: %s"
            (path
             |> List.map (fun p -> p.Name)
             |> String.concat " --> ")

    let private buildActionName (ci: CallerInformation) =
        if ci.LineNumber > 0 then
            $"Member: {ci.MemberName}, Line #: {ci.LineNumber}, File: {ci.FilePath}"
        else
            $"{ci.FilePath}.{ci.MemberName}"

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

    let private runActions (reporter: IReporter<'a, 'b>) (config: ScrutinyConfig) (state: PageState<'a, 'b>) =
        if config.ComprehensiveActions then
            state.Actions
            |> List.map (fun (ci, a) ->
                fun () ->
                    task {
                        reporter.PushAction(buildActionName ci)
                        return! (a state.LocalState)
                    })
            |> simpleTraverse
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
            |> List.map (fun (ci, a) ->
                fun () ->
                    task {
                        reporter.PushAction(buildActionName ci)
                        return! (a state.LocalState)
                    })
            |> simpleTraverse
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

    let private performStateActions (reporter: IReporter<_, _>) config current =
        task {
            try
                do! current.OnEnter current.LocalState
                do! runActions reporter config current
                do! current.OnExit current.LocalState
            with exn ->
                handleError exn (State(current.Name, convertException exn)) reporter config current
        }

    let private transitionToNextState (reporter: IReporter<_, _>) config globalState (current, next) =
        task {
            try
                let transition =
                    current.Transitions
                    |> Seq.find (fun t ->
                        let state = t.ToState globalState
                        state.Name = next.Name)

                reporter.PushTransition next

                do! transition.TransitionFn current.LocalState
            with exn ->
                handleError exn (Transition(current.Name, next.Name, convertException exn)) reporter config current
        }

    let private findExit (config: ScrutinyConfig) (allStates: AdjacencyGraph<PageState<'a, 'b>>) =
        let random = Random(config.Seed)

        let exitNode =
            allStates
            |> Seq.filter (fun (node, _) -> node.ExitActions |> Seq.isEmpty |> not)
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.tryHead

        exitNode

    let private navigateStateMachine reporter config allStates globalState startState =
        let random = Random(config.Seed)
        let findPath = Navigator.shortestPathFunction allStates

        let nextNode (visitMap: Map<PageState<'a, 'b>, int>) =
            let possiblePageState = visitMap |> Map.filter (fun _ v -> v >= 1)

            let shouldContinue =
                if config.ComprehensiveStates then
                    not possiblePageState.IsEmpty
                else
                    possiblePageState.Count > (allStates.Length / 2)

            if shouldContinue then
                Some(Map.randomItem random possiblePageState)
            else
                None

        let decrementNumberOfVisits (visitMap: Map<PageState<'a, 'b>, int>) (pageState: PageState<'a, 'b>) =
            visitMap.Change(pageState, Option.map (fun c -> c - 1))

        let rec clickAround (visitMap: Map<PageState<'a, 'b>, int>) (currentPath: PageState<'a, 'b> list) =
            task {
                let visitMap = decrementNumberOfVisits visitMap currentPath.Head

                if currentPath.Length = 1 then
                    match nextNode visitMap with
                    | None -> return currentPath.Head
                    | Some nextNode ->
                        let path = findPath currentPath.Head nextNode

                        if path.Length = 1 then
                            do! performStateActions reporter config path.Head
                            return path.Head
                        else
                            printPath config.Logger path

                            return! clickAround visitMap path

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

                    return! clickAround visitMap tail
            }

        let rec travelDirectly (currentPath: PageState<'a, 'b> list) =
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

        task {
            let pageStateVisitMap =
                allStates
                |> List.map (fun (g, _) -> g, 1)
                |> Map.ofList

            let! finalNode = clickAround pageStateVisitMap [ startState ]

            match findExit config allStates with
            | None -> return ()
            | Some (exitNode, _) ->
                let t =
                    task {
                        let path = findPath finalNode exitNode

                        let! exitNode = travelDirectly path

                        let a =
                            exitNode.ExitActions
                            |> Seq.sortBy (fun _ -> random.Next())
                            |> Seq.tryHead
                            |> Option.map (fun ea -> (ea exitNode.LocalState))

                        match a with
                        | None -> return ()
                        | Some asd -> return! asd
                    }

                return! t
        }


    let private baseScrutinize<'a, 'b>
        (reporter: IReporter<'a, 'b>)
        (config: ScrutinyConfig)
        (globalState: 'a)
        (startFn: 'a -> PageState<'a, 'b>)
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
        let reporter = Reporter<'a, 'b>(config.ScrutinyResultFilePath) :> IReporter<'a, 'b>
        baseScrutinize<'a, 'b> reporter config

    let page = PageBuilder()

    /// Alias for page, just in case you want a different term for your page states
    let state = page

    let scrutinizeWithDefaultConfig<'a, 'b> = scrutinize<'a, 'b> ScrutinyConfig.Default