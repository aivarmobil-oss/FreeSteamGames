namespace FreeSteamGames.Models;

public record SteamRegion(string Code, string DisplayName)
{
    public static readonly List<SteamRegion> Common = new()
    {
        new("ru", "Россия — ₽ RUB"),
        new("lv", "Latvija — € EUR"),
        new("lt", "Lietuva — € EUR"),
        new("ee", "Eesti — € EUR"),
        new("de", "Германия — € EUR"),
        new("fr", "Франция — € EUR"),
        new("pl", "Польша — zł PLN"),
        new("us", "США — $ USD"),
        new("gb", "Великобритания — £ GBP"),
        new("ua", "Украина — ₴ UAH"),
        new("kz", "Казахстан — ₸ KZT"),
        new("by", "Беларусь — Br BYN"),
        new("tr", "Турция — ₺ TRY"),
    };
}
