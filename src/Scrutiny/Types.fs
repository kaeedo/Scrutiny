namespace rec Scrutiny

open System

type ScrutinyException(message, innerException: Exception) = inherit Exception(message, innerException)

//http://www.fssnip.net/av/title/NinetyNine-F-Problems-Problems-80-89-Graphs

type Edge<'a> = 'a * 'a

type Graph<'a> = 'a list * Edge<'a> list

type Node<'a> = 'a * 'a list

type AdjacencyGraph<'a> = 'a Node list

type ScrutinyConfig = 
    { Seed: int
      MapOnly: bool
      ComprehensiveActions: bool
      ComprehensiveStates: bool }

type Transition<'a> =
    { TransitionFn: unit -> unit
      ToState: 'a -> PageState<'a> }

[<CustomComparison; CustomEquality>]
type PageState<'a> =
    { Id: Guid
      Name: string
      EntryCheck: unit -> unit
      Transitions: Transition<'a> list
      Actions: List<unit -> unit>
      // DesiredOutcome: List of actions transition to PageState?
      Exit: unit -> unit }

    interface IComparable<PageState<'a>> with
        member this.CompareTo other =
            compare this.Name other.Name
    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | null -> 1
            | :? PageState<'a> as other -> (this :> IComparable<_>).CompareTo other
            | _ -> invalidArg "obj" "not a PageState"

    interface IEquatable<PageState<'a>> with
        member this.Equals other =
            this.Name = other.Name

    override this.Equals obj =
        match obj with
          | :? PageState<'a> as other -> (this :> IEquatable<_>).Equals other
          | _ -> false
    override this.GetHashCode() =
        hash this.Name

