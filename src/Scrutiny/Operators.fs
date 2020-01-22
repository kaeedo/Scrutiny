namespace Scrutiny

module Operators =
    let (==>) (usingFn: unit -> unit) (toState: Lazy<PageState>) =
        usingFn, toState

