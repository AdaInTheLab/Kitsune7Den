using System.Windows.Controls;
using Kitsune7Den.ViewModels;

namespace Kitsune7Den.Views;

public partial class LogsView : UserControl
{
    private bool _loaded;

    public LogsView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!_loaded && DataContext is LogsViewModel vm)
        {
            vm.RefreshCommand.Execute(null);
            _loaded = true;
        }
    }
}
