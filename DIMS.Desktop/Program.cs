using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using MsBox.Avalonia;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using ReactiveUI;

namespace DIMS.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        SetupUnhandledExceptionHandling();

        var builder = BuildAvaloniaApp();
        if (args.Contains("--drm"))
        {
            SilenceConsole();

            // If Card0, Card1 and Card2 all don't work. You can also try:                 
            // return builder.StartLinuxFbDev(args);
            // return builder.StartLinuxDrm(args, "/dev/dri/card1");
            return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
        }

        return builder.StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new FontManagerOptions { DefaultFamilyName = "Microsoft YaHei UI" })
            .WithInterFont()
            //.WithCustomFont()
            .LogToTrace()
            .UseReactiveUI();
    }

    private static void SetupUnhandledExceptionHandling()
    {
        // Catch exceptions from all threads in the AppDomain.
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            _ = ShowUnhandledExceptionAsync(args.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException", false);

        // Catch exceptions from each AppDomain that uses a task scheduler for async operations.
        TaskScheduler.UnobservedTaskException += (sender, args) =>
            _ = ShowUnhandledExceptionAsync(args.Exception, "TaskScheduler.UnobservedTaskException", false);

        // Catch exceptions from a single specific UI dispatcher thread.
        //Dispatcher.UnhandledException += (sender, args) =>
        //{
        //    if (!Debugger.IsAttached)
        //    {
        //        args.Handled = true;
        //        ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException", true);
        //    }
        //};
    }

    private static async Task ShowUnhandledExceptionAsync(Exception? e, string unhandledExceptionType, bool promptUserForShutdown)
    {
        if (e is System.Net.Sockets.SocketException || e?.InnerException is System.Net.Sockets.SocketException)
            return;

        var messageBoxTitle = $"Unexpected Error Occurred: {unhandledExceptionType}";
        var messageBoxMessage = $"The following exception occurred: \n\n{e}";
        var messageBoxButtons = MsBox.Avalonia.Enums.ButtonEnum.Ok;

        if (e != null)
            Splat.LogHost.Default.Debug(e, messageBoxTitle);

        if (promptUserForShutdown)
        {
            messageBoxMessage += "\n\nNormally the app would die now. Should we let it die?";
            messageBoxButtons = MsBox.Avalonia.Enums.ButtonEnum.YesNo;
        }

        var main = (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) ? desktop.MainWindow : null;
        var messageBoxWindow = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(messageBoxTitle, messageBoxMessage, messageBoxButtons);
        if (await messageBoxWindow.ShowAsPopupAsync(main) == MsBox.Avalonia.Enums.ButtonResult.Yes)
        {
            Environment.Exit(-1);
        }
    }

    private static void SilenceConsole()
    {
        new Thread(() =>
        {
            Console.CursorVisible = false;
            while (true)
                Console.ReadKey(true);
        })
        { IsBackground = true }.Start();
    }
}
