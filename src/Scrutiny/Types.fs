namespace Scrutiny

open System
open System.IO

type ScrutinyException(message, innerException: Exception) =
    inherit Exception(message, innerException)

//http://www.fssnip.net/av/title/NinetyNine-F-Problems-Problems-80-89-Graphs

type Edge<'a> = 'a * 'a

type Graph<'a> = 'a list * Edge<'a> list

type Node<'a> = 'a * 'a list

type AdjacencyGraph<'a> = 'a Node list

type ScrutinyConfig =
    { Seed: int
      MapOnly: bool
      ComprehensiveActions: bool
      ComprehensiveStates: bool
      ScrutinyResultFilePath: string }

      static member Default =
        { ScrutinyConfig.Seed = Environment.TickCount
          MapOnly = false
          ComprehensiveActions = true
          ComprehensiveStates = true
          ScrutinyResultFilePath = Directory.GetCurrentDirectory() + "/ScrutinyResult.html" }

type Transition<'a, 'b> =
    { TransitionFn: 'b -> unit
      ToState: 'a -> PageState<'a, 'b> }

and [<CustomComparison; CustomEquality>] PageState<'a, 'b> =
    { Name: string
      LocalState: 'b
      OnEnter: 'b -> unit
      OnExit: 'b -> unit
      Transitions: Transition<'a, 'b> list
      Actions: List<'b -> unit>
      // OnAction?
      ExitAction: ('b -> unit) option }

    interface IComparable<PageState<'a, 'b>> with
        member this.CompareTo other = compare this.Name other.Name

    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | null -> 1
            | :? PageState<'a, 'b> as other -> (this :> IComparable<_>).CompareTo other
            | _ -> invalidArg "obj" "not a PageState"

    interface IEquatable<PageState<'a, 'b>> with
        member this.Equals other = this.Name = other.Name

    override this.Equals obj =
        match obj with
        | :? PageState<'a, 'b> as other -> (this :> IEquatable<_>).Equals other
        | _ -> false

    override this.GetHashCode() = hash this.Name
