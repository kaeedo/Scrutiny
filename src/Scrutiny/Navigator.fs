namespace Scrutiny

//http://www.fssnip.net/av/title/NinetyNine-F-Problems-Problems-80-89-Graphs

type Edge<'a> = 'a * 'a

type Graph<'a> = 'a list * Edge<'a> list

type Node<'a> = 'a * 'a list

type AdjacencyGraph<'a> = 'a Node list

module internal Navigator =
    let graph2AdjacencyGraph ((ns, es) : 'a Graph) : 'a AdjacencyGraph = 
        let nodeMap = ns |> List.map(fun n -> n, []) |> Map.ofList
        (nodeMap,es) 
        ||> List.fold(fun map (a,b) -> map |> Map.add a (b::map.[a]) |> Map.add b (a::map.[b]))
        |> Map.toList
    
    let adjacencyGraph2Graph (ns : 'a AdjacencyGraph) : 'a Graph= 
        let sort ((a,b) as e) = if a > b then (b, a) else e
        let nodes = ns |> List.map fst
        let edges = 
            (Set.empty, ns) 
            ||> List.fold(fun set (a,ns) -> (set, ns) ||> List.fold(fun s b -> s |> Set.add (sort (a,b))) ) 
            |> Set.toSeq 
            |> Seq.sort 
            |> Seq.toList
        (nodes, edges)


    let constructAdjacencyGraph (startState: PageState) =
        let getTransitions node : PageState list= 
            node.Transitions
            |> List.map (fun t -> (snd t)())

        let startTransitionNodes = getTransitions startState

        let addTransitions (node: (PageState * (PageState list))) (transitions: PageState list) =
            let rec buildNode (currentNode: (PageState * (PageState list))) (possibleTransitions: PageState list) =
                match possibleTransitions with
                | [] -> currentNode
                | head :: tail ->
                    if (snd currentNode) |> List.exists (fun cn -> cn.Name = head.Name)
                    then buildNode currentNode tail
                    else 
                        let (currentPageState, currentTransitions) = currentNode
                        buildNode (currentPageState, head :: currentTransitions) tail

            buildNode node transitions

        (*let mutable graph: AdjacencyGraph<PageState> = []

        graph <- (startState, getTransitions startState) :: graph
        for t in startState.Transitions do
            let transitionedState = (snd t)()
            if not (graph |> List.exists (fun g -> (fst g).Name = transitionedState.Name))
            then 
                graph <- (transitionedState, getTransitions transitionedState) :: graph

        let secondPhase = graph |> List.collect (snd)*)

        ////////////////////////
        (*
        final = []
        nodes2Visit = some kind of queue
        while nodes2Visit has any
            currentNode = nodes2Visit.dequeue
            finals.push(currentNode, currentNode.neighbors)

            let neighborsNotInFinal = nieghbors not in final
            nodes2Visit.AddRange(neighborsNotInFinal)
        *)
        ////////////////////////

        let mutable graph: Map<PageState, (PageState list)> = Map.empty

        graph

    let g: Graph<char> = (['b';'c';'d';'f';'g';'h';'k'],[('b','c');('b','f');('c','f');('f','k');('g','h')])
    let ga: AdjacencyGraph<char> = [('b',['c'; 'f']); ('c',['b'; 'f']); ('d',[]); ('f',['b'; 'c'; 'k']); ('g',['h']); ('h',['g']); ('k',['f'])]

