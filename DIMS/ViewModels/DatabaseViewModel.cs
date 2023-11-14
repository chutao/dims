using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using System.Collections.ObjectModel;
using DIMS.Models;
using DIMS.Helpers;
using System.Drawing;
using System.Runtime.CompilerServices;
using Splat;
using DynamicData;
using Avalonia.Controls;
using System.IO;

namespace DIMS.ViewModels
{
    public class DatabaseViewModel : ViewModelBase
    {
        public DatabaseViewModel()
        {
            this.WhenAnyValue(vm => vm.SelectedTabIndex)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x =>
                    {
                        SelectedRow = null;

                        if (x == 0)
                            SelectedResult = DeviceDataSource;
                        else if (x == 1)
                            SelectedResult = ProductDataSource;
                        else if (x == 2)
                            SelectedResult = ProductionDataSource;
                        else
                            SelectedResult = null;
                    });

            this.WhenAnyValue(vm => vm.SelectedRow)
                //.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (SelectedTabIndex == 0)
                        SelectedDeviceObject = x;
                    else if (SelectedTabIndex == 1)
                        SelectedProductObject = x;
                });

            this.WhenAnyValue(vm => vm.SelectedDeviceObject)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (x is DeviceDataModel m)
                    {
                        DisplayDeviceDescription = m.Description;
                        DisplayDeviceName = m.DeviceName;
                        DisplayDeviceId = m.Id;
                    }
                });

            this.WhenAnyValue(vm => vm.SelectedProductObject)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (x is ProductDataModel m)
                    {
                        DisplayProductCategoryId = m.Category;
                        DisplayProductCodeLength = m.CodeLength;
                        DisplayProductModelId = m.Model;
                        DisplayProductModelName = m.ModelName;
                        DisplayProductName = m.ProductName;
                        DisplayProductPosId = m.POSCode;
                    }
                });

            //CreateTestData();
        }

        // -------------------------------------------
        // Fields

        // -------------------------------------------
        // Properties

        public Interaction<string?, bool> ShowDialog { get; } = new Interaction<string?, bool>();

        public Interaction<string?, string?> FilePicker { get; } = new Interaction<string?, string?>();

        public Interaction<string?, string?> FileSavePicker { get; } = new Interaction<string?, string?>();

        private int _SelectedTabIndex;
        public int SelectedTabIndex
        {
            get => _SelectedTabIndex;
            set => this.RaiseAndSetIfChanged(ref _SelectedTabIndex, value);
        }

        public ObservableCollection<DeviceDataModel> DeviceDataSource { get; } = new ObservableCollection<DeviceDataModel>();

        public ObservableCollection<ProductDataModel> ProductDataSource { get; } = new ObservableCollection<ProductDataModel>();

        public ObservableCollection<ProductionDataModel> ProductionDataSource { get; } = new ObservableCollection<ProductionDataModel>();

        private object? _SelectResult = null;
        public object? SelectedResult
        {
            get => _SelectResult;
            set => this.RaiseAndSetIfChanged(ref _SelectResult, value);
        }

        private object? _SelectedRow = null;
        public object? SelectedRow
        {
            get => _SelectedRow;
            set => this.RaiseAndSetIfChanged(ref _SelectedRow, value);
        }

        #region Device Page Properties
        private object? _SelectedDeviceObject = null;
        public object? SelectedDeviceObject
        {
            get => _SelectedDeviceObject;
            set => this.RaiseAndSetIfChanged(ref _SelectedDeviceObject, value);
        }

        private int _DisplayDeviceId;
        public int DisplayDeviceId
        {
            get => _DisplayDeviceId;
            set => this.RaiseAndSetIfChanged(ref _DisplayDeviceId, value);
        }

        private string? _DisplayDeviceName;
        public string? DisplayDeviceName
        {
            get => _DisplayDeviceName;
            set => this.RaiseAndSetIfChanged(ref _DisplayDeviceName, value);
        }

        private string? _DisplayDeviceDescription;
        public string? DisplayDeviceDescription
        {
            get => _DisplayDeviceDescription;
            set => this.RaiseAndSetIfChanged(ref _DisplayDeviceDescription, value);
        }
        #endregion

        #region Products Page Properties
        private object? _SelectedProductObject = null;
        public object? SelectedProductObject
        {
            get => _SelectedProductObject;
            set => this.RaiseAndSetIfChanged(ref _SelectedProductObject, value);
        }

        private string? _DisplayProductModelId;
        public string? DisplayProductModelId
        {
            get => _DisplayProductModelId;
            set => this.RaiseAndSetIfChanged(ref _DisplayProductModelId, value);
        }

        private string? _DisplayProductModelName;
        public string? DisplayProductModelName
        {
            get => _DisplayProductModelName;
            set => this.RaiseAndSetIfChanged(ref _DisplayProductModelName, value);
        }

        private string? _DisplayProductPosId;
        public string? DisplayProductPosId
        {
            get => _DisplayProductPosId;
            set => this.RaiseAndSetIfChanged(ref _DisplayProductPosId, value);
        }

        private string? _DisplayProductName;
        public string? DisplayProductName
        {
            get => _DisplayProductName;
            set => this.RaiseAndSetIfChanged(ref _DisplayProductName, value);
        }

        private int _DisplayProductCodeLength;
        public int DisplayProductCodeLength
        {
            get => _DisplayProductCodeLength;
            set => this.RaiseAndSetIfChanged(ref _DisplayProductCodeLength, value);
        }

        private int _DisplayProductCategoryId;
        public int DisplayProductCategoryId
        {
            get => _DisplayProductCategoryId;
            set => this.RaiseAndSetIfChanged(ref _DisplayProductCategoryId, value);
        }
        #endregion

        #region History Page
        private DateTime? _DisplayHistoryBeginTime;
        public DateTime? DisplayHistoryBeginTime
        {
            get => _DisplayHistoryBeginTime;
            set => this.RaiseAndSetIfChanged(ref _DisplayHistoryBeginTime, value);
        }

        private DateTime? _DisplayHistoryEndTime;
        public DateTime? DisplayHistoryEndTime
        {
            get => _DisplayHistoryEndTime;
            set => this.RaiseAndSetIfChanged(ref _DisplayHistoryEndTime, value);
        }

        private string? _DisplayHistoryPosCode;
        public string? DisplayHistoryPosCode
        {
            get => _DisplayHistoryPosCode;
            set => this.RaiseAndSetIfChanged(ref _DisplayHistoryPosCode, value);
        }

        private string? _DisplayHistoryModelName;
        public string? DisplayHistoryModelName
        {
            get => _DisplayHistoryModelName;
            set => this.RaiseAndSetIfChanged(ref _DisplayHistoryModelName, value);
        }

        private string? _DisplayHistoryProductCode;
        public string? DisplayHistoryProductCode
        {
            get => _DisplayHistoryProductCode;
            set => this.RaiseAndSetIfChanged(ref _DisplayHistoryProductCode, value);
        }

        private string? _DisplayHistoryTrayCode;
        public string? DisplayHistoryTrayCode
        {
            get => _DisplayHistoryTrayCode;
            set => this.RaiseAndSetIfChanged(ref _DisplayHistoryTrayCode, value);
        }
        #endregion

        // -------------------------------------------
        // Methods
        private bool CanInsert()
        {
            if (SelectedTabIndex == 0 && !string.IsNullOrEmpty(DisplayDeviceName) && DisplayDeviceId > 0)
                return true;
            if (SelectedTabIndex == 1 && !string.IsNullOrEmpty(DisplayProductPosId) && !string.IsNullOrEmpty(DisplayProductModelId)
                && DisplayProductCodeLength > 0 && DisplayProductCategoryId >= 0)
                return true;

            return false;
        }

        private bool CanModify()
        {
            if (SelectedTabIndex == 0 && !string.IsNullOrEmpty(DisplayDeviceName) && DisplayDeviceId > 0)
                return true;
            if (SelectedTabIndex == 1 && !string.IsNullOrEmpty(DisplayProductPosId) && !string.IsNullOrEmpty(DisplayProductModelId)
                && DisplayProductCodeLength > 0 && DisplayProductCategoryId >= 0)
                return true;

            return false;
        }

        private bool CanDelete()
        {
            if (SelectedTabIndex == 0 && DisplayDeviceId > 0)
                return true;
            if (SelectedTabIndex == 1 && !string.IsNullOrEmpty(DisplayProductPosId))
                return true;

            return false;
        }

        private void CreateTestData()
        {
            for (int i = 0; i < 3; i++)
                DeviceDataSource.Add(new DeviceDataModel() { Id = i + 1, DeviceName = $"测试设备{i + 1}" });
            for (int i = 0; i < 5; i++)
                ProductDataSource.Add(new ProductDataModel() { Model = $"系列{i + 1}", POSCode = "1234", Category = 0 });
            for (int i = 0; i < 10; i++)
                ProductionDataSource.Add(new ProductionDataModel() { ProductCode = $"8888{i}", TrayCode = $"T00{i}", Timestamp = DateTime.Now });
        }

        // -------------------------------------------
        // Commands
        #region Insert Command
        private async Task OnInsertAsync()
        {
            var confirm = await ShowDialog.Handle("确认要将当前输入内容插入数据库中吗?");
            if (confirm)
            {
                switch (SelectedTabIndex)
                {
                    case 0:
                        // Device Table
                        {
                            var model = new DeviceDataModel()
                            {
                                Id = DisplayDeviceId,
                                DeviceName = DisplayDeviceName!.Trim(),
                                Description = DisplayDeviceDescription
                            };
                            bool result = MysqlDbHelper.Default.DeviceInsert(model);
                            var notify = Locator.Current.GetService<INotifyService>();
                            var type = (result) ? Avalonia.Controls.Notifications.NotificationType.Information : Avalonia.Controls.Notifications.NotificationType.Warning;
                            notify?.Notify("消息", $"数据库插入操作{(result ? "成功" : "失败")}!", type, TimeSpan.FromSeconds(3));
                        }
                        break;
                    case 1:
                        // Product Table
                        {
                            var model = new ProductDataModel()
                            {
                                ProductName = DisplayProductName!.Trim(),
                                POSCode = DisplayProductPosId!.Trim(),
                                Model = DisplayProductModelId!.Trim(),
                                ModelName = DisplayProductModelName!.Trim(),
                                Category = DisplayProductCategoryId,
                                CodeLength = DisplayProductCodeLength
                            };
                            bool result = MysqlDbHelper.Default.ProductInsert(model);
                            var notify = Locator.Current.GetService<INotifyService>();
                            var type = (result) ? Avalonia.Controls.Notifications.NotificationType.Information : Avalonia.Controls.Notifications.NotificationType.Warning;
                            notify?.Notify("消息", $"数据库插入操作{(result ? "成功" : "失败")}!", type, TimeSpan.FromSeconds(3));
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private ReactiveCommand<Unit, Unit>? _InsertCommand;
        public ReactiveCommand<Unit, Unit>? InsertCommand
        {
            get
            {
                if (_InsertCommand == null)
                {
                    var s1 = this.WhenAnyValue(v => v.SelectedTabIndex);
                    var s2 = this.WhenAnyValue(v => v.DisplayDeviceId, v => v.DisplayDeviceName);
                    var s3 = this.WhenAnyValue(v => v.DisplayProductModelId, v => v.DisplayProductPosId, v => v.DisplayProductCategoryId, v => v.DisplayProductCodeLength);
                    var canExec = Observable.CombineLatest(s1, s2, s3, (a, b, c) => CanInsert());

                    _InsertCommand = ReactiveCommand.CreateFromTask(() => OnInsertAsync(), canExec);
                }

                return _InsertCommand;
            }
        }
        #endregion

        #region Modify Command
        private async Task OnModifyAsync()
        {
            var confirm = await ShowDialog.Handle("确认要将当前输入内容更新至数据库中吗?");
            if (confirm)
            {
                switch (SelectedTabIndex)
                {
                    case 0:
                        // Device Table
                        {
                            var model = new DeviceDataModel()
                            {
                                Id = DisplayDeviceId,
                                DeviceName = DisplayDeviceName!.Trim(),
                                Description = DisplayDeviceDescription
                            };
                            bool result = MysqlDbHelper.Default.DeviceUpdate(model);
                            var notify = Locator.Current.GetService<INotifyService>();
                            var type = (result) ? Avalonia.Controls.Notifications.NotificationType.Information : Avalonia.Controls.Notifications.NotificationType.Warning;
                            notify?.Notify("消息", $"数据库更新操作{(result ? "成功" : "失败")}!", type, TimeSpan.FromSeconds(3));
                        }
                        break;
                    case 1:
                        // Product Table
                        {
                            var model = new ProductDataModel()
                            {
                                ProductName = DisplayProductName!.Trim(),
                                POSCode = DisplayProductPosId!.Trim(),
                                Model = DisplayProductModelId!.Trim(),
                                ModelName = DisplayProductModelName!.Trim(),
                                Category = DisplayProductCategoryId,
                                CodeLength = DisplayProductCodeLength
                            };
                            bool result = MysqlDbHelper.Default.ProductUpdate(model);
                            var notify = Locator.Current.GetService<INotifyService>();
                            var type = (result) ? Avalonia.Controls.Notifications.NotificationType.Information : Avalonia.Controls.Notifications.NotificationType.Warning;
                            notify?.Notify("消息", $"数据库更新操作{(result ? "成功" : "失败")}!", type, TimeSpan.FromSeconds(3));
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private ReactiveCommand<Unit, Unit>? _ModifyCommand;
        public ReactiveCommand<Unit, Unit>? ModifyCommand
        {
            get
            {
                if (_ModifyCommand == null)
                {
                    var s1 = this.WhenAnyValue(v => v.SelectedTabIndex);
                    var s2 = this.WhenAnyValue(v => v.DisplayDeviceId, v => v.DisplayDeviceName);
                    var s3 = this.WhenAnyValue(v => v.DisplayProductModelId, v => v.DisplayProductPosId, v => v.DisplayProductCategoryId, v => v.DisplayProductCodeLength);
                    var canExec = Observable.CombineLatest(s1, s2, s3, (a, b, c) => CanModify());

                    _ModifyCommand = ReactiveCommand.CreateFromTask(() => OnModifyAsync(), canExec);
                }

                return _ModifyCommand;
            }
        }
        #endregion

        #region Delete Command
        private async Task OnDeleteAsync()
        {
            var confirm = await ShowDialog.Handle("确认要根据当前输入条件删除数据库记录吗?");
            if (confirm)
            {
                switch (SelectedTabIndex)
                {
                    case 0:
                        // Device Table
                        {
                            bool result = MysqlDbHelper.Default.DeviceDelete(DisplayDeviceId);
                            var notify = Locator.Current.GetService<INotifyService>();
                            var type = (result) ? Avalonia.Controls.Notifications.NotificationType.Information : Avalonia.Controls.Notifications.NotificationType.Warning;
                            notify?.Notify("消息", $"数据库删除操作{(result ? "成功" : "失败")}!", type, TimeSpan.FromSeconds(3));
                        }
                        break;
                    case 1:
                        // Product Table
                        {
                            var model = new ProductDataModel()
                            {
                                ProductName = DisplayProductName!.Trim(),
                                POSCode = DisplayProductPosId!.Trim(),
                                Model = DisplayProductModelId!.Trim(),
                                ModelName = DisplayProductModelName!.Trim(),
                                Category = DisplayProductCategoryId,
                                CodeLength = DisplayProductCodeLength
                            };
                            bool result = MysqlDbHelper.Default.ProductDelete(DisplayProductPosId);
                            var notify = Locator.Current.GetService<INotifyService>();
                            var type = (result) ? Avalonia.Controls.Notifications.NotificationType.Information : Avalonia.Controls.Notifications.NotificationType.Warning;
                            notify?.Notify("消息", $"数据库删除操作{(result ? "成功" : "失败")}!", type, TimeSpan.FromSeconds(3));
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private ReactiveCommand<Unit, Unit>? _DeleteCommand;
        public ReactiveCommand<Unit, Unit>? DeleteCommand
        {
            get
            {
                if (_DeleteCommand == null)
                {
                    var s1 = this.WhenAnyValue(v => v.SelectedTabIndex);
                    var s2 = this.WhenAnyValue(v => v.DisplayDeviceId);
                    var s3 = this.WhenAnyValue(v => v.DisplayProductPosId);
                    var canExec = Observable.CombineLatest(s1, s2, s3, (a, b, c) => CanDelete());

                    _DeleteCommand = ReactiveCommand.CreateFromTask(() => OnDeleteAsync(), canExec);
                }

                return _DeleteCommand;
            }
        }
        #endregion

        #region Query Command
        private void OnQuery()
        {
            switch (SelectedTabIndex)
            {
                case 0:
                    // Device Table
                    {
                        DeviceDataSource.Clear();
                        var result = MysqlDbHelper.Default.DeviceQuery(DisplayDeviceName?.Trim(), (DisplayDeviceId > 0 ? DisplayDeviceId.ToString() : null));
                        if (result != null)
                        {
                            DeviceDataSource.AddRange(result);
                        }
                    }
                    break;
                case 1:
                    // Product Table
                    {
                        ProductDataSource.Clear();
                        var result = MysqlDbHelper.Default.ProductQuery(
                            DisplayProductPosId?.Trim(), 
                            DisplayProductModelId?.Trim());
                        if (result != null)
                            ProductDataSource.AddRange(result);
                    }
                    break;
                case 2:
                    // History Table
                    {
                        ProductionDataSource.Clear();
                        var result = MysqlDbHelper.Default.HistoryQuery(
                            DisplayHistoryBeginTime, 
                            DisplayHistoryEndTime, 
                            DisplayHistoryTrayCode?.Trim(), 
                            DisplayHistoryProductCode?.Trim());
                        if (result != null)
                            ProductionDataSource.AddRange(result);
                    }
                    break;
                default:
                    break;
            }
        }

        private ReactiveCommand<Unit, Unit>? _QueryCommand;
        public ReactiveCommand<Unit, Unit>? QueryCommand
        {
            get
            {
                if (_QueryCommand == null)
                {
                    _QueryCommand = ReactiveCommand.Create(() => OnQuery());
                }

                return _QueryCommand;
            }
        }
        #endregion

        #region Reset Query Command
        private void OnResetQuery()
        {
            switch (SelectedTabIndex)
            {
                case 0:
                    DisplayDeviceId = 0;
                    DisplayDeviceName = null;
                    DisplayDeviceDescription = null;
                    break;

                case 1:
                    DisplayProductName = null;
                    DisplayProductModelName = null;
                    DisplayProductPosId = null;
                    DisplayProductModelId = null;
                    break;
                case 2:
                    DisplayHistoryBeginTime = null;
                    DisplayHistoryEndTime = null;
                    DisplayHistoryTrayCode = null;
                    DisplayHistoryProductCode = null;
                    DisplayHistoryPosCode = null;
                    DisplayHistoryModelName = null;
                    break;
                default:
                    break;
            }
        }

        private ReactiveCommand<Unit, Unit>? _ResetQueryCommand;
        public ReactiveCommand<Unit, Unit>? ResetQueryCommand
        {
            get
            {
                if (_ResetQueryCommand == null)
                {
                    _ResetQueryCommand = ReactiveCommand.Create(() => OnResetQuery());
                }

                return _ResetQueryCommand;
            }
        }
        #endregion

        #region ImportProducts Command 
        private ReactiveCommand<Unit, Unit>? _ImportProductsCommand = null;

        private async Task OnImportProductsAsync()
        {
            string? file = await FilePicker.Handle("请选择要导入的CSV数据文件路径:");
            if (File.Exists(file))
            {
                try
                {
                    string[]? lines = null;
                    using (StreamReader sr = new StreamReader(file))
                    { 
                        string content = await sr.ReadToEndAsync();
                        lines = content.Split('\n');
                        sr.Close();
                    }

                    if (lines != null && lines.Length > 1)
                    {
                        for (int i = 1; i < lines.Length; i++)
                        {
                            string line = lines[i];
                            if (string.IsNullOrEmpty(line))
                                continue;

                            line = line.Trim();
                            string[] columns = line.Split(',');
                            if(columns == null || columns.Length < 7)
                                continue;

                            string pcode = columns[0].Trim();
                            string pname = columns[1].Trim();
                            string mcode = columns[2].Trim();
                            string mname = columns[3].Trim();
                            int category = int.Parse(columns[4].Trim());
                            int codelen = int.Parse(columns[5].Trim());
                            bool correction = bool.Parse(columns[6].Trim());

                            ProductDataModel model = new ProductDataModel()
                            {
                                POSCode = pcode,
                                ProductName = pname,
                                Model = mcode,
                                ModelName = mname,
                                Category = category,
                                CodeLength = codelen,
                                Correction = correction
                            };

                            bool success = MysqlDbHelper.Default.ProductInsert(model);
                            if(!success)
                                MysqlDbHelper.Default.ProductUpdate(model);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Log().Debug(ex, "Failed to import data to database");
                }
            }
        }

        public ReactiveCommand<Unit, Unit> ImportProductsCommand
        {
            get
            {
                if (_ImportProductsCommand == null)
                {
                    // var canExec = 
                    _ImportProductsCommand = ReactiveCommand.CreateFromTask(() => OnImportProductsAsync());
                }

                return _ImportProductsCommand;
            }
        }
        #endregion

        #region ExportProducts Command 
        private ReactiveCommand<Unit, Unit>? _ExportProductsCommand = null;

        private async Task OnExportProductsAsync()
        {
            string? file = await FileSavePicker.Handle("请选择要导出的CSV数据文件路径:");
            try
            {
                var table = MysqlDbHelper.Default.ProductQuery(null, null);
                if(table != null)
                {
                    using(StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8))
                    {
                        await sw.WriteLineAsync("PCODE,PNAME,MCODE,MNAME,CATEGORY,CODELEN,CORRECT");

                        foreach(ProductDataModel model in table)
                        {
                            await sw.WriteLineAsync($"{model.POSCode},{model.ProductName},{model.Model},{model.ModelName},{model.Category},{model.CodeLength},{model.Correction}");
                        }

                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex, "Failed to export data from database");
            }
        }

        public ReactiveCommand<Unit, Unit> ExportProductsCommand
        {
            get
            {
                if (_ExportProductsCommand == null)
                {
                    // var canExec = 
                    _ExportProductsCommand = ReactiveCommand.CreateFromTask(() => OnExportProductsAsync());
                }

                return _ExportProductsCommand;
            }
        }
        #endregion
    }
}
