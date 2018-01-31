module Utils

open System
open System.Reflection
open Microsoft.VisualStudio.Shell.Interop
open Microsoft.FSharp.Compiler.SourceCodeServices
open EnvDTE
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range
open System.Diagnostics
open EventHandling
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor

//let inline isNotNull v = not (isNull v)
[<Sealed>]
type MaybeBuilder () =
    // 'T -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Return value: 'T option =
        Some value

    // M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.ReturnFrom value: 'T option =
        value

    // unit -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Zero (): unit option =
        Some ()     // TODO: Should this be None?

    // (unit -> M<'T>) -> M<'T>
    [<DebuggerStepThrough>]
    member __.Delay (f: unit -> 'T option): 'T option =
        f ()

    // M<'T> -> M<'T> -> M<'T>
    // or
    // M<unit> -> M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Combine (r1, r2: 'T option): 'T option =
        match r1 with
        | None ->
            None
        | Some () ->
            r2

    // M<'T> * ('T -> M<'U>) -> M<'U>
    [<DebuggerStepThrough>]
    member inline __.Bind (value, f: 'T -> 'U option): 'U option =
        Option.bind f value

    // 'T * ('T -> M<'U>) -> M<'U> when 'U :> IDisposable
    [<DebuggerStepThrough>]
    member __.Using (resource: ('T :> System.IDisposable), body: _ -> _ option): _ option =
        try body resource
        finally
            if not <| obj.ReferenceEquals (null, box resource) then
                resource.Dispose ()

    // (unit -> bool) * M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member x.While (guard, body: _ option): _ option =
        if guard () then
            // OPTIMIZE: This could be simplified so we don't need to make calls to Bind and While.
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    // seq<'T> * ('T -> M<'U>) -> M<'U>
    // or
    // seq<'T> * ('T -> M<'U>) -> seq<M<'U>>
    [<DebuggerStepThrough>]
    member x.For (sequence: seq<_>, body: 'T -> unit option): _ option =
        // OPTIMIZE: This could be simplified so we don't need to make calls to Using, While, Delay.
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (
                enum.MoveNext,
                x.Delay (fun () ->
                    body enum.Current)))
[<Sealed>]
type AsyncMaybeBuilder () =
    [<DebuggerStepThrough>]
    member __.Return value : Async<'T option> = Some value |> async.Return

    [<DebuggerStepThrough>]
    member __.ReturnFrom value : Async<'T option> = value

    [<DebuggerStepThrough>]
    member __.ReturnFrom (value: 'T option) : Async<'T option> = async.Return value

    [<DebuggerStepThrough>]
    member __.Zero () : Async<unit option> =
        Some () |> async.Return

    [<DebuggerStepThrough>]
    member __.Delay (f : unit -> Async<'T option>) : Async<'T option> = f ()

    [<DebuggerStepThrough>]
    member __.Combine (r1, r2 : Async<'T option>) : Async<'T option> =
        async {
            let! r1' = r1
            match r1' with
            | None -> return None
            | Some () -> return! r2
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: Async<'T option>, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            let! value' = value
            match value' with
            | None -> return None
            | Some result -> return! f result
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: 'T option, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            match value with
            | None -> return None
            | Some result -> return! f result
        }

    [<DebuggerStepThrough>]
    member __.Using (resource : ('T :> IDisposable), body : _ -> Async<_ option>) : Async<_ option> =
        try body resource
        finally 
            //if isNotNull resource then 
            resource.Dispose()

    [<DebuggerStepThrough>]
    member x.While (guard, body : Async<_ option>) : Async<_ option> =
        if guard () then
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    [<DebuggerStepThrough>]
    member x.For (sequence : seq<_>, body : 'T -> Async<unit option>) : Async<_ option> =
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (enum.MoveNext, x.Delay (fun () -> body enum.Current)))

    [<DebuggerStepThrough>]
    member inline __.TryWith (computation : Async<'T option>, catchHandler : exn -> Async<'T option>) : Async<'T option> =
            async.TryWith (computation, catchHandler)

    [<DebuggerStepThrough>]
    member inline __.TryFinally (computation : Async<'T option>, compensation : unit -> unit) : Async<'T option> =
            async.TryFinally (computation, compensation)

type EarlyBuilder() =
    member inline __.Bind (v, f) = Option.bind f v
    member inline __.Zero() = None
    member inline __.Delay f = f
    member inline __.Run f = f()
    member inline __.Combine (a, b) = match a with | Some _ -> a | None -> b()
    member inline __.ReturnFrom x = x
    member inline __.Return x = Some x

let maybe = MaybeBuilder()
let early = EarlyBuilder()
let asyncMaybe = AsyncMaybeBuilder()

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Option =
    let inline ofNull value =
        if obj.ReferenceEquals(value, null) then None else Some value

    let inline ofNullable (value: Nullable<'T>) =
        if value.HasValue then Some value.Value else None

    let inline toNullable (value: 'T option) =
        match value with
        | Some x -> Nullable<_> x
        | None -> Nullable<_> ()

    let inline attempt (f: unit -> 'T) = try Some <| f() with _ -> None

    /// Gets the value associated with the option or the supplied default value.
    let inline getOrElse v =
        function
        | Some x -> x
        | None -> v

    /// Gets the option if Some x, otherwise the supplied default value.
    let inline orElse v =
        function
        | Some x -> Some x
        | None -> v

    /// Gets the value if Some x, otherwise try to get another value by calling a function
    let inline getOrTry f =
        function
        | Some x -> x
        | None -> f()

    /// Gets the option if Some x, otherwise try to get another value
    let inline orTry f =
        function
        | Some x -> Some x
        | None -> f()

    /// Some(Some x) -> Some x | None -> None
    let inline flatten x =
        match x with
        | Some x -> x
        | None -> None

    let inline toList x =
        match x with
        | Some x -> [x]
        | None -> []

    let inline iterElse someAction noneAction opt =
        match opt with
        | Some x -> someAction x
        | None   -> noneAction ()

type SnapshotPoint with
    member x.InSpan (span: SnapshotSpan) = 
        // The old snapshot might not be available anymore, we compare on updated snapshot
        let point = x.TranslateTo(span.Snapshot, PointTrackingMode.Positive)
        point.CompareTo span.Start >= 0 && point.CompareTo span.End <= 0
type ITextSnapshot with
    member x.FullSpan =
        SnapshotSpan(x, 0, x.Length)
    member inline x.LineBounds (snapshotSpan:SnapshotSpan) =
        let startLineNumber = x.GetLineNumberFromPosition (snapshotSpan.Span.Start)
        let endLineNumber = x.GetLineNumberFromPosition (snapshotSpan.Span.End)
        (startLineNumber, endLineNumber)
    member inline x.LineText num =  x.GetLineFromLineNumber(num).GetText()

type ITextBuffer with
    member x.GetSnapshotPoint (position: CaretPosition) = 
        Option.ofNullable <| position.Point.GetPoint(x, position.Affinity)
    
    member x.TriggerTagsChanged (sender: obj) (event: Event<_,_>) =
        let span = x.CurrentSnapshot.FullSpan
        event.Trigger(sender, SnapshotSpanEventArgs(span))

type DTE with
    member x.GetActiveDocument() =
        let doc =
            maybe {
                let! doc = Option.attempt (fun _ -> x.ActiveDocument) |> Option.bind Option.ofNull
                let! _ = Option.ofNull doc.ProjectItem
                return doc }
        doc

    member x.GetProjectItem filePath =
         x.Solution.FindProjectItem filePath |> Option.ofNull
         
    member x.GetCurrentDocument filePath =
        match x.GetActiveDocument() with
        | Some doc when doc.FullName = filePath -> 
            Some doc
        | docOpt ->
            // If there is no current document or it refers to a different path,
            // we try to find the exact document from solution by path.
            let result = 
                x.GetProjectItem filePath 
                |> Option.bind (fun item -> Option.ofNull item.Document)
            result

    member x.TryGetProperty(category, page, name) = 
        x.Properties(category, page)
        |> Seq.cast
        |> Seq.tryPick (fun (prop: Property) ->
            if prop.Name = name then Some (prop.Value)
            else None)

type ISuggestion =
    abstract Text: string
    abstract Invoke: unit -> unit
    abstract NeedsIcon: bool

type private Expr = System.Linq.Expressions.Expression
let precompileFieldGet<'R>(f : FieldInfo) =
    let p = Expr.Parameter(typeof<obj>)
    let lambda = Expr.Lambda<Func<obj, 'R>>(Expr.Field(Expr.Convert(p, f.DeclaringType) :> Expr, f) :> Expr, p)
    lambda.Compile().Invoke

let getSourcesAndFlags project =
    let instanceNonPublic = BindingFlags.Instance ||| BindingFlags.NonPublic
    let underlyingProjectField = project.GetType().GetField("project", instanceNonPublic)
    let underlyingProject = underlyingProjectField.GetValue(project)

    let sourcesAndFlagsField = underlyingProject.GetType().GetField("sourcesAndFlags", instanceNonPublic)
    let getter = precompileFieldGet<option<string[] * string[]>> sourcesAndFlagsField
    getter underlyingProject 

let getOptionsAndFiles project =
    match getSourcesAndFlags(project) with
    | Some(x) -> x
    | _ -> ([||], [||])

let getProjectOptions fileName files options = 
    { ProjectFileName = fileName
      ProjectFileNames = files
      OtherOptions = options
      IsIncompleteTypeCheckEnvironment = false
      UseScriptResolutionRules = false
      LoadTime = DateTime.Now
      UnresolvedReferences = None
      ReferencedProjects = [||] }

let tryFindMatch pos parsedInput =
    let inline getIfPosInRange range f =
        if rangeContainsPos range pos then f()
        else None
    let rec walkImplFileInput (ParsedImplFileInput(_name, _isScript, _fileName, _scopedPragmas, _hashDirectives, moduleOrNamespaceList, _)) = 
        List.tryPick walkSynModuleOrNamespace moduleOrNamespaceList
    and walkSynModuleOrNamespace(SynModuleOrNamespace(_lid, _isModule, decls, _xmldoc, _attributes, _access, range)) =
        getIfPosInRange range (fun () -> 
            List.tryPick walkSynModuleDecl decls
        )
    and walkSynModuleDecl(decl: SynModuleDecl) =
        getIfPosInRange decl.Range (fun () ->
            match decl with
            | SynModuleDecl.Let(_isRecursive, bindings, _range) ->
                List.tryPick walkBinding bindings
            | _ -> 
                None
        )
    and walkBinding (Binding(_access, _bindingKind, _isInline, _isMutable, _attrs, _xmldoc, _valData, _headPat, retTy, expr, _bindingRange, seqPoint) as binding) =
        getIfPosInRange binding.RangeOfBindingAndRhs (fun () ->
            walkExpr expr
        )
    and walkExpr expr =
        getIfPosInRange expr.Range (fun () ->
            match expr with
            | SynExpr.Match(_sequencePointInfoForBinding, synExpr, synMatchClauseList, _, _range) ->
                getIfPosInRange synExpr.Range (fun () -> Some(expr))
            | SynExpr.LetOrUse(_, _, _, body, _range) ->
                walkExpr body
            | SynExpr.Sequential(_, _, expr1, expr2, _range) ->
                early {
                    return! walkExpr expr1
                    return! walkExpr expr2
                }
            | _ -> None
        )
    match parsedInput with
    | Some(ParsedInput.ImplFile(input)) -> walkImplFileInput input
    | _ -> None

let isBoolConstValue = function
    | SynMatchClause.Clause(pattern, _expr1, expr2, _, _) ->
        match pattern with
        | SynPat.Const(constant, _) ->
            match constant with
            | SynConst.Bool value -> Some value, Some expr2
            | _ -> None, None
        | _ -> None, None

let checkMatchExpression matchExpr =
    match matchExpr with
    | None -> None
    | Some _match ->
        match _match with
        | SynExpr.Match(_, _, clauses, _, _range) ->
            match clauses with
            | clause1::clause2::[] ->
                let pat1, body1 = isBoolConstValue clause1
                let pat2, body2 = isBoolConstValue clause2
                match pat1, pat2 with
                | Some p1, Some _ ->
                    Some <| sprintf "if %s then %s else %s" (p1.ToString()) (body1.ToString()) (body2.ToString())
                | _ -> None
            | _ -> None
        | _ -> None