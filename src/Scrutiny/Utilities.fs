namespace Scrutiny

open System
open System.Threading.Tasks

module Utilities =
    let simpleTraverse (tasks: (unit -> Task<unit>) list) : unit -> Task<unit> =
        match tasks with
        | [] -> fun () -> Task.FromResult(())
        | x ->
            x
            |> List.reduce (fun accumulator element ->
                fun () ->
                    task {
                        do! accumulator ()
                        return! element ()
                    })

    [<RequireQualifiedAccess>]
    module Map =
        let randomItem (random: Random) (map: Map<_, _>) =
            map.Keys
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.head

        let weightedRandomItem (random: Random) (map: Map<_, int>) =
            let weightedKeys =
                seq {
                    for kvp in map do
                        match kvp.Value with
                        | 0 -> yield! [ kvp.Key; kvp.Key; kvp.Key ]
                        | 1 -> yield! [ kvp.Key; kvp.Key ]
                        | _ -> yield! [ kvp.Key ]
                }

            weightedKeys
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.head

        let tryRandomItemBy (random: Random) predicate (map: Map<_, _>) =
            let map = map |> Map.filter predicate

            map.Keys
            |> Seq.sortBy (fun _ -> random.Next())
            |> Seq.tryHead
