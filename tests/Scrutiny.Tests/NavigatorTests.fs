module NavigatorTests

open System
open Expecto
open Scrutiny
open Swensen.Unquote
open FsCheck

let shuffle (r: Random) xs = xs |> Seq.sortBy (fun _ -> r.Next())

type AdjacencyGraphGen() =
    static member Graph() : Arbitrary<AdjacencyGraph<int>> =
        let rnd = Random()

        let genNodes: Gen<AdjacencyGraph<int>> =
            gen {
                let! size = Gen.choose (6, 15)

                let! nodes =
                    gen {
                        return
                            [ 0..size ]
                            |> List.map (fun _ -> rnd.Next(1, 99))
                            |> List.distinct
                    }

                let! nodesWithEdges =
                    gen {
                        let nodeWithEdges =
                            nodes
                            |> List.map (fun n ->
                                let edges =
                                    nodes
                                    |> shuffle rnd
                                    |> Seq.take (rnd.Next(nodes.Length - 1))
                                    |> Seq.except [ n ]
                                    |> Seq.toList

                                n, edges)

                        return nodeWithEdges
                    }


                return nodesWithEdges
            }

        genNodes |> Arb.fromGen

let adjacencyGraphGenConfig =
    { FsCheckConfig.defaultConfig with arbitrary = [ typeof<AdjacencyGraphGen> ] }

module rec TestPages =
    let ignoreTask _ = task { return () }

    let page1 =
        fun _ ->
            page {
                name "Page1"

                transition {
                    via ignoreTask
                    destination page2
                }
            }

    let page2 =
        fun _ ->
            page {
                name "Page2"

                transition {
                    via ignoreTask
                    destination page1
                }

                transition {
                    via ignoreTask
                    destination page3
                }
            }

    let page3 =
        fun _ ->
            page {
                name "Page3"

                transition {
                    via ignoreTask
                    destination page4
                }

                transition {
                    via ignoreTask
                    destination page5
                }
            }

    let page4 =
        fun _ ->
            page {
                name "Page4"

                transition {
                    via ignoreTask
                    destination page3
                }

                transition {
                    via ignoreTask
                    destination page5
                }
            }

    let page5 =
        fun _ ->
            page {
                name "Page5"

                transition {
                    via ignoreTask
                    destination page2
                }

                transition {
                    via ignoreTask
                    destination page3
                }
            }

[<Tests>]
let navigatorTests =
    testList
        "Navigator Tests"
        [ testList
              "Construct Adjacency Graph"
              [ Tests.test "Should construct graph" {
                    let ag = Navigator.constructAdjacencyGraph (TestPages.page1 ()) ()

                    test <@ ag |> List.length = 5 @>
                } ]

          testList
              "Shortest Path Function"
              ([ (1, 4, [ 1; 2; 3; 4 ])
                 (1, 5, [ 1; 2; 3; 5 ])
                 (4, 2, [ 4; 5; 2 ])
                 (3, 2, [ 3; 5; 2 ])
                 (1, 2, [ 1; 2 ]) ]
               |> List.map (fun (startNode, endNode, path) ->
                   Tests.test $"Should find a path from %i{startNode} to %i{endNode}" {
                       let ag =
                           [ (1, [ 2 ])
                             (2, [ 1; 3 ])
                             (3, [ 4; 5 ])
                             (4, [ 3; 5 ])
                             (5, [ 2; 3 ]) ]

                       let findPath = Navigator.shortestPathFunction ag

                       test <@ findPath startNode endNode = path @>
                   })) ]
