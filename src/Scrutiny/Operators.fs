namespace Scrutiny

module Operators =
    let (==>) (usingFn: unit -> unit) (toState: unit -> PageState) =
        { Transition.TransitionFn = usingFn; ToState = toState }