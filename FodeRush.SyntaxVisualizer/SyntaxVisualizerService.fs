namespace FodeRush.SyntaxVisualizer

open System.Diagnostics
open System.ComponentModel.Composition
open FodeRush.Platform.Interfaces
open FodeRush.SyntaxVisualizer.Interfaces

[<Export(typeof<IStartable>)>]
[<Export(typeof<ISyntaxVisualizerService>)>]
type SyntaxVisualizerService [<ImportingConstructor>](vsService:IVSService) =
    let textViewChanged = Event<ITextView>()

    let activated = 
        Handler<ITextView>(
            fun _ arg -> textViewChanged.Trigger arg)

    let deactivated =
        Handler<ITextView>(
            fun _ arg -> ())

    interface IStartable with
        member this.Order = 10
        member this.Start() =
            vsService.TextViewActivated.AddHandler activated
            vsService.TextViewDeactivated.AddHandler deactivated
        member this.Stop() =
            vsService.TextViewActivated.RemoveHandler activated
            vsService.TextViewDeactivated.RemoveHandler deactivated

    interface ISyntaxVisualizerService with
        [<CLIEvent>]
        member this.TextViewChanged = textViewChanged.Publish
        member this.Select view span =
            view.Selection.Select span
