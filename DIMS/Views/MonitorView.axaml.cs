using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using DIMS.Helpers;
using DIMS.ViewModels;
using ReactiveUI;
using System.Globalization;
using System.Reactive.Disposables;

namespace DIMS.Views
{
    public partial class MonitorView : ReactiveUserControl<MonitorViewModel>
    {
        public MonitorView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                var top = TopLevel.GetTopLevel(this) as MainWindow;
                this.DataContext = (top?.DataContext as MainViewModel)?.MonitorViewModel;

                this.OneWayBind(ViewModel, vm => vm.IsTrayScannerConnected, v => v.TrayScannerLink.Text, x => LinkStateToTextConverter.Instance.Convert(x, typeof(string), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsTrayScannerConnected, v => v.TrayScannerIcon.Value, x => LinkStateToIconConverter.Instance.Convert(x, typeof(string), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsTrayScannerConnected, v => v.TrayScannerIcon.Foreground, x => BooleanToBrushConverter.Instance.Convert(x, typeof(Brush), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CurrentTrayCode, v => v.CurrentTrayCode.Text).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.TestTrayScanCommand, v => v.TestTrayScanButton).DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.IsProductScannerConnected, v => v.RfidLink.Text, x => LinkStateToTextConverter.Instance.Convert(x, typeof(string), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsProductScannerConnected, v => v.RfidIcon.Value, x => LinkStateToIconConverter.Instance.Convert(x, typeof(string), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsProductScannerConnected, v => v.RfidIcon.Foreground, x => BooleanToBrushConverter.Instance.Convert(x, typeof(Brush), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CurrentProductCode, v => v.RfidText.Text).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.TestRfidScanCommand, v => v.TestRfidButton).DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.IsPlcConnected, v => v.PlcLink.Text, x => LinkStateToTextConverter.Instance.Convert(x, typeof(string), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsPlcConnected, v => v.PlcIcon.Value, x => LinkStateToIconConverter.Instance.Convert(x, typeof(string), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsPlcConnected, v => v.PlcIcon.Foreground, x => BooleanToBrushConverter.Instance.Convert(x, typeof(Brush), null, CultureInfo.CurrentCulture)).DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.IsTrayReady, v => v.TrayReadyIcon.Background, x => BooleanToBrushConverter.Instance.Convert(x, typeof(Brush), null, CultureInfo.CurrentCulture)).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.TestReadySignalCommand, v => v.TestTrayReadyButton).DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.AutoStep, v => v.AutoStep.Text).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.AutoTimeMeasure, v => v.AutoTimeMeasure.Text, x => x.ToString("F1")).DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.IsProductScanSuccess, v => v.RfidLabel.Background, x => x ? Brushes.Green : Brushes.Red).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsTrayScanSuccess, v => v.TrayLabel.Background, x => x ? Brushes.Green : Brushes.Red).DisposeWith(d);
            });
        }
    }
}
