namespace Scrutiny

open System

[<AutoOpen>]
[<Obsolete("Remove")>]
module Operators =
    let inline (==>) usingFn (toState: 'a -> PageState<'a, 'b>) =
        { Transition.DependantActions = []
          TransitionFn = usingFn
          ToState = toState }
