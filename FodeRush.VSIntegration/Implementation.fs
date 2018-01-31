namespace FodeRush.VSIntegration

open System
open FodeRush.Platform.Utils
open FodeRush.Platform.Interfaces

module Implementation =

    type VSTextCaret() =
        interface ITextCaret with
            member this.Position = 0

    type VSTextSelection(selection:Microsoft.VisualStudio.Text.Editor.ITextSelection) =
        interface ITextSelection with
            member this.StartPosition = 0
            member this.EndPosition = 0
            member this.Select span =
                let snapshot = selection.TextView.TextSnapshot
                let getPosition (position:Position) =
                    let line = Math.Min(0, position.Line - 1)
                    let snapshotLine = snapshot.GetLineFromLineNumber line
                    if snapshotLine = null then -1
                    else
                        Math.Min(snapshotLine.Start.Position + position.Column, snapshotLine.End.Position)
                let textSpan = Microsoft.VisualStudio.Text.Span.FromBounds(span.Start |> getPosition, span.End |> getPosition)
                let snapshotSpan = Microsoft.VisualStudio.Text.SnapshotSpan(snapshot, textSpan)
                selection.Select(snapshotSpan, false)

    type VSTextBuffer(buffer:Microsoft.VisualStudio.Text.ITextBuffer) =
        interface ITextBuffer with
            member this.GetText() =
                buffer.CurrentSnapshot.GetText()

    type VSTextDocument(document:Microsoft.VisualStudio.Text.ITextDocument) = 
        interface ITextDocument with
            member this.FilePath =
                document.FilePath
            member this.Buffer =
                VSTextBuffer(document.TextBuffer) :> _

    type VSTextView(wpfView:Microsoft.VisualStudio.Text.Editor.IWpfTextView,
                    documentFactory:Microsoft.VisualStudio.Text.ITextDocumentFactoryService) =
        interface ITextView with
            member this.Document =
                let exist, document = documentFactory.TryGetTextDocument wpfView.TextBuffer
                if exist then Some(VSTextDocument(document) :> _)
                else None
            member this.Caret =
                VSTextCaret() :> _
            member this.Selection =
                VSTextSelection(wpfView.Selection) :> _
