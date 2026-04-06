using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kitsune7Den.Models;
using Kitsune7Den.Services;

namespace Kitsune7Den.ViewModels;

public partial class ConfigViewModel : ObservableObject
{
    private readonly ConfigService _configService;

    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isFormView = true;
    [ObservableProperty] private string _rawXml = "";

    public ObservableCollection<ConfigGroup> Groups { get; } = [];
    private List<ServerConfigProperty> _allProperties = [];

    public ConfigViewModel(ConfigService configService)
    {
        _configService = configService;
    }

    [RelayCommand]
    private void Load()
    {
        _allProperties = _configService.LoadConfig();
        BuildGroups();
        RawXml = _configService.GetRawConfig() ?? "";

        // Wire up change tracking
        foreach (var prop in _allProperties)
        {
            prop.PropertyChanged += (_, _) => HasChanges = true;
        }

        HasChanges = false;
        StatusMessage = $"Loaded {_allProperties.Count} properties";
    }

    [RelayCommand]
    private void Save()
    {
        bool success;
        if (IsFormView)
        {
            success = _configService.SaveConfig(_allProperties);
        }
        else
        {
            success = _configService.SaveRawConfig(RawXml);
            if (success) Load(); // Reload form from saved XML
        }

        StatusMessage = success ? "Saved (backup created)" : "Save failed — check XML validity";
        HasChanges = false;
    }

    [RelayCommand]
    private void Reset()
    {
        Load();
        StatusMessage = "Reset to last saved values";
    }

    [RelayCommand]
    private void SwitchToForm()
    {
        IsFormView = true;
    }

    [RelayCommand]
    private void SwitchToRaw()
    {
        IsFormView = false;
        RawXml = _configService.GetRawConfig() ?? "";
    }

    private void BuildGroups()
    {
        Groups.Clear();

        // Group by category, using the defined order
        var grouped = _allProperties.GroupBy(p => p.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var category in FieldDefinitions.CategoryOrder)
        {
            if (grouped.TryGetValue(category, out var props))
            {
                Groups.Add(new ConfigGroup(category, props));
                grouped.Remove(category);
            }
        }

        // Any remaining "Other" categories
        foreach (var (category, props) in grouped)
        {
            Groups.Add(new ConfigGroup(category, props));
        }
    }
}

public class ConfigGroup
{
    public string Name { get; }
    public string NameUpper => Name.ToUpperInvariant();
    public List<ServerConfigProperty> Properties { get; }

    public ConfigGroup(string name, List<ServerConfigProperty> properties)
    {
        Name = name;
        Properties = properties;
    }
}
