using DIMS.Core;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DIMS.Tester
{
    internal class MainWindowViewModel : ReactiveObject
    {
        private string? _Address = "192.168.8.11"; 
        public string? Address
        {
            get => _Address;
            set => this.RaiseAndSetIfChanged(ref _Address, value);
        }

        private int _Port = 12000;
        public int Port
        {
            get => _Port;
            set => this.RaiseAndSetIfChanged(ref _Port, value);
        }

        private JsonRpcClient? _client = null;

        private string? _TrayCode;
        public string? TrayCode
        {
            get => _TrayCode;
            set => this.RaiseAndSetIfChanged(ref _TrayCode, value);
        }

        private string? _Message;
        public string? Message
        {
            get => _Message;
            set => this.RaiseAndSetIfChanged(ref _Message, value);
        }

        #region Query Command 
        private ReactiveCommand<Unit, Unit>? _QueryCommand = null;

        private async Task OnQueryAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Address))
                    return;

                if (_client == null)
                    _client = new JsonRpcClient(Address, Port);

                for (int i = 0; i < 10000; i++)
                {
                    TrayCode = i.ToString();
                    var result = await _client.QueryAsync(TrayCode);

                    if (result == null)
                        Message = "返回为空!";
                    else
                    {
                        Message = JsonConvert.SerializeObject(result, Formatting.Indented);
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            { 
            
            }
            
        }

        public ReactiveCommand<Unit, Unit> QueryCommand
        {
            get
            {
                if (_QueryCommand == null)
                {
                    var canExec = this.WhenAnyValue(x => x.TrayCode).Select(x => !string.IsNullOrEmpty(x));
                    _QueryCommand = ReactiveCommand.CreateFromTask(() => OnQueryAsync(), canExec);
                }

                return _QueryCommand;
            }
        }
        #endregion
    }
}
