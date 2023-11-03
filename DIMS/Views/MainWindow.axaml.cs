using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using DIMS.Models;
using DIMS.ViewModels;
using NLog;
using ReactiveUI;
using Splat;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace DIMS.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>, INotifyService
{
   
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.StatusbarMessage, v => v.Message.Text).DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.RunCommand, v => v.Run).DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.StopCommand, v => v.Stop).DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.ConfigurationCommand, v => v.Settings).DisposeWith(d);

            Observable.Start(() => Unit.Default)
                .InvokeCommand(ViewModel, vm => vm.RunCommand);
        }); 

        Locator.CurrentMutable.Register(() => this, typeof(INotifyService));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _notificationManager = new WindowNotificationManager(this) { MaxItems = 3, Position = NotificationPosition.TopRight, Margin = new Avalonia.Thickness(0, 100, 80, 0) };
    }

    private WindowNotificationManager? _notificationManager;
    public WindowNotificationManager? NotificationManager => _notificationManager;

    public void Notify(string? title, string? msg, NotificationType type, TimeSpan? timeout = null)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_notificationManager != null && _notificationManager.IsInitialized)
            {
                _notificationManager.Show(new Notification(title, msg, type, timeout));
            }
        });
    }
}
