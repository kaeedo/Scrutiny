namespace Scrutiny

open System
open System.Threading.Tasks

//type RequiredAction = RequiredAction of string

type TransitionBuilder() =
    member inline _.Yield(()) =
        { Transition.DependantActions = []
          TransitionFn = fun _ -> Task.FromResult()
          ToState = fun _ -> Unchecked.defaultof<PageState<'a, 'b>> }

    [<CustomOperation("dependantActions")>]
    member inline _.DependantActions(previous, actions) : Transition<'a, 'b> =
        { previous with DependantActions = actions }


// https://github.com/sleepyfran/sharp-point
// https://sleepyfran.github.io/blog/posts/fsharp/ce-in-fsharp/
[<RequireQualifiedAccess>]
type PageStateProperties<'a, 'b> =
    | Name of string
    | Transition of Transition<'a, 'b>

type Page2Builder() =
    member inline _.Yield(()) = ()

    member inline _.Yield(transition: Transition<'a, 'b>) =
        PageStateProperties.Transition transition

    member inline _.Delay(f: unit -> PageStateProperties<'a, 'b> list) = f ()
    member inline _.Delay(f: unit -> PageStateProperties<'a, 'b>) = [ f () ]

    member inline _.Combine(newProp: PageStateProperties<'a, 'b>, previousProp: PageStateProperties<'a, 'b> list) =
        newProp :: previousProp

    member inline x.Run(props: PageStateProperties<'a, 'b> list) =
        props
        |> List.fold
            (fun ps prop ->
                match prop with
                | PageStateProperties.Name name -> { ps with Name = name }
                | PageStateProperties.Transition transition -> { ps with Transitions = transition :: ps.Transitions })
            { PageState.Name = String.Empty
              LocalState = Unchecked.defaultof<'b>
              OnEnter = fun _ -> Task.FromResult()
              OnExit = fun _ -> Task.FromResult()
              Transitions = []
              Actions = []
              ExitActions = [] }

    member inline x.Run(prop: PageStateProperties<'a, 'b>) = x.Run([ prop ])

    [<CustomOperation("name")>]
    member inline _.Name((), name: string) = PageStateProperties.Name name

    member inline x.For(prop: PageStateProperties<'a, 'b>, f: unit -> PageStateProperties<'a, 'b> list) =
        x.Combine(prop, f ())



(*
    //-----------------
    // Transitions
    //-----------------

    [<CustomOperation("transition")>]
    member _.Transitions(state, handler) : PageState<'a, 'b> =
        { state with Transitions = handler :: state.Transitions } // TODO string list for required actions


    //-----------------
    // Actions
    //-----------------

    // Unnamed action without task
    [<CustomOperation("action")>]
    member _.Actions
        (
            state,
            handler: 'b -> unit,
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler = fun localState -> Task.FromResult(handler localState)

        { state with
            Actions =
                (callerInformation, (None, [], handler))
                :: state.Actions }

    // Unnamed action with task
    [<CustomOperation("action")>]
    member _.Actions
        (
            state,
            handler: 'b -> Task<unit>,
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        { state with
            Actions =
                (callerInformation, (None, [], handler))
                :: state.Actions }

    // Unnamed action without task with dependencies
    [<CustomOperation("actionWith")>]
    member _.Actions
        (
            state,
            handler: (string list) * ('b -> unit),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let dependencies, handler = handler
            None, dependencies, (fun localState -> Task.FromResult(handler localState))

        { state with Actions = (callerInformation, handler) :: state.Actions }

    // Unnamed action with task with dependencies
    [<CustomOperation("actionWith")>]
    member _.Actions
        (
            state,
            handler: string list * ('b -> Task<unit>),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        { state with
            Actions =
                (callerInformation, (None, (fst handler), (snd handler)))
                :: state.Actions }

    // Named action without task
    [<CustomOperation("actionN")>]
    member _.Actions
        (
            state,
            handler: string * ('b -> unit),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let name = fst handler
            Some name, [], (fun localState -> Task.FromResult((snd handler) localState))

        { state with Actions = (callerInformation, handler) :: state.Actions }

    // Named action with task
    [<CustomOperation("actionN")>]
    member _.Actions
        (
            state,
            handler: string * ('b -> Task<unit>),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let name = fst handler
            Some name, [], snd handler

        { state with Actions = (callerInformation, handler) :: state.Actions }

    // Named action without task with dependencies
    [<CustomOperation("actionNWith")>]
    member _.Actions
        (
            state,
            handler: string * string list * ('b -> unit),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let name, dependencies, handler = handler
            Some name, dependencies, (fun localState -> Task.FromResult(handler localState))

        { state with Actions = (callerInformation, handler) :: state.Actions }

    // Named action with task with dependencies
    [<CustomOperation("actionNWith")>]
    member _.Actions
        (
            state,
            handler: string * string list * ('b -> Task<unit>),
            [<CallerMemberName>] ?memberName: string,
            [<CallerLineNumber>] ?lineNumber: int,
            [<CallerFilePath>] ?filePath: string
        ) : PageState<'a, 'b> =
        let callerInformation =
            { CallerInformation.MemberName = defaultArg memberName ""
              LineNumber = defaultArg lineNumber 0
              FilePath = defaultArg filePath "" }

        let handler =
            let name, dependencies, handler = handler
            Some name, dependencies, handler

        { state with Actions = (callerInformation, handler) :: state.Actions }
        *)
