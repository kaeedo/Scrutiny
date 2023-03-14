namespace Scrutiny

open System.Collections.Generic
open System.Runtime.CompilerServices

module internal Navigator =
    [<assembly: InternalsVisibleTo("Scrutiny.Tests")>]
    do ()

    let constructAdjacencyGraph<'a> (startState: PageState<'a>) (globalState: 'a) : AdjacencyGraph<PageState<'a>> =
        let getTransitions node : PageState<'a> list =
            node.Transitions
            |> List.map (fun t -> t.Destination globalState)

        let mutable final = []
        let nodes2Visit = Queue<PageState<'a>>()
        nodes2Visit.Enqueue(startState)

        while nodes2Visit.Count > 0 do
            let currentNode = nodes2Visit.Dequeue()
            let neighbors = getTransitions currentNode
            final <- (currentNode, neighbors) :: final

            neighbors
            |> List.except (final |> List.map fst)
            |> List.iter (fun n -> nodes2Visit.Enqueue(n))

        final |> List.distinctBy (fst)

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
            if current.Equals(start) then
                start :: path
            else if previous.ContainsKey(current) then
                shortestPath (current :: path) previous.[current]
            else
                start :: path

        shortestPath []
