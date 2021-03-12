module ElmishEventModel.Core.App

open Elmish
open Elmish.WPF

type Model =
    { CounterByOne: CounterByOne.Model
      CounterByTwo: CounterByTwo.Model }

type CmdMsg =
    | NoOp

let init () =
    { CounterByOne = CounterByOne.init ()
      CounterByTwo = CounterByTwo.init () }

type Msg =
    | CounterByOneMsg of CounterByOne.Msg
    | CounterByTwoMsg of CounterByTwo.Msg

let update msg m =
    match msg, m with
    | CounterByOneMsg msg, _ ->
        { m with CounterByOne = CounterByOne.update msg m.CounterByOne }
    | CounterByTwoMsg msg, _ ->
        { m with CounterByTwo = CounterByTwo.update msg m.CounterByTwo }

let bindings () = [
    "CounterByOne" |> Binding.subModel
        ( fun m -> m.CounterByOne
        , snd
        , CounterByOneMsg
        , CounterByOne.Platform.bindings )
    "CounterByTwo" |> Binding.subModel
        ( fun m -> m.CounterByTwo
        , snd
        , CounterByTwoMsg
        , CounterByTwo.Platform.bindings )
]

let main fwkElement =
    Program.mkSimpleWpf init update bindings
    // TODO: Simplify this call to use Cmd.ofSub
    |> Program.withSubscription (CounterByOne.Platform.CounterByOneSub.sub >> Cmd.map Msg.CounterByOneMsg)
    |> Program.withSubscription (CounterByTwo.Platform.CounterByTwoSub.sub >> Cmd.map Msg.CounterByTwoMsg)
    |> Program.startElmishLoop
        { ElmConfig.Default with
            LogConsole = true
            Measure = true }
        fwkElement
