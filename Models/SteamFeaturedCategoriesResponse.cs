using System.Text.Json.Serialization;

namespace FreeSteamGames.Models;

public class SteamFeaturedCategoriesResponse
{
    [JsonPropertyName("specials")]
    public SteamSpecialsSection? Specials { get; set; }
}

public class SteamSpecialsSection
{
    [JsonPropertyName("items")]
    public List<SteamSpecialItem> Items { get; set; } = new();
}

public class SteamSpecialItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("discount_percent")]
    public int DiscountPercent { get; set; }

    [JsonPropertyName("original_price")]
    public int? OriginalPriceCents { get; set; }

    [JsonPropertyName("final_price")]
    public int? FinalPriceCents { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("header_image")]
    public string? HeaderImage { get; set; }

    [JsonPropertyName("large_capsule_image")]
    public string? LargeCapsuleImage { get; set; }

    [JsonPropertyName("discount_expiration")]
    public long? DiscountExpirationUnix { get; set; }
}
