namespace Scrutiny

open System

(*type ScrutinizeState =
    { Pages: PageState seq
      CurrentState: PageState
      EntryFunction: unit -> unit -> PageState
      EntryPage: PageBuilder }*)

type PageBuilder() =
    member __.Yield(_): PageState =
        { PageState.Id = Guid.NewGuid()
          Name = ""
          EntryCheck = fun _ -> ()
          Transitions = []
          Actions = []
          Exit = fun _ -> () }

    [<CustomOperation("name")>]
    member __.Name(state, handler): PageState =
        { state with Name = handler }

    [<CustomOperation("entryCheck")>]
    member __.EntryCheck(state, handler): PageState =
        { state with EntryCheck = handler }

    [<CustomOperation("transition")>]
    member __.Transitions(state, handler): PageState =
        { state with Transitions = handler :: state.Transitions }

    [<CustomOperation("action")>]
    member __.Actions(state, handler): PageState = 
        { state with Actions = handler :: state.Actions }

    [<CustomOperation("exitFunction")>]
    member __.ExitFunction(state, handler): PageState =
        { state with Exit = handler }

module Scrutiny =
    let adjacencyGraph = [||]

    let timer = Diagnostics.Stopwatch()
    let page = PageBuilder()

    let scrutinize (startFn: unit -> PageState) =
        let startState = startFn()
        let allStates =
            Navigator.constructAdjacencyGraph startState
        // seed 11 = Home --> Comment
        let random = new Random(11) // TODO: get seed from config
        let findPath = Navigator.shortestPathFunction allStates

        let rec clickAround (alreadyVisited: string list) (nodes: AdjacencyGraph<PageState>) startNode =
            let mutable alreadyVisited = alreadyVisited
            let mutable startNode = startNode
            let nextNode = 
                // TODO: Refactor this
                let unvisitedNodes: AdjacencyGraph<PageState> =
                    nodes
                    |> List.map (fun n -> (fst n).Name)
                    |> List.except alreadyVisited
                    |> List.map (fun n -> nodes |> List.find (fun ps -> (fst ps).Name = n))

                if unvisitedNodes |> List.isEmpty then
                    None
                else
                    let next = random.Next(unvisitedNodes.Length)
                    Some (fst unvisitedNodes.[next])

            match nextNode with
            | None -> ()
            | Some nextNode ->
                let path = findPath startNode nextNode
                if path.Length = 1 then
                    ()
                else
                    printfn "path: %s"
                        (path
                        |> List.map (fun p -> p.Name)
                        |> String.concat " --> ")

                    path
                    |> List.pairwise
                    |> List.iter (fun (current, next) ->
                        alreadyVisited <- current.Name :: alreadyVisited
                        current.EntryCheck()

                        let nextTransition, nextNode =
                            current.Transitions
                            |> List.find (fun t ->
                                let state = (snd t)()
                                state.Name = next.Name
                            )

                        startNode <- nextNode()
                        nextTransition()
                    )
                    clickAround alreadyVisited nodes startNode

        clickAround [] allStates startState

        ()
