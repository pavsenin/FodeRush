namespace FodeRush.Platform

module Builders =

    [<Sealed>]
    type MaybeBuilder () =
        member __.Bind (value, f) =
            Option.bind f value
        member __.Return value =
            Some value
        member __.ReturnFrom value =
            value

    let maybe = MaybeBuilder()