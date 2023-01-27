namespace Scrutiny.CSharp

open System
open System.IO
open Scrutiny
open Microsoft.FSharp.Core

type Configuration() =
    member val Seed = Environment.TickCount with get, set
    member val MapOnly = false with get, set
    member val ComprehensiveActions = true with get, set
    member val ComprehensiveStates = true with get, set

    member val ScrutinyResultFilePath =
        Directory.GetCurrentDirectory()
        + "/ScrutinyResult.html" with get, set

    member val Logger = System.Action<string>(fun s -> printfn "%s" s) with get, set

    member x.ToScrutinyConfig() =
        { ScrutinyConfig.Seed = x.Seed
          MapOnly = x.MapOnly
          ComprehensiveActions = x.ComprehensiveActions
          ComprehensiveStates = x.ComprehensiveStates
          ScrutinyResultFilePath = x.ScrutinyResultFilePath
          Logger = (FuncConvert.FromAction<string>) (x.Logger) }

    static member FromScrutinyConfig config =
        Configuration(
            Seed = config.Seed,
            MapOnly = config.MapOnly,
            ComprehensiveActions = config.ComprehensiveActions,
            ComprehensiveStates = config.ComprehensiveStates,
            ScrutinyResultFilePath = config.ScrutinyResultFilePath,
            Logger = System.Action<string>(config.Logger)
        )

type Step internal (name: string, actions: string seq) =
    member val PageStateName = name
    member val PerformedActions = actions

type ScrutinizedStates internal (graph: AdjacencyGraph<PageState<_>>, steps: Step<_> seq) =
    let steps =
        steps
        |> Seq.map (fun s -> Step(s.PageState.Name, s.Actions))

    let graph =
        graph
        |> Seq.map (fun (ps, psList) ->
            {| PageState = ps
               PossibleTransitions = psList |> Seq.ofList |})

    member val Graph = graph
    member val Steps = steps
