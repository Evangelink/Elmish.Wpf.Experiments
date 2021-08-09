namespace WpfApp3
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Windows;
    using Telerik.Windows.Controls;
    using Telerik.Windows.Controls.MaterialControls;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeEffectsHelper.IsAcrylicEnabled = false;

            var window = new MainWindow();
            var previewClosedObs = Observable.FromEventPattern<WindowPreviewClosedEventArgs>(window, nameof(RadWindow.PreviewClosed))
                .Select<EventPattern<WindowPreviewClosedEventArgs>, Action>(x => () => x.EventArgs.Cancel = true);

            window.Show();

            // use this to see correct behavior using latest release of Elmish.WPF
            //ClassLib.App.main(window, previewClosedObs);

            // Use this to see incorrect behavior using beta of Elmish.WPF
            ClassLib2.App.main(window, previewClosedObs);
        }
    }
}
