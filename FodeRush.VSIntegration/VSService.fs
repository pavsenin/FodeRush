namespace FodeRush.VSIntegration
open FodeRush.Platform.Interfaces
open System.ComponentModel.Composition

[<Export(typeof<IVSService>)>]
type VSService() =
    let textViewActivated = Event<ITextView>()
    let textViewDeactivated = Event<ITextView>()

    interface IVSService with
        [<CLIEvent>]
        member this.TextViewActivated = textViewActivated.Publish
        [<CLIEvent>]
        member this.TextViewDeactivated = textViewDeactivated.Publish

        member this.RaiseTextViewActivated arg = textViewActivated.Trigger arg
        member this.RaiseTextViewDeactivated arg = textViewDeactivated.Trigger arg
