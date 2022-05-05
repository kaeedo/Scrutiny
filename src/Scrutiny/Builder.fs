namespace Scrutiny

open System
open System.Runtime.CompilerServices
open System.Threading.Tasks

type PageBuilder() =
    member _.Yield _: PageState<'a, 'b> =
        { PageState.Name = ""
          LocalState = Unchecked.defaultof<'b>
          OnEnter = fun _ -> Task.FromResult()
          OnExit = fun _ -> Task.FromResult()
          Transitions = []
          Actions = []
          ExitActions = [] } // TODO. states can have many exit actions. one is chosen at random anyway.

    [<CustomOperation("name")>]
    member _.Name(state, handler): PageState<'a, 'b> = { state with Name = handler }

    [<CustomOperation("localState")>]
    member _.LocalState(state, handler): PageState<'a, 'b> = { state with LocalState = handler }

    [<CustomOperation("onEnter")>]
    member _.OnEnter(state, handler: 'b -> unit): PageState<'a, 'b> =
        let handler = fun localState -> Task.FromResult(handler localState)
        { state with OnEnter = handler }
        
    [<CustomOperation("onEnter")>]
    member _.OnEnter(state, handler: 'b -> Task<unit>): PageState<'a, 'b> = { state with OnEnter = handler }

    [<CustomOperation("onExit")>]
    member _.OnExit(state, handler: 'b -> unit): PageState<'a, 'b> =
        let handler = fun localState -> Task.FromResult(handler localState)
        { state with OnExit = handler }
    
    [<CustomOperation("onExit")>]
    member _.OnExit(state, handler: 'b -> Task<unit>): PageState<'a, 'b> = { state with OnExit = handler }

    [<CustomOperation("transition")>]
    member _.Transitions(state, handler): PageState<'a, 'b> = { state with Transitions = handler :: state.Transitions }

    [<CustomOperation("action")>]
    member _.Actions(state, handler: 'b -> unit, [<CallerMemberName>]?memberName: string, [<CallerLineNumber>]?lineNumber: int, [<CallerFilePath>]?filePath: string): PageState<'a, 'b> = 
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }
            
        let handler = fun localState -> Task.FromResult(handler localState)
        { state with Actions = (callerInformation, handler) :: state.Actions }
        
    [<CustomOperation("action")>]
    member _.Actions(state, handler: 'b -> Task<unit>, [<CallerMemberName>]?memberName: string, [<CallerLineNumber>]?lineNumber: int, [<CallerFilePath>]?filePath: string): PageState<'a, 'b> = 
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        { state with Actions = (callerInformation, handler) :: state.Actions }
        
    [<CustomOperation("exitAction")>]
    member _.ExitAction(state, handler: 'b -> unit): PageState<'a, 'b> =
        let handler = fun localState -> Task.FromResult(handler localState)
        { state with ExitActions = handler :: state.ExitActions }
        
    [<CustomOperation("exitAction")>]
    member _.ExitAction(state, handler: 'b -> Task<unit>): PageState<'a, 'b> =
        { state with ExitActions = handler :: state.ExitActions }

[<AutoOpen>]
module Scrutiny =
    let private printPath logger path =
        logger <| sprintf "path: %s"
            (path
             |> List.map (fun p -> p.Name)
             |> String.concat " --> ")

    let private buildActionName (ci: CallerInformation) =
        if ci.LineNumber > 0
        then $"Member: {ci.MemberName}, Line #: {ci.LineNumber}, File: {ci.FilePath}"
        else $"{ci.FilePath}.{ci.MemberName}"
        
    let private simpleTraverse (tasks: (unit -> Task<unit>) list): unit -> Task<unit> =
        match tasks with
        | [] -> fun () -> Task.FromResult(())
        | x ->
            x
            |> List.reduce (fun accumulator element ->
                fun () ->
                    task {
                        do! accumulator()
                        return! element()
                    }
            )

    let private runActions (reporter: IReporter<'a, 'b>) (config: ScrutinyConfig) (state: PageState<'a, 'b>) =
        if config.ComprehensiveActions then
            state.Actions
            |> List.map (fun (ci, a) ->
                fun () ->
                    task {
                        reporter.PushAction (buildActionName ci)
                        return! (a state.LocalState)
                    }
            )
            |> simpleTraverse
            |> fun fn -> fn()
        else
            let random = Random(config.Seed)

            let amount =
                random.Next(if state.Actions.Length > 1 then state.Actions.Length else 1)
            
            state.Actions
            |> List.sortBy (fun _ -> random.Next())
            |> List.take amount
            |> List.map (fun (ci, a) ->
                fun () -> 
                    task {
                        reporter.PushAction (buildActionName ci)
                        return! (a state.LocalState)
                    }
            )
            |> simpleTraverse
            |> fun fn -> fn()

    let private unvisitedNodes allStates alreadyVisited: AdjacencyGraph<PageState<'a, 'b>> =
        allStates
        |> Seq.map fst
        |> Seq.except alreadyVisited
        |> Seq.map (fun n -> allStates |> List.find (fun ps -> (fst ps) = n))
        |> List.ofSeq

    let private convertException (e: exn): SerializableException =
        let rec convertInner (currentException: exn) =
            let inner = 
                if currentException.InnerException = null 
                then None 
                else Some <| convertInner currentException.InnerException
            
            { SerializableException.Type = currentException.GetType().ToString()
              Message = currentException.Message
              StackTrace = currentException.StackTrace
              InnerException = inner }

        convertInner e

    let private performStateActions (reporter: IReporter<_, _>) config globalState (current, next) =
        task {
            try
                do! current.OnEnter current.LocalState
                do! runActions reporter config current
                do! current.OnExit current.LocalState
            with exn ->
                let message =
                    $"System under test failed scrutiny.
        To re-run this exact test, specify the seed in the config with the value: '%i{config.Seed}'.
        The error occurred in state: '%s{current.Name}'
        The error that occurred is of type: '%A{exn}%s{Environment.NewLine}'"

                let exn = ScrutinyException(message, exn)

                reporter.OnError (State (current.Name, convertException exn))

                raise <| exn

            try
                let transition =
                    current.Transitions
                    |> Seq.find (fun t ->
                        let state = t.ToState globalState
                        state.Name = next.Name)

                reporter.PushTransition next

                do! transition.TransitionFn current.LocalState
            with exn ->
                let message =
                    $"System under test failed scrutiny.
        To re-run this exact test, specify the seed in the config with the value: '%i{config.Seed}'.
        The error occurred in state: '%s{current.Name}'
        The error that occurred is of type: '%A{exn}%s{Environment.NewLine}'"

                let exn = ScrutinyException(message, exn)

                reporter.OnError (Transition (current.Name, next.Name, convertException exn))

                raise <| exn
        }

    let private findExit (config: ScrutinyConfig) (allStates: AdjacencyGraph<PageState<'a, 'b>>) =
        let random = Random(config.Seed)

        let exitNode =
            allStates
            |> Seq.filter (fun (node, _) -> node.ExitActions |> Seq.isEmpty |> not)
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.tryHead

        exitNode
        
    let private navigateStateMachine reporter config allStates globalState startState =
        let random = Random(config.Seed)
        let findPath = Navigator.shortestPathFunction allStates

        let nextNode alreadyVisited =
            let unvisitedNodes = unvisitedNodes allStates alreadyVisited

            let shouldContinue =
                if config.ComprehensiveStates then
                    unvisitedNodes
                    |> Seq.isEmpty
                    |> not
                else
                    unvisitedNodes.Length > (allStates.Length / 2)

            if shouldContinue then
                let next = random.Next(unvisitedNodes.Length)
                Some(fst unvisitedNodes.[next])
            else
                None

        let rec clickAround (isDirectPath: bool) (alreadyVisited: PageState<'a, 'b> list) (currentPath: PageState<'a, 'b> list) =
            task {
                match currentPath with
                | [ head ] ->
                    match nextNode alreadyVisited with
                    | None -> return head
                    | Some nextNode ->
                        let path = findPath head nextNode

                        if path.Length = 1 then
                            do! runActions reporter config path.Head
                            return path.Head
                        else
                            printPath config.Logger path
                            if isDirectPath
                            then return path.Head
                            else
                                return! clickAround false alreadyVisited path
                | head :: tail ->
                    do! 
                        currentPath
                        |> Seq.pairwise
                        |> Seq.find (fun (current, _) -> current = head)
                        |> performStateActions reporter config globalState

                    return! clickAround isDirectPath (head :: alreadyVisited) tail
            }

        let navigateDirectly = clickAround true

        task {
            let! finalNode = clickAround false [] [ startState ]

            match findExit config allStates with
            | None -> return ()
            | Some(exitNode, _) ->
                let t =
                    task {
                        let path = findPath finalNode exitNode

                        let! exitNode = navigateDirectly [] path
                        
                        let a =
                            exitNode.ExitActions 
                            |> Seq.sortBy (fun _ -> random.Next())
                            |> Seq.tryHead
                            |> Option.map (fun ea -> (ea exitNode.LocalState))
                            
                        match a with
                        | None -> return ()
                        | Some asd -> return! asd
                    }
                    
                return! t
        }
                

    let private baseScrutinize<'a, 'b> (reporter: IReporter<'a, 'b>) (config: ScrutinyConfig) (globalState: 'a) (startFn: 'a -> PageState<'a, 'b>) =
        task {
            config.Logger <| $"Scrutinizing system under test with seed: %i{config.Seed}"
            let startState = startFn globalState

            let allStates = Navigator.constructAdjacencyGraph startState globalState
            reporter.Start (allStates, startState)
            
            try
                if not config.MapOnly then
                    do! navigateStateMachine reporter config allStates globalState startState
            finally
                config.Logger $"Scrutiny Result written to: {config.ScrutinyResultFilePath}" 
                reporter.GenerateMap()

            return reporter.Finish()
        }

    let scrutinize<'a, 'b> config = 
        let reporter = Reporter<'a, 'b>(config.ScrutinyResultFilePath) :> IReporter<'a, 'b>
        baseScrutinize<'a, 'b> reporter config

    let page = PageBuilder()

    let scrutinizeWithDefaultConfig<'a, 'b> = scrutinize<'a, 'b> ScrutinyConfig.Default
