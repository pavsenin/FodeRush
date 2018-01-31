namespace SyntaxVisualizer

open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range

module AstUtils =

    let getRange list funRange =
        list |> List.fold (fun state x -> unionRanges state (funRange x)) range.Zero
    let getIdentRange(list:LongIdent) = getRange list (fun x -> x.idRange)