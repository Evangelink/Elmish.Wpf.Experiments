module ClassLib.App

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

    | RequestClose Start, { HasUnsavedChanges = false } -> model, []

    | RequestClose Finish, _ -> model, [ ShutdownApp ]

    | RequestClose Start, { PostSaveRequest = Some CloseApp }
    | RequestClose Cancel, _ -> { model with PostSaveRequest = None }, []

    | RequestClose Start, _ ->
        { model with PostSaveRequest = Some CloseApp }, [ AskSaveOrNotOrCancel ]

    | CmdException _, _ -> model, []

    | Saved, _ -> { model with HasUnsavedChanges = false }, []

let bindings () = []

let mutable cancelCloseAction = Action(fun () -> ())

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

let askUserUnsavedChangesAction (cancelCloseAction: Action) () = async {
    cancelCloseAction.Invoke()
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
            (askUserUnsavedChangesAction cancelCloseAction)
            ()
            id
            CmdException

    | ShutdownApp ->
        Application.Current.Shutdown ()
        Cmd.none

let main window (previewCloseObs: IObservable<Action>) =
    let onPreviewClose dispatch =
        previewCloseObs |> Observable.add (fun cancelClose ->
            cancelCloseAction <- cancelClose
            dispatch (RequestClose Start))

    Program.mkProgramWpfWithCmdMsg init update bindings cmdMsgToCmd
    |> Program.withSubscription (fun _ -> Cmd.ofSub onPreviewClose)
    |> Program.startElmishLoop ElmConfig.Default window