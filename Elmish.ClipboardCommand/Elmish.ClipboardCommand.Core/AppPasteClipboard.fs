module Elmish.ClipboardCommand.Core.AppPasteClipboard

open Elmish
open Elmish.WPF
open System.Windows

type Model =
    { Items: string list
      SelectedItem: string option }

type CmdMsgs =
    | SetToClipboard of string

let init () =
    let items = [ "A"; "B"; "C" ]
    { Items =  items; SelectedItem = None }, []

type Msg =
    | SelectItem of string option
    | Copy of string
    | Paste of string
    | CmdException of exn

let update msg model =
    match msg with
    | SelectItem x -> { model with SelectedItem = x }, []
    | Copy x -> model, [ SetToClipboard x ]
    | Paste x -> { model with Items = List.append model.Items [ sprintf "%s+" x ] }, []
    | CmdException _ -> model, []

let ClipboardKey = "Some key for test"

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

    //"Paste" |> Binding.cmdParamIf (fun _ _ ->
    "Paste" |> Binding.cmdIf (fun _ ->
        // On the first run, you will see that after you do a copy the paste is still disabled,
        // if you put a breakpoint (or look at the output window) you will see
        // "Is some? true" being printed. You will have to click somewhere to regenerate the state.
        // Now restart the app and you will see that the button is always enabled because the previous
        // copy is still active.
        // Close the app, copy anything else (like this text), re-run the app and you are back to square 1.
        // Note1: You can use Ctrl+V which seems to be always taking the latest state compared to the button.
        // Note2: Using cmd.ParamIf works.
        let result =
            if Clipboard.ContainsData(ClipboardKey)
            then Clipboard.GetData(ClipboardKey) :?> string |> Paste |> Some
            else None

        System.Diagnostics.Debug.WriteLine(sprintf "Is some? %b" result.IsSome)

        result)
]

let bindCmd = function
    | SetToClipboard x ->
        Cmd.OfFunc.attempt
            (fun () -> Clipboard.SetData(ClipboardKey, x)) ()
            CmdException

let main fwkElement =
    Program.mkProgramWpfWithCmdMsg init update bindings bindCmd
    |> Program.startElmishLoop
        { ElmConfig.Default with
            LogConsole = true
            Measure = true }
        fwkElement
