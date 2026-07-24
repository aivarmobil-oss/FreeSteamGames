using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using FreeSteamGames.Models;

namespace FreeSteamGames.Services;

public class UpdateCheckService
{
    private const string VersionUrl =
        "https://raw.githubusercontent.com/aivarmobil-oss/FreeSteamGames/main/version.json";

    private readonly HttpClient _httpClient;

    public UpdateCheckService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public async Task<string?> CheckForUpdateMessageAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(VersionUrl, ct);
            var manifest = JsonSerializer.Deserialize<VersionManifest>(json);
            if (manifest is null) return null;

            if (!Version.TryParse(manifest.LatestVersion, out var latest))
                return null;

            return CurrentVersion < latest ? manifest.Message : null;
        }
        catch
        {
            // Нет сети, репозиторий недоступен и т.д. — молча пропускаем проверку.
            return null;
        }
    }
}
