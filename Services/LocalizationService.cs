using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using FreeSteamGames.Models;

namespace FreeSteamGames.Services;

public class LocalizationService : ObservableObject
{
    private const string RawBaseUrl =
        "https://raw.githubusercontent.com/aivarmobil-oss/FreeSteamGames/main/locales/";

    private static readonly string LocalesDir = Path.Combine(AppContext.BaseDirectory, "Locales");

    private readonly HttpClient _httpClient;
    private LocalePack _current = FallbackPack();

    public LocalizationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        Directory.CreateDirectory(LocalesDir);
    }

    public LocalePack Current => _current;

    public string this[string key] => _current.Strings.TryGetValue(key, out var value) ? value : key;

    public List<LocalePack> GetInstalledPacks()
    {
        var packs = new List<LocalePack>();
        if (!Directory.Exists(LocalesDir)) return packs;

        foreach (var file in Directory.GetFiles(LocalesDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var pack = JsonSerializer.Deserialize<LocalePack>(json);
                if (pack is not null && !string.IsNullOrEmpty(pack.Code))
                    packs.Add(pack);
            }
            catch (JsonException)
            {
                // повреждённый файл пакета — пропускаем
            }
        }

        return packs;
    }

    public void Apply(LocalePack pack)
    {
        _current = pack;
        OnPropertyChanged(string.Empty);
        OnPropertyChanged("Item[]");
    }

    public bool TryLoadByCode(string code, out LocalePack? pack)
    {
        pack = GetInstalledPacks().FirstOrDefault(p => p.Code == code);
        return pack is not null;
    }

    public string DetectPreferredCode()
    {
        var region = RegionInfo.CurrentRegion.TwoLetterISORegionName.ToLowerInvariant();
        var uiLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

        // Простая эвристика: код пакета = язык интерфейса Windows (en, ru, de, ...).
        // Если такого пакета нет ни локально, ни на GitHub — используется английский по умолчанию.
        return uiLanguage;
    }

    public async Task<List<LocaleIndexEntry>> GetRemoteIndexAsync(CancellationToken ct = default)
    {
        var json = await _httpClient.GetStringAsync(RawBaseUrl + "index.json", ct);
        return JsonSerializer.Deserialize<List<LocaleIndexEntry>>(json) ?? new List<LocaleIndexEntry>();
    }

    public async Task<LocalePack> DownloadPackAsync(string code, CancellationToken ct = default)
    {
        var json = await _httpClient.GetStringAsync(RawBaseUrl + $"{code}.json", ct);
        var pack = JsonSerializer.Deserialize<LocalePack>(json)
                   ?? throw new InvalidOperationException("Некорректный языковой пакет");

        File.WriteAllText(Path.Combine(LocalesDir, $"{code}.json"), json);
        return pack;
    }

    private static LocalePack FallbackPack() => new()
    {
        Code = "en",
        DisplayName = "English",
        SteamLang = "english",
        Strings = new Dictionary<string, string>
        {
            ["free_badge"] = "FREE",
            ["grab_button"] = "Grab it",
            ["no_deals"] = "No free giveaways right now. Check back later.",
        }
    };
}
