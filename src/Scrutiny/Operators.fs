namespace Scrutiny

module Operators =
    let (==>) (usingFn: 'b option -> unit) (toState: 'a -> PageState<'a, 'b>) =
        { Transition.TransitionFn = usingFn
          ToState = toState }
