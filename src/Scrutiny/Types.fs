namespace rec Scrutiny

open System

//http://www.fssnip.net/av/title/NinetyNine-F-Problems-Problems-80-89-Graphs

type Edge<'a> = 'a * 'a

type Graph<'a> = 'a list * Edge<'a> list

type Node<'a> = 'a * 'a list

type AdjacencyGraph<'a> = 'a Node list

[<CustomComparison; CustomEquality>]
type PageState =
    { Id: Guid
      Name: string
      EntryCheck: unit -> unit
      Transitions: List<((unit -> unit) * (unit -> PageState))>
      Actions: List<unit -> unit>
      // DesiredOutcome: List of actions transition to PageState?
      Exit: unit -> unit }

    interface IComparable<PageState> with
        member this.CompareTo other =
            compare this.Name other.Name
    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | null -> 1
            | :? PageState as other -> (this :> IComparable<_>).CompareTo other
            | _ -> invalidArg "obj" "not a PageState"

    interface IEquatable<PageState> with
        member this.Equals other =
            this.Name = other.Name

    override this.Equals obj =
        match obj with
          | :? PageState as other -> (this :> IEquatable<_>).Equals other
          | _ -> false
    override this.GetHashCode() =
        hash this.Name

