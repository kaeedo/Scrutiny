module Tests

open System
open Expecto
open Scrutiny
open Swensen.Unquote
open FsCheck

let shuffle (r: Random) xs = xs |> Seq.sortBy (fun _ -> r.Next())

type GraphGen() =
    static member Graph(): Arbitrary<Graph<int>> =
        let genNodes: Gen<Graph<int>> =
            gen {
                let! size = Gen.choose(6, 15)
                let lowerSize = int <| (float size) * 1.5
                let upperSize = size * 2
                let! nodes = Gen.listOfLength size (Gen.choose (1, 99))

                let! edges =
                    gen {
                        return ([lowerSize..upperSize] |> List.map (fun _ ->
                            nodes
                            |> shuffle (Random())
                            |> Seq.take 2
                            |> Seq.pairwise
                            |> Seq.head
                        ))
                    }

                return (nodes, edges)
            }

        genNodes |> Arb.fromGen

let config =
    { FsCheckConfig.defaultConfig with
        arbitrary = [typeof<GraphGen>] }
[<Tests>]
let tests =
    testList "Navigator Tests" [

        testList "Graph to adjacency graph" [
            Tests.test "Should construct adjacency graph of appropriate length" {
                let graph =
                    ([1; 2; 3; 4; 5; 6; 7], [(1, 2); (1, 4); (2, 5); (6, 7); (4, 5)])

                let adjacencyGraph = Navigator.graph2AdjacencyGraph graph
                test <@ adjacencyGraph.Length = 7 @>
            }

            Tests.test "Should construct adjacency graph that contains specific element" {
                let graph =
                    ([1; 2; 3; 4; 5; 6; 7], [(1, 2); (1, 4); (2, 5); (6, 7); (4, 5)])

                let adjacencyGraph = Navigator.graph2AdjacencyGraph graph
                test <@ adjacencyGraph |> Seq.exists (fun ag -> ag = (4, [5; 1])) @>
            }

            // TODO write better generator
            ptestPropertyWithConfig config "Amount of nodes should be equal" <| fun (g: Graph<int>) ->
                let adjacencyGraph = Navigator.graph2AdjacencyGraph g
                test <@ (fst g).Length = adjacencyGraph.Length @>

            testPropertyWithConfig config "Adjacency graph should contain same edge as graph" <| fun (g: Graph<int>) ->
                let adjacencyGraph: AdjacencyGraph<int> = Navigator.graph2AdjacencyGraph g

                let getNodeWithEdge (ag: AdjacencyGraph<int>) =
                    ag
                    |> List.filter (fun (_, edges) -> edges |> Seq.isEmpty |> not)
                    |> Seq.head
                    |> fun (node, edges) -> node, edges.Head

                let flippedEdge (node: int, edge: int) = (edge, node)

                test <@
                        let graphEdges = snd g
                        let adjacencyGraphEdge = getNodeWithEdge adjacencyGraph
                        graphEdges |> List.contains adjacencyGraphEdge || graphEdges |> List.contains (flippedEdge adjacencyGraphEdge) @>
        ]
    ]
