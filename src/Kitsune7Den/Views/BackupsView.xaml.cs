using System.Windows.Controls;
using System.Windows.Input;
using Kitsune7Den.Models;
using Kitsune7Den.ViewModels;

namespace Kitsune7Den.Views;

public partial class BackupsView : UserControl
{
    private bool _loaded;

    public BackupsView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!_loaded && DataContext is BackupsViewModel vm)
        {
            vm.RefreshCommand.Execute(null);
            _loaded = true;
        }
    }

    private void BackupCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement { Tag: BackupInfo backup } &&
            DataContext is BackupsViewModel vm)
        {
            vm.SelectedBackup = backup;
        }
    }
}
