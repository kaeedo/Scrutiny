namespace Scrutiny.CSharp

open System
open System.Collections.Generic

// TODO Rename this class
[<AbstractClass>]
type PageState<'a, 'b>(name: string) =
    member val Name = name

    abstract OnEnter: unit -> unit
    //default this.OnEnter() = ()
    abstract OnExit: unit -> unit
    //default this.OnExit() = ()
    //abstract ExitAction: unit -> unit
    //default this.ExitAction() = ()
    abstract Transitions: unit -> IEnumerable<Func<PageState<'a, 'b>>>
    //  default this.Transitions() = Seq.empty
    abstract Actions: 'b -> IEnumerable<Action>
//default this.Actions(ls: 'b) = Seq.empty
