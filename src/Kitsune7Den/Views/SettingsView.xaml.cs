using System.Windows.Controls;
using Kitsune7Den.ViewModels;

namespace Kitsune7Den.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    // WPF PasswordBox doesn't support binding, so we wire it manually
    private void TelnetPasswordBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
        {
            pb.Password = vm.TelnetPassword;
        }
    }

    private void TelnetPasswordBox_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
        {
            vm.TelnetPassword = pb.Password;
        }
    }
}
