module Utils


// TODO: Get rid of this type
type GenericSub<'a>() =
    // A "dispatch" function which can send an Analysis message to the update function
    // This is mutable and a dummy function initially because we don't have access to it yet
    let mutable privateDispatch: ('a -> unit) = ignore

    member _.Dispatch msg = privateDispatch msg
    // A "sub" which is wired into the main Elmish program (in App.fs).
    // It has a dispatch function which we can assign to our mutable dispatch function above.
    member _.Sub _ =
        (fun d -> privateDispatch <- d)
        |> Elmish.Cmd.ofSub
