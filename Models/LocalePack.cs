using System.Text.Json.Serialization;

namespace FreeSteamGames.Models;

public class LocalePack
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("steamLang")]
    public string SteamLang { get; set; } = "english";

    [JsonPropertyName("strings")]
    public Dictionary<string, string> Strings { get; set; } = new();
}

public class LocaleIndexEntry
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
}
