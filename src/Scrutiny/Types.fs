namespace Scrutiny

open System
open System.IO
open System.Threading.Tasks

type internal ScrutinyException(message, innerException: Exception) =
    inherit Exception(message, innerException)

//http://www.fssnip.net/av/title/NinetyNine-F-Problems-Problems-80-89-Graphs
type internal Node<'a> = 'a * 'a list

type internal AdjacencyGraph<'a> = Node<'a> list

type ScrutinyConfig =
    { Seed: int
      MapOnly: bool
      ComprehensiveActions: bool
      ComprehensiveStates: bool
      ScrutinyResultFilePath: string
      Logger: string -> unit }

    static member Default =
        { ScrutinyConfig.Seed = Environment.TickCount
          MapOnly = false
          ComprehensiveActions = true
          ComprehensiveStates = true
          ScrutinyResultFilePath =
            Directory.GetCurrentDirectory()
            + "/ScrutinyResult.html"
          Logger = printfn "%s" }

type CallerInformation =
    { MemberName: string
      LineNumber: int
      FilePath: string }

type StateAction =
    { CallerInformation: CallerInformation
      Name: string
      DependantActions: string list
      IsExit: bool
      ActionFn: unit -> Task<unit> }

type Transition<'a> =
    { DependantActions: string list
      ViaFn: unit -> Task<unit>
      Destination: 'a -> PageState<'a> }

and [<CustomComparison; CustomEquality>] PageState<'a> =
    { Name: string
      // OnAction?
      OnEnter: unit -> Task<unit>
      OnExit: unit -> Task<unit>
      mutable Transitions: (Transition<'a>) list
      Actions: StateAction list }

    interface IComparable<PageState<'a>> with
        member this.CompareTo other = compare this.Name other.Name

    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | null -> 1
            | :? PageState<'a> as other -> (this :> IComparable<_>).CompareTo other
            | _ -> invalidArg "obj" "not a PageState"

    interface IEquatable<PageState<'a>> with
        member this.Equals other = this.Name = other.Name

    override this.Equals obj =
        match obj with
        | :? PageState<'a> as other -> (this :> IEquatable<_>).Equals other
        | _ -> false

    override this.GetHashCode() = hash this.Name

type SerializableException =
    { Type: string
      Message: string
      StackTrace: string
      InnerException: SerializableException option }

type ErrorLocation =
    | State of string * SerializableException
    | Transition of string * string * SerializableException

type Step<'a> =
    { PageState: PageState<'a>
      Actions: string list
      Error: ErrorLocation option }

type ScrutinizedStates<'a> =
    { Graph: AdjacencyGraph<PageState<'a>>
      Steps: Step<'a> list }
