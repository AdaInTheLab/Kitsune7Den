using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Input;
using Kitsune7Den.ViewModels;

namespace Kitsune7Den.Views;

public partial class ConsoleView : UserControl
{
    public ConsoleView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ConsoleViewModel vm)
            {
                vm.LogLines.CollectionChanged += (_, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add && vm.AutoScroll && LogListBox.Items.Count > 0)
                    {
                        LogListBox.ScrollIntoView(LogListBox.Items[^1]);
                    }
                };
            }
        };
    }

    private void CommandBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ConsoleViewModel vm) return;

        switch (e.Key)
        {
            case Key.Enter:
                vm.SendCommandCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Up:
                vm.HistoryUpCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Down:
                vm.HistoryDownCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
