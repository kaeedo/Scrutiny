namespace Scrutiny

open System
open System.IO

type internal State<'a, 'b> =
    { Graph: AdjacencyGraph<PageState<'a, 'b>>
      Transitions: PageState<'a, 'b> list
      Error: string }

type private ReporterMessage<'a, 'b> =
| Start of AdjacencyGraph<PageState<'a, 'b>>
| PushTransition of PageState<'a, 'b>
| OnError of string
| Finish of AsyncReplyChannel<State<'a, 'b>>

[<RequireQualifiedAccess>]
type internal IReporter<'a, 'b> =
    abstract Start: AdjacencyGraph<PageState<'a, 'b>> -> unit
    abstract PushTransition: PageState<'a, 'b> -> unit
    abstract OnError: string -> unit
    abstract Finish: unit -> State<'a, 'b>

type internal Reporterr<'a, 'b>() =
    let mailbox =
        MailboxProcessor.Start (fun inbox ->
            let rec loop (state: State<'a, 'b>) =
                async {
                    match! inbox.Receive() with
                    | Start ag ->
                        return! loop { State.Graph = ag; Transitions = []; Error = String.Empty }
                    | PushTransition ps ->
                        return! loop { state with Transitions = ps :: state.Transitions }
                    | OnError s ->
                        return! loop { state with Error = s }
                    | Finish reply ->
                        reply.Reply state
                        return ()
                }
            loop { State.Graph = []; Transitions = []; Error = String.Empty}
        )

    interface IReporter<'a, 'b> with
          member this.Start ag = mailbox.Post (Start ag)
          member this.PushTransition ps = mailbox.Post (PushTransition ps)
          member this.OnError s = mailbox.Post (OnError s)
          member this.Finish () = mailbox.PostAndReply Finish

[<RequireQualifiedAccess>]
module internal Reporter =
    type internal IMarker =
        interface
        end

    let private assembly = typeof<IMarker>.Assembly

    let private js =
        use jsStream = assembly.GetManifestResourceStream("Scrutiny.wwwroot.app.js")
        use jsReader = new StreamReader(jsStream)
        jsReader.ReadToEnd()

    let private html =
        use htmlStream = assembly.GetManifestResourceStream("Scrutiny.wwwroot.graph.template.html")
        use htmlReader = new StreamReader(htmlStream)
        htmlReader.ReadToEnd()

    let private file scrutinyResultFilePath =
        let fileInfo = FileInfo(scrutinyResultFilePath)

        let fileName =
            if fileInfo.Name = String.Empty then "ScrutinyResult.html" else fileInfo.Name

        fileInfo.DirectoryName, fileName

    let generateMap (config: ScrutinyConfig) (graph: AdjacencyGraph<PageState<_, _>>) =
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

        let (filePath, fileName) = file config.ScrutinyResultFilePath

        File.WriteAllText(sprintf "%s/app.js" filePath, js)
        File.WriteAllText(sprintf "%s/%s" filePath fileName, output)
