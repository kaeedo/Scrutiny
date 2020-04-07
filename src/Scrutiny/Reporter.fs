namespace Scrutiny

open System
open System.IO

[<RequireQualifiedAccess>]
module internal Reporter =
    type internal Marker = interface end

    let file scrutinyResultFilePath =
        let fileInfo = new FileInfo(scrutinyResultFilePath)

        let fileName = 
            if fileInfo.Name = String.Empty 
            then "ScrutinyResult.html" 
            else fileInfo.Name

        fileInfo.DirectoryName, fileName

    let generateMap (config: ScrutinyConfig) (graph: AdjacencyGraph<PageState<_>>) =

        let assembly = typeof<Marker>.Assembly

        use jsStream = assembly.GetManifestResourceStream("Scrutiny.wwwroot.app.js")
        use jsReader = new StreamReader(jsStream)
        let js = jsReader.ReadToEnd()

        use htmlStream = assembly.GetManifestResourceStream("Scrutiny.wwwroot.graph.template.html")
        use htmlReader = new StreamReader(htmlStream)
        let html = htmlReader.ReadToEnd()
        
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
