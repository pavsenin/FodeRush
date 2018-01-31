namespace FodeRush.Platform.Interfaces

open FodeRush.Platform.Utils

type IStartable =
    abstract member Order: int
    abstract member Start : unit -> unit
    abstract member Stop : unit -> unit

type ITextBuffer =
    abstract member GetText: unit -> string

type ITextCaret =
    abstract member Position: int

type ITextSelection =
    abstract member StartPosition: int
    abstract member EndPosition: int
    abstract member Select: Span -> unit

type ITextDocument =
    abstract member FilePath: string
    abstract member Buffer: ITextBuffer

type ITextView =
    abstract member Document: ITextDocument option
    abstract member Caret: ITextCaret
    abstract member Selection: ITextSelection

type IVSService =
    [<CLIEvent>]
    abstract member TextViewActivated: IEvent<ITextView>
    [<CLIEvent>]
    abstract member TextViewDeactivated: IEvent<ITextView>
    
    abstract member RaiseTextViewActivated: ITextView -> unit
    abstract member RaiseTextViewDeactivated: ITextView -> unit
