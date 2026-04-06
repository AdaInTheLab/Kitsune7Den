using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kitsune7Den.Models;
using Kitsune7Den.Services;

namespace Kitsune7Den.ViewModels;

public partial class ModsViewModel : ObservableObject
{
    private readonly ModManagerService _modManager;

    [ObservableProperty] private ModInfo? _selectedMod;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private int _enabledCount;
    [ObservableProperty] private int _totalCount;

    public ObservableCollection<ModInfo> Mods { get; } = [];

    public ModsViewModel(ModManagerService modManager)
    {
        _modManager = modManager;

        _modManager.StatusChanged += msg =>
        {
            Application.Current.Dispatcher.Invoke(() => StatusMessage = msg);
        };
    }

    [RelayCommand]
    private void Refresh()
    {
        var mods = _modManager.GetInstalledMods();
        Mods.Clear();
        foreach (var mod in mods)
            Mods.Add(mod);
        TotalCount = mods.Count;
        EnabledCount = mods.Count(m => m.IsEnabled);
        StatusMessage = $"{TotalCount} mods ({EnabledCount} enabled)";
    }

    [RelayCommand]
    private void Toggle(ModInfo? mod)
    {
        var target = mod ?? SelectedMod;
        if (target is null) return;
        if (_modManager.ToggleMod(target.FolderName))
        {
            StatusMessage = target.IsEnabled
                ? $"Disabled {target.DisplayName}"
                : $"Enabled {target.DisplayName}";
            Refresh();
        }
    }

    [RelayCommand]
    private void Delete(ModInfo? mod)
    {
        var target = mod ?? SelectedMod;
        if (target is null) return;

        var result = MessageBox.Show(
            $"Delete mod '{target.DisplayName}'?\nThis will remove all files and cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            if (_modManager.DeleteMod(target.FolderName))
            {
                StatusMessage = $"Deleted {target.DisplayName}";
                Refresh();
            }
        }
    }

    [RelayCommand]
    private void InstallFromZip()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Mod Archives|*.zip|All Files|*.*",
            Title = "Select Mod Zip File",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true) return;

        foreach (var file in dialog.FileNames)
        {
            _modManager.InstallFromZip(file);
        }

        Refresh();
    }

    [RelayCommand]
    private void OpenModsFolder()
    {
        try
        {
            var settings = AppSettings.Load();
            var modsDir = System.IO.Path.Combine(settings.ServerDirectory, "Mods");
            if (System.IO.Directory.Exists(modsDir))
                System.Diagnostics.Process.Start("explorer.exe", modsDir);
        }
        catch { /* best effort */ }
    }
}
