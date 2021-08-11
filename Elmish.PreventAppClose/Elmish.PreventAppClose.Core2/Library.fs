module ClassLib2.App

open System
open Elmish.WPF
open Elmish
open System.Windows

type PostSaveRequest =
    | CloseApp

type Model = {
    HasUnsavedChanges: bool
    PostSaveRequest: PostSaveRequest option
}

type CmdMsg =
    | ShutdownApp
    | AskSaveOrNotOrCancel

let init () =
    { HasUnsavedChanges = true; PostSaveRequest = None }, []

type UnsavedContent =
    | SaveChanges
    | DontSaveChanges
    | CancelChanges

type Request =
    | Start
    | Cancel
    | Finish

type Msg =
    | NoOp
    | CmdException of exn
    | RequestClose of Request
    | Saved

let update msg model =
    match msg, model with
    | NoOp, _ -> model, []

    | RequestClose Start, { HasUnsavedChanges = false } -> model, [ ShutdownApp ]

    | RequestClose Finish, _ -> model, [ ShutdownApp ]

    | RequestClose Start, { PostSaveRequest = Some CloseApp }
    | RequestClose Cancel, _ -> { model with PostSaveRequest = None }, []

    | RequestClose Start, _ ->
        { model with PostSaveRequest = Some CloseApp }, [ AskSaveOrNotOrCancel ]

    | CmdException _, _ -> model, []

    | Saved, _ -> { model with HasUnsavedChanges = false }, []

let bindings () = [
  "PreviewClosed" |> Binding.cmdParam (fun obj ->
    let args = obj :?> Telerik.Windows.Controls.WindowPreviewClosedEventArgs
    args.Cancel <- true
    RequestClose Start)
]

[<RequireQualifiedAccess>]
module Dispatch =
    let funcOnMainThread (f: 'T1 -> 'T2) (arg: 'T1) =
        System.Windows.Application.Current.Dispatcher.Invoke (fun () ->
            let guiCtx = Threading.SynchronizationContext.Current

            async {
                do! Async.SwitchToContext guiCtx
                return f arg
            })

module Dialogs =
    let promptUser =
        Dispatch.funcOnMainThread (fun () ->
            match MessageBox.Show("Save changes?", "", MessageBoxButton.YesNoCancel) with
            | MessageBoxResult.Yes -> SaveChanges
            | MessageBoxResult.No -> DontSaveChanges
            | _ -> CancelChanges)

let askUserUnsavedChangesAction () = async {
    let! result = Dialogs.promptUser ()

    return
        match result with
        | SaveChanges -> Saved
        | DontSaveChanges -> RequestClose Finish
        | CancelChanges -> RequestClose Cancel
}

let cmdMsgToCmd = function
    | AskSaveOrNotOrCancel ->
        Cmd.OfAsync.either
            askUserUnsavedChangesAction
            ()
            id
            CmdException

    | ShutdownApp ->
        Application.Current.Shutdown ()
        Cmd.none

let main window =
    //let onPreviewClose dispatch =
    //    previewCloseObs |> Observable.add (fun cancelClose ->
    //        cancelCloseAction <- cancelClose
    //        dispatch (RequestClose Start))

    WpfProgram.mkProgramWithCmdMsg init update bindings cmdMsgToCmd
    //|> WpfProgram.withSubscription (fun _ -> Cmd.ofSub onPreviewClose)
    |> WpfProgram.startElmishLoop window