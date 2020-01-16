namespace Scrutiny

module Operators =
    let (==>) (usingFn: unit -> unit) (toState: PageState) =
        usingFn, toState

