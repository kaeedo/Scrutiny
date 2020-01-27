namespace Scrutiny

module Operators =
    let (==>) (usingFn: unit -> unit) (toState: unit -> PageState) =
        usingFn, toState

