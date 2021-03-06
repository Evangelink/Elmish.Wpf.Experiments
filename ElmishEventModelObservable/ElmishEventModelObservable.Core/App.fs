﻿module ElmishEventModel.Core.App

open Elmish
open Elmish.WPF
open System.Windows
open System.Windows.Input
open FSharp.Control.Reactive

type Model =
    { Count: uint
      IsChecked: bool }

type CmdMsg =
    | NoOp

let modelObs = new System.Reactive.Subjects.Subject<Model>()

let init () =
    let model =
        { IsChecked = false
          Count = 0u }

    model, []

type Msg =
    | NoOp
    | AddOne
    | SetIsChecked of bool

module AppSub =
    // A "dispatch" function which can send an Analysis message to the update function
    // This is mutable and a dummy function initially because we don't have access to it yet
    let mutable privateDispatch: (Msg -> unit) = ignore

    let dispatch msg = privateDispatch msg
    // A "sub" which is wired into the main Elmish program (in App.fs).
    // It has a dispatch function which we can assign to our mutable dispatch function above.
    let sub _ = (fun d -> privateDispatch <- d) |> Elmish.Cmd.ofSub

let update msg model =
    let model', msgCmds =
        match msg, model with
        | NoOp, _ -> model, []
        | AddOne, { IsChecked = false } -> { model with Count = model.Count + 1u }, []
        | SetIsChecked v, _ -> { model with IsChecked = v }, []
        | _ -> failwith "Unexpected state"

    modelObs.OnNext(model')

    model', msgCmds


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

let registerEvents (fwkElement: FrameworkElement) =
    fwkElement.KeyDown
    |> Observable.filter (fun ev -> ev.Key = Key.D && ev.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
    |> Observable.withLatestFrom (fun _ m -> m) modelObs
    |> Observable.filter (fun m -> not m.IsChecked)
    |> Observable.add (fun _ -> AddOne |> AppSub.dispatch)

let main fwkElement =
    registerEvents fwkElement

    Program.mkProgramWpfWithCmdMsg init update bindings bindCmd
    |> Program.withSubscription (AppSub.sub)
    |> Program.startElmishLoop
        { ElmConfig.Default with
            LogConsole = true
            Measure = true }
        fwkElement
