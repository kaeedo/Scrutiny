namespace Scrutiny

[<AutoOpen>]
module Operators =
    let inline (==>) usingFn (toState: 'a -> PageState<'a, 'b>) =
        { Transition.TransitionFn = usingFn
          ToState = toState }
