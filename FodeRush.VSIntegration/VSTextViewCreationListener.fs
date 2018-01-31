namespace FodeRush.VSIntegration

open System.ComponentModel.Composition
open Microsoft.VisualStudio.Utilities
open Microsoft.VisualStudio.Editor
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open FodeRush.Platform.Builders
open FodeRush.Platform.Interfaces

[<Export(typeof<IWpfTextViewCreationListener>)>]
[<Export(typeof<IStartable>)>]
[<ContentType("text")>]
[<TextViewRole(PredefinedTextViewRoles.Document)>]
type VSTextViewCreationListener
    [<ImportingConstructor>]
    (adaptersFactory:IVsEditorAdaptersFactoryService,
     documentFactory:ITextDocumentFactoryService,
     vsService:IVSService) =

    interface IWpfTextViewCreationListener with
        member this.TextViewCreated wpfView =
            let vsView = adaptersFactory.GetViewAdapter(wpfView)
            wpfView.Closed.Add(fun e -> ())
            wpfView.Properties.GetOrCreateSingletonProperty(
                TextViewManager.PropertyName,
                fun() -> TextViewManager(vsView, wpfView, vsService, documentFactory))
            |> ignore

    interface IStartable with
        member this.Order = 5
        member this.Start() = ()
        member this.Stop() = ()
