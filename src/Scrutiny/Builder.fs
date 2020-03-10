namespace Scrutiny

open System

(*type ScrutinizeState =
    { Pages: PageState seq
      CurrentState: PageState
      EntryFunction: unit -> unit -> PageState
      EntryPage: PageBuilder }*)

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

    let private printPath path =
        printfn "path: %s"
            (path
            |> List.map (fun p -> p.Name)
            |> String.concat " --> ")

    let scrutinize<'a> (globalState: 'a) (startFn: 'a -> PageState<'a>) =
        let startState = startFn globalState
        let allStates =
            Navigator.constructAdjacencyGraph startState globalState
        // seed 11 = Home --> Comment
        let random = new Random(11) // TODO: get seed from config
        let findPath = Navigator.shortestPathFunction allStates
        
        let nextNode alreadyVisited =
            let unvisitedNodes: AdjacencyGraph<PageState<'a>> = 
                allStates
                |> Seq.map (fun s -> fst s)
                |> Seq.except alreadyVisited
                |> Seq.map (fun n -> allStates |> List.find (fun ps -> (fst ps) = n))
                |> List.ofSeq

            if unvisitedNodes |> Seq.isEmpty then
                None
            else
                let next = random.Next(unvisitedNodes.Length)
                Some (fst unvisitedNodes.[next])

        let runActions (state: PageState<'a>) =
            state.Actions
            |> Seq.iter (fun a -> a())

        let rec clickAround (alreadyVisited: PageState<'a> list) (currentPath: PageState<'a> list) =
            match currentPath with
            | path when path.Length = 1 ->
                match nextNode alreadyVisited with
                | None -> ()
                | Some nextNode ->
                    let path = findPath path.Head nextNode

                    if path.Length = 1 then
                        runActions path.Head
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
                        runActions current
                        transition.TransitionFn()
                    with
                    | exn ->
                        let message = 
                            sprintf "System under test failed scrutiny.
To re-run this exact test, specify the seed in the config with the value: %i.
The error occured in state: %s
The error that occured is of type: %A%s" 
                                11 
                                current.Name
                                exn 
                                Environment.NewLine
                        raise <| ScrutinyException(message, exn)
                
                clickAround (head :: alreadyVisited) tail

        clickAround [] [startState]
