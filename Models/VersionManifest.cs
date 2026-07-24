using System.Text.Json.Serialization;

namespace FreeSteamGames.Models;

public class VersionManifest
{
    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; } = "0.0.0";

    [JsonPropertyName("min_version")]
    public string MinVersion { get; set; } = "0.0.0";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
