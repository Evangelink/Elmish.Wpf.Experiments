module CounterByTwo

open Elmish.WPF
open Utils

type Model =
    { Counter: int }

type Msg =
    | Increment

let init () =
    { Counter = 0 }

let update msg m =
    match msg with
    | Increment -> { m with Counter = m.Counter + 2 }

module Platform =

    let bindings () : Binding<Model, Msg> list = [
        "Counter" |> Binding.oneWay (fun r -> r.Counter)
    ]

    // TODO: Get rid of this module
    [<RequireQualifiedAccess>]
    module CounterByTwoSub =
        let private handler = GenericSub<Msg>()
        let dispatch msg = handler.Dispatch msg
        let sub x = handler.Sub x

    let timerTick =
      let timer = new System.Timers.Timer(100.)
      timer.Elapsed.Add (fun _ ->
        CounterByTwoSub.dispatch <| Increment
      )
      timer.Start()
