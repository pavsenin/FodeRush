namespace FodeRush.VSIntegration

open Microsoft.VisualStudio.TextManager.Interop
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.OLE.Interop
open FodeRush.Platform.Interfaces
open Microsoft.VisualStudio.Text
open Implementation

type TextViewManager(vsView:IVsTextView, wpfView:IWpfTextView, vsService:IVSService, documentFactory:ITextDocumentFactoryService) as this =
    let mutable connectionCookie = 0u
    let mutable eventSink:IConnectionPoint = null
    do this.Start()

    static member PropertyName = "TextViewManager"
    member this.Start() =
        let cpContainer = vsView :?> IConnectionPointContainer
        if cpContainer <> null then
            let riid = typeof<IVsTextViewEvents>.GUID
            let eventSink = cpContainer.FindConnectionPoint(ref riid)
            connectionCookie <- eventSink.Advise this
    member this.Stop() =
        if connectionCookie > 0u then
            eventSink.Unadvise connectionCookie
            connectionCookie <- 0u

    interface IVsTextViewEvents with
        member this.OnChangeCaretLine(view, iNewLine, iOldLine) = ()
        member this.OnChangeScrollInfo(view, bar, minUnit, maxUnits, visibleUnits, firstVisibleUnit) = ()
        member this.OnKillFocus(view) =
            let textView = VSTextView(wpfView, documentFactory)
            vsService.RaiseTextViewDeactivated textView
        member this.OnSetBuffer(view, pBuffer) = ()
        member this.OnSetFocus(view) =
            let textView = VSTextView(wpfView, documentFactory)
            vsService.RaiseTextViewActivated textView