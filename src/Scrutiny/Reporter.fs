namespace Scrutiny

open System.IO

[<RequireQualifiedAccess>]
module internal Reporter =
    let generateMap (config: ScrutinyConfig) (graph: AdjacencyGraph<PageState<_>>) =
        let html = File.ReadAllText("./wwwroot/graph.template.html")
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


        File.Copy("./wwwroot/app.js", sprintf "%s/app.js" config.ReportPath)
        File.WriteAllText(sprintf "%s/report.html" config.ReportPath, output)
