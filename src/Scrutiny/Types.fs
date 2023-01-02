namespace Scrutiny

open System
open System.IO
open System.Threading.Tasks

type internal ScrutinyException(message, innerException: Exception) =
    inherit Exception(message, innerException)

//http://www.fssnip.net/av/title/NinetyNine-F-Problems-Problems-80-89-Graphs

type internal Edge<'a> = 'a * 'a

type internal Graph<'a> = 'a list * Edge<'a> list

type internal Node<'a> = 'a * 'a list

type internal AdjacencyGraph<'a> = 'a Node list

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

type Action<'b> =
    { CallerInformation: CallerInformation
      Name: string
      DependantActions: string list
      IsExit: bool
      ActionFn: 'b -> Task<unit> }

type Transition<'a, 'b> =
    { DependantActions: string list
      ViaFn: 'b -> Task<unit>
      Destination: 'a -> PageState<'a, 'b> }

and [<CustomComparison; CustomEquality>] PageState<'a, 'b> =
    { Name: string
      LocalState: 'b
      // OnAction?
      OnEnter: 'b -> Task<unit>
      OnExit: 'b -> Task<unit>
      // TODO can we make this not mutable?
      // It's required right now because of the C# builder
      mutable Transitions: (Transition<'a, 'b>) list
      Actions: Action<'b> list }

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

type SerializableException =
    { Type: string
      Message: string
      StackTrace: string
      InnerException: SerializableException option }

type ErrorLocation =
    | State of string * SerializableException
    | Transition of string * string * SerializableException

type Step<'a, 'b> =
    { PageState: PageState<'a, 'b>
      Actions: string seq
      Error: ErrorLocation option }

type ScrutinizedStates<'a, 'b> =
    { Graph: AdjacencyGraph<PageState<'a, 'b>>
      Steps: Step<'a, 'b> seq }
