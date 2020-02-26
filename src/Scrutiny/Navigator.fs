namespace Scrutiny

open System.Collections.Generic
open System

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

    let createPath (graph: AdjacencyGraph<PageState>) from until =
        let path = Queue<PageState>()

        let visitedPageStates = 
            graph
            |> List.map (fun n ->
                (fst n), false
            )
            |> dict
            
        let rec traverse (pageState: PageState) (visisted: IDictionary<PageState, bool>) =
            visisted.[pageState] <- true
            path.Enqueue(pageState)
            let neighbors = 
                graph 
                |> List.find (fun (n, _) -> n = pageState) 
                |> snd
            
            //if pageState = until 
            //then path
            //else traverse 
            1
        1

    //void DFSUtil(int v, bool[] visited) 
    //{ 
    //    // Mark the current node as visited 
    //    // and print it  
    //    visited[v] = true; 
    //    Console.Write(v + " "); 
  
    //    // Recur for all the vertices  
    //    // adjacent to this vertex  
    //    List<int> vList = adj[v]; 
    //    foreach (var n in vList) 
    //    { 
    //        if (!visited[n]) 
    //            DFSUtil(n, visited); 
    //    } 
    //} 


    let constructAdjacencyGraph (startState: PageState) : AdjacencyGraph<PageState> =
        let getTransitions node : PageState list = 
            node.Transitions
            |> List.map (fun t -> (snd t)())

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

    let g: Graph<char> = (['b';'c';'d';'f';'g';'h';'k'],[('b','c');('b','f');('c','f');('f','k');('g','h')])
    let ga: AdjacencyGraph<char> = [('b',['c'; 'f']); ('c',['b'; 'f']); ('d',[]); ('f',['b'; 'c'; 'k']); ('g',['h']); ('h',['g']); ('k',['f'])]

