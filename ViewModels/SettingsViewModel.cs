using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSteamGames.Models;
using FreeSteamGames.Services;

namespace FreeSteamGames.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly AutoStartService _autoStartService;
    private readonly AppSettings _settings;
    private readonly LocalizationService _localizationService;
    private readonly RegionService _regionService;
    private readonly Action _requestRestart;
    private readonly Func<Task> _requestRefresh;
    private bool _isInitializing = true;

    [ObservableProperty]
    private bool autoStart;

    [ObservableProperty]
    private bool launchAsWindow;

    [ObservableProperty]
    private bool notificationsEnabled;

    [ObservableProperty]
    private int intervalMinutes;

    [ObservableProperty]
    private LocalePack? selectedLocale;

    [ObservableProperty]
    private bool isLoadingMoreLocales;

    [ObservableProperty]
    private SteamRegion? selectedRegion;

    public LocalizationService Loc => _localizationService;

    public ObservableCollection<LocalePack> InstalledLocales { get; } = new();
    public ObservableCollection<LocaleIndexEntry> MoreLocales { get; } = new();
    public List<SteamRegion> AvailableRegions { get; } = SteamRegion.Common;

    public IRelayCommand LoadMoreLocalesCommand { get; }
    public IAsyncRelayCommand<LocaleIndexEntry> DownloadLocaleCommand { get; }

    public SettingsViewModel(
        SettingsService settingsService,
        AutoStartService autoStartService,
        AppSettings settings,
        LocalizationService localizationService,
        RegionService regionService,
        Action requestRestart,
        Func<Task> requestRefresh)
    {
        _settingsService = settingsService;
        _autoStartService = autoStartService;
        _settings = settings;
        _localizationService = localizationService;
        _regionService = regionService;
        _requestRestart = requestRestart;
        _requestRefresh = requestRefresh;

        LoadMoreLocalesCommand = new AsyncRelayCommand(LoadMoreLocalesAsync);
        DownloadLocaleCommand = new AsyncRelayCommand<LocaleIndexEntry>(DownloadLocaleAsync);

        AutoStart = _autoStartService.IsEnabled();
        LaunchAsWindow = settings.LaunchMode == LaunchMode.Window;
        NotificationsEnabled = settings.NotificationsEnabled;
        IntervalMinutes = settings.IntervalMinutes;

        foreach (var pack in _localizationService.GetInstalledPacks())
            InstalledLocales.Add(pack);
        SelectedLocale = InstalledLocales.FirstOrDefault(p => p.Code == _localizationService.Current.Code);
        SelectedRegion = AvailableRegions.FirstOrDefault(r => r.Code == _regionService.CurrentCode);

        _isInitializing = false;
    }

    partial void OnAutoStartChanged(bool value)
    {
        if (_isInitializing) return;
        _autoStartService.SetEnabled(value);
        _settings.AutoStart = value;
        _settingsService.Save(_settings);
    }

    partial void OnLaunchAsWindowChanged(bool value)
    {
        if (_isInitializing) return;
        _settings.LaunchMode = value ? LaunchMode.Window : LaunchMode.TrayOnly;
        _settingsService.Save(_settings);
    }

    partial void OnNotificationsEnabledChanged(bool value)
    {
        if (_isInitializing) return;
        _settings.NotificationsEnabled = value;
        _settingsService.Save(_settings);
    }

    partial void OnIntervalMinutesChanged(int value)
    {
        if (_isInitializing) return;
        _settings.IntervalMinutes = value;
        _settingsService.Save(_settings);
    }

    partial void OnSelectedLocaleChanged(LocalePack? value)
    {
        if (_isInitializing || value is null) return;
        if (value.Code == _settings.LocaleCode) return;

        _settings.LocaleCode = value.Code;
        _settingsService.Save(_settings);

        var prompt = _localizationService["settings_restart_prompt"];
        var result = MessageBox.Show(prompt, "Free Games Radar", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
            _requestRestart();
    }

    partial void OnSelectedRegionChanged(SteamRegion? value)
    {
        if (_isInitializing || value is null) return;
        if (value.Code == _settings.SteamRegionCode) return;

        _regionService.CurrentCode = value.Code;
        _settings.SteamRegionCode = value.Code;
        _settingsService.Save(_settings);

        _ = _requestRefresh();
    }

    private async Task LoadMoreLocalesAsync()
    {
        IsLoadingMoreLocales = true;
        MoreLocales.Clear();
        try
        {
            var index = await _localizationService.GetRemoteIndexAsync();
            var installedCodes = InstalledLocales.Select(p => p.Code).ToHashSet();
            foreach (var entry in index.Where(e => !installedCodes.Contains(e.Code)))
                MoreLocales.Add(entry);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось получить список языков: {ex.Message}", "Free Games Radar");
        }
        finally
        {
            IsLoadingMoreLocales = false;
        }
    }

    private async Task DownloadLocaleAsync(LocaleIndexEntry? entry)
    {
        if (entry is null) return;
        try
        {
            var pack = await _localizationService.DownloadPackAsync(entry.Code);
            InstalledLocales.Add(pack);
            MoreLocales.Remove(entry);
            SelectedLocale = pack;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось скачать язык: {ex.Message}", "Free Games Radar");
        }
    }
}
