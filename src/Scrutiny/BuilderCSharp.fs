namespace Scrutiny.CSharp

open System
open System.Collections.Generic

// TODO Pass in localstate
// TODO Rename this class
[<AbstractClass>]
type PageState<'a, 'b>(name: string) =
    member val Name = name

    abstract OnEnter: unit -> unit
    default this.OnEnter() = ()
    abstract OnExit: unit -> unit
    default this.OnExit() = ()
    abstract ExitAction: unit -> unit
    default this.ExitAction() = ()
    abstract Transitions: unit -> IEnumerable<Func<PageState<'a, 'b>>>
    default this.Transitions() = Seq.empty
    abstract Actions: unit -> IEnumerable<Action>
    default this.Actions() = Seq.empty
