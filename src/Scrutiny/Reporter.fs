namespace Scrutiny

open System
open System.IO
open Thoth.Json.Net

[<RequireQualifiedAccess>]
module internal Encoders =
    let transition (t: Transition<'a>) =
        Encode.object
            [ "dependantActions", Encode.list (List.map Encode.string t.DependantActions)
              "destination", Encode.string (t.Destination(Unchecked.defaultof<'a>)).Name ]

    let callerInformation (ci: CallerInformation) =
        Encode.object
            [ "memberName", Encode.string ci.MemberName
              "lineNumber", Encode.int ci.LineNumber
              "filePath", Encode.string ci.FilePath ]

    let action (a: StateAction) =
        Encode.object
            [ "dependantActions", Encode.list (List.map Encode.string a.DependantActions)
              "name", Encode.string a.Name
              "callerInformation", callerInformation a.CallerInformation ]

    let pageState (ps: PageState<'a>) =
        Encode.object
            [ "name", Encode.string ps.Name
              "transitions", Encode.list (List.map transition ps.Transitions)
              "actions", Encode.list (List.map action ps.Actions) ]

    let node (n: Node<PageState<'a>>) =
        Encode.object
            [ "from", pageState (fst n)
              "destinations", Encode.list (List.map pageState (snd n)) ]

    let adjacencyGraph (ag: AdjacencyGraph<PageState<'a>>) = Encode.list (List.map node ag)

    let rec serializableException (se: SerializableException) =
        Encode.object
            [ "type", Encode.string se.Type
              "message", Encode.string se.Message
              "stackTrace", Encode.string se.StackTrace
              "innerException", Encode.option serializableException se.InnerException ]

    let errorLocation (el: ErrorLocation) =
        match el with
        | State (n, se) ->
            Encode.object
                [ "case", Encode.string "state"
                  "name", Encode.string n
                  "exception", serializableException se ]
        | Transition (f, d, se) ->
            Encode.object
                [ "case", Encode.string "transition"
                  "from", Encode.string f
                  "destination", Encode.string d
                  "exception", serializableException se ]

    let step (s: Step<_>) =
        Encode.object
            [ "pageState", pageState s.PageState
              "actions", Encode.list (List.map Encode.string s.Actions)
              "errorLocation", Encode.option errorLocation s.Error ]

    let scrutinizedState (ss: ScrutinizedStates<'a>) =
        Encode.object
            [ "graph", adjacencyGraph ss.Graph
              "steps", Encode.list (List.map step ss.Steps) ]

type private ReporterMessage<'a, 'b> =
    | Start of AdjacencyGraph<PageState<'a>> * PageState<'a>
    | PushTransition of PageState<'a>
    | PushAction of string
    | OnError of ErrorLocation
    | GenerateMap
    | Finish of AsyncReplyChannel<ScrutinizedStates<'a>>

[<RequireQualifiedAccess>]
type internal IReporter<'a> =
    abstract Start: (AdjacencyGraph<PageState<'a>> * PageState<'a>) -> unit
    abstract PushTransition: PageState<'a> -> unit
    abstract PushAction: string -> unit
    abstract OnError: ErrorLocation -> unit
    abstract GenerateMap: unit -> unit
    abstract Finish: unit -> ScrutinizedStates<'a>

[<RequireQualifiedAccess>]
type internal Reporter<'a>(filePath: string) =
    let assembly = typeof<Reporter<'a>>.Assembly

    let html =
        use htmlStream =
            assembly.GetManifestResourceStream("Scrutiny.wwwroot.graph.template.html")

        use htmlReader = new StreamReader(htmlStream)
        htmlReader.ReadToEnd()

    let file =
        let fileInfo = FileInfo(filePath)

        let fileName =
            if fileInfo.Name = String.Empty then
                "ScrutinyResult.html"
            else
                fileInfo.Name

        fileInfo.DirectoryName, fileName

    let generateMap (graph: ScrutinizedStates<'a>) =
        let json = Encoders.scrutinizedState graph
        let output = html.Replace("\"{{REPORT}}\"", json.ToString())

        let (filePath, fileName) = file

        File.WriteAllText(sprintf "%s/%s" filePath fileName, output)

    let removeLast steps =
        let steps = steps |> Seq.toList
        steps.[0 .. (steps.Length) - 2]

    let mailbox =
        MailboxProcessor.Start(fun inbox ->
            let rec loop (state: ScrutinizedStates<'a>) =
                async {
                    match! inbox.Receive() with
                    | Start (ag, startState) ->
                        let step =
                            { Step.PageState = startState
                              Actions = []
                              Error = None }

                        return!
                            loop
                                { ScrutinizedStates.Graph = ag
                                  Steps = [ step ] }
                    | PushTransition ps ->
                        let nextStep =
                            { Step.PageState = ps
                              Actions = []
                              Error = None }

                        let steps = [ yield! state.Steps; nextStep ]

                        return! loop { state with Steps = steps }
                    | PushAction name ->
                        let current = state.Steps |> Seq.last
                        let current = { current with Actions = [ yield! current.Actions; name ] }

                        let steps =
                            [ yield! removeLast state.Steps
                              current ]

                        return! loop { state with Steps = steps }
                    | OnError el ->
                        let current = state.Steps |> Seq.last
                        let current = { current with Error = Some el }

                        let steps =
                            [ yield! removeLast state.Steps
                              current ]

                        return! loop { state with Steps = steps }
                    | GenerateMap ->
                        generateMap state

                        return! loop state
                    | Finish reply ->
                        reply.Reply state
                        return ()
                }

            loop
                { ScrutinizedStates.Graph = []
                  Steps = [] })

    interface IReporter<'a> with
        member this.Start st = mailbox.Post(Start st)
        member this.PushTransition next = mailbox.Post(PushTransition next)
        member this.PushAction actionName = mailbox.Post(PushAction actionName)
        member this.OnError errorLocation = mailbox.Post(OnError errorLocation)
        member this.GenerateMap() = mailbox.Post GenerateMap
        member this.Finish() = mailbox.PostAndReply Finish
