module ElmishThrottle.Core.App

open Elmish
open Elmish.WPF
open System
open System.Drawing
open System.Windows.Controls
open System.IO
open System.Windows.Media.Imaging

[<RequireQualifiedAccess>]
module Math =
    let roundAwayFromZero (x: float) =
        Math.Round(x, MidpointRounding.AwayFromZero) |> int

module Async =
    let ofFunc f a =
        async { return f a }

module Dispatch =
    open Elmish
    open FSharp.Control.Reactive
    open System.Reactive.Subjects

    let throttle (dueTime: TimeSpan) (dispatch: Dispatch<_>) : Dispatch<_> =
        let subject = new Subject<_>()
        subject :> IObservable<_> |> Observable.sample dueTime |> Observable.subscribe dispatch |> ignore
        subject.OnNext |> Async.ofFunc >> Async.Start

type Model =
    { CurrentIndex: uint
      MaxIndex: uint }

type CmdMsg =
    | DrawFrameCmd

let init () =
    let model = { CurrentIndex = 0u; MaxIndex = 10000u }
    model, [ DrawFrameCmd ]

type Msg =
    | SelectFrame of uint
    | CmdException of exn

let update msg model =
    match msg with
    | CmdException exn -> model, []
    | SelectFrame index -> { model with CurrentIndex = index }, [ DrawFrameCmd ]

let imageViewer =
    lazy Image(Width = 200., Height = 100.)

let bindings () = [
    "ImageViewer" |> Binding.oneWay (fun _ -> imageViewer.Value)
    "CurrentFrameIndex" |> Binding.twoWay
        ( fun (m: Model) -> m.CurrentIndex |> float
        , Math.roundAwayFromZero >> uint32 >> SelectFrame )
        //, Dispatch.throttle (TimeSpan.FromMilliseconds 50.))
    "FrameMaxIndex" |> Binding.oneWay (fun m -> m.MaxIndex |> float)
    ]

let displayImage (imageViewer: Image) =
    use bitmap = Image.FromFile("elmish-wpf-logo.bmp") :?> Bitmap
    let ms = new MemoryStream()
    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp)
    let bitmapImage = new BitmapImage()
    bitmapImage.BeginInit()
    ms.Seek(0L, SeekOrigin.Begin) |> ignore
    bitmapImage.StreamSource <- ms
    bitmapImage.EndInit()

    imageViewer.Source <- bitmapImage
    System.Threading.Thread.Sleep(25)

let bindCmd = function
    | CmdMsg.DrawFrameCmd ->
        Cmd.OfFunc.attempt
            displayImage imageViewer.Value
            CmdException

let main fwkElement =
    Program.mkProgramWpfWithCmdMsg init update bindings bindCmd
    |> Program.startElmishLoop
        { ElmConfig.Default with
            LogConsole = true
            Measure = true }
        fwkElement
