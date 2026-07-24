using System;

namespace FreeSteamGames.Models;

public class FreeGame
{
    public int AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HeaderImageUrl { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset? DiscountExpiration { get; set; }
    public string? TimeLeftLabel { get; set; }

    public string StoreUrl => $"https://store.steampowered.com/app/{AppId}";

    public string FormattedOriginalPrice =>
        OriginalPrice > 0 ? $"{OriginalPrice:0.00} {Currency}" : string.Empty;
}
