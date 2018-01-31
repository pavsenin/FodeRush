namespace FodeRush.SyntaxVisualizer.Interfaces

open FodeRush.Platform.Utils
open FodeRush.Platform.Interfaces
open ViewModels

type ISyntaxVisualizerService =
    [<CLIEvent>]
    abstract member TextViewChanged: IEvent<ITextView>
    abstract member Select: ITextView -> Span -> unit