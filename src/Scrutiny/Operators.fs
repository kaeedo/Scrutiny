namespace Scrutiny

module Operators =
    let (==>) (usingFn: unit -> unit) (toState: 'a -> PageState<'a>) =
        { Transition.TransitionFn = usingFn; ToState = toState }