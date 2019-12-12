namespace Scrutiny
open System

type PageState =
    { Name: string
      EntryCheck: unit -> unit
      Transitions: (unit -> string) seq
      Exit: unit -> unit }

type PageBuilder() =
    let random = new Random()
    let randomTransition (transitions: (unit -> string) seq) =
        let upper = transitions |> Seq.length
        let randomIndex = random.Next(upper)

        let transitions = transitions |> Seq.toList
        transitions.[randomIndex]

    member __.Yield(_): PageState =
        { PageState.Name = ""
          EntryCheck = fun _ -> ()
          Transitions = []
          Exit = fun _ -> () }

    member __.Run(state: PageState) =
        if random.NextDouble() >= 0.2
        then
            let transition = randomTransition state.Transitions
            ()
        else
            state.Exit()

    [<CustomOperation("name")>]
    member __.Name(state, handler): PageState =
        { state with Name = handler }

    [<CustomOperation("entryCheck")>]
    member __.EntryCheck(state, handler): PageState =
        { state with EntryCheck = handler }

    [<CustomOperation("transitions")>]
    member __.Transitions(state, handler): PageState =
        { state with Transitions = handler }

    [<CustomOperation("exitFunction")>]
    member __.ExitFunction(state, handler): PageState =
        { state with Exit = handler }



type ScrutinizeState =
    { Pages: PageState seq
      EntryFunction: unit -> PageState
      EntryPage: PageBuilder }

type ScrutinizeBuilder() =
    let random = new Random()
    let randomTransition (transitions: (unit -> string) seq) =
        let upper = transitions |> Seq.length
        let randomIndex = random.Next(upper)

        let transitions = transitions |> Seq.toList
        transitions.[randomIndex]()
        
    member __.Yield(_): ScrutinizeState =
        { ScrutinizeState.Pages = []
          EntryFunction = fun _ ->
              { PageState.Name = ""
                EntryCheck = fun _ -> ()
                Transitions = []
                Exit = fun _ -> () }
          EntryPage = new PageBuilder() }

    member __.Run(state: ScrutinizeState) =
        let nextState = state.EntryFunction()
        nextState.EntryCheck()
        
        (*let page =
            state.Pages
            |> Seq.find (fun p -> p.Name = nextState)
        
        page.EntryCheck()
        while random.NextDouble() >= 0.2 do
            let nextState = randomTransition page.Transitions
            let page =
                state.Pages
                |> Seq.find (fun p -> p.Name = nextState)
            page.EntryCheck()
            
        page.Exit()*)
        
            
    [<CustomOperation("entryFunction")>]
    member __.EntryFunction(state, handler) =
        { state with EntryFunction = handler }

    [<CustomOperation("pages")>]
    member __.Pages(state, handler) =
        { state with Pages = handler }

module Scrutiny =
    let scrutinize = new ScrutinizeBuilder()
    let page = new PageBuilder()
