module NavigatorTests

open System
open Expecto
open Scrutiny
open Swensen.Unquote
open FsCheck
open Scrutiny.Scrutiny
open Scrutiny.Operators

let shuffle (r: Random) xs = xs |> Seq.sortBy (fun _ -> r.Next())

type GraphGen() =
    static member Graph(): Arbitrary<Graph<int>> =
        let rnd = Random()

        let genNodes: Gen<Graph<int>> =
            gen {
                let! size = Gen.choose(6, 15)
                let lowerSize = int <| (float size) * 1.5
                let upperSize = size * 2
                let! nodes =
                    gen {
                        return [0..size]
                               |> List.map (fun _ -> rnd.Next(1, 99))
                               |> List.distinct
                    }

                let! edges =
                    gen {
                        return ([lowerSize..upperSize] |> List.map (fun _ ->
                            nodes
                            |> shuffle (rnd)
                            |> Seq.take 2
                            |> Seq.pairwise
                            |> Seq.head
                        ))
                    }

                return (nodes, edges)
            }

        genNodes |> Arb.fromGen

type AdjacencyGraphGen() =
    static member Graph(): Arbitrary<AdjacencyGraph<int>> =
        let rnd = Random()
        let genNodes: Gen<AdjacencyGraph<int>> =
            gen {
                let! size = Gen.choose(6, 15)
                let! nodes =
                    gen {
                        return [0..size]
                               |> List.map (fun _ -> rnd.Next(1, 99))
                               |> List.distinct
                    }

                let! nodesWithEdges =
                    gen {
                        let nodeWithEdges =
                            nodes |> List.map (fun n ->
                                let edges =
                                    nodes
                                    |> shuffle (rnd)
                                    |> Seq.take (rnd.Next(nodes.Length - 1))
                                    |> Seq.except [n]
                                    |> Seq.toList

                                n, edges
                            )

                        return nodeWithEdges
                    }


                return nodesWithEdges
            }

        genNodes |> Arb.fromGen

let graphGenConfig =
    { FsCheckConfig.defaultConfig with
        arbitrary = [typeof<GraphGen>] }

let adjacencyGraphGenConfig =
    { FsCheckConfig.defaultConfig with
        arbitrary = [typeof<AdjacencyGraphGen>] }

module rec TestPages =
    let page1 = fun _ ->
        page {
            name "Page1"
            transition (ignore ==> page2)
        }

    let page2 = fun _ ->
        page {
            name "Page2"
            transition (ignore ==> page1)
            transition (ignore ==> page3)
        }

    let page3 = fun _ ->
        page {
            name "Page3"
            transition (ignore ==> page4)
            transition (ignore ==> page5)
        }

    let page4 = fun _ ->
        page {
            name "Page4"
            transition (ignore ==> page3)
            transition (ignore ==> page5)
        }

    let page5 = fun _ ->
        page {
            name "Page5"
            transition (ignore ==> page2)
            transition (ignore ==> page3)
        }

[<Tests>]
let navigatorTests =
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

            testPropertyWithConfig graphGenConfig "Nodes in both graphs should be equal" <| fun (g: Graph<int>) ->
                let adjacencyGraph = Navigator.graph2AdjacencyGraph g
                test <@ ((fst g) |> List.sort) = (adjacencyGraph |> List.map fst |> List.sort) @>

            testPropertyWithConfig graphGenConfig "Adjacency graph should contain same edge as graph" <| fun (g: Graph<int>) ->
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

        testList "Adjacency graph tests" [
            testPropertyWithConfig adjacencyGraphGenConfig "Nodes in both graphs should be equal" <| fun (ag: AdjacencyGraph<int>) ->

                let graph = Navigator.adjacencyGraph2Graph ag
                test <@ ag.Length = (fst graph).Length @>

            testPropertyWithConfig adjacencyGraphGenConfig "Should share an edge" <| fun (ag: AdjacencyGraph<int>) ->
                let graph = Navigator.adjacencyGraph2Graph ag

                let getNodeWithEdge (ag: AdjacencyGraph<int>) =
                    ag
                    |> List.filter (fun (_, edges) -> edges |> Seq.isEmpty |> not)
                    |> Seq.head
                    |> fun (node, edges) -> node, edges.Head

                let flippedEdge (node: int, edge: int) = (edge, node)

                test <@
                        let graphEdges = snd graph
                        let adjacencyGraphEdge = getNodeWithEdge ag
                        graphEdges |> List.contains adjacencyGraphEdge || graphEdges |> List.contains (flippedEdge adjacencyGraphEdge) @>
        ]

        testList "Construct Adjacency Graph" [
            Tests.test "Should construct graph" {
                let ag = Navigator.constructAdjacencyGraph (TestPages.page1 ()) ()

                test <@ ag |> List.length = 5 @>
            }
        ]

        testList "Shortest Path Function" (
            [ (1, 4, [1; 2; 3; 4])
              (1, 5, [1; 2; 3; 5])
              (4, 2, [4; 5; 2])
              (3, 2, [3; 5; 2])
              (1, 2, [1; 2])
            ]
            |> List.map (fun (startNode, endNode, path) ->
                Tests.test (sprintf "Should find a path from %i to %i" startNode endNode) {
                    let ag =
                        [
                            (1, [2])
                            (2, [1; 3])
                            (3, [4; 5])
                            (4, [3; 5])
                            (5, [2; 3])
                        ]
                    let findPath = Navigator.shortestPathFunction ag

                    test <@ findPath startNode endNode = path @>
                }
            )
        )
    ]
