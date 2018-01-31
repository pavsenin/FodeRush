namespace ViewModels

open FSharp.ViewModule
open FSharp.ViewModule.Validation
open FsXaml
open System.Windows.Forms
open System.Windows.Forms.Integration
open SyntaxVisualizer.AstProcessing
open FodeRush.SyntaxVisualizer.Interfaces
open FodeRush.SyntaxVisualizer
open FodeRush.Platform.Interfaces
open FodeRush.Platform.Utils

type SyntaxVisualizerView = XAML<"SyntaxVisualizerView.xaml", true>

type SyntaxVisualizerViewModel() as this = 
    inherit EventViewModelBase<Event>()

    let mutable service:ISyntaxVisualizerService = Unchecked.defaultof<ISyntaxVisualizerService>
    let mutable activeView:ITextView = Unchecked.defaultof<ITextView>

    let propertyGrid = new PropertyGrid(Dock = DockStyle.Fill, 
                                        PropertySort = PropertySort.Alphabetical,
                                        HelpVisible = false, ToolbarVisible = false,
                                        CommandsVisibleIfAvailable = false)
    let propertyHost = new WindowsFormsHost(Child = propertyGrid)
    let syntaxTree = this.Factory.Backing(<@ this.SyntaxTree @>, [])
    let selectedNodeType = this.Factory.Backing(<@ this.SelectedNodeType @>, null)
    let selectedNodeAst = this.Factory.Backing(<@ this.SelectedNodeAst @>, null)
    let propertiesVisible = this.Factory.Backing(<@ this.PropertiesVisible @>, false)
    let showFullAst = this.Factory.Backing(<@ this.ShowFullAst @>, false)
    let showFullAstCommand = this.Factory.CommandSync (fun param -> 
        let ast, _ = getAst propertyGrid.SelectedObject -1
        MessageBox.Show(ast, "Full Ast") |> ignore)

    let handleEvent event =
        match event with
        | SelectNode(Some node) ->
            let ast, cut = getAst node.Original 60
            this.SelectedNodeAst <- ast
            this.ShowFullAst <- cut
            this.SelectedNodeType <- getType node.Original
            this.PropertiesVisible <- node.Original <> null
            propertyGrid.SelectedObject <- node.Original
            service.Select activeView node.Span
        | ChangeView(Some view) ->
            activeView <- view
            let buildSyntaxTree (doc:ITextDocument) =
                let tree = doc.Buffer.GetText() |> getSyntaxTree doc.FilePath
                this.SyntaxTree <- tree |> List.map (fun m -> NodeViewModel m)
            activeView.Document |> ubind buildSyntaxTree
        | _ -> ()
    do
        this.EventStream
        |> Observable.subscribe handleEvent
        |> ignore

    member this.Initialize s =
        let textViewChanged =
            Handler<ITextView>(
                fun _ arg -> handleEvent(ChangeView(Some arg)))
        service <- s
        service.TextViewChanged.AddHandler textViewChanged

    member this.PropertyHost = propertyHost
    member this.SyntaxTree with get() = syntaxTree.Value and set v = syntaxTree.Value <- v
    member this.SelectedNodeType with get() = selectedNodeType.Value and set v = selectedNodeType.Value <- v
    member this.SelectedNodeAst with get() = selectedNodeAst.Value and set v = selectedNodeAst.Value <- v
    member this.PropertiesVisible with get() = propertiesVisible.Value and set v = propertiesVisible.Value <- v
    member this.ShowFullAst with get() = showFullAst.Value and set v = showFullAst.Value <- v
    member this.ShowFullAstCommand = showFullAstCommand
    member val SelectedItemChangedCommand = this.Factory.EventValueCommand()