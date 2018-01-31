namespace FodeRush.Foundation

open EventHandling
open Utils
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text
open EnvDTE

type ConvertMatchToIfRefactoring(view: ITextView, textDocument: ITextDocument, dte:DTE) as self = 
  let buffer = view.TextBuffer
  let changed = Event<_>()
  let mutable currentWord: SnapshotSpan option = None
  let mutable suggestions: ISuggestion list = []
  let updateAtCaretPosition (CallInUIContext callInUIContext) =
    async {
        match buffer.GetSnapshotPoint view.Caret.Position, currentWord with
        | Some point, Some word when word.Snapshot = view.TextSnapshot && point.InSpan word -> return ()
        | (Some _ | None), _ ->
            let! result = asyncMaybe {
                let! point = buffer.GetSnapshotPoint view.Caret.Position
                let! doc = dte.GetCurrentDocument textDocument.FilePath
                let projectItem = dte.Solution.FindProjectItem textDocument.FilePath
                let project = projectItem.ContainingProject
                return! Some(project)
                (*
                let! word, _ = vsLanguageService.GetSymbol (point, doc.FullName, project) 
                    
                do! match currentWord with
                    | None -> Some()
                    | Some oldWord -> 
                        if word <> oldWord then Some()
                        else None

                currentWord <- Some word
                suggestions <- []
                let! source = openDocumentTracker.TryGetDocumentText textDocument.FilePath
                let vsDocument = VSDocument(source, doc, point.Snapshot)
                let! symbolRange, recordExpression, recordDefinition, insertionPos =
                    tryFindRecordDefinitionFromPos codeGenService project point vsDocument
                // Recheck cursor position to ensure it's still in new word
                let! point = buffer.GetSnapshotPoint view.Caret.Position
                if point.InSpan symbolRange && shouldGenerateRecordStub recordExpression recordDefinition then
                    return! Some (recordExpression, recordDefinition, insertionPos, word.Snapshot)
                else
                    return! None
                *)
            } 
            //suggestions <- result |> Option.map getSuggestions |> Option.getOrElse []
            do! callInUIContext <| fun _ -> changed.Trigger self
    }
  let docEventListener = new DocumentEventListener ([ViewChange.layoutEvent view; ViewChange.caretEvent view], 500us, updateAtCaretPosition)
