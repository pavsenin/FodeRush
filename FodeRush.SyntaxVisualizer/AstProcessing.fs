namespace SyntaxVisualizer

open System
open System.Reflection
open Microsoft.FSharp.Core
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range
open FodeRush.Platform.Utils
open FodeRush.SyntaxVisualizer.Models
open AstUtils

module AstProcessing =

    let getType node = node.GetType().ToString()
    let getAst node cutLength =
        let filter =
            match cutLength with
            | -1 -> t
            | _ -> (fun c -> not(List.exists (fun x -> x = c) ['\r';'\n']))
        let ast = sprintf "%A" node |> Seq.filter filter
        let astLength = Seq.length ast
        if (cutLength = -1 || astLength <= cutLength) then
            ast |> Seq.take astLength |> String.Concat, false
        else
            ast |> Seq.take cutLength |> String.Concat, true

    let toPosition (pos:pos) = Position(pos.Line, pos.Column)
    let toSpan (range:range) = Span(range.Start |> toPosition, range.End |> toPosition)

    let toTokenWithSpan name (span:Span) original = Token(name, span, original)
    let toToken name (range:range) original = toTokenWithSpan name (toSpan range) original

    let toNodeWithSpan name (span:Span) children original = Node(name, span, children, original)
    let toNode name (range:range) children original = toNodeWithSpan name (toSpan range) children original
    let toNodeNoChildren name (range:range) original = toNode name range [] original
    let toNodeZeroRange name original = toNode name range.Zero [] original
    let toUnknownNode original = toNode "Unknown node" range.Zero [] original

    let toNodes moduleOrNamespace =
        let rec synModuleOrNamespaceToNode(SynModuleOrNamespace(longIdent, _, decls, _, attrs, _, range) as modOrNam) =
            let children =
                [longIdent |> longIdentToNode] @
                (decls |> List.map synModuleDeclToNode)
            toNode "SynModuleOrNamespace" range children modOrNam

        and synModuleDeclToNode decl =
            match decl with
            | SynModuleDecl.Attributes(attributes, range) ->
                let children = attributes |> List.map synAttributeToNode
                toNode "SynModuleDecl.Attributes" range children decl
            | SynModuleDecl.DoExpr(infoForBinding, synExpr, range) ->
                toNode "SynModuleDecl.DoExpr" range [synExpr |> synExprToNode] decl
            | SynModuleDecl.Exception(_synExceptionDefn, _range) ->
                toUnknownNode decl
            | SynModuleDecl.HashDirective(_parsedHashDirective, _range) ->
                toUnknownNode decl
            | SynModuleDecl.Let(_, bindings, range) ->
                let children = bindings |> List.map bindingToNode
                toNode "SynModuleDecl.Let" range children decl
            | SynModuleDecl.ModuleAbbrev(ident, longIdent, range) ->
                toNode "SynModuleDecl.ModuleAbbrev" range [] decl
            | SynModuleDecl.NamespaceFragment(synModuleOrNamespace) ->
                toUnknownNode decl
            | SynModuleDecl.NestedModule(componentInfo, decls, _, range) ->
                let children = decls |> List.map synModuleDeclToNode
                toNode "SynModuleDecl.NestedModule" range children decl
            | SynModuleDecl.Open(longIdent, range) ->
                toNode "SynModuleDecl.Open" range [longIdent |> longIdentWithDotsToNode] decl
            | SynModuleDecl.Types(synTypeDefnList, range) ->
                let children = synTypeDefnList |> List.map synTypeDefnToNode
                toNode "SynModuleDecl.Types" range children decl

        and synTypeDefnToNode(TypeDefn(componentInfo, typeDefnRepr, members, range) as typeDefn) = 
            let children = 
                [componentInfo |> synComponentInfoToNode] @
                [typeDefnRepr |> synTypeDefnReprToNode] @
                (members |> List.map synMemberDefnToNode)
            toNode "SynTypeDefn" range children typeDefn

        and synTypeDefnReprToNode typeDefnRepr =
            match typeDefnRepr with
            | SynTypeDefnRepr.ObjectModel(kind, members, range) ->
                let children = members |> List.map synMemberDefnToNode
                toNode "SynTypeDefnRepr.ObjectModel" range [] typeDefnRepr
            | SynTypeDefnRepr.Simple(typeDefnSimpleRepr, range) ->
                toNode "SynTypeDefnRepr.Simple" range [] typeDefnRepr

        and synComponentInfoToNode(ComponentInfo(attrs, typeParams, constraints, id, _, _, _, range) as info) =
            let children = 
                (attrs |> List.map synAttributeToNode) @
                (typeParams |> List.map synTyParDeclToNode) @
                (constraints |> List.map synTypeConstraintToNode) @
                [id |> longIdentToNode]
            toNode "SynComponentInfo" range children info

        and synTyParDeclToNode(TyparDecl(attrs, synTypar) as typeParam) =
            toNode "SynTyparDecl" range.Zero [] typeParam

        and synTypeConstraintToNode constr =
            match constr with
            | SynTypeConstraint.WhereTyparDefaultsToType(_, _, range) ->
                toNode "SynTypeConstraint.WhereTyparDefaultsToType" range [] constr
            | _ ->
                toNode "SynTypeConstraint._" range.Zero [] constr

        and synMemberDefnToNode memberDefn =
            match memberDefn with
            | SynMemberDefn.AbstractSlot(valSig, flags, range) ->
                let children = [valSig |> synValSigToNode]
                toNode "SynMemberDefn.AbstractSlot" range children memberDefn
            | SynMemberDefn.AutoProperty(_) ->
                toUnknownNode memberDefn
            | SynMemberDefn.ImplicitCtor(_) ->
                toUnknownNode memberDefn
            | SynMemberDefn.ImplicitInherit(_) ->
                toUnknownNode memberDefn
            | SynMemberDefn.Inherit(typeName, id, range) ->
                let children = [typeName |> synTypeToNode]
                toNode "SynMemberDefn.Inherit" range children memberDefn
            | SynMemberDefn.Interface(_) ->
                toUnknownNode memberDefn
            | SynMemberDefn.LetBindings(_) ->
                toUnknownNode memberDefn
            | SynMemberDefn.Member(binding, range) ->
                toNode "SynMemberDefn.Member" range [binding |> bindingToNode] memberDefn
            | SynMemberDefn.NestedType(_) ->
                toUnknownNode memberDefn
            | SynMemberDefn.Open(_) ->
                toUnknownNode memberDefn
            | SynMemberDefn.ValField(_) ->
                toUnknownNode memberDefn

        and synValSigToNode(ValSpfn(attrs, id, typeParams, typeName, valInfo, _, _, _, _, _, range) as valSig) =
            toNode "ValSpfn" range [id |> identToNode] valSig

        and synTypeToNode synType  =
            match synType with
            | SynType.LongIdent(longIdent) ->
                toNode "SynType.LongIdent" synType.Range [longIdent |> longIdentWithDotsToNode] synType
            | _ ->
                toUnknownNode synType

        and synAttributeToNode({TypeName = typeName; ArgExpr = argExpr; Target = target; Range = range } as attr) =
            let children =
                [typeName |> longIdentWithDotsToNode] @
                [argExpr |> synExprToNode]
            toNode "SynAttribute" range children attr

        and bindingToNode(Binding(_, _, _, _, _, _, _, _, _, expr, range, _) as binding) =
            toNode "Binding" range [expr |> synExprToNode] binding

        and synExprToNode expr =
            match expr with
            | SynExpr.AddressOf(_) ->
                toUnknownNode expr
            | SynExpr.App(_, _, funcExpr, argExpr, range) ->
                let children = [funcExpr |> synExprToNode; argExpr |> synExprToNode]
                toNode "SynExpr.App" range children expr
            | SynExpr.ArbitraryAfterError(_) ->
                toUnknownNode expr
            | SynExpr.ArrayOrList(_) ->
                toUnknownNode expr
            | SynExpr.ArrayOrListOfSeqExpr(_) ->
                toUnknownNode expr
            | SynExpr.Assert(_) ->
                toUnknownNode expr
            | SynExpr.CompExpr(_) ->
                toUnknownNode expr
            | SynExpr.Const(constant, range) ->
                toNode "SynExpr.Const" range [range |> constantToNode constant] expr
            | SynExpr.DiscardAfterMissingQualificationAfterDot(_) ->
                toUnknownNode expr
            | SynExpr.Do(doExpr, range) ->
                toNode "SynExpr.Do" range [doExpr |> synExprToNode] expr
            | SynExpr.DoBang(_) ->
                toUnknownNode expr
            | SynExpr.DotGet(_) ->
                toUnknownNode expr
            | SynExpr.DotIndexedGet(_) ->
                toUnknownNode expr
            | SynExpr.DotIndexedSet(_) ->
                toUnknownNode expr
            | SynExpr.DotNamedIndexedPropertySet(_) ->
                toUnknownNode expr
            | SynExpr.DotSet(_) ->
                toUnknownNode expr
            | SynExpr.Downcast(_) ->
                toUnknownNode expr
            | SynExpr.For(_) ->
                toUnknownNode expr
            | SynExpr.ForEach(_) ->
                toUnknownNode expr
            | SynExpr.FromParseError(_) ->
                toUnknownNode expr
            | SynExpr.Ident(ident) ->
                toNode "SynExpr.Ident" ident.idRange [ident |> identToNode] expr
            | SynExpr.IfThenElse(exprGuard, exprThen, _, _, _, _, range) ->
                let children = [exprGuard |> synExprToNode; exprThen |> synExprToNode]
                toNode "SynExpr.IfThenElse" range children expr
            | SynExpr.ImplicitZero(_) ->
                toUnknownNode expr
            | SynExpr.InferredDowncast(_) ->
                toUnknownNode expr
            | SynExpr.InferredUpcast(_) ->
                toUnknownNode expr
            | SynExpr.JoinIn(_) ->
                toUnknownNode expr
            | SynExpr.Lambda(_) ->
                toUnknownNode expr
            | SynExpr.Lazy(_) ->
                toUnknownNode expr
            | SynExpr.LetOrUse(isRec, isUse, bindings, body, range) ->
                let bindingsChildren = bindings |> List.map bindingToNode
                let children = bindingsChildren @ [body |> synExprToNode]
                toNode "SynExpr.LetOrUse" range children expr
            | SynExpr.LetOrUseBang(_) ->
                toUnknownNode expr
            | SynExpr.LibraryOnlyILAssembly(_) ->
                toUnknownNode expr
            | SynExpr.LibraryOnlyStaticOptimization(_) ->
                toUnknownNode expr
            | SynExpr.LibraryOnlyUnionCaseFieldGet(_) ->
                toUnknownNode expr
            | SynExpr.LibraryOnlyUnionCaseFieldSet(_) ->
                toUnknownNode expr
            | SynExpr.LongIdent(_, longIdent, _, range) ->
                toNode "SynExpr.LongIdent" range [longIdent |> longIdentWithDotsToNode] expr
            | SynExpr.LongIdentSet(_) ->
                toUnknownNode expr
            | SynExpr.Match(_) ->
                toUnknownNode expr
            | SynExpr.MatchLambda(_) ->
                toUnknownNode expr
            | SynExpr.NamedIndexedPropertySet(_) ->
                toUnknownNode expr
            | SynExpr.New(_) ->
                toUnknownNode expr
            | SynExpr.Null(_) ->
                toUnknownNode expr
            | SynExpr.ObjExpr(_) ->
                toUnknownNode expr
            | SynExpr.Paren(parenExpr, _, _, range) ->
                toNode "SynExpr.Paren" range [parenExpr |> synExprToNode] expr
            | SynExpr.Quote(_) ->
                toUnknownNode expr
            | SynExpr.Record(_) ->
                toUnknownNode expr
            | SynExpr.Sequential(_) ->
                toUnknownNode expr
            | SynExpr.TraitCall(_) ->
                toUnknownNode expr
            | SynExpr.TryFinally(_) ->
                toUnknownNode expr
            | SynExpr.TryWith(_) ->
                toUnknownNode expr
            | SynExpr.Tuple(_) ->
                toUnknownNode expr
            | SynExpr.TypeApp(_) ->
                toUnknownNode expr
            | SynExpr.TypeTest(_) ->
                toUnknownNode expr
            | SynExpr.Typed(_) ->
                toUnknownNode expr
            | SynExpr.Upcast(_) ->
                toUnknownNode expr
            | SynExpr.While(_) ->
                toUnknownNode expr
            | SynExpr.YieldOrReturn(_) ->
                toUnknownNode expr
            | SynExpr.YieldOrReturnFrom(_) ->
                toUnknownNode expr

        and identToNode ident =
            toToken ident.idText ident.idRange ident

        and longIdentToNode longIdent =
            let range = getIdentRange longIdent
            let children = longIdent |> List.map identToNode
            toNode "LongIdent" range children longIdent

        and longIdentWithDotsToNode(LongIdentWithDots(id, dots) as longIdent) =
            let range = getIdentRange id
            let children = [id |> longIdentToNode]
            toNode "LongIdentWithDots" range children longIdent

        and constantToNode constant parentRange =
            match constant with
            | SynConst.Bool(value) -> toToken (value |> toS) parentRange constant
            | SynConst.String(text, range) -> toToken text range constant
            | SynConst.Unit -> toToken "Unit" parentRange constant
            | _ -> toUnknownNode constant

        synModuleOrNamespaceToNode moduleOrNamespace

    let getSyntaxTree file source =
        let checker = FSharpChecker.Create()
        let projOptions = checker.GetProjectOptionsFromScript(file, source) |> Async.RunSynchronously
        let parseResults = checker.ParseFileInProject(file, source, projOptions) |> Async.RunSynchronously
        match parseResults.ParseTree with
        | Some (ParsedInput.ImplFile(implFile) as input) ->
            let (ParsedImplFileInput(fn, script, name, _, _, modules, _)) = implFile
            modules |> List.map toNodes
        | _ -> failwith "Not supported"