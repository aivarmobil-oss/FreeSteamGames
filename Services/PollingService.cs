using FreeSteamGames.Models;
using FreeSteamGames.ViewModels;

namespace FreeSteamGames.Services;

public class PollingService
{
    private readonly MainViewModel _mainViewModel;
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly Action<string> _showNotification;

    public PollingService(
        MainViewModel mainViewModel,
        SettingsService settingsService,
        AppSettings settings,
        Action<string> showNotification)
    {
        _mainViewModel = mainViewModel;
        _settingsService = settingsService;
        _settings = settings;
        _showNotification = showNotification;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await TickAsync();

            var delay = TimeSpan.FromMinutes(Math.Max(1, _settings.IntervalMinutes));
            try
            {
                await Task.Delay(delay, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task TickAsync()
    {
        await _mainViewModel.RefreshCommand.ExecuteAsync(null);

        var currentIds = _mainViewModel.FreeGames.Select(g => g.AppId).ToList();
        var newGames = _mainViewModel.FreeGames
            .Where(g => !_settings.LastSeenAppIds.Contains(g.AppId))
            .ToList();

        if (newGames.Count > 0 && _settings.NotificationsEnabled && _settings.LastSeenAppIds.Count > 0)
        {
            var names = string.Join(", ", newGames.Select(g => g.Name));
            _showNotification($"Бесплатно сейчас: {names}");
        }

        _settings.LastSeenAppIds = currentIds;
        _settingsService.Save(_settings);
    }
}
