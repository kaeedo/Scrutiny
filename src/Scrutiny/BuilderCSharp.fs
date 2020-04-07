namespace Scrutiny.CSharp

open System
open System.Collections.Generic

[<AbstractClass>]
type PageState<'a>(name: string) =
    member val Name = name with get

    abstract member OnEnter: unit -> unit
    default this.OnEnter() = ()
    abstract member OnExit: unit -> unit
    default this.OnExit() = ()
    abstract member ExitAction: unit -> unit
    default this.ExitAction() = ()

    abstract member Transitions: unit -> IEnumerable<struct(Action * PageState<'a>)>
    abstract member Actions: unit -> IEnumerable<Action>
