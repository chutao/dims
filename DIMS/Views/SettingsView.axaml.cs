using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DIMS.ViewModels;

namespace DIMS.Views
{
    public partial class SettingsView : Window
    {
        public SettingsView()
        {
            InitializeComponent();
            this.DataContext = new SettingsViewModel(this);
        }
    }
}