using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FreeSteamGames.Models;
using FreeSteamGames.Services;
using FreeSteamGames.ViewModels;
using H.NotifyIcon;

namespace FreeSteamGames;

public partial class App : Application
{
    public static bool IsExiting { get; set; } = false;

    private MainWindow? _mainWindow;
    private MainViewModel? _mainViewModel;
    private TaskbarIcon? _trayIcon;
    private SettingsWindow? _settingsWindow;

    private SettingsService _settingsService = null!;
    private AutoStartService _autoStartService = null!;
    private AppSettings _settings = null!;
    private PollingService _pollingService = null!;
    private LocalizationService _localizationService = null!;
    private RegionService _regionService = null!;
    private readonly CancellationTokenSource _pollingCts = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();
        _autoStartService = new AutoStartService();
        _settings = _settingsService.Load();

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FreeSteamGames/1.0");

        _localizationService = new LocalizationService(httpClient);
        await ResolveInitialLocaleAsync();

        _regionService = new RegionService
        {
            CurrentCode = _settings.SteamRegionCode ?? RegionService.DetectRegionCode()
        };
        if (_settings.SteamRegionCode is null)
        {
            _settings.SteamRegionCode = _regionService.CurrentCode;
            _settingsService.Save(_settings);
        }

        var steamSpecialsService = new SteamSpecialsService(httpClient);
        _mainViewModel = new MainViewModel(steamSpecialsService, _localizationService, _regionService);

        _mainWindow = new MainWindow(_mainViewModel);

        SetupTrayIcon();

        var forceShowWindow = e.Args.Contains("--show-window");
        if (_settings.LaunchMode == LaunchMode.Window || forceShowWindow)
            _mainWindow.Show();

        _pollingService = new PollingService(_mainViewModel, _settingsService, _settings, ShowTrayNotification);
        _ = _pollingService.RunAsync(_pollingCts.Token);
    }

    private async Task ResolveInitialLocaleAsync()
    {
        // 1. Уже выбран язык вручную или сохранён с прошлого запуска.
        if (_settings.LocaleCode is not null &&
            _localizationService.TryLoadByCode(_settings.LocaleCode, out var savedPack) && savedPack is not null)
        {
            _localizationService.Apply(savedPack);
            return;
        }

        // 2. Автоопределение по региону/языку Windows — сначала среди уже установленных пакетов.
        var preferredCode = _localizationService.DetectPreferredCode();
        if (_localizationService.TryLoadByCode(preferredCode, out var detectedPack) && detectedPack is not null)
        {
            _localizationService.Apply(detectedPack);
            _settings.LocaleCode = preferredCode;
            _settingsService.Save(_settings);
            return;
        }

        // 3. Пробуем скачать подходящий пакет с GitHub. Если не вышло (нет сети/нет такого языка) — остаёмся на английском по умолчанию.
        try
        {
            var downloaded = await _localizationService.DownloadPackAsync(preferredCode);
            _localizationService.Apply(downloaded);
            _settings.LocaleCode = preferredCode;
            _settingsService.Save(_settings);
        }
        catch
        {
            if (_localizationService.TryLoadByCode("en", out var fallbackPack) && fallbackPack is not null)
            {
                _localizationService.Apply(fallbackPack);
                _settings.LocaleCode = "en";
                _settingsService.Save(_settings);
            }
        }
    }

    private void ShowTrayNotification(string message)
    {
        _trayIcon?.ShowNotification("Free Steam Games", message);
    }

    private void SetupTrayIcon()
    {
        var loc = _localizationService;

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Free Steam Games",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/tray.ico"))
        };

        var openItem = new MenuItem { Header = loc["tray_open"] };
        openItem.Click += (_, _) => ShowMainWindow();

        var checkNowItem = new MenuItem { Header = loc["tray_check_now"] };
        checkNowItem.Click += async (_, _) =>
        {
            if (_mainViewModel is not null)
                await _mainViewModel.RefreshCommand.ExecuteAsync(null);
        };

        var settingsItem = new MenuItem { Header = loc["tray_settings"] };
        settingsItem.Click += (_, _) => OpenSettings();

        var exitItem = new MenuItem { Header = loc["tray_exit"] };
        exitItem.Click += (_, _) => ExitApplication();

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(checkNowItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;
        _trayIcon.TrayLeftMouseUp += (_, _) => ShowMainWindow();
        _trayIcon.ForceCreate();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var settingsViewModel = new SettingsViewModel(
            _settingsService, _autoStartService, _settings, _localizationService, _regionService,
            RestartApplication, RefreshGamesAsync);
        _settingsWindow = new SettingsWindow(settingsViewModel);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private async Task RefreshGamesAsync()
    {
        if (_mainViewModel is not null)
            await _mainViewModel.RefreshCommand.ExecuteAsync(null);
    }

    private void RestartApplication()
    {
        IsExiting = true;
        var exePath = Environment.ProcessPath;
        if (exePath is not null)
        {
            var wasWindowVisible = _mainWindow?.IsVisible == true;
            var startInfo = new System.Diagnostics.ProcessStartInfo(exePath);
            if (wasWindowVisible)
                startInfo.ArgumentList.Add("--show-window");
            System.Diagnostics.Process.Start(startInfo);
        }
        _trayIcon?.Dispose();
        Shutdown();
    }

    private void ExitApplication()
    {
        IsExiting = true;
        _pollingCts.Cancel();
        _trayIcon?.Dispose();
        Shutdown();
    }
}
