module Elmish.ClipboardCommand.Core.AppPasteOnModel

open Elmish
open Elmish.WPF
open System.Windows

type Model =
    { Items: string list
      SelectedItem: string option
      CanPaste: bool }

type CmdMsgs =
    | SetToClipboard of string

let ClipboardKey = "Some key for test"

let init () =
    let items = [ "A"; "B"; "C" ]
    // Reinitializing the value from the clipboard here breaks the purity of the function
    // but I can't find any good way to restore the state properly...
    { Items =  items; SelectedItem = None; CanPaste = Clipboard.ContainsData(ClipboardKey) }, []

type Msg =
    | SelectItem of string option
    | Copy of string
    | CopyDone
    | Paste of string
    | CmdException of exn

let update msg model =
    match msg with
    | SelectItem x -> { model with SelectedItem = x }, []
    | Copy x -> model, [ SetToClipboard x ]
    | CopyDone -> { model with CanPaste = true }, []
    | Paste x -> { model with Items = List.append model.Items [ sprintf "%s+" x ] }, []
    | CmdException _ -> model, []


let bindings () = [
    "Items" |> Binding.subModelSeq
        ( fun m -> m.Items
        , id
        , fun () -> [])

    "SelectedItem" |> Binding.subModelSelectedItem
        ( "Items"
        , fun m -> m.SelectedItem
        , SelectItem)

    "Copy" |> Binding.cmdIf (fun m ->
        match m.SelectedItem with
        | Some x -> Copy x |> Some
        | None -> None)

    "Paste" |> Binding.cmdIf (fun m ->
        let result =
            if m.CanPaste
            then Clipboard.GetData(ClipboardKey) :?> string |> Paste |> Some
            else None

        System.Diagnostics.Debug.WriteLine(sprintf "Is some? %b" result.IsSome)

        result)
]

let bindCmd = function
    | SetToClipboard x ->
        Cmd.OfFunc.either
            (fun () -> Clipboard.SetData(ClipboardKey, x)) ()
            (fun () -> CopyDone)
            CmdException

let main fwkElement =
    Program.mkProgramWpfWithCmdMsg init update bindings bindCmd
    |> Program.startElmishLoop
        { ElmConfig.Default with
            LogConsole = true
            Measure = true }
        fwkElement
