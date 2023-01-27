open System.Xml
open System.IO

let replaceXPathInnerText xpath innerTextValue (doc: XmlDocument) =
    let node = doc.SelectSingleNode xpath

    if isNull node then
        failwithf "XML node '%s' not found" xpath
    else
        node.InnerText <- innerTextValue
        doc

let private load (fileName: string) (doc: XmlDocument) =
    use fs = File.OpenRead(fileName)
    doc.Load fs

let loadDoc (path: string) =
    let xmlDocument = new XmlDocument()
    load path xmlDocument
    xmlDocument

let saveDoc (fileName: string) (doc: XmlDocument) =
    use fs = File.Open(fileName, FileMode.Truncate, FileAccess.Write)
    doc.Save fs

let pokeInnerText (fileName: string) xpath innerTextValue =
    let doc = new XmlDocument()
    load fileName doc
    replaceXPathInnerText xpath innerTextValue doc |> saveDoc fileName


let changelog = File.ReadLines("./CHANGELOG.md")

let releaseVersion =
    let latestVersion = changelog |> Seq.find (fun c -> c.StartsWith("###"))
    latestVersion.Substring(4)

let entryProject = "./src/Scrutiny/Scrutiny.fsproj"

pokeInnerText entryProject "/Project/PropertyGroup[1]/Version[1]" releaseVersion
saveDoc entryProject (loadDoc entryProject)

printfn "%s" releaseVersion
