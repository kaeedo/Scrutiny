namespace Scrutiny.CSharp

open Scrutiny
open System.Reflection

module ScrutinyCSharp =
    let private buildTransition toState (methodInfo: MethodInfo) constructed =
        let tranFn =
            if methodInfo.GetParameters().Length = 1
            then fun ls -> (methodInfo.Invoke(constructed, [|ls|]) |> ignore)
            else fun _ -> (methodInfo.Invoke(constructed, [||]) |> ignore)
            
        { Transition.TransitionFn = tranFn
          ToState = fun _ -> toState }


    let start<'globalState> (gs: 'globalState) startingPageState: unit = 
        let t = startingPageState.GetType()

        let pageStatesTypes = 
            seq {
                for t in t.Assembly.GetTypes() do
                    if t.GetCustomAttributes(typeof<PageStateAttribute>, true).Length > 0 then
                        yield t
            }
            |> Seq.toList

        let defs =
            pageStatesTypes
            |> List.map (fun pst ->
                let ps = 
                    { PageState.Name = pst.Name
                      LocalState = obj()
                      OnEnter = fun _ -> ()
                      OnExit = fun _ -> ()
                      ExitAction = None
                      Actions = []
                      Transitions = [] }
                ps, pst
            )

        let defs =
            defs 
            |> List.map (fun (ps, psType) ->
                let transitionsForPageState =
                    psType.GetMethods()
                    |> Seq.toList
                    |> List.filter (fun method ->
                        method.GetCustomAttributes(typeof<TransitionToAttribute>, true).Length > 0
                    )
                    // Extract anonymous fun
                    |> List.map (fun method ->
                        let transitionToAttr =
                            method.GetCustomAttributes(typeof<TransitionToAttribute>, true)
                            |> Seq.head
                            :?> TransitionToAttribute

                        let toState =
                            defs
                            |> Seq.find (fun (ps, _) ->
                                ps.Name = transitionToAttr.Name
                            )
                            |> fst

                        let constructedPageState =
                            let ctor = psType.GetConstructor([|typeof<'globalState>|])
                            ctor.Invoke([|gs|])

                        buildTransition toState method constructedPageState
                    )

                ps.Transitions <- transitionsForPageState
                ps
            )

        let starting =
            defs
            |> List.find (fun d -> d.Name = t.Name)
        
        Scrutiny.scrutinize ScrutinyConfig.Default (obj()) (fun _ -> starting)
        
