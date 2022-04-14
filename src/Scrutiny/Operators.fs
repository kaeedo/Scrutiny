namespace Scrutiny

open System.Threading.Tasks

[<AutoOpen>]
module Operators =
    let inline (==>) usingFn (toState: 'a -> PageState<'a, 'b>) =
        let usingFn = fun localState -> Task.FromResult(usingFn localState)
        { Transition.TransitionFn = usingFn
          ToState = toState }
        
    let inline (==>!) usingFn (toState: 'a -> PageState<'a, 'b>) =
        { Transition.TransitionFn = usingFn
          ToState = toState }
