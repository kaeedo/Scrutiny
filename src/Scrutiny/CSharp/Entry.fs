namespace Scrutiny.CSharp

open Scrutiny
open System
open System.Threading.Tasks
open System.Reflection

module internal ScrutinyCSharp =
    let private constructPageState<'globalState> (gs: 'globalState) (psType: Type) =
        let constructed = 
            psType.GetConstructors()
            |> Seq.tryFind (fun ctor ->
                ctor.GetParameters() |> Array.isEmpty |> not
            )
            |> function
               | Some ctor -> ctor.Invoke([|gs|])
               | None -> psType.GetConstructor([||]).Invoke([||])

        let invalidMethods =
            constructed.GetType().GetTypeInfo().DeclaredMethods
            |> Seq.filter (fun m ->
                let hasTransitions = m.GetCustomAttributes<TransitionToAttribute>(true) |> Seq.isEmpty |> not
                let hasOnEnters = m.GetCustomAttributes<OnEnterAttribute>(true) |> Seq.isEmpty |> not
                let hasOnExits = m.GetCustomAttributes<OnExitAttribute>(true) |> Seq.isEmpty |> not
                let hasActions = m.GetCustomAttributes<ActionAttribute>(true) |> Seq.isEmpty |> not
                let hasExitActions = m.GetCustomAttributes<ExitActionAttribute>(true) |> Seq.isEmpty |> not

                hasTransitions || hasOnEnters || hasOnExits || hasActions || hasExitActions
            )
            |> Seq.filter (fun m ->
                m.GetParameters() |> Array.length > 0
            )

        constructed, invalidMethods

    let private getMethodsWithAttribute attr constructedPageState  =
        constructedPageState.GetType().GetMethods()
        |> Seq.toList
        |> List.filter (fun method ->
            method.GetCustomAttributes(attr, true).Length > 0
        )

    let private buildMethod (m: MethodInfo) constructed =
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
                method.GetCustomAttribute<TransitionToAttribute>(true)

            let toState =
                defs
                |> Seq.find (fun (ps, _) ->
                    ps.Name = transitionToAttr.Name
                )
                |> fst

            { Transition.TransitionFn = buildMethod method constructedPageState
              ToState = fun _ -> toState }
        )

    let private buildMethodWithAttribute attr constructedPageState =
        if getMethodsWithAttribute attr constructedPageState |> Seq.length > 1
        then raise <| ScrutinyException($"Only one \"{attr.Name}\" per PageState. Check \"{constructedPageState.GetType().Name}\" for duplicate attribute usage.", null)

        match getMethodsWithAttribute attr constructedPageState |> Seq.tryHead with
        | None -> ignore
        | Some m -> buildMethod m constructedPageState

    let start<'startState> gs (config: Configuration): unit = 
        let config = config.ToScrutiynConfig()
        let t = typeof<'startState> 

        let pageStatesTypes = 
            seq {
                for t in t.Assembly.GetTypes() do
                    if t.GetCustomAttributes<PageStateAttribute>(true) |> Seq.isEmpty |> not then
                        yield t
            }
            |> Seq.toList

        let defs =
            pageStatesTypes
            |> Seq.map (constructPageState gs)

        let invalidMethods = 
            defs
            |> Seq.filter (snd >> Seq.isEmpty >> not)
            |> Seq.map (snd)
            |> Seq.concat
            |> Seq.toList

        match invalidMethods with
        | [] -> ()
        | [m] ->
            let containingClass = m.ReflectedType.Name
            raise <| ScrutinyException($"\"{containingClass}.{m.Name}\" is not allowed to have any parameters.", null)
        | m ->
            let methods = 
                m 
                |> List.map (fun m -> 
                    let containingClass = m.ReflectedType.Name
                    $"{containingClass}.{m.Name}"
                )
            let methods = String.Join("\n", methods)
            raise <| ScrutinyException($"Methods with scrutiny attributes are not allowed to have parameters. Following methods are invalid: {methods}", null)

        let defs =
            defs
            |> Seq.map (fst)
            |> Seq.map (fun constructed ->
                let ps = 
                    { PageState.Name = constructed.GetType().Name
                      LocalState = obj()
                      OnEnter = buildMethodWithAttribute typeof<OnEnterAttribute> constructed
                      OnExit = buildMethodWithAttribute typeof<OnExitAttribute> constructed
                      ExitActions = getMethodsWithAttribute typeof<ExitActionAttribute> constructed |> List.map (fun m -> buildMethod m constructed)
                      Actions = getMethodsWithAttribute typeof<ActionAttribute> constructed |> List.map (fun m -> buildMethod m constructed)
                      Transitions = [] }
                ps, constructed
            )
            |> List.ofSeq

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
        
[<AbstractClass; Sealed>]
type Scrutinize private () =
    static member Start<'startState> (globalState) = 
        ScrutinyCSharp.start<'startState> globalState (Configuration.FromScrutinyConfig(ScrutinyConfig.Default))

    static member Start<'startState> (globalState, configuration) = 
        ScrutinyCSharp.start<'startState> globalState configuration