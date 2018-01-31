namespace FodeRush.Foundation

open System
open System.Threading.Tasks
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Language.Intellisense
open Microsoft.FSharp.Compiler.SourceCodeServices
open EnvDTE
open Utils
open Microsoft.VisualStudio.Text.Editor
open Microsoft.FSharp.Compiler.Range

type SuggestedActionsSource(parseResults:FSharpParseFileResults, view: ITextView) =
    let buffer = view.TextBuffer
    let actionsChanged = Event<_,_>()
    let invoke span text =
        buffer.Replace(span, text) |> ignore
    interface ISuggestedActionsSource with
        member __.Dispose() = ()
        member __.GetSuggestedActions (_requestedActionCategories, _range, _ct) =
            let position = _range.Start.Position
            let snapshot = buffer.CurrentSnapshot
            let snapshotLine = snapshot.GetLineFromPosition position
            let line = snapshotLine.LineNumber
            let column = view.Caret.Position.BufferPosition.Position - position
            let pos = Pos.fromZ line column
            let matchExpr = tryFindMatch pos parseResults.ParseTree
            let text = checkMatchExpression matchExpr
            let matchRange = matchExpr.Value.Range
            let matchStart = snapshotLine.Start.Position + matchRange.Start.Column
            let matchEndSnapshotLine = snapshot.GetLineFromLineNumber matchRange.End.Line
            let matchEnd = matchEndSnapshotLine.End.Position
            let span = Span(matchStart, matchEnd - matchStart)
            match text with
            | None -> Seq.empty
            | Some value ->
                let actions = 
                    ["Convert to If"] |>
                    List.map (fun text ->
                        { new ISuggestedAction with
                            member __.DisplayText = text
                            member __.Dispose() = ()
                            member __.GetActionSetsAsync _ct = Task.FromResult <| seq []
                            member __.GetPreviewAsync _ct = Task.FromResult null
                            member __.HasActionSets = false
                            member __.HasPreview = false
                            member __.IconAutomationText = null
                            member __.IconMoniker = Unchecked.defaultof<_>
                            member __.InputGestureText = null
                            member __.Invoke _ct = invoke span value
                            member __.TryGetTelemetryId _telemetryId = false })
                [ SuggestedActionSet actions ] |> Seq.ofList
        member __.HasSuggestedActionsAsync (_requestedCategories, _range, _ct) =
            Task.FromResult(true)
        [<CLIEvent>]
        member __.SuggestedActionsChanged: IEvent<EventHandler<EventArgs>, EventArgs> = 
            actionsChanged.Publish
        member __.TryGetTelemetryId telemetryId = 
            telemetryId <- Guid.Empty; 
            false