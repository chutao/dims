using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using DIMS.Hardware;
using DIMS.Helpers;
using DIMS.Models;
using DynamicData;
using DynamicData.Binding;
using Microsoft.VisualStudio.Threading;
using ReactiveUI;
using Splat;
using StreamJsonRpc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DIMS.ViewModels;

public class MainViewModel : ViewModelBase, IDataProvider, Helpers.ILogger, IEnableLogger
{
    public MainViewModel()
    {
        _Trays
            .Connect()
            .Sort(SortExpressionComparer<TrayViewModel>.Ascending(x => x.Index))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _ObservableTrays)
            .Subscribe();

        _Logs
            .Connect()
            .Sort(SortExpressionComparer<LogMessage>.Ascending(x => x.TimeStamp))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _ObservableLogs)
            .Subscribe();

        if(!Settings.Instance.Load())
            Settings.Instance.Save();

        _Scanner = Locator.Current.GetService<Infoscanner>() ?? new Infoscanner();
        _Scanner.Address = Settings.Instance.Application.TrayScannerIpAddress;
        _Scanner.Port = Settings.Instance.Application.TrayScannerPort;

        _Rfid = Locator.Current.GetService<MR6100>() ?? new MR6100();
        _Rfid.IpAddress = Settings.Instance.Application.RfidAddress;
        _Rfid.Port = Settings.Instance.Application.RfidPort;
        _Rfid.EnableAntenna1 = Settings.Instance.Rfid.EnableAntenna1;
        _Rfid.EnableAntenna2 = Settings.Instance.Rfid.EnableAntenna2;
        _Rfid.EnableAntenna3 = Settings.Instance.Rfid.EnableAntenna3;
        _Rfid.EnableAntenna4 = Settings.Instance.Rfid.EnableAntenna4;
        _Rfid.Power = Settings.Instance.Rfid.Power;

        _Plc = Locator.Current.GetService<PlcCommunication>() ?? new PlcCommunication();
        _Plc.Address = Settings.Instance.Application.LinePlcAddress;
        _Plc.Port = Settings.Instance.Application.LinePlcPort;
        _Plc.ProductReadyChanged += (s, e) => { 
            _ = Dispatcher.UIThread.InvokeAsync(() => {
                MonitorViewModel.IsTrayReady = _Plc.IsProductReady;
            });
        };
        _Plc.Connect();

#if false
        Observable
            .Interval(TimeSpan.FromSeconds(3.0))
            .Subscribe(_ =>
            {
                TrayPushIntoQueue(DateTime.Now.ToString("yyyyMMddHHmmss"), "");
                if(_Trays.Count >= _TrayQueueCapacity)
                    TrayPopOutQueue();
            });
#endif

        Info("应用程序启动");

        _JsonRpcThread = new Thread(new ThreadStart(JsonRpcThreacProc));
        _JsonRpcThread.IsBackground = true;
        _JsonRpcThread.Start();
    }

  

    // ---------------------------------------------------------------------------------------------------------------------------------
    // Properties

    private ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
    public ReaderWriterLockSlim Locker => _locker;

    private int _TrayQueueCapacity = 10;
    public int TrayQueueCapacity
    {
        get => _TrayQueueCapacity;
        set => this.RaiseAndSetIfChanged(ref _TrayQueueCapacity, value);
    }

    private object? _SelectedLoggerItem;
    public object? SelectedLoggerItem
    {
        get => _SelectedLoggerItem;
        set => this.RaiseAndSetIfChanged(ref _SelectedLoggerItem, value);
    }

    private bool _IsAutorun = false;
    public bool IsAutorun
    {
        get => _IsAutorun;
        set => this.RaiseAndSetIfChanged(ref _IsAutorun, value);
    }

    private string? _StatusbarMessage;
    public string? StatusbarMessage
    {
        get => _StatusbarMessage;
        set => this.RaiseAndSetIfChanged(ref _StatusbarMessage, value);
    }

    private object? _SelectedQueueItem = null;
    public object? SelectedQueueItem
    {
        get => _SelectedQueueItem;
        set => this.RaiseAndSetIfChanged(ref _SelectedQueueItem, value);
    }

    public DatabaseViewModel DatabaseViewModel { get; } = new DatabaseViewModel();

    public MonitorViewModel MonitorViewModel { get; } = new MonitorViewModel();

    // ----------------------------------------------------------------------------------------------------------------------------------
    // Fields

    private static uint _TrayCounter = 0;

    private SourceList<TrayViewModel> _Trays = new SourceList<TrayViewModel>();

    private ReadOnlyObservableCollection<TrayViewModel>? _ObservableTrays = null;
    public ReadOnlyObservableCollection<TrayViewModel>? ObservableTrays => _ObservableTrays;

    private SourceList<LogMessage> _Logs = new SourceList<LogMessage>();
    private ReadOnlyObservableCollection<LogMessage>? _ObservableLogs = null;
    public ReadOnlyObservableCollection<LogMessage>? ObservableLogs => _ObservableLogs;

    private PlcCommunication _Plc;
    private Infoscanner _Scanner;
    private MR6100 _Rfid;

    private Thread _JsonRpcThread;
    private volatile bool _JsonRpcThreadExitFlag = false;

    private Thread? _AutoThread;
    private volatile bool _AutoThreadExitFlag = false;

    public Interaction<string, bool> Confirm { get; } = new Interaction<string, bool>();

    // ----------------------------------------------------------------------------------------------------------------------------------
    // Methods

    public bool TrayPushIntoQueue(string? rfid, string? traycode)
    {
        bool result = false;

        _locker.EnterWriteLock();

        if (_Trays.Count < _TrayQueueCapacity)
        {
            var model = new TrayViewModel() { Index = _TrayCounter++, Rfid = rfid, TrayCode = traycode };
            var last = Helper.GetLastProductionRecord(rfid);
            if (last != null)
            {
                model.IsExist = true;
                model.StatusWord = last.State;
            }

            var productModel = MysqlDbHelper.Default.ProductQuery(Helper.GetProductPosId(rfid), null)?.FirstOrDefault();
            if (productModel != null)
            {
                model.Model = productModel.Model;
                model.ModelIndex = productModel.Category;
                model.Product = productModel.ModelName;
            }
            
            MysqlDbHelper.Default.HistoryInsert(new ProductionDataModel() { ProductCode = rfid, TrayCode = traycode, Timestamp = DateTime.Now });
            _Trays.Add(model);
            result = true;
        }

        _locker.ExitWriteLock();

        return result;
    }

    public bool TrayPopOutQueue()
    {
        bool result = false;

        _locker.EnterWriteLock();
        if (_Trays.Count > 0)
        {
            _Trays.RemoveAt(0);
            result = true;
        }
        _locker.ExitWriteLock();

        return result;
    }

    public IEnumerable<TrayViewModel>? GetTrays()
    {
        return _Trays.Items;
    }

    #region ILogger Implements
    private void AppendLogger(LogLevel level, string msg, Exception? ex = null)
    {
        if (ex == null)
            _Logs.Add(new LogMessage() { LogLevel = level, Message = msg, TimeStamp = DateTime.Now });
        else
            _Logs.Add(new LogMessage() { LogLevel = level, Message = msg + " " + ex.Message, TimeStamp = DateTime.Now });

        if (_Logs.Count > 1000)
            _Logs.RemoveAt(0);

        _ = Dispatcher.UIThread.InvokeAsync(() => {
            SelectedLoggerItem = _Logs.Count > 0 ? _Logs.Items.Last() : null;
        });
    }


    public void Info(string msg)
    {
        this.Log().Info(msg);
        AppendLogger(LogLevel.Info, msg);
    }

    public void Info(Exception ex, string msg)
    {
        this.Log().Info(ex, msg);
        AppendLogger(LogLevel.Info, msg, ex);
    }

    public void Debug(string msg)
    {
        this.Log().Debug(msg);
        AppendLogger(LogLevel.Debug, msg);
    }

    public void Debug(Exception ex, string msg)
    {
        this.Log().Debug(ex, msg);
        AppendLogger(LogLevel.Debug, msg, ex);
    }

    public void Warn(string msg)
    {
        this.Log().Warn(msg);
        AppendLogger(LogLevel.Warn, msg);
    }

    public void Warn(Exception ex, string msg)
    {
        this.Log().Warn(ex, msg);
        AppendLogger(LogLevel.Warn, msg, ex);
    }

    public void Error(string msg)
    {
        this.Log().Error(msg);
        AppendLogger(LogLevel.Error, msg);
    }

    public void Error(Exception ex, string msg)
    {
        this.Log().Error(ex, msg);
        AppendLogger(LogLevel.Error, msg, ex);
    }

    public void Fatal(string msg)
    {
        this.Log().Fatal(msg);
        AppendLogger(LogLevel.Fatal, msg);
    }

    public void Fatal(Exception ex, string msg)
    {
        this.Log().Fatal(ex, msg);
        AppendLogger(LogLevel.Fatal, msg, ex);
    }
    #endregion

    #region JSON-RPC Implements
    public void TerminateJsonRpcThread()
    {
        _JsonRpcThreadExitFlag = true;
        _JsonRpcThread.Join();
    }

    private void JsonRpcThreacProc()
    {
        try
        {
            int port = Settings.Instance.Application.JsonRpcPort;
            if (port <= 0)
                port = 12000;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Info($"JSON-RPC服务开始监听0.0.0.0:{port}");

            while (!_JsonRpcThreadExitFlag)
            {
                TcpClient client = listener.AcceptTcpClient();
                Info($"接收到客户端{client.Client.RemoteEndPoint}请求");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var endPoint = client.Client.RemoteEndPoint;
                        NetworkStream stream = client.GetStream();
                        var rpc = JsonRpc.Attach(stream, new JsonRpcServer(this));
                        await rpc.Completion;
                        Info($"断开客户端{endPoint}连接");
                        rpc.Dispose();
                        await stream.DisposeAsync();
                        client.Dispose();
                    }
                    catch (Exception te)
                    {
                        Debug(te, "应答线程报错");
                    }
                });
            }

            listener.Stop();
        }
        catch (Exception ex)
        {
            Debug(ex, "服务线程异常");
        }
        finally
        {
            Fatal("服务线程退出");
        }
    }
    #endregion

    #region Auto Thread Proc
    private uint _step = 0;
    private string? _cachedTrayCode;
    private string? _cachedProductCode;
    private bool _cachedTrayReady;
    private Stopwatch _autoTimeMeasureWatch = new Stopwatch();
#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void AutoThreadProc()
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        Dispatcher.UIThread.Post(() => IsAutorun = true);

        try
        {
            while (!_AutoThreadExitFlag)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    MonitorViewModel.AutoTimeMeasure = _autoTimeMeasureWatch.ElapsedMilliseconds * 0.001;
                    MonitorViewModel.AutoStep = _step;
                });
                
                switch (_step)
                {
                    case 0: // 初始化状态
                        {
                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                MonitorViewModel.CurrentProductCode = "";
                                MonitorViewModel.CurrentTrayCode = "";
                            });

                            _cachedProductCode = null;
                            _cachedTrayCode = null;
                            _step = 10;
                        }
                        break;

                    case 10: // 等待产品就绪
                        {
                            if (MonitorViewModel.IsTrayReady.HasValue && MonitorViewModel.IsTrayReady.Value != _cachedTrayReady)
                            {
                                _cachedTrayReady = MonitorViewModel.IsTrayReady.Value;
                                if (_cachedTrayReady)
                                {
                                    _autoTimeMeasureWatch.Restart();
                                    _step = 20;
                                }
                            }
                        }
                        break;

                    case 20: // 触发扫码
                        {
                            _cachedTrayCode = await _Scanner.GetBarcodeAsync();
                            List<string>? items = await _Rfid.ReadAsync();

                            if(items != null && items.Count > 0)
                                _cachedProductCode = Helper.TrimProductCode(items[0]);

                            if (!string.IsNullOrEmpty(_cachedTrayCode) && !string.IsNullOrEmpty(_cachedProductCode))
                            {
                                _ = Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    MonitorViewModel.CurrentProductCode = _cachedProductCode;
                                    MonitorViewModel.CurrentTrayCode = _cachedTrayCode;

                                    MonitorViewModel.IsProductScanSuccess = true;
                                    MonitorViewModel.IsTrayScanSuccess = true;
                                });

                                if(_Plc != null && _Plc.IsConnected)
                                    _Plc.SetBindState(true, true);

                                _step = 30;
                            }
                            else
                            {
                                _step = 100;
                            }
                        }
                        break;

                    case 30: // 绑定条码
                        {
                            if (_Trays.Count >= TrayQueueCapacity)
                                TrayPopOutQueue();
                            if (!TrayPushIntoQueue(_cachedProductCode, _cachedTrayCode))
                            { 
                                this.Log().Warn($"Failed to bind rfid {_cachedProductCode} with tray {_cachedTrayCode}");
                            }

                            _step = 40;
                        }
                        break;

                    case 40: // 清理结果
                        {
                            _autoTimeMeasureWatch.Stop();
                            _step = 0;
                        }
                        break;

                    case 100: // 绑定失败
                        { 

                            bool rfidOk = true, barcodeOk = true;
                            if (string.IsNullOrEmpty(_cachedTrayCode))
                            {
                                barcodeOk = false;
                            }

                            if (string.IsNullOrEmpty(_cachedProductCode))
                            {
                                rfidOk = false;
                            }

                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                MonitorViewModel.CurrentTrayCode = _cachedTrayCode;
                                MonitorViewModel.IsTrayScanSuccess = barcodeOk;

                                MonitorViewModel.CurrentProductCode = _cachedProductCode;
                                MonitorViewModel.IsProductScanSuccess = rfidOk;
                            });

                            if(_Plc != null && _Plc.IsConnected)
                                _Plc.SetBindState(rfidOk, barcodeOk);

                            var notify = Locator.Current.GetService<INotifyService>();
                            if (notify != null){
                                string msgEx = "";
                                if(rfidOk == false && barcodeOk == true)
                                    msgEx = "RFID读取失败";
                                else if(rfidOk == true && barcodeOk == false)
                                    msgEx = "托盘扫码读取失败";
                                else
                                    msgEx = "RFID及托盘扫码均读取失败";
                                    
                                notify.Notify("警告", "绑定条码失败!\n原因:" + msgEx, Avalonia.Controls.Notifications.NotificationType.Warning);
                            }

                            _autoTimeMeasureWatch.Stop();                           
                            _step = 0;
                        }
                        break;
                    default:
                        break;
                }

                Thread.Sleep(2);
            }
        }
        catch (Exception ex)
        { 
            Debug(ex, "自动流程异常退出");
        }

        Dispatcher.UIThread.Post(() => IsAutorun = false);
    }
    #endregion

    // ---------------------------------------------------------------------------------------------
    // Commands

    #region Run Command 
    private ReactiveCommand<Unit, Unit>? _RunCommand = null;

    private void OnRun()
    {
        if (_AutoThread != null)
            OnStop();

        _AutoThread = new Thread(new ThreadStart(AutoThreadProc));
        _AutoThread.Name = "AutoTaskThread";
        _AutoThread.IsBackground = true;

        _AutoThreadExitFlag = false;
        _AutoThread.Start();
    }

    public ReactiveCommand<Unit, Unit> RunCommand
    {
        get
        {
            if (_RunCommand == null)
            {
                var canExec = this.WhenAnyValue(vm => vm.IsAutorun).Select(x => !x);
                _RunCommand = ReactiveCommand.Create(() => OnRun(), canExec);
            }

            return _RunCommand;
        }
    }
    #endregion

    #region Stop Command 
    private ReactiveCommand<Unit, Unit>? _StopCommand = null;

    private void OnStop()
    {
        if (_AutoThread != null)
        {
            _AutoThreadExitFlag = true;

            if (_AutoThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                _AutoThread.Interrupt();

            _AutoThread.Join();
            _AutoThread = null;
        }
    }

    public ReactiveCommand<Unit, Unit> StopCommand
    {
        get
        {
            if (_StopCommand == null)
            {
                var canExec = this.WhenAnyValue(vm => vm.IsAutorun);
                _StopCommand = ReactiveCommand.Create(() => OnStop(), canExec);
            }

            return _StopCommand;
        }
    }
    #endregion

    #region Configuration Command 
    private ReactiveCommand<Unit, Unit>? _ConfigurationCommand = null;

    private async Task OnConfigurationAsync()
    {
        var main = Locator.Current.GetService<INotifyService>();
        System.Diagnostics.Debug.Assert(main != null);
        var settings = new Views.SettingsView();
        await settings.ShowDialog<bool>((Window)main);
    }

    public ReactiveCommand<Unit, Unit> ConfigurationCommand
    {
        get
        {
            if (_ConfigurationCommand == null)
            {
                // var canExec = 
                _ConfigurationCommand = ReactiveCommand.CreateFromTask(() => OnConfigurationAsync());
            }

            return _ConfigurationCommand;
        }
    }
    #endregion

    #region ClearQueue Command 
    private ReactiveCommand<Unit, Unit>? _ClearQueueCommand = null;

    private async Task OnClearQueueAsync()
    {
        bool confirm = await Confirm.Handle("确认要清除所有队列内容?");
        if (confirm)
        {
            lock(_locker)
            {
                _Trays.Clear();
            }
        }
    }

    public ReactiveCommand<Unit, Unit> ClearQueueCommand
    {
        get
        {
            if (_ClearQueueCommand == null)
            {
                // var canExec = 
                _ClearQueueCommand = ReactiveCommand.CreateFromTask(() => OnClearQueueAsync());
            }

            return _ClearQueueCommand;
        }
    }
    #endregion

    #region DeleteQueueItem Command 
    private ReactiveCommand<Unit, Unit>? _DeleteQueueItemCommand = null;

    private async Task OnDeleteQueueItemAsync()
    {
        if (SelectedQueueItem is TrayViewModel item)
        {
            bool confirm = await Confirm.Handle("确认要删除选中项?");
            if (confirm)
            {
                lock (_locker)
                {
                    _Trays.Remove(item);
                }
            }
        }
    }

    public ReactiveCommand<Unit, Unit> DeleteQueueItemCommand
    {
        get
        {
            if (_DeleteQueueItemCommand == null)
            {
                var canExec = this.WhenAnyValue(vm => vm.SelectedQueueItem).Select(x => x is TrayViewModel model && model != null);
                _DeleteQueueItemCommand = ReactiveCommand.CreateFromTask(() => OnDeleteQueueItemAsync(), canExec);
            }

            return _DeleteQueueItemCommand;
        }
    }
    #endregion
}
