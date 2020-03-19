namespace Scrutiny

open System

type PageBuilder() =
    member __.Yield(_): PageState<'a> =
        { PageState.Id = Guid.NewGuid()
          Name = ""
          EntryCheck = fun _ -> ()
          Transitions = []
          Actions = []
          Exit = fun _ -> () }

    [<CustomOperation("name")>]
    member __.Name(state, handler): PageState<'a> =
        { state with Name = handler }

    [<CustomOperation("entryCheck")>]
    member __.EntryCheck(state, handler): PageState<'a> =
        { state with EntryCheck = handler }

    [<CustomOperation("transition")>]
    member __.Transitions(state, handler): PageState<'a> =
        { state with Transitions = handler :: state.Transitions }

    [<CustomOperation("action")>]
    member __.Actions(state, handler): PageState<'a> = 
        { state with Actions = handler :: state.Actions }

    [<CustomOperation("exitFunction")>]
    member __.ExitFunction(state, handler): PageState<'a> =
        { state with Exit = handler }

module Scrutiny =
    let page = PageBuilder()

    let defaultConfig =
        { ScrutinyConfig.Seed = Environment.TickCount
          MapOnly = false
          ComprehensiveActions = true
          ComprehensiveStates = true }

    let private printPath path =
        printfn "path: %s"
            (path
            |> List.map (fun p -> p.Name)
            |> String.concat " --> ")

    let private runActions (config: ScrutinyConfig) (state: PageState<'a>) =
        if config.ComprehensiveActions then
            state.Actions
            |> Seq.iter (fun a -> a())
        else
            let random = new Random(config.Seed)
            let amount = random.Next(if state.Actions.Length > 1 then state.Actions.Length else 1)
            state.Actions
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.take amount
            |> Seq.iter (fun a -> a())

    let scrutinize<'a> (config: ScrutinyConfig) (globalState: 'a) (startFn: 'a -> PageState<'a>) =
        printfn "Scrutinizing system under test with seed: %i" config.Seed
        let startState = startFn globalState
        let allStates =
            Navigator.constructAdjacencyGraph startState globalState
        let runConfigureActions = runActions config

        if not config.MapOnly then
            let random = new Random(config.Seed)
            let findPath = Navigator.shortestPathFunction allStates
        
            let nextNode alreadyVisited =
                let unvisitedNodes: AdjacencyGraph<PageState<'a>> = 
                    allStates
                    |> Seq.map (fun s -> fst s)
                    |> Seq.except alreadyVisited
                    |> Seq.map (fun n -> allStates |> List.find (fun ps -> (fst ps) = n))
                    |> List.ofSeq

                let shouldContinue =
                    if config.ComprehensiveStates then
                        if unvisitedNodes |> Seq.isEmpty then false
                        else true
                    else
                        if unvisitedNodes.Length > (allStates.Length / 2) then true
                        else false

                if shouldContinue then
                    let next = random.Next(unvisitedNodes.Length)
                    Some (fst unvisitedNodes.[next])
                else None

            let rec clickAround (alreadyVisited: PageState<'a> list) (currentPath: PageState<'a> list) =
                match currentPath with
                | path when path.Length = 1 ->
                    match nextNode alreadyVisited with
                    | None -> ()
                    | Some nextNode ->
                        let path = findPath path.Head nextNode

                        if path.Length = 1 then
                            runConfigureActions path.Head
                            ()
                        else
                            printPath path
                            clickAround alreadyVisited path
                | head :: tail -> 
                    currentPath
                    |> Seq.pairwise
                    |> Seq.find (fun (current, _) ->
                        current = head
                    )
                    |> fun (current, next) ->
                        try
                            current.EntryCheck()
                            let transition =
                                head.Transitions
                                |> Seq.find (fun t ->
                                    let state = t.ToState globalState
                                    state.Name = next.Name
                                )
                            runConfigureActions current
                            transition.TransitionFn()
                        with
                        | exn ->
                            let message = 
                                sprintf "System under test failed scrutiny.
    To re-run this exact test, specify the seed in the config with the value: %i.
    The error occured in state: %s
    The error that occured is of type: %A%s" 
                                    config.Seed
                                    current.Name
                                    exn 
                                    Environment.NewLine
                            raise <| ScrutinyException(message, exn)
                
                    clickAround (head :: alreadyVisited) tail

            clickAround [] [startState]
        Reporter.generateMap allStates

    let scrutinizeWithDefaultConfig<'a> = scrutinize<'a> defaultConfig
