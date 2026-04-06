using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kitsune7Den.Models;
using Kitsune7Den.Services;

namespace Kitsune7Den.ViewModels;

public partial class BackupsViewModel : ObservableObject
{
    private readonly BackupService _backupService;
    private readonly AppSettings _settings;

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _saveDirectory = "";
    [ObservableProperty] private string _backupDirectory = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private BackupInfo? _selectedBackup;

    // Schedule
    [ObservableProperty] private bool _scheduledEnabled;
    [ObservableProperty] private string _selectedInterval = "360";
    [ObservableProperty] private int _maxBackups = 20;
    [ObservableProperty] private bool _scheduleRunning;
    [ObservableProperty] private string _nextBackupText = "--";

    public ObservableCollection<BackupInfo> Backups { get; } = [];

    public static readonly ScheduleOption[] IntervalOptions =
    [
        new("30", "Every 30 minutes"),
        new("60", "Every hour"),
        new("120", "Every 2 hours"),
        new("240", "Every 4 hours"),
        new("360", "Every 6 hours"),
        new("720", "Every 12 hours"),
        new("1440", "Every day"),
    ];

    public BackupsViewModel(BackupService backupService, AppSettings settings)
    {
        _backupService = backupService;
        _settings = settings;
        BackupDirectory = backupService.BackupDirectory;
        SaveDirectory = backupService.FindSaveDirectory() ?? "Not found";

        // Load schedule settings
        ScheduledEnabled = settings.ScheduledBackupsEnabled;
        SelectedInterval = settings.BackupIntervalMinutes.ToString();
        MaxBackups = settings.MaxBackups;
        ScheduleRunning = backupService.IsScheduleRunning;

        _backupService.StatusChanged += msg =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = msg;
                UpdateNextBackupText();
            });
        };

        // Auto-start schedule if enabled
        if (ScheduledEnabled)
            StartSchedule();
    }

    [RelayCommand]
    private void Refresh()
    {
        var backups = _backupService.GetBackups();
        Backups.Clear();
        foreach (var b in backups)
            Backups.Add(b);
        SaveDirectory = _backupService.FindSaveDirectory() ?? "Not found";
        UpdateNextBackupText();
        StatusMessage = $"{backups.Count} backup(s) found";
    }

    [RelayCommand]
    private async Task CreateBackup()
    {
        IsBusy = true;
        await _backupService.CreateBackupAsync("manual");
        Refresh();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task Restore()
    {
        if (SelectedBackup is null) return;

        var result = MessageBox.Show(
            $"Restore backup '{SelectedBackup.FileName}'?\n\nA safety backup will be created first.\nThe server should be stopped before restoring.",
            "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        await _backupService.RestoreBackupAsync(SelectedBackup);
        Refresh();
        IsBusy = false;
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedBackup is null) return;

        var result = MessageBox.Show(
            $"Delete backup '{SelectedBackup.FileName}'?\nThis cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        _backupService.DeleteBackup(SelectedBackup);
        Refresh();
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", BackupDirectory);
        }
        catch { /* best effort */ }
    }

    [RelayCommand]
    private void SaveSchedule()
    {
        _settings.ScheduledBackupsEnabled = ScheduledEnabled;
        _settings.BackupIntervalMinutes = int.TryParse(SelectedInterval, out var m) ? m : 360;
        _settings.MaxBackups = MaxBackups;
        _settings.Save();

        if (ScheduledEnabled)
            StartSchedule();
        else
            StopSchedule();

        StatusMessage = "Schedule settings saved";
    }

    private void StartSchedule()
    {
        _settings.BackupIntervalMinutes = int.TryParse(SelectedInterval, out var m) ? m : 360;
        _backupService.StartSchedule();
        ScheduleRunning = true;
        UpdateNextBackupText();
    }

    private void StopSchedule()
    {
        _backupService.StopSchedule();
        ScheduleRunning = false;
        NextBackupText = "--";
    }

    private void UpdateNextBackupText()
    {
        if (_backupService.NextScheduledBackup is { } next)
            NextBackupText = next.ToString("HH:mm:ss");
        else
            NextBackupText = "--";
    }
}

public record ScheduleOption(string Value, string Label)
{
    public override string ToString() => Label;
}
