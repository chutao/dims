using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DIMS.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;

namespace DIMS.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();

        var logger = this.FindControl<DataGrid>("LoggerWindow");
        Debug.Assert(logger != null);

        logger.SelectionChanged += OnSelectionChanged;

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.ObservableTrays, v => v.Queue.ItemsSource).DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.ObservableLogs, v => v.LoggerWindow.ItemsSource).DisposeWith(d);
            this.Bind(ViewModel, vm => vm.SelectedLoggerItem, v => v.LoggerWindow.SelectedItem).DisposeWith(d);
            this.Bind(ViewModel, vm => vm.SelectedQueueItem, v => v.Queue.SelectedItem).DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ClearQueueCommand, v => v.ClearQueue).DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.DeleteQueueItemCommand, v => v.DeleteSelected).DisposeWith(d);

            ViewModel?
                .Confirm
                .RegisterHandler(async (interaction) =>
                {
                    var dlg = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard("操作确认",
                        interaction.Input,
                        MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                        MsBox.Avalonia.Enums.Icon.Question);
                    var result = await dlg.ShowAsync();
                    interaction.SetOutput(result == MsBox.Avalonia.Enums.ButtonResult.Yes);
                }).DisposeWith(d);
        }); 
    }

    public void OnSelectionChanged(object? sender, RoutedEventArgs e)
    {
        DataGrid? dataGrid = sender as DataGrid;
        if (dataGrid != null)
        {
            _ = Dispatcher.UIThread.InvokeAsync((Action)(() => dataGrid.ScrollIntoView(dataGrid.SelectedItem, null)), DispatcherPriority.ContextIdle);
        }
    }
}
