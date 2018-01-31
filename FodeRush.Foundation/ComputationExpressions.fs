module ComputationExpressions

type LoggingBuilder() =
    let log p = printfn "Expression is %A" p

    member this.Bind(x, f) =
        log x
        f x
    member this.Return(x) =
        x

let log = new LoggingBuilder()

let loggerWorkflow = 
    log {
        let! x = 42
        let! y = 43
        let! z = x + y
        return z
    }

let divideBy bottom top =
    match bottom with
    | 0 -> None
    | _ -> Some(top / bottom)

let divideByWorkflow init x y z =
    let r = init |> divideBy x
    match r with
    | None -> None
    | Some r' ->
        let r = r' |> divideBy y
        match r with
        | None -> None
        | Some r' ->
            let r = r' |> divideBy z
            match r with
            | None -> None
            | Some r' -> Some r'

let good = divideByWorkflow 12 2 3 1
let bad = divideByWorkflow 12 2 0 1

type MayBeBuilder() = 
    member m.Bind(x, f) =
        match x with
        | None -> None
        | Some x' -> f x'
    member m.Return(x) =
        Some x

let mayBe = new MayBeBuilder()

let divideByWorkflow' init x y z = 
    mayBe {
        let! x' = init |> divideBy x
        let! y' = x' |> divideBy y
        let! z' = y' |> divideBy z
        return z'
    }

let good' = divideByWorkflow' 12 2 3 1
let bad' = divideByWorkflow' 12 2 0 1