namespace Scrutiny

module Operators =
    let (==>) (fromState: PageState, navigationKey: string) (toState: PageState) =
        fromState, fun () ->
            fromState.Links.[navigationKey] ()
            toState

