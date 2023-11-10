using Avalonia.Threading;
using DIMS.Hardware;
using DIMS.Helpers;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIMS.ViewModels
{
    public class MonitorViewModel : ViewModelBase, IActivatableViewModel
    {
        public MonitorViewModel()
        {
            this.WhenActivated(d =>
            {
                Observable
                .Interval(TimeSpan.FromSeconds(0.1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    IsTrayScannerConnected = TrayScanner!.IsConnected;
                    IsProductScannerConnected = RfidScanner!.IsConnected;
                    IsPlcConnected = Plc!.IsConnected;
                })
                .DisposeWith(d);
            });
        }

        // -----------------------------------------------------------------------------------------
        // Properties

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        private string? _CurrentTrayCode = "---";
        public string? CurrentTrayCode
        {
            get => _CurrentTrayCode;
            set => this.RaiseAndSetIfChanged(ref _CurrentTrayCode, value);
        }

        private string? _CurrentProductCode = "---";
        public string? CurrentProductCode
        {
            get => _CurrentProductCode;
            set => this.RaiseAndSetIfChanged(ref _CurrentProductCode, value);
        }

        private bool _IsTrayScannerConnected;
        public bool IsTrayScannerConnected
        {
            get => _IsTrayScannerConnected;
            set => this.RaiseAndSetIfChanged(ref _IsTrayScannerConnected, value);
        }

        private bool _IsProductScannerConnected;
        public bool IsProductScannerConnected
        {
            get => _IsProductScannerConnected;
            set => this.RaiseAndSetIfChanged(ref _IsProductScannerConnected, value);
        }

        private bool _IsPlcConnected;
        public bool IsPlcConnected
        {
            get => _IsPlcConnected;
            set => this.RaiseAndSetIfChanged(ref _IsPlcConnected, value);
        }

        private bool? _IsTrayReady;
        public bool? IsTrayReady
        {
            get => _IsTrayReady;
            set => this.RaiseAndSetIfChanged(ref _IsTrayReady, value);
        }

        private uint _AutoStep;
        public uint AutoStep
        {
            get => _AutoStep;
            set => this.RaiseAndSetIfChanged(ref _AutoStep, value);
        }

        private double _AutoTimeMeasure;
        public double AutoTimeMeasure
        {
            get => _AutoTimeMeasure;
            set => this.RaiseAndSetIfChanged(ref _AutoTimeMeasure, value);
        }

        private bool _IsProductScanSuccess = false;
        public bool IsProductScanSuccess
        {
            get => _IsProductScanSuccess;
            set => this.RaiseAndSetIfChanged(ref _IsProductScanSuccess, value);
        }

        private bool _IsTrayScanSuccess = false;
        public bool IsTrayScanSuccess
        {
            get => _IsTrayScanSuccess;
            set => this.RaiseAndSetIfChanged(ref _IsTrayScanSuccess, value);
        }

        private Infoscanner? _TrayScanner = null;
        public Infoscanner? TrayScanner
        {
            get
            {
                if (_TrayScanner == null)
                { 
                    _TrayScanner = Locator.Current.GetService<Infoscanner>();
                }

                return _TrayScanner;
            }
        }

        private MR6100? _RfidScanner = null;
        public MR6100? RfidScanner
        {
            get
            {
                if (_RfidScanner == null)
                {
                    _RfidScanner = Locator.Current.GetService<MR6100>();
                }

                return _RfidScanner;
            }
        }

        private PlcCommunication? _Plc = null;
        public PlcCommunication? Plc
        {
            get
            {
                if (_Plc == null)
                {
                    _Plc = Locator.Current.GetService<PlcCommunication>();
                }

                return _Plc;
            }
        }

        // -----------------------------------------------------------------------------------------
        // Fields


        // -----------------------------------------------------------------------------------------
        // Methods


        // -----------------------------------------------------------------------------------------
        // Commands

        #region TestTrayScan Command 
        private ReactiveCommand<Unit, Unit>? _TestTrayScanCommand = null;

        private async Task OnTestTrayScanAsync()
        {
            if (TrayScanner != null)
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(3000);
                string? code = await TrayScanner.GetBarcodeAsync(cancellationTokenSource.Token);
                await Dispatcher.UIThread.InvokeAsync(() => { CurrentTrayCode = code; });
            }
        }

        public ReactiveCommand<Unit, Unit> TestTrayScanCommand
        {
            get
            {
                if (_TestTrayScanCommand == null)
                {
                    // var canExec = 
                    _TestTrayScanCommand = ReactiveCommand.CreateFromTask(() => OnTestTrayScanAsync());
                }

                return _TestTrayScanCommand;
            }
        }
        #endregion

        #region TestRfidScan Command 
        private ReactiveCommand<Unit, Unit>? _TestRfidScanCommand = null;

        private async Task OnTestRfidScanAsync()
        {
            if (RfidScanner != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    CurrentProductCode = "---";
                });

                if (!RfidScanner.IsConnected)
                    RfidScanner.Connect();

                if (RfidScanner.IsConnected)
                {
                    var result = await RfidScanner.ReadAsync();
                    if (result != null && result.Count > 0)
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            CurrentProductCode = Helper.TrimProductCode(result[0]);
                            var main = Locator.Current.GetService<IDataProvider>() as MainViewModel;
                            if (main != null)
                            {
                                main.DatabaseViewModel.DisplayProductPosId = Helper.GetProductPosId(CurrentProductCode);
                            }
                        });
                }
            }
        }

        public ReactiveCommand<Unit, Unit> TestRfidScanCommand
        {
            get
            {
                if (_TestRfidScanCommand == null)
                {
                    // var canExec = 
                    _TestRfidScanCommand = ReactiveCommand.CreateFromTask(() => OnTestRfidScanAsync());
                }

                return _TestRfidScanCommand;
            }
        }
        #endregion

        #region TestReadySignal Command 
        private ReactiveCommand<Unit, Unit>? _TestReadySignalCommand = null;

        private void OnTestReadySignal()
        {
            _ = Task.Run(() =>
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => { IsTrayReady = true; });
                Thread.Sleep(1000);
                _ = Dispatcher.UIThread.InvokeAsync(() => { IsTrayReady = false; });
            });
        }

        public ReactiveCommand<Unit, Unit> TestReadySignalCommand
        {
            get
            {
                if (_TestReadySignalCommand == null)
                {
                    //var canExec = this.WhenAnyValue(vm => vm.IsPlcConnected);
                    _TestReadySignalCommand = ReactiveCommand.Create(() => OnTestReadySignal());
                }

                return _TestReadySignalCommand;
            }
        }
        #endregion
    }
}
