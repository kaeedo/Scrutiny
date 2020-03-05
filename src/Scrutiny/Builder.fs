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
        let printPath path =
            printfn "path: %s"
                (path
                |> List.map (fun p -> p.Name)
                |> String.concat " --> ")

        let getUnvisitedNodes visited =
            allStates
            |> Seq.map (fun s -> fst s)
            |> Seq.except visited
            |> Seq.map (fun n -> allStates |> List.find (fun ps -> (fst ps) = n))
            |> List.ofSeq

        let nextNode alreadyVisited =
            let unvisitedNodes: AdjacencyGraph<PageState> = getUnvisitedNodes alreadyVisited

            if unvisitedNodes |> Seq.isEmpty then
                None
            else
                let next = random.Next(unvisitedNodes.Length)
                Some (fst unvisitedNodes.[next])
            

        let rec clickAround (alreadyVisited: PageState list) (currentPath: PageState list) (startNode: PageState) =
            match currentPath with
            | path when path.Length = 1 ->
                match nextNode alreadyVisited with
                | None -> ()
                | Some nextNode ->
                    let path = findPath path.Head nextNode

                    if path.Length = 1 then
                        ()
                    else
                        printPath path
                        clickAround alreadyVisited path startNode
            | head :: tail -> 
                currentPath
                |> List.pairwise
                |> List.find (fun (current, _) ->
                    current = head
                )
                |> fun (_, next) ->
                    head.Transitions
                    |> List.find (fun t ->
                        let state = (snd t)()
                        state.Name = next.Name
                    )
                |> fun (n, _) -> n()
                
                clickAround (head :: alreadyVisited) tail startNode
            | [] -> 
                match nextNode alreadyVisited with
                | None -> ()
                | Some nextNode ->
                    let path = findPath startNode nextNode

                    printPath path
                    clickAround alreadyVisited path startNode
        clickAround [] [] startState

        ()
