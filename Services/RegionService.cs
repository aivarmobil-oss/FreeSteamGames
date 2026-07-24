using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using FreeSteamGames.Models;

namespace FreeSteamGames.Services;

public partial class RegionService : ObservableObject
{
    [ObservableProperty]
    private string currentCode = "us";

    public static string DetectRegionCode()
    {
        try
        {
            return RegionInfo.CurrentRegion.TwoLetterISORegionName.ToLowerInvariant();
        }
        catch
        {
            return "us";
        }
    }
}
