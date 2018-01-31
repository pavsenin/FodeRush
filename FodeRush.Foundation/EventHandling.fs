module EventHandling

open System
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open System.Threading
open System.Windows.Threading

/// Try to run a given function, resorting to a default value if it throws exceptions
let protectOrDefault f defaultVal =
    try
        f()
    with e ->
        defaultVal

/// Try to run a given function and catch its exceptions
let protect f = protectOrDefault f ()

let synchronize f = 
        let ctx = SynchronizationContext.Current
        
        let thread = 
            match ctx with
            | null -> null // saving a thread-local access
            | _ -> Thread.CurrentThread
        f (fun g arg -> 
            let nctx = SynchronizationContext.Current
            match ctx, nctx with
            | null, _ -> g arg
            | _, _ when Object.Equals(ctx, nctx) && thread.Equals(Thread.CurrentThread) -> g arg
            | _ -> ctx.Post((fun _ -> g (arg)), null))

type Async with
    /// An equivalence of Async.StartImmediate which catches and logs raised exceptions
    static member StartImmediateSafe(computation, ?cancellationToken) =
        let comp =
            async {
                try
                    return! computation
                with e ->
                    failwith e.Message
            }
        Async.StartImmediate(comp, ?cancellationToken = cancellationToken)

    /// An equivalence of Async.Start which catches and logs raised exceptions
    static member StartInThreadPoolSafe(computation, ?cancellationToken) =
        let comp =
            async {
                try
                    return! computation
                with e ->
                    failwith "The following exception occurs inside async blocks"
            }
        Async.Start(comp, ?cancellationToken = cancellationToken)

type Microsoft.FSharp.Control.Async with
        static member EitherEvent(ev1: IObservable<'T>, ev2: IObservable<'U>) = 
            synchronize (fun f -> 
                Async.FromContinuations((fun (cont, _econt, _ccont) -> 
                    let rec callback1 = 
                        (fun value -> 
                        remover1.Dispose()
                        remover2.Dispose()
                        f cont (Choice1Of2(value)))
                    
                    and callback2 = 
                        (fun value -> 
                        remover1.Dispose()
                        remover2.Dispose()
                        f cont (Choice2Of2(value)))
                    
                    and remover1: IDisposable = ev1.Subscribe(callback1)
                    and remover2: IDisposable = ev2.Subscribe(callback2)
                    ())))

[<RequireQualifiedAccess>]
module ViewChange =    
    open Microsoft.VisualStudio.Text.Tagging

    let layoutEvent (view: ITextView) = 
        view.LayoutChanged |> Event.choose (fun e -> if e.NewSnapshot <> e.OldSnapshot then Some() else None)
    
    let viewportHeightEvent (view: ITextView) =  
        view.ViewportHeightChanged |> Event.map (fun _ -> ())

    let caretEvent (view: ITextView) = 
        view.Caret.PositionChanged |> Event.map (fun _ -> ())

    let bufferEvent (buffer: ITextBuffer) = 
        buffer.ChangedLowPriority |> Event.map (fun _ -> ())

    let tagsEvent (tagAggregator: ITagAggregator<_>) = 
        tagAggregator.TagsChanged |> Event.map (fun _ -> ())

[<NoComparison; NoEquality>]
type CallInUIContext = CallInUIContext of ((unit -> unit) -> Async<unit>)
    with static member FromCurrentThread() = 
                         let uiContext = SynchronizationContext.Current
                         CallInUIContext (fun f ->
                             async {
                                 let ctx = SynchronizationContext.Current
                                 do! Async.SwitchToContext uiContext
                                 protect f
                                 do! Async.SwitchToContext ctx
                             })

type DocumentEventListener (events: IEvent<unit> list, delayMillis: uint16, update: CallInUIContext -> Async<unit>) =
    // Start an async loop on the UI thread that will execute the update action after the delay
    do if List.isEmpty events then invalidArg "events" "Events must be a non-empty list"
    let events = events |> List.reduce Event.merge
    let timer = DispatcherTimer(DispatcherPriority.ApplicationIdle,      
                                Interval = TimeSpan.FromMilliseconds (float delayMillis))
    let tokenSource = new CancellationTokenSource()
    let mutable disposed = false

    // This is a none or for-all option for unit testing purpose only
    static let mutable skipTimerDelay = false

    let startNewTimer() = 
        timer.Stop()
        timer.Start()
        
    let rec awaitPauseAfterChange() =
        async { 
            let! e = Async.EitherEvent(events, timer.Tick)
            match e with
            | Choice1Of2 _ -> 
                startNewTimer()
                do! awaitPauseAfterChange()
            | _ -> ()
        }
        
    do 
       let callUIContext = CallInUIContext.FromCurrentThread()
       let startUpdate (cts: CancellationTokenSource) = Async.StartInThreadPoolSafe (update callUIContext, cts.Token)

       let computation =
           async { 
               let cts = ref (new CancellationTokenSource())
               startUpdate !cts

               while true do
                   do! Async.AwaitEvent events
                   if not skipTimerDelay then
                       startNewTimer()
                       do! awaitPauseAfterChange()
                   (!cts).Cancel()
                   (!cts).Dispose()
                   cts := new CancellationTokenSource()
                   startUpdate !cts }
       
       Async.StartInThreadPoolSafe (computation, tokenSource.Token)

    /// Skip all timer events in order to test events instantaneously
    static member internal SkipTimerDelay 
        with get () = skipTimerDelay
        and set v = skipTimerDelay <- v

    interface IDisposable with
        member __.Dispose() =
            if not disposed then
                tokenSource.Cancel()
                tokenSource.Dispose()
                timer.Stop()
                disposed <- true
   