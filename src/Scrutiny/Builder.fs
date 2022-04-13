namespace Scrutiny

open System
open System.Runtime.CompilerServices
open System.Threading.Tasks

type PageBuilder() =
    member _.Yield _: PageState<'a, 'b> =
        { PageState.Name = ""
          LocalState = Unchecked.defaultof<'b>
          OnEnter = fun _ -> Task.FromResult()
          OnExit = fun _ -> ()
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
    member _.OnExit(state, handler): PageState<'a, 'b> = { state with OnExit = handler }

    [<CustomOperation("transition")>]
    member _.Transitions(state, handler): PageState<'a, 'b> = { state with Transitions = handler :: state.Transitions }

    [<CustomOperation("action")>]
    member _.Actions(state, handler, [<CallerMemberName>]?memberName: string, [<CallerLineNumber>]?lineNumber: int, [<CallerFilePath>]?filePath: string): PageState<'a, 'b> = 
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        { state with Actions = (callerInformation, handler) :: state.Actions }
        
    [<CustomOperation("exitAction")>]
    member _.ExitAction(state, handler): PageState<'a, 'b> =  { state with ExitActions = handler :: state.ExitActions }

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
        

    let private runActions (reporter: IReporter<'a, 'b>) (config: ScrutinyConfig) (state: PageState<'a, 'b>) =
        if config.ComprehensiveActions then
            state.Actions |> Seq.iter (fun (ci, a) -> 
                reporter.PushAction (buildActionName ci)
                a state.LocalState
            )
        else
            let random = Random(config.Seed)

            let amount =
                random.Next(if state.Actions.Length > 1 then state.Actions.Length else 1)
            state.Actions
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.take amount
            |> Seq.iter (fun (ci, a) -> 
                reporter.PushAction (buildActionName ci)
                a state.LocalState
            )

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
        let runActions = runActions reporter config
        // TODO Wrap functions in try function instead of try catching entire block
        try
            (task {
                do! current.OnEnter current.LocalState
            }).GetAwaiter().GetResult()
            runActions current
            current.OnExit current.LocalState
        with exn ->

            let message =
                sprintf "System under test failed scrutiny.
    To re-run this exact test, specify the seed in the config with the value: '%i'.
    The error occurred in state: '%s'
    The error that occurred is of type: '%A%s'" config.Seed current.Name exn Environment.NewLine

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

            transition.TransitionFn current.LocalState
        with exn ->
            let message =
                sprintf "System under test failed scrutiny.
    To re-run this exact test, specify the seed in the config with the value: '%i'.
    The error occurred in state: '%s'
    The error that occurred is of type: '%A%s'" config.Seed current.Name exn Environment.NewLine

            let exn = ScrutinyException(message, exn)

            reporter.OnError (Transition (current.Name, next.Name, convertException exn))

            raise <| exn

    let private findExit (config: ScrutinyConfig) (allStates: AdjacencyGraph<PageState<'a, 'b>>) =
        let random = Random(config.Seed)

        let exitNode =
            allStates
            |> Seq.filter (fun (node, _) -> node.ExitActions |> Seq.isEmpty |> not)
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.tryHead

        exitNode

    let private baseScrutinize<'a, 'b> (reporter: IReporter<'a, 'b>) (config: ScrutinyConfig) (globalState: 'a) (startFn: 'a -> PageState<'a, 'b>) =
        config.Logger <| sprintf "Scrutinizing system under test with seed: %i" config.Seed
        let startState = startFn globalState
        let runActions = runActions reporter config

        let allStates = Navigator.constructAdjacencyGraph startState globalState
        reporter.Start (allStates, startState)
        
        try
            if not config.MapOnly then
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

                let rec clickAround
                        (isExitPath: bool)
                        (alreadyVisited: PageState<'a, 'b> list)
                        (currentPath: PageState<'a, 'b> list)
                    =
                    match currentPath with
                    | [ head ] ->
                        match nextNode alreadyVisited with
                        | None -> head
                        | Some nextNode ->
                            let path = findPath head nextNode

                            if path.Length = 1 then
                                runActions path.Head
                                path.Head
                            else
                                printPath config.Logger path
                                if isExitPath then path.Head else clickAround false alreadyVisited path
                    | head :: tail ->
                        currentPath
                        |> Seq.pairwise
                        |> Seq.find (fun (current, _) -> current = head)
                        |> performStateActions reporter config globalState

                        clickAround false (head :: alreadyVisited) tail

                let navigateDirectly = clickAround true

                let finalNode = clickAround false [] [ startState ]

                match findExit config allStates with
                | None -> ()
                | Some(exitNode, _) ->
                    let path = findPath finalNode exitNode

                    let exitNode = navigateDirectly [] path
                    exitNode.ExitActions 
                    |> Seq.sortBy (fun _ -> random.Next())
                    |> Seq.tryHead
                    |> Option.iter (fun ea -> ea exitNode.LocalState)
        finally
            config.Logger $"Scrutiny Result written to: {config.ScrutinyResultFilePath}" 
            reporter.GenerateMap()

        reporter.Finish()

    let scrutinize<'a, 'b> config = 
        let reporter = Reporter<'a, 'b>(config.ScrutinyResultFilePath) :> IReporter<'a, 'b>
        baseScrutinize<'a, 'b> reporter config

    let page = PageBuilder()

    let scrutinizeWithDefaultConfig<'a, 'b> = scrutinize<'a, 'b> ScrutinyConfig.Default
