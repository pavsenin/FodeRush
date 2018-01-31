namespace FodeRush.Platform

module Utils =
    let t _ = true
    let f _ = false
    let toS x = x.ToString()
    let ubind f = function
        | Some x -> f x |> ignore
        | None -> ()

    [<Struct>]
    type Position(line:int, column:int) =
        member this.Line = line
        member this.Column = column
    [<Struct>]
    type Span(start:Position, _end:Position) =
        member this.Start = start
        member this.End = _end
        override this.ToString() =
            sprintf "(%d,%d-%d,%d)" this.Start.Line this.Start.Column this.End.Line this.End.Column
