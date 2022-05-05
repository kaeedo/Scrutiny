namespace Scrutiny

open System.Collections.Generic
open System.Runtime.CompilerServices;

module internal Navigator =
    [<assembly: InternalsVisibleTo("Scrutiny.Tests")>]
    do()

    let graph2AdjacencyGraph ((ns, es): 'a Graph): 'a AdjacencyGraph =
        let nodeMap =
            ns
            |> List.map (fun n -> n, [])
            |> Map.ofList
        (nodeMap, es)
        ||> List.fold (fun map (a, b) ->
                map
                |> Map.add a (b :: map.[a])
                |> Map.add b (a :: map.[b]))
        |> Map.toList

    let adjacencyGraph2Graph (ns: 'a AdjacencyGraph): 'a Graph =
        let sort ((a, b) as e) =
            if a > b then (b, a) else e

        let nodes = ns |> List.map fst

        let edges =
            (Set.empty, ns)
            ||> List.fold (fun set (a, ns) -> (set, ns) ||> List.fold (fun s b -> s |> Set.add (sort (a, b))))
            |> Set.toSeq
            |> Seq.sort
            |> Seq.toList
        (nodes, edges)

    // TODO: Refactor this to recursion?
    let constructAdjacencyGraph<'a, 'b>
        (startState: PageState<'a, 'b>)
        (globalState: 'a)
        : AdjacencyGraph<PageState<'a, 'b>> =
        let getTransitions node: PageState<'a, 'b> list = node.Transitions |> List.map (fun t -> t.ToState globalState)

        let mutable final = []
        let nodes2Visit = Queue<PageState<'a, 'b>>()
        nodes2Visit.Enqueue(startState)

        while nodes2Visit.Count > 0 do
            let currentNode = nodes2Visit.Dequeue()
            let neighbors = getTransitions currentNode
            final <- (currentNode, neighbors) :: final

            neighbors
            |> List.except (final |> List.map fst)
            |> List.iter (fun n -> nodes2Visit.Enqueue(n))

        final

    let shortestPathFunction (graph: AdjacencyGraph<'a>) (start: 'a) =
        let previous = new Dictionary<'a, 'a>()

        let queue = new Queue<'a>()
        queue.Enqueue(start)

        while (queue.Count > 0) do
            let vertex = queue.Dequeue()

            let neighbors =
                graph
                |> Seq.find (fun (node, _) -> node = vertex)
                |> snd
            for neighbor in neighbors do
                if not (previous.ContainsKey(neighbor)) then
                    previous.[neighbor] <- vertex
                    queue.Enqueue(neighbor)

        let rec shortestPath (path: 'a list) (current: 'a) =
            if current.Equals(start) then start :: path
            else if previous.ContainsKey(current) then shortestPath (current :: path) previous.[current]
            else start :: path

        shortestPath []
