namespace Scrutiny.CSharp

open Scrutiny
open System
open System.Threading.Tasks
open System.Reflection

module ScrutinyCSharp =
    let private constructPageState<'globalState> (gs: 'globalState) (psType: Type) =
        let ctor = psType.GetConstructor([|typeof<'globalState>|])
        ctor.Invoke([|gs|])

    let private getMethodsWithAttribute attr constructedPageState  =
        constructedPageState.GetType().GetMethods()
        |> Seq.toList
        |> List.filter (fun method ->
            method.GetCustomAttributes(attr, true).Length > 0
        )

    let private buildFn (m: MethodInfo) constructed =
        if m.ReturnType = typeof<Task>
        then (fun _ -> 
            (m.Invoke(constructed, [||]) :?> Task 
             |> Async.AwaitTask 
             |> Async.RunSynchronously)
        )
        else fun _ -> (m.Invoke(constructed, [||]) |> ignore)

    let private buildTransition constructedPageState defs =
        getMethodsWithAttribute typeof<TransitionToAttribute> constructedPageState
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

            { Transition.TransitionFn = buildFn method constructedPageState
              ToState = fun _ -> toState }
        )

    let private buildFnWithAttribute attr constructedPageState =
        match getMethodsWithAttribute attr constructedPageState |> Seq.tryHead with
        | None -> ignore
        | Some m -> buildFn m constructedPageState

    let private buildActions constructedPageState =
        getMethodsWithAttribute typeof<ActionAttribute> constructedPageState
        |> List.map (fun m -> buildFn m constructedPageState)

    let private buildExitAction constructedPageState =
        getMethodsWithAttribute typeof<ExitActionAttribute> constructedPageState |> Seq.tryHead
        |> Option.bind (fun m ->
            Some (buildFn m constructedPageState)
        )

    let start<'globalState> (config: Configuration) (gs: 'globalState) startingPageState: unit = 
        let config = config.ToScrutiynConfig()
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
                let constructed = constructPageState gs pst
                let ps = 
                    { PageState.Name = pst.Name
                      LocalState = obj()
                      OnEnter = buildFnWithAttribute typeof<OnEnterAttribute> constructed
                      OnExit = buildFnWithAttribute typeof<OnExitAttribute> constructed
                      ExitAction = buildExitAction constructed
                      Actions = buildActions constructed
                      Transitions = [] }
                ps, constructed
            )

        let defs =
            defs 
            |> List.map (fun (ps, constructed) ->
                let transitionsForPageState = buildTransition constructed defs

                ps.Transitions <- transitionsForPageState
                ps
            )

        let starting =
            defs
            |> List.find (fun d -> d.Name = t.Name)
        
        Scrutiny.scrutinize config (obj()) (fun _ -> starting)

    let startWithDefaultConfig<'globalState> (gs: 'globalState) startingPageState =
        start (Configuration.FromScrutinyConfig(ScrutinyConfig.Default)) gs startingPageState
        
