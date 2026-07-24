namespace FreeSteamGames.Models;

public enum LaunchMode
{
    TrayOnly,
    Window
}

public class AppSettings
{
    public bool AutoStart { get; set; } = false;
    public LaunchMode LaunchMode { get; set; } = LaunchMode.TrayOnly;
    public bool NotificationsEnabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public List<int> LastSeenAppIds { get; set; } = new();
    public string? LocaleCode { get; set; } = null;
    public string? SteamRegionCode { get; set; } = null;
}
