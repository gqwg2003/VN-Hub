using System.Text.Json;

namespace VnHub.Services;

public class AppSettings
{
    public string Theme { get; set; } = "dark";
    public string DefaultFolder { get; set; } = "";
    public string Language { get; set; } = "en";
    public string DbPath { get; set; } = "";
    public bool VndbEnabled { get; set; } = true;
    public bool AutoStart { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = true;
    public int MaxBackups { get; set; } = 5;
    public string BackupInterval { get; set; } = "startup";
    public string ProxyAddress { get; set; } = "";
    public string SortBy { get; set; } = "title";
    public string SortDir { get; set; } = "asc";
    public string GridSize { get; set; } = "medium";
    public Dictionary<string, string> Shortcuts { get; set; } = new();
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; } = false;

    public List<string> ScanBlacklistExe { get; set; } = new();
    public List<string> ScanBlacklistDirs { get; set; } = new();
    public string ScanSortBy { get; set; } = "title";
    public string ScanSortDir { get; set; } = "asc";
    public bool ScanSkipExisting { get; set; } = true;
    public bool ScanRecursive { get; set; } = false;
}

public static class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VnHub");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        var json = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        File.WriteAllText(SettingsPath, json);
    }
}
