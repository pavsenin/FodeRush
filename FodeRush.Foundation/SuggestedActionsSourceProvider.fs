namespace FodeRush.Foundation

open System
open System.ComponentModel.Composition
open Microsoft.VisualStudio.Language.Intellisense
open Microsoft.VisualStudio.Utilities
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Shell.Interop
open Microsoft.FSharp.Compiler.SourceCodeServices
open EnvDTE
open EventHandling
open Utils
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range

[<Export(typeof<ISuggestedActionsSourceProvider>)>]
[<Name "FodeRush Suggested Actions">]
[<ContentType "F#">]
[<TextViewRole(PredefinedTextViewRoles.Editable)>]
type SuggestedActionsSourceProvider
    [<ImportingConstructor>]
    ([<Import(typeof<SVsServiceProvider>)>] serviceProvider: IServiceProvider, textDocumentFactory: ITextDocumentFactoryService) = 
    interface ISuggestedActionsSourceProvider with
        member sp.CreateSuggestedActionsSource(textView: ITextView, buffer: ITextBuffer): ISuggestedActionsSource =
            let _, textDocument = textDocumentFactory.TryGetTextDocument buffer
            let dte = serviceProvider.GetService(typeof<SDTE>) :?> DTE
            let projectItem = dte.Solution.FindProjectItem textDocument.FilePath
            let project = projectItem.ContainingProject
            let (files, options) = getOptionsAndFiles project
            let projOptions = getProjectOptions project.FileName files options

            let source = buffer.CurrentSnapshot.GetText()
            let checker = FSharpChecker.Create()
            if Array.isEmpty projOptions.OtherOptions then
                null
            else
                let parseResults, _ =
                    checker.ParseAndCheckFileInProject(textDocument.FilePath, 0, source, projOptions)
                    |> Async.RunSynchronously
                new SuggestedActionsSource(parseResults, textView) :> _
