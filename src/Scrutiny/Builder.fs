namespace Scrutiny

open System
open System.IO

type PageBuilder() =

    member __.Yield(_): PageState<'a, 'b> =
        { PageState.Name = ""
          LocalState = Unchecked.defaultof<'b>
          OnEnter = fun _ -> ()
          OnExit = fun _ -> ()
          Transitions = []
          Actions = []
          ExitAction = None }

    [<CustomOperation("name")>]
    member __.Name(state, handler): PageState<'a, 'b> = { state with Name = handler }

    [<CustomOperation("localState")>]
    member __.LocalState(state, handler): PageState<'a, 'b> = { state with LocalState = handler }

    [<CustomOperation("onEnter")>]
    member __.OnEnter(state, handler): PageState<'a, 'b> = { state with OnEnter = handler }

    [<CustomOperation("onExit")>]
    member __.OnExit(state, handler): PageState<'a, 'b> = { state with OnExit = handler }

    [<CustomOperation("transition")>]
    member __.Transitions(state, handler): PageState<'a, 'b> = { state with Transitions = handler :: state.Transitions }

    [<CustomOperation("action")>]
    member __.Actions(state, handler): PageState<'a, 'b> = { state with Actions = handler :: state.Actions }

    [<CustomOperation("exitAction")>]
    member __.ExitAction(state, handler): PageState<'a, 'b> = { state with ExitAction = Some handler }


module Scrutiny =
    let private printPath path =
        printfn "path: %s"
            (path
             |> List.map (fun p -> p.Name)
             |> String.concat " --> ")

    let private runActions (config: ScrutinyConfig) (state: PageState<'a, 'b>) =
        if config.ComprehensiveActions then
            state.Actions |> Seq.iter (fun a -> a state.LocalState)
        else
            let random = Random(config.Seed)

            let amount =
                random.Next(if state.Actions.Length > 1 then state.Actions.Length else 1)
            state.Actions
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.take amount
            |> Seq.iter (fun a -> a state.LocalState)

    let private unvisitedNodes allStates alreadyVisited: AdjacencyGraph<PageState<'a, 'b>> =
        allStates
        |> Seq.map fst
        |> Seq.except alreadyVisited
        |> Seq.map (fun n -> allStates |> List.find (fun ps -> (fst ps) = n))
        |> List.ofSeq

    let private performStateActions (reporter: IReporter<_, _>) config globalState (current, next) =
        try
            current.OnEnter current.LocalState
            let transition =
                current.Transitions
                |> Seq.find (fun t ->
                    let state = t.ToState globalState
                    state.Name = next.Name)
            runActions config current
            current.OnExit current.LocalState

            reporter.PushTransition next

            transition.TransitionFn current.LocalState
        with exn ->

            let message =
                sprintf "System under test failed scrutiny.
    To re-run this exact test, specify the seed in the config with the value: %i.
    The error occurred in state: %s
    The error that occurred is of type: %A%s" config.Seed current.Name exn Environment.NewLine

            reporter.OnError message

            raise <| ScrutinyException(message, exn)

    let private findExit (config: ScrutinyConfig) (allStates: AdjacencyGraph<PageState<'a, 'b>>) =
        let random = Random(config.Seed)

        let exitNode =
            allStates
            |> Seq.filter (fun (node, _) -> Option.isSome node.ExitAction)
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.tryHead

        exitNode

    let private baseScrutinize<'a, 'b> (reporter: IReporter<'a, 'b>) (config: ScrutinyConfig) (globalState: 'a) (startFn: 'a -> PageState<'a, 'b>) =
        printfn "Scrutinizing system under test with seed: %i" config.Seed
        let startState = startFn globalState

        let allStates = Navigator.constructAdjacencyGraph startState globalState
        reporter.Start allStates

        if not config.MapOnly then
            let random = Random(config.Seed)
            let findPath = Navigator.shortestPathFunction allStates

            let nextNode alreadyVisited =
                let unvisitedNodes = unvisitedNodes allStates alreadyVisited

                let shouldContinue =
                    if config.ComprehensiveStates then
                        unvisitedNodes
                        |> Seq.isEmpty
                        |> not
                    else
                        unvisitedNodes.Length > (allStates.Length / 2)

                if shouldContinue then
                    let next = random.Next(unvisitedNodes.Length)
                    Some(fst unvisitedNodes.[next])
                else
                    None

            let rec clickAround
                    (isExitPath: bool)
                    (alreadyVisited: PageState<'a, 'b> list)
                    (currentPath: PageState<'a, 'b> list)
                =
                match currentPath with
                | head :: [] ->
                    match nextNode alreadyVisited with
                    | None -> head
                    | Some nextNode ->
                        let path = findPath head nextNode

                        if path.Length = 1 then
                            runActions config path.Head
                            path.Head
                        else
                            printPath path
                            if isExitPath then path.Head else clickAround false alreadyVisited path
                | head :: tail ->
                    currentPath
                    |> Seq.pairwise
                    |> Seq.find (fun (current, _) -> current = head)
                    |> performStateActions reporter config globalState

                    clickAround false (head :: alreadyVisited) tail

            let navigateDirectly = clickAround true

            let finalNode = clickAround false [] [ startState ]

            match findExit config allStates with
            | None -> ()
            | Some(exitNode, _) ->
                let path = findPath finalNode exitNode

                let exitNode = navigateDirectly [] path
                exitNode.ExitAction |> Option.iter (fun ea -> ea exitNode.LocalState)

        reporter.Finish () |> ignore
        printfn "Scrutiny Result written to: %s" config.ScrutinyResultFilePath

    let scrutinize<'a, 'b> config = baseScrutinize<'a, 'b> (Reporter<'a, 'b>(config.ScrutinyResultFilePath)) config

    let page = PageBuilder()

    let scrutinizeWithDefaultConfig<'a, 'b> = scrutinize<'a, 'b> ScrutinyConfig.Default
