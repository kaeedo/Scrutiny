namespace Scrutiny

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

type private ReporterMessage<'a, 'b> =
| Start of AdjacencyGraph<PageState<'a, 'b>> * PageState<'a, 'b>
| PushTransition of PageState<'a, 'b>
| PushAction of string
| OnError of ErrorLocation
| Finish of AsyncReplyChannel<ScrutinizedStates<'a, 'b>>

[<RequireQualifiedAccess>]
type internal IReporter<'a, 'b> =
    abstract Start: (AdjacencyGraph<PageState<'a, 'b>> * PageState<'a, 'b>) -> unit
    abstract PushTransition: PageState<'a, 'b> -> unit
    abstract PushAction: string -> unit
    abstract OnError: ErrorLocation -> unit
    abstract Finish: unit -> ScrutinizedStates<'a, 'b>

[<RequireQualifiedAccess>]
type internal Reporter<'a, 'b>(filePath: string) =
    let assembly = typeof<Reporter<'a, 'b>>.Assembly

    let html =
        use htmlStream = assembly.GetManifestResourceStream("Scrutiny.wwwroot.graph.template.html")
        use htmlReader = new StreamReader(htmlStream)
        htmlReader.ReadToEnd()

    let file =
        let fileInfo = FileInfo(filePath)

        let fileName =
            if fileInfo.Name = String.Empty then "ScrutinyResult.html" else fileInfo.Name

        fileInfo.DirectoryName, fileName

    let generateMap (graph: ScrutinizedStates<_, _>) =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())
        options.ReferenceHandler <- ReferenceHandler.Preserve

        let output = html.Replace("\"{{REPORT}}\"", JsonSerializer.Serialize(graph, options))

        let (filePath, fileName) = file
            
        File.WriteAllText(sprintf "%s/%s" filePath fileName, output)

    let removeLast steps =
        let steps = steps |> Seq.toList
        steps.[0..(steps.Length) - 2]

    let mailbox =
        MailboxProcessor.Start (fun inbox ->
            let rec loop (state: ScrutinizedStates<'a, 'b>) =
                async {
                    match! inbox.Receive() with
                    | Start (ag, startState) ->
                        let step =
                            { Step.PageState = startState
                              Actions = []
                              Error = None }

                        return! loop { ScrutinizedStates.Graph = ag; Steps = [ step ] }
                    | PushTransition ps ->
                        let nextStep =
                            { Step.PageState = ps
                              Actions = []
                              Error = None }

                        let steps = [ yield! state.Steps; nextStep ]

                        return! loop { state with Steps = steps }
                    | PushAction name ->
                        let current = state.Steps |> Seq.last
                        let current = { current with Actions = [yield! current.Actions; name] }
                        let steps = seq { yield! removeLast state.Steps; current }

                        return! loop { state with Steps = steps }
                    | OnError el ->
                        let current = state.Steps |> Seq.last
                        let current = { current with Error = Some el }

                        let steps = seq { yield! removeLast state.Steps; current }

                        return! loop { state with Steps = steps }
                    | Finish reply ->
                        generateMap state
                        
                        reply.Reply state
                        return ()
                } 
            loop { ScrutinizedStates.Graph = []; Steps = [] }
        )

    interface IReporter<'a, 'b> with
          member this.Start st = mailbox.Post (Start st)
          member this.PushTransition next = mailbox.Post (PushTransition next)
          member this.PushAction actionName = mailbox.Post (PushAction actionName)
          member this.OnError errorLocation = mailbox.Post (OnError errorLocation)
          member this.Finish () = mailbox.PostAndReply Finish
