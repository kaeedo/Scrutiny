namespace Scrutiny.CSharp

open Scrutiny
open System
open System.Threading.Tasks

module ScrutinyCSharp =
    let private constructPageState<'globalState> (gs: 'globalState) (psType: Type) =
        let ctor = psType.GetConstructor([|typeof<'globalState>|])
        ctor.Invoke([|gs|])

    let private getMethodsWithAttribute attr (psType: Type)  =
        psType.GetMethods()
        |> Seq.toList
        |> List.filter (fun method ->
            method.GetCustomAttributes(attr, true).Length > 0
        )

    let private buildTransition<'globalState> (gs: 'globalState) defs (psType: Type)  =
        getMethodsWithAttribute typeof<TransitionToAttribute> psType
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

            let constructedPageState = constructPageState gs psType

            let tranFn =
                if method.GetParameters().Length = 1
                then fun ls -> (method.Invoke(constructedPageState, [|ls|]) |> ignore)
                else fun _ -> (method.Invoke(constructedPageState, [||]) |> ignore)
                
            { Transition.TransitionFn = tranFn
              ToState = fun _ -> toState }
        )

    let buildFnWithAttribute<'globalState> attr (gs: 'globalState) (psType) =
        match getMethodsWithAttribute attr psType |> Seq.tryHead with
        | None -> ignore
        | Some m ->
            let constructedPageState = constructPageState gs psType

            if m.ReturnType = typeof<Task>
            then 
                if m.GetParameters().Length = 1
                then fun ls -> (m.Invoke(constructedPageState, [|ls|]) :?> Task |> Async.AwaitTask |> Async.RunSynchronously |> ignore)
                else fun _ -> (m.Invoke(constructedPageState, [||]) :?> Task |> Async.AwaitTask |> Async.RunSynchronously |> ignore)
            else
                if m.GetParameters().Length = 1
                then fun ls -> (m.Invoke(constructedPageState, [|ls|]) |> ignore)
                else fun _ -> (m.Invoke(constructedPageState, [||]) |> ignore)

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
                      OnEnter = buildFnWithAttribute typeof<OnEnterAttribute> gs pst
                      OnExit = buildFnWithAttribute typeof<OnExitAttribute> gs pst
                      ExitAction = None
                      Actions = []
                      Transitions = [] }
                ps, pst
            )

        let defs =
            defs 
            |> List.map (fun (ps, psType) ->
                let transitionsForPageState = buildTransition gs defs psType

                ps.Transitions <- transitionsForPageState
                ps
            )

        let starting =
            defs
            |> List.find (fun d -> d.Name = t.Name)
        
        Scrutiny.scrutinize ScrutinyConfig.Default (obj()) (fun _ -> starting)
        
