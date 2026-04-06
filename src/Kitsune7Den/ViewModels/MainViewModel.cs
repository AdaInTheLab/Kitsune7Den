using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kitsune7Den.Models;
using Kitsune7Den.Services;

namespace Kitsune7Den.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ServerProcessService _processService;
    private readonly TelnetService _telnetService;

    [ObservableProperty] private ObservableObject? _currentView;
    [ObservableProperty] private string _selectedNav = "Dashboard";
    [ObservableProperty] private string _serverStateText = "Stopped";
    [ObservableProperty] private int _playerCount;
    [ObservableProperty] private string _telnetStateText = "Disconnected";

    public DashboardViewModel Dashboard { get; }
    public ConsoleViewModel Console { get; }
    public PlayersViewModel Players { get; }
    public ConfigViewModel Config { get; }
    public ModsViewModel Mods { get; }
    public SettingsViewModel Settings { get; }
    public BackupsViewModel Backups { get; }
    public LogsViewModel Logs { get; }

    public MainViewModel(
        DashboardViewModel dashboard,
        ConsoleViewModel console,
        PlayersViewModel players,
        ConfigViewModel config,
        ModsViewModel mods,
        SettingsViewModel settings,
        BackupsViewModel backups,
        LogsViewModel logs,
        ServerProcessService processService,
        TelnetService telnetService)
    {
        Dashboard = dashboard;
        Console = console;
        Players = players;
        Config = config;
        Mods = mods;
        Settings = settings;
        Backups = backups;
        Logs = logs;
        _processService = processService;
        _telnetService = telnetService;

        CurrentView = dashboard;

        _processService.StateChanged += state =>
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ServerStateText = state.ToString();
            });
        };

        _telnetService.ConnectionStateChanged += connected =>
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                TelnetStateText = connected ? "Connected" : "Disconnected";
            });
        };
    }

    [RelayCommand]
    private void Navigate(string destination)
    {
        SelectedNav = destination;
        CurrentView = destination switch
        {
            "Dashboard" => Dashboard,
            "Console" => Console,
            "Players" => Players,
            "Config" => Config,
            "Mods" => Mods,
            "Backups" => Backups,
            "Logs" => Logs,
            "Settings" => Settings,
            _ => Dashboard
        };
    }
}
