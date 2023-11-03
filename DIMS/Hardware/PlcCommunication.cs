using Avalonia.Controls.Notifications;
using DIMS.Models;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIMS.Hardware
{
    public class PlcCommunication : IEnableLogger
    {
        private string _address = "";
        private int _port = 0;
        private Thread _thread;
        private volatile bool _exit = false;
        private HslCommunication.Profinet.Melsec.MelsecMcNet _MelsecMcHandler;

        public PlcCommunication()
        {
            _MelsecMcHandler = new HslCommunication.Profinet.Melsec.MelsecMcNet();
            _MelsecMcHandler.ReceiveTimeOut = 3000;
            _MelsecMcHandler.ConnectTimeOut = 1000;

            _thread = new Thread(new ThreadStart(BackgroundThreadProc));
            _thread.Name = nameof(BackgroundThreadProc);
            _thread.IsBackground = true;
        }

        public PlcCommunication(string? address, int port)
            : this()
        {
            _address = address ?? "127.0.0.1";
            _port = port;
        }

        public bool IsConnected { get; private set; }

        public event EventHandler? ProductReadyEvent;
        private bool _IsProductReady;
        public bool IsProductReady
        {
            get => _IsProductReady;
            set
            {
                if (_IsProductReady != value)
                {
                    _IsProductReady = value;
                    if(_IsProductReady)
                        ProductReadyEvent?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string? Address
        {
            get => _address;
            set => _address = value ?? "127.0.0.1";
        }

        public int Port
        {
            get => _port;
            set => _port = value;
        }

        public bool Connect()
        {
            if (IsConnected)
                return true;

            _exit = false;
            _thread.Start();

            return true;
        }

        public void Disconnect()
        {
            _exit = true;
            _thread.Join(100);
            IsConnected = false;
        }

        public bool SetBindState(bool rfidOk, bool barcodeOk)
        {
            if (!IsConnected)
                return false;

            short flag = 0;
            if(rfidOk && barcodeOk)
                flag = 1;
            else if(!rfidOk)
                flag = 2;
            else if(!barcodeOk)
                flag = 3;
            var result = _MelsecMcHandler.Write("D1010", flag);
            if(result.IsSuccess)
            {
                _ = Task.Factory.StartNew(() => {
                    Thread.Sleep(2000);
                    _MelsecMcHandler.Write("D1010", (short)0);
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
            }

            return result.IsSuccess;
        }

        private void BackgroundThreadProc()
        {
            INotifyService? notify = Locator.Current.GetService<INotifyService>();

            try
            {
                while (!_exit)
                {
                    try
                    {
                        if (!IsConnected)
                        {
                            _MelsecMcHandler.IpAddress = _address;
                            _MelsecMcHandler.Port = _port;
                            var state = _MelsecMcHandler.ConnectServer();
                            IsConnected = state.IsSuccess;
                            if (!IsConnected)
                            {
                                notify?.Notify("警告", $"PLC通讯连接失败!\n{state.Message}", NotificationType.Warning, TimeSpan.FromSeconds(10));

                                Thread.Sleep(3000);
                                continue;
                            }

                            _MelsecMcHandler.Write("D1010", (short)0);
                        }
                        else
                        {
                            var d1000 = _MelsecMcHandler.ReadInt16("D1000");
                            if (d1000.IsSuccess)
                                IsProductReady = d1000.Content == 1;
                            else
                                IsConnected = false;
                        }
                    }
                    catch (Exception e)
                    {
                        notify?.Notify("警告", $"PLC通讯异常!\n{e.Message}", NotificationType.Warning, TimeSpan.FromSeconds(10));
                        IsConnected = false;
                        _MelsecMcHandler.ConnectClose();
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex, "PLC communication failed");
                notify?.Notify("警告", "PLC通讯线程异常退出!", NotificationType.Warning, TimeSpan.FromSeconds(30));
            }
        }
    }
}
