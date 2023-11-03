using Avalonia.Threading;
using Avalonia.Controls;
using DIMS.Hardware;
using DIMS.Models;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.ViewModels
{
    internal class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel(Window view)
        {
            _View = view;

            OnLoad(Settings.Instance);
        }


        //-------------------------------------------------------------------------------
        // Fields
        private readonly Window _View; 

        //-------------------------------------------------------------------------------
        // Properties
        private string? _PlcAddress;
        public string? PlcAddress
        {
            get => _PlcAddress;
            set => this.RaiseAndSetIfChanged(ref _PlcAddress, value);
        }

        private int _PlcPort;
        public int PlcPort
        {
            get => _PlcPort;
            set => this.RaiseAndSetIfChanged(ref _PlcPort, value);
        }

        private string? _TrayScannerAddress;
        public string? TrayScannerAddress
        {
            get => _TrayScannerAddress;
            set => this.RaiseAndSetIfChanged(ref _TrayScannerAddress, value);
        }

        private int _TrayScannerPort;
        public int TrayScannerPort
        {
            get => _TrayScannerPort;
            set => this.RaiseAndSetIfChanged(ref _TrayScannerPort, value);
        }

        private string? _ProductScannerAddress;
        public string? ProductScannerAddress
        {
            get => _ProductScannerAddress;
            set => this.RaiseAndSetIfChanged(ref _ProductScannerAddress, value);
        }

        private int _ProductScannerPort;
        public int ProductScannerPort
        {
            get => _ProductScannerPort;
            set => this.RaiseAndSetIfChanged(ref _ProductScannerPort, value);
        }
        
        private int _LineCapacity;
        public int LineCapacity 
        {
            get => _LineCapacity;
            set => this.RaiseAndSetIfChanged(ref _LineCapacity, value);
        }

        private int _ListenPort;
        public int ListenPort 
        {
            get => _ListenPort;
            set => this.RaiseAndSetIfChanged(ref _ListenPort, value);
        }

        //-------------------------------------------------------------------------------
        // Methods
        void OnLoad(Settings instance)
        {
            PlcAddress = instance.Application.LinePlcAddress;
            PlcPort = instance.Application.LinePlcPort;
            TrayScannerAddress = instance.Application.TrayScannerIpAddress;
            TrayScannerPort = instance.Application.TrayScannerPort;
            ProductScannerAddress = instance.Application.RfidAddress;
            ProductScannerPort = instance.Application.RfidPort;
            LineCapacity = instance.Application.WorkstationCount;
            ListenPort = instance.Application.JsonRpcPort;
        }

        //-------------------------------------------------------------------------------
        // Commands
        #region Confirm Command
        private ReactiveCommand<Unit, Unit>? _ConfirmCommand;
        public ReactiveCommand<Unit, Unit>? ConfirmCommand
        {
            get
            {
                if(_ConfirmCommand == null)
                {
                    _ConfirmCommand = ReactiveCommand.Create(() => OnConfirm());
                }

                return _ConfirmCommand;
            }
        }

        private void OnConfirm()
        {
            var settings = Settings.Instance;

            settings.Application.LinePlcAddress = PlcAddress;
            settings.Application.LinePlcPort = PlcPort;
            settings.Application.TrayScannerIpAddress = TrayScannerAddress;
            settings.Application.TrayScannerPort = TrayScannerPort;
            settings.Application.RfidAddress = ProductScannerAddress;
            settings.Application.RfidPort = ProductScannerPort;
            settings.Application.WorkstationCount = LineCapacity;
            settings.Application.JsonRpcPort = ListenPort;

            settings.Save();

            if(_View != null)
                _View.Close(true);
        }
        #endregion        

        #region Cancel Command
        private ReactiveCommand<Unit, Unit>? _CancelCommand;
        public ReactiveCommand<Unit, Unit>? CancelCommand
        {
            get
            {
                if(_CancelCommand == null)
                {
                    _CancelCommand = ReactiveCommand.Create(() => OnCancel());
                }

                return _CancelCommand;
            }
        }

        private void OnCancel()
        {

            if(_View != null)
                _View.Close(false);
        }
        #endregion        

        #region LoadDefault Command
        private ReactiveCommand<Unit, Unit>? _LoadDefaultCommand;
        public ReactiveCommand<Unit, Unit>? LoadDefaultCommand
        {
            get
            {
                if(_LoadDefaultCommand == null)
                {
                    _LoadDefaultCommand = ReactiveCommand.Create(() => OnLoadDefault());
                }

                return _LoadDefaultCommand;
            }
        }

        private void OnLoadDefault()
        {
            OnLoad(new Settings());           
        }
        #endregion  
    }
}
