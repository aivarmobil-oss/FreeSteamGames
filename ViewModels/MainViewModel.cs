using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSteamGames.Models;
using FreeSteamGames.Services;

namespace FreeSteamGames.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SteamSpecialsService _steamSpecialsService;
    private readonly RegionService _regionService;

    public LocalizationService Loc { get; }

    public ObservableCollection<FreeGame> FreeGames { get; } = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool updateBannerVisible;

    [ObservableProperty]
    private string? updateBannerMessage;

    public MainViewModel(SteamSpecialsService steamSpecialsService, LocalizationService localizationService, RegionService regionService)
    {
        _steamSpecialsService = steamSpecialsService;
        Loc = localizationService;
        _regionService = regionService;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var games = await _steamSpecialsService.GetFreeGamesAsync(_regionService.CurrentCode, Loc.Current);
            FreeGames.Clear();
            foreach (var game in games)
                FreeGames.Add(game);

            StatusMessage = FreeGames.Count == 0 ? Loc["no_deals"] : null;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Loc["fetch_error_format"], ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private static void OpenStorePage(FreeGame? game)
    {
        if (game is null) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = game.StoreUrl,
            UseShellExecute = true
        });
    }

    public void ShowUpdateBanner(string message)
    {
        UpdateBannerMessage = message;
        UpdateBannerVisible = true;
    }
}
