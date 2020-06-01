namespace Scrutiny

open System
open System.IO

type internal ErrorLocation<'a, 'b> =
| State of string * exn
| Transition of string * string * exn

type internal PerformedTransition<'a, 'b> =
    { From: PageState<'a, 'b>
      To: PageState<'a, 'b>
      Error: ErrorLocation<'a, 'b> option }

type internal State<'a, 'b> =
    { Graph: AdjacencyGraph<PageState<'a, 'b>>
      PerformedTransitions: PerformedTransition<'a, 'b> list }

type private ReporterMessage<'a, 'b> =
| Start of AdjacencyGraph<PageState<'a, 'b>>
| PushTransition of PageState<'a, 'b> * PageState<'a, 'b>
| OnError of ErrorLocation<'a, 'b>
| Finish of AsyncReplyChannel<State<'a, 'b>>

[<RequireQualifiedAccess>]
type internal IReporter<'a, 'b> =
    abstract Start: AdjacencyGraph<PageState<'a, 'b>> -> unit
    abstract PushTransition: (PageState<'a, 'b> * PageState<'a, 'b>) -> unit
    abstract OnError: ErrorLocation<'a, 'b> -> unit
    abstract Finish: unit -> State<'a, 'b>

[<RequireQualifiedAccess>]
type internal Reporter<'a, 'b>(filePath: string) =
    let assembly = typeof<Reporter<'a, 'b>>.Assembly

    let js =
        use jsStream = assembly.GetManifestResourceStream("Scrutiny.wwwroot.app.js")
        use jsReader = new StreamReader(jsStream)
        jsReader.ReadToEnd()

    let html =
        use htmlStream = assembly.GetManifestResourceStream("Scrutiny.wwwroot.graph.template.html")
        use htmlReader = new StreamReader(htmlStream)
        htmlReader.ReadToEnd()

    let file =
        let fileInfo = FileInfo(filePath)

        let fileName =
            if fileInfo.Name = String.Empty then "ScrutinyResult.html" else fileInfo.Name

        fileInfo.DirectoryName, fileName

    let generateMap (graph: AdjacencyGraph<PageState<_, _>>) =
        let jsCode (node, sibling) = sprintf "[\"%s\", \"%s\"]" node sibling

        let jsFunctionCalls =
            seq {
                for (node, siblings) in graph do
                    for sib in siblings do
                        yield node.Name, sib.Name
            }
            |> Seq.map jsCode
            |> String.concat (",")

        let output = html.Replace("{{REPLACE}}", sprintf "[%s]" jsFunctionCalls)

        let (filePath, fileName) = file

        File.WriteAllText(sprintf "%s/app.js" filePath, js)
        File.WriteAllText(sprintf "%s/%s" filePath fileName, output)

    let mailbox =
        MailboxProcessor.Start (fun inbox ->
            let rec loop (state: State<'a, 'b>) =
                async {
                    match! inbox.Receive() with
                    | Start ag ->
                        return! loop { State.Graph = ag; PerformedTransitions = [] }
                    | PushTransition (f, t) ->
                        let transition =
                            { PerformedTransition.From = f
                              To = t
                              Error = None }
                        return! loop { state with PerformedTransitions = transition :: state.PerformedTransitions }
                    | OnError el ->
                        let performedTransitions = state.PerformedTransitions |> List.tail
                        let errorNode = state.PerformedTransitions |> List.head
                        let performedTransitions = { errorNode with Error = Some el } :: performedTransitions
                        return! loop { state with PerformedTransitions = performedTransitions }
                    | Finish reply ->
                        reply.Reply { state with PerformedTransitions = state.PerformedTransitions |> List.rev }
                        return ()
                }
            loop { State.Graph = []; PerformedTransitions = [] }
        )

    interface IReporter<'a, 'b> with
          member this.Start ag = mailbox.Post (Start ag)
          member this.PushTransition t = mailbox.Post (PushTransition t)
          member this.OnError errorLocation = mailbox.Post (OnError errorLocation)
          member this.Finish () = mailbox.PostAndReply Finish
