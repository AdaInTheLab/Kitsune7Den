using System.Windows.Controls;
using Kitsune7Den.ViewModels;

namespace Kitsune7Den.Views;

public partial class ConfigView : UserControl
{
    private bool _loaded;

    public ConfigView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!_loaded && DataContext is ConfigViewModel vm)
        {
            vm.LoadCommand.Execute(null);
            _loaded = true;
        }
    }
}
