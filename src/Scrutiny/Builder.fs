namespace Scrutiny

open System

[<CustomComparison; CustomEquality>]
type PageState =
    { Name: string
      EntryCheck: unit -> unit
      Transitions: List<((unit -> unit) * (unit -> PageState))>
      Exit: unit -> unit }

    interface IComparable<PageState> with
        member this.CompareTo other =
            compare this.Name other.Name
    interface IComparable with
        member this.CompareTo obj =
            match obj with
              | null -> 1
              | :? PageState as other -> (this :> IComparable<_>).CompareTo other
              | _ -> invalidArg "obj" "not a Category"

    interface IEquatable<PageState> with
        member this.Equals other =
            this.Name = other.Name

    override this.Equals obj =
        match obj with
          | :? PageState as other -> (this :> IEquatable<_>).Equals other
          | _ -> false
    override this.GetHashCode() =
        hash this.Name

type PageBuilder() =
    member __.Yield(_): PageState =
        { PageState.Name = ""
          EntryCheck = fun _ -> ()
          Transitions = []
          Exit = fun _ -> () }

    [<CustomOperation("name")>]
    member __.Name(state, handler): PageState =
        { state with Name = handler }

    [<CustomOperation("entryCheck")>]
    member __.EntryCheck(state, handler): PageState =
        { state with EntryCheck = handler }

    [<CustomOperation("transition")>]
    member __.Links(state, handler): PageState =
        { state with Transitions = handler :: state.Transitions }

    [<CustomOperation("exitFunction")>]
    member __.ExitFunction(state, handler): PageState =
        { state with Exit = handler }

type ScrutinizeState =
    { Pages: PageState seq
      CurrentState: PageState
      EntryFunction: unit -> unit -> PageState
      // TODO refactor this to new model
      Navigations: Map<PageState, (unit -> PageState) list>
      EntryPage: PageBuilder }

type ClickFlowBuilder() =
    member __.Yield(_): ScrutinizeState =
        let defaultState =
            { PageState.Name = ""
              EntryCheck = fun _ -> ()
              Transitions = []
              Exit = fun _ -> () }

        { ScrutinizeState.Pages = []
          // TODO refactor this to new model
          Navigations = Map.empty
          CurrentState = defaultState
          EntryFunction = fun _ -> fun _ -> defaultState
          EntryPage = new PageBuilder() }

    member __.Run(state: ScrutinizeState) =
        state

    [<CustomOperation("entryFunction")>]
    member __.EntryFunction(state, handler) =
        { state with EntryFunction = handler }

    [<CustomOperation("navigation")>]
    member __.Navigation(state, (handler: PageState * (unit -> PageState))) =
        let fromState, navigation = handler
        let pageNavigations =
            match state.Navigations.TryFind fromState with
            | None -> []
            | Some n -> n
        let pageNavigations = navigation :: pageNavigations

        let navigations = state.Navigations.Remove fromState

        { state with Navigations = navigations.Add(fromState, pageNavigations) }

    [<CustomOperation("pages")>]
    member __.Pages(state, handler) =
        { state with Pages = handler }

module Scrutiny =
    let clickFlow = ClickFlowBuilder()
    let page = PageBuilder()
    let scrutinize (state: ScrutinizeState) =
        let random = new Random()

        let randomTransition (transitions: List<(unit -> unit) * (unit -> PageState)>) =
            let upper = transitions |> Seq.length
            let randomIndex = random.Next(upper)

            let transition, nextState = transitions.[randomIndex]
            transition() |> ignore
            nextState()
            
        let rec clickAround next =
            next.EntryCheck()
            let chance = random.NextDouble()
            printfn "Chance: %f " chance
            match chance > 0.9 with
            | true -> next.Exit()
            | false ->
                let possibleTransitions = next.Transitions //.Navigations.[next]
                let nextState = randomTransition possibleTransitions
                clickAround nextState

        let nextState = state.EntryFunction()
        clickAround (nextState())
