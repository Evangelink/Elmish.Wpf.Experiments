module ElmishEventModel.Core.App

open Elmish
open Elmish.WPF
open System
open System.Windows
open System.Windows.Input
open FSharp.Control.Reactive

type Model =
    { Count: uint
      IsChecked: bool }

type CmdMsg =
    | NoOp

let init () =
    let model =
        { IsChecked = false
          Count = 0u }

    model, []

type Msg =
    | NoOp
    | AddOne
    | SetIsChecked of bool

let update msg model =
    match msg, model with
    | NoOp, _ -> model, []
    | AddOne, { IsChecked = false } -> { model with Count = model.Count + 1u }, []
    // I need to handle this case and do nothing but ideally I don't want to care
    // and would like to not have the specific case here.
    | AddOne, { IsChecked = true } -> model, []
    | SetIsChecked v, _ -> { model with IsChecked = v }, []


let bindings () = [
    "IsChecked" |> Binding.twoWay
        ( fun m -> m.IsChecked
        , fun v -> SetIsChecked v )

    "Count" |> Binding.oneWay (fun m -> m.Count)

    "AddOne" |> Binding.cmdIf (fun m ->
        match m.IsChecked with
        | true -> None
        | false -> Some AddOne )
    ]

let bindCmd = function
    | CmdMsg.NoOp -> Cmd.ofMsg NoOp

let registerEvents (fwkElement: FrameworkElement) dispatch =
    fwkElement.KeyDown
    |> Observable.filter (fun ev -> ev.Key = Key.D && ev.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
    // Idea would be to have access to some IObs<Model> which I could combine and filter-on here
    // so that I don't trigger the AddOne when the command is disabled
    |> Observable.add (fun _ -> AddOne |> dispatch)

let main fwkElement =
    Program.mkProgramWpfWithCmdMsg init update bindings bindCmd
    |> Program.withSubscription (fun _ -> Cmd.ofSub (registerEvents fwkElement))
    |> Program.startElmishLoop
        { ElmConfig.Default with
            LogConsole = true
            Measure = true }
        fwkElement
