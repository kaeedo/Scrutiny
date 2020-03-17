namespace Scrutiny

open System.IO
open System

[<RequireQualifiedAccess>]
module internal Reporter =
    let generateMap (graph: AdjacencyGraph<PageState<_>>) =
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

        let output = html.Replace("{{REPLACE}}", sprintf "window.graphEdges=[%s];" jsFunctionCalls)
        File.WriteAllText("report.html", output)
