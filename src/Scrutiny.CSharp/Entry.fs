namespace Scrutiny.CSharp

open Scrutiny
open System.Collections.Generic
open System

module ScrutinyCSharp =
    let private pageStateDefinitions: Dictionary<string, PageState<obj, obj> * Type> = Dictionary<string, PageState<obj, obj> * Type>()
    
    let start<'globalState> (gs: 'globalState) startingPageState: unit = 
        let t = startingPageState.GetType()
        let ass = t.Assembly

        let pageStatesTypes = 
            seq {
                for t in ass.GetTypes() do
                    if t.GetCustomAttributes(typeof<PageStateAttribute>, true).Length > 0 then
                        yield t
            }

        pageStatesTypes
        |> Seq.iter (fun pst ->
            let ps = 
                { PageState.Name = pst.Name
                  LocalState = obj()
                  OnEnter = fun _ -> ()
                  OnExit = fun _ -> ()
                  ExitAction = None
                  Actions = []
                  Transitions = [] }
            pageStateDefinitions.[pst.Name] <- (ps, pst)
        )

        for KeyValue(name, (ps, t)) in pageStateDefinitions do
            let methods = t.GetMethods()
            let attrs = 
                methods
                |> Seq.filter (fun m ->
                    m.GetCustomAttributes(typeof<TransitionToAttribute>, false).Length > 0
                )
                |> List.ofSeq

            let next =
                attrs
                |> List.map (fun m ->
                    let foo =
                        m.GetCustomAttributes(typeof<TransitionToAttribute>, true).[0] :?> TransitionToAttribute
                    let toState = pageStateDefinitions.[foo.Name] |> fst

                    let ctor = t.GetConstructors().[0]
                    let constructed = ctor.Invoke([|gs|])

                    // from the wrong pageState
                    let tranFn =
                        if m.GetParameters().Length = 1
                        then fun ls -> (m.Invoke(constructed, [|ls|]) |> ignore)
                        else fun _ -> (m.Invoke(constructed, [||]) |> ignore)
                        
                    let trans = 
                        { Transition.TransitionFn = tranFn
                          ToState = fun _ -> toState }
                    foo.Name, trans
                )

            next
            |> List.iter (fun (namee, t) ->
                let ps = pageStateDefinitions.[name] |> fst
                ps.Transitions <- t :: ps.Transitions
            )

        let starting = 
            pageStateDefinitions
            |> Seq.find (fun (KeyValue(name, _)) -> name = t.Name)
        
        Scrutiny.scrutinize { ScrutinyConfig.Default with Seed = -1752473656 } (obj()) (fun _ -> starting.Value |> fst)
        
