using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DIMS.Hardware;
using DIMS.ViewModels;
using DIMS.Views;
using Splat;
using Splat.NLog;

namespace DIMS;

public partial class App : Application
{
    internal static Control? MainWindow { get; private set; }

    public override void Initialize()
    {
        Locator.CurrentMutable.UseNLogWithWrappingFullLogger();

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Locator.CurrentMutable.RegisterLazySingleton(() => new MR6100(), typeof(MR6100));
        Locator.CurrentMutable.RegisterLazySingleton(() => new Infoscanner(), typeof(Infoscanner));
        Locator.CurrentMutable.RegisterLazySingleton(() => new PlcCommunication(), typeof(PlcCommunication));

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            desktop.MainWindow = mainWindow;
            MainWindow = mainWindow;
            Locator.CurrentMutable.Register(() => MainWindow.DataContext, typeof(IDataProvider));
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var mainWindow = new MainView
            {
                DataContext = new MainViewModel()
            };

            singleViewPlatform.MainView = mainWindow;
            MainWindow = mainWindow;
            Locator.CurrentMutable.Register(() => MainWindow.DataContext, typeof(IDataProvider));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
