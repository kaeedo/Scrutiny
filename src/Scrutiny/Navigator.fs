namespace Scrutiny

open System.Collections.Generic
open System

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

    // TODO: Refactor this to recursion?
    let constructAdjacencyGraph (startState: PageState) : AdjacencyGraph<PageState> =
        let getTransitions node : PageState list = 
            node.Transitions
            |> List.map (fun t -> t.ToState())

        let mutable final = []
        let nodes2Visit = Queue<PageState>()
        nodes2Visit.Enqueue(startState)

        while nodes2Visit.Count > 0 do
            let currentNode = nodes2Visit.Dequeue()
            let neighbors = getTransitions currentNode
            final <- (currentNode, neighbors) :: final

            neighbors
            |> List.except (final |> List.map fst)
            |> List.iter (fun n ->
                nodes2Visit.Enqueue(n)
            )

        final

    let shortestPathFunction (graph: AdjacencyGraph<PageState>) (start: PageState) =
        let previous = new Dictionary<PageState, PageState>()

        let queue = new Queue<PageState>()
        queue.Enqueue(start)

        while (queue.Count > 0) do
            let vertex = queue.Dequeue()
            let neighbors = 
                graph
                |> Seq.find (fun (node, _) -> node = vertex)
                |> snd
            for neighbor in neighbors do
                if not (previous.ContainsKey(neighbor))
                then 
                    previous.[neighbor] <- vertex
                    queue.Enqueue(neighbor)

        // TODO: Refactor this to recursion?
        let shortestPath (v: PageState) =
            let mutable path: PageState list = []
            let mutable current = v
            while not (current.Equals(start)) do
                path <- path @ [current]
                current <- previous.[current]

            path <- path @ [start]
            path |> List.rev

        shortestPath

