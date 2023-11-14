using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DIMS.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using ReactiveUI;
using Avalonia.Platform.Storage;
using System.Reactive.Disposables;
using System;
using System.ComponentModel;
using Avalonia.Threading;

namespace DIMS.Views
{
    public partial class DatabaseView : ReactiveUserControl<DatabaseViewModel>
    {
        public DatabaseView()
        {
            this.WhenActivated(d =>
            {
                var top = TopLevel.GetTopLevel(this) as MainWindow;
                this.DataContext = (top?.DataContext as MainViewModel)?.DatabaseViewModel;

                d(this.BindCommand(ViewModel, vm => vm.InsertCommand, v => v.InsertButton));
                d(this.BindCommand(ViewModel, vm => vm.ModifyCommand, v => v.ModifyButton));
                d(this.BindCommand(ViewModel, vm => vm.DeleteCommand, v => v.DeleteButton));
                d(this.BindCommand(ViewModel, vm => vm.QueryCommand, v => v.QueryButton));
                d(this.BindCommand(ViewModel, vm => vm.ResetQueryCommand, v => v.ClearButton));
                d(this.BindCommand(ViewModel, vm => vm.ImportProductsCommand, v => v.ImportButton));
                d(this.BindCommand(ViewModel, vm => vm.ExportProductsCommand, v => v.ExportButton));

                d(this.Bind(ViewModel, vm => vm.SelectedTabIndex, v => v.TabContainer.SelectedIndex));

                d(this.Bind(ViewModel, vm => vm.DisplayDeviceId, v => v.DeviceId.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayDeviceName, v => v.DeviceName.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayDeviceDescription, v => v.DeviceDescription.Text));

                d(this.Bind(ViewModel, vm => vm.DisplayProductCategoryId, v => v.ProductCategoryId.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayProductCodeLength, v => v.ProductCodeLength.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayProductModelId, v => v.ProductModelId.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayProductModelName, v => v.ProductModelName.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayProductName, v => v.ProductName.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayProductPosId, v => v.ProductPosId.Text));

                d(this.Bind(ViewModel, vm => vm.DisplayHistoryBeginTime, v => v.HistoryBeginTime.SelectedDate, 
                    x => (DateTimeOffset)(x ?? DateTime.Now), 
                    x => (x ?? DateTimeOffset.Now).DateTime));
                d(this.Bind(ViewModel, vm => vm.DisplayHistoryEndTime, v => v.HistoryEndTime.SelectedDate,
                    x => (DateTimeOffset)(x ?? DateTime.Now),
                    x => (x ?? DateTimeOffset.Now).DateTime));
                d(this.Bind(ViewModel, vm => vm.DisplayHistoryModelName, v => v.HistoryModelName.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayHistoryPosCode, v => v.HistoryPosCode.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayHistoryProductCode, v => v.HistoryProductCode.Text));
                d(this.Bind(ViewModel, vm => vm.DisplayHistoryTrayCode, v => v.HistoryTrayCode.Text));

                d(this.OneWayBind(ViewModel, vm => vm.SelectedResult, v => v.QuerySheet.ItemsSource));
                d(this.Bind(ViewModel, vm => vm.SelectedRow, v => v.QuerySheet.SelectedItem));

                d(ViewModel!.ShowDialog.RegisterHandler(async (interaction) =>
                {
                    bool result = await Dispatcher.UIThread.Invoke(async () => {
                        var param = new MessageBoxStandardParams
                        {
                            FontFamily = "Microsoft YaHei UI",
                            Icon = MsBox.Avalonia.Enums.Icon.Warning,
                            ContentTitle = "Message Dialog",
                            ContentMessage = interaction.Input,
                            ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };

                        var box = MessageBoxManager.GetMessageBoxStandard(param);

                        return await box.ShowAsync() == MsBox.Avalonia.Enums.ButtonResult.Yes;
                    });
                    
                    interaction.SetOutput(result);
                }));

                d(ViewModel!.FilePicker.RegisterHandler(async (interaction) =>
                {
                    var top = TopLevel.GetTopLevel(this);
                    if (top != null)
                    {
                        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                        {
                            AllowMultiple = false,
                            FileTypeFilter = new[] { new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" }, MimeTypes = new[] { "text/csv" } } },
                            Title = interaction.Input
                        });

                        if (files != null && files.Count > 0)
                        {
                            interaction.SetOutput(files[0].Path.LocalPath);
                        }
                        else
                        {
                            interaction.SetOutput(null);
                        }
                    }
                    else
                    { 
                        interaction.SetOutput(null);
                    }
                }));

                d(ViewModel!.FileSavePicker.RegisterHandler(async (interaction) =>
                {
                    var top = TopLevel.GetTopLevel(this);
                    if (top != null)
                    {
                        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
                        {
                            DefaultExtension = "csv", 
                            ShowOverwritePrompt = true, 
                            FileTypeChoices = new[] { new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" }, MimeTypes = new[] { "text/csv" } } },
                            Title = interaction.Input,
                            SuggestedFileName = "products.csv"
                        });

                        if (file != null)
                        {
                            interaction.SetOutput(file.Path.LocalPath);
                        }
                        else
                        {
                            interaction.SetOutput(null);
                        }
                    }
                    else
                    { 
                        interaction.SetOutput(null);
                    }
                }));
            });

            InitializeComponent();
        }
    }
}
