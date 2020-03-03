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

(*type ClickFlowBuilder() =
    member __.Yield(_): ScrutinizeState =
        let defaultState =
            { PageState.Id = Guid.NewGuid()
              Name = ""
              EntryCheck = fun _ -> ()
              Transitions = []
              Actions = []
              Exit = fun _ -> () }

        { ScrutinizeState.Pages = []
          CurrentState = defaultState
          EntryFunction = fun _ -> fun _ -> defaultState
          EntryPage = new PageBuilder() }

    [<CustomOperation("entryFunction")>]
    member __.EntryFunction(state, handler) =
        { state with EntryFunction = handler }

    [<CustomOperation("pages")>]
    member __.Pages(state, handler) =
        { state with Pages = handler }*)

module Scrutiny =
    let adjacencyGraph = [||]

    let timer = System.Diagnostics.Stopwatch()
    //let clickFlow = ClickFlowBuilder()
    let page = PageBuilder()

    let scrutinize (startFn: unit -> PageState) =
        let startState = startFn()
        let allStates =
            Navigator.constructAdjacencyGraph startState

        let random = new Random()
        let nextNode = 
            let next = random.Next(allStates |> List.length)
            fst allStates.[next]
        
        let path = Navigator.shortestPathFunction allStates startState nextNode
        
        path
        |> List.iter (fun p ->
            printf "%s --> " p.Name
        )


        path
        |> List.pairwise
        |> List.iter (fun (current, next) ->
            current.EntryCheck()

            let nextTransition =
                current.Transitions
                |> List.find (fun t ->
                    let state = (snd t)()
                    state.Name = next.Name
                )
                |> fst
            nextTransition()
        )

        //let nextState = startFn()
        //clickAround (nextState())
        ()
