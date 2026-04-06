using System.Windows.Controls;
using Kitsune7Den.ViewModels;

namespace Kitsune7Den.Views;

public partial class ModsView : UserControl
{
    private bool _loaded;

    public ModsView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!_loaded && DataContext is ModsViewModel vm)
        {
            vm.RefreshCommand.Execute(null);
            _loaded = true;
        }
    }
}
