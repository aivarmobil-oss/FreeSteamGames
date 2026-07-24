using System.Net.Http;
using System.Text.Json;
using FreeSteamGames.Models;

namespace FreeSteamGames.Services;

public class SteamSpecialsService
{
    private readonly HttpClient _httpClient;

    // Порог отбора: 100 = только полностью бесплатные раздачи.
    // Временно можно снизить (например, до 50) для проверки рендеринга UI на реальных данных,
    // когда прямо сейчас нет живых 100%-раздач.
    public int DiscountThreshold { get; set; } = 100;

    public SteamSpecialsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<FreeGame>> GetFreeGamesAsync(string steamCC, LocalePack locale, CancellationToken ct = default)
    {
        var url = $"https://store.steampowered.com/api/featuredcategories?cc={steamCC}&l={locale.SteamLang}";

        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<SteamFeaturedCategoriesResponse>(stream, cancellationToken: ct);

        var items = data?.Specials?.Items ?? new List<SteamSpecialItem>();

        return items
            .Where(i => i.DiscountPercent >= DiscountThreshold)
            .Select(item => ToFreeGame(item, locale))
            .ToList();
    }

    private static FreeGame ToFreeGame(SteamSpecialItem item, LocalePack locale)
    {
        DateTimeOffset? expiration = item.DiscountExpirationUnix is > 0
            ? DateTimeOffset.FromUnixTimeSeconds(item.DiscountExpirationUnix.Value)
            : null;

        return new FreeGame
        {
            AppId = item.Id,
            Name = item.Name,
            HeaderImageUrl = item.HeaderImage ?? item.LargeCapsuleImage ?? string.Empty,
            OriginalPrice = (item.OriginalPriceCents ?? 0) / 100m,
            Currency = item.Currency,
            DiscountExpiration = expiration,
            TimeLeftLabel = BuildTimeLeftLabel(expiration, locale)
        };
    }

    private static string? BuildTimeLeftLabel(DateTimeOffset? expiration, LocalePack locale)
    {
        if (expiration is null) return null;
        var remaining = expiration.Value - DateTimeOffset.Now;
        if (remaining <= TimeSpan.Zero) return null;

        var format = remaining.TotalHours >= 24
            ? locale.Strings.GetValueOrDefault("days_left_format", "{0}d left")
            : locale.Strings.GetValueOrDefault("hours_left_format", "{0}h left");

        var value = remaining.TotalHours >= 24 ? (int)remaining.TotalDays : (int)remaining.TotalHours;
        return string.Format(format, value);
    }
}
