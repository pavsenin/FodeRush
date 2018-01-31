namespace SyntaxVisualizer.Tests

open System.Reflection
open System.IO
open System.IO.Compression
open Xunit
open SyntaxVisualizer.AstProcessing
open Models

module AstProcessingTests =
    let getResourceName (resourceName : string) =
        Assembly.GetCallingAssembly().GetManifestResourceNames()
        |> Array.tryFind (fun n -> n.ToLower().EndsWith(resourceName.ToLower()))

    let getSources resName entryCount sourcesCount =
        let resourceName = getResourceName resName
        match resourceName with
        | None -> failwith "Invalid resource name"
        | Some name ->
            use stream = Assembly.GetCallingAssembly().GetManifestResourceStream(name)
            Assert.NotNull stream
            use archive = new ZipArchive(stream)
            Assert.Equal(entryCount, archive.Entries.Count)
            let sourceFiles = 
                archive.Entries
                |> Seq.filter (fun e -> e.FullName.EndsWith(".fs"))
            Assert.Equal(sourcesCount, Seq.length sourceFiles)
            sourceFiles
            |> Seq.map (fun sf -> new StreamReader(sf.Open()))
            |> Seq.map (fun sr -> sr.ReadToEnd())
            |> Seq.toList

    let rec assertNoUnknownNodes model =
        match model with
        | Node(name, _, children, _) ->
            Assert.NotEqual<string>("Unknown node", name)
            children |> List.iter assertNoUnknownNodes
        | Token(name, _, _) ->
            Assert.NotEqual<string>("Unknown node", name)

    [<Fact>]
    let ``Process FSharp.ViewModule without unknown nodes`` () =
        getSources "FSharp.ViewModule.160415.zip" 98 15
        |> List.map (fun s -> (getSyntaxTree "/home/user/Test.fsx" s), s)
        |> List.iter (fun (tree, s) ->
            tree |> List.iter (fun model -> assertNoUnknownNodes model))