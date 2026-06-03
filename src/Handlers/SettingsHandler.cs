using System.Text.Json;
using Microsoft.Win32;
using VnHub.Common;
using VnHub.Database;
using VnHub.Models;
using VnHub.Services;

namespace VnHub.Handlers;

public static class SettingsHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "getSettings":
            {
                SendSettings();
                break;
            }

            case "saveSettings":
            {
                var s = Bridge.Deserialize<AppSettings>(payload);
                if (s == null) break;
                s.ProxyAddress = Validation.SanitizeProxy(s.ProxyAddress);
                SettingsService.Save(s);
                VndbService.ConfigureProxy(s.ProxyAddress);
                IgdbService.Instance.ConfigureProxy(s.ProxyAddress);
                AniListService.Instance.ConfigureProxy(s.ProxyAddress);
                BangumiService.Instance.ConfigureProxy(s.ProxyAddress);
                SteamService.Instance.ConfigureProxy(s.ProxyAddress);
                RawgService.Instance.ConfigureProxy(s.ProxyAddress);
                Bridge.SendToJs("settingsSaved", new { ok = true, proxyAddress = s.ProxyAddress });
                break;
            }

            case "openDbFolder":
            {
                var folder = Path.GetDirectoryName(AppDb.DbPath);
                if (folder != null && Directory.Exists(folder))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = folder,
                        UseShellExecute = true
                    });
                }
                break;
            }

            case "exportLibrary":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new SaveFileDialog
                    {
                        Filter = "JSON|*.json",
                        FileName = "vnhub-library.json"
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var entries = LibraryService.GetLibrary();
                            var json = JsonSerializer.Serialize(entries, Bridge.JsonOpts);
                            File.WriteAllText(dialog.FileName, json);
                            Bridge.SendToJs("exportDone", new { path = dialog.FileName });
                            LogService.Info($"Library exported to {dialog.FileName} ({entries.Count} entries)");
                        }
                        catch (Exception ex)
                        {
                            LogService.Error("Export failed", ex);
                            Bridge.SendToJs("onError", new { message = $"Export failed: {ex.Message}" });
                        }
                    }
                });
                break;
            }

            case "importLibrary":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "JSON|*.json|All files|*.*"
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var json = File.ReadAllText(dialog.FileName);
                            var entries = JsonSerializer.Deserialize<List<VnEntry>>(json, Bridge.JsonOpts);
                            if (entries == null)
                            {
                                Bridge.SendToJs("onError", new { message = "Invalid JSON format" });
                                return;
                            }
                            var count = VnRepository.BulkImport(entries);
                            Bridge.SendToJs("importDone", new { count });
                            LogService.Info($"Library imported from {dialog.FileName} ({count} new entries)");
                        }
                        catch (Exception ex)
                        {
                            LogService.Error("Import failed", ex);
                            Bridge.SendToJs("onError", new { message = $"Import failed: {ex.Message}" });
                        }
                    }
                });
                break;
            }

            case "setAutoStart":
            {
                var p = Bridge.Deserialize<Bridge.AutoStartPayload>(payload);
                if (p == null) break;
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if (key != null)
                    {
                        if (p.Enabled)
                            key.SetValue("VnHub", $"\"{Application.ExecutablePath}\"");
                        else
                            key.DeleteValue("VnHub", throwOnMissingValue: false);
                    }
                    Bridge.SendToJs("autoStartSet", new { enabled = p.Enabled });
                    LogService.Info($"AutoStart set to {p.Enabled}");
                }
                catch (Exception ex)
                {
                    LogService.Error("AutoStart toggle failed", ex);
                    Bridge.SendToJs("onError", new { message = $"AutoStart failed: {ex.Message}" });
                }
                break;
            }

            case "getBackups":
            {
                var backups = BackupService.GetBackups();
                Bridge.SendToJs("receiveBackups", backups);
                break;
            }

            case "backupNow":
            {
                var path = await Task.Run(() => BackupService.CreateBackupNow());
                if (path != null)
                    Bridge.SendToJs("backupDone", new { ok = true, path });
                else
                    Bridge.SendToJs("onError", new { message = "Backup failed" });
                break;
            }

            case "restoreBackup":
            {
                var p = Bridge.Deserialize<Bridge.FileNamePayload>(payload);
                if (p == null) break;
                if (!PathGuard.IsSafeFileName(p.FileName, ".db"))
                {
                    Bridge.SendToJs("onError", new { message = "Invalid backup file name." });
                    break;
                }
                var ok = await Task.Run(() => BackupService.RestoreBackup(p.FileName));
                if (ok)
                {
                    Bridge.SendToJs("backupRestored", new { ok = true });
                    var lib = await Task.Run(() => LibraryService.GetLibrary());
                    Bridge.SendToJs("receiveLibrary", lib);
                }
                else
                {
                    Bridge.SendToJs("onError", new { message = "Restore failed" });
                }
                break;
            }

            case "openCoversFolder":
            {
                var coversDir = AppPaths.EnsureCoversDir();
                if (Directory.Exists(coversDir))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = coversDir,
                        UseShellExecute = true
                    });
                }
                break;
            }

            case "openLogsFolder":
            {
                var logsDir = LogService.GetLogDir();
                if (Directory.Exists(logsDir))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logsDir,
                        UseShellExecute = true
                    });
                }
                break;
            }

            case "clearLogs":
            {
                var count = LogService.ClearLogs();
                Bridge.SendToJs("logsCleaned", new { count });
                LogService.Info($"Logs cleared: {count} files removed");
                break;
            }

            case "readLogs":
            {
                var content = await Task.Run(() => LogService.ReadRecentLog(500));
                Bridge.SendToJs("logsLoaded", new { content });
                break;
            }

            case "exportCsv":
            {
                var entries = await Task.Run(() => VnRepository.GetAll());
                var path = await Task.Run(() => ExportService.WriteCsv(entries));
                Bridge.SendToJs("exportDone", new { path, format = "csv" });
                LogService.Info($"Exported CSV: {path}");
                break;
            }

            case "exportHtml":
            {
                var entries = await Task.Run(() => VnRepository.GetAll());
                var path = await Task.Run(() => ExportService.WriteHtml(entries));
                Bridge.SendToJs("exportDone", new { path, format = "html" });
                LogService.Info($"Exported HTML: {path}");
                break;
            }

            case "exportJson":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new SaveFileDialog
                    {
                        Filter = "JSON|*.json",
                        FileName = "vnhub-library.json"
                    };
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    try
                    {
                        var entries = VnRepository.GetAll();
                        var path = ExportService.WriteJson(entries, dialog.FileName);
                        Bridge.SendToJs("exportDone", new { path, format = "json" });
                        LogService.Info($"Library exported to JSON {path} ({entries.Count} entries)");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("JSON export failed", ex);
                        Bridge.SendToJs("onError", new { message = $"Export failed: {ex.Message}" });
                    }
                });
                break;
            }

            case "exportSettings":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new SaveFileDialog
                    {
                        Filter = "JSON|*.json",
                        FileName = "vnhub-settings.json"
                    };
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    try
                    {
                        var settings = SettingsService.Load();
                        var json = JsonSerializer.Serialize(settings, SettingsJsonOpts);
                        File.WriteAllText(dialog.FileName, json);
                        Bridge.SendToJs("settingsExported", new { path = dialog.FileName });
                        LogService.Info($"Settings exported to {dialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("Settings export failed", ex);
                        Bridge.SendToJs("onError", new { message = $"Settings export failed: {ex.Message}" });
                    }
                });
                break;
            }

            case "importSettings":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "JSON|*.json|All files|*.*"
                    };
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    try
                    {
                        var json = File.ReadAllText(dialog.FileName);
                        var imported = JsonSerializer.Deserialize<AppSettings>(json, SettingsJsonOpts);
                        if (imported == null)
                        {
                            Bridge.SendToJs("onError", new { message = "Invalid settings file." });
                            return;
                        }
                        imported.ProxyAddress = Validation.SanitizeProxy(imported.ProxyAddress);
                        SettingsService.Save(imported);
                        VndbService.ConfigureProxy(imported.ProxyAddress);
                        IgdbService.Instance.ConfigureProxy(imported.ProxyAddress);
                        AniListService.Instance.ConfigureProxy(imported.ProxyAddress);
                        BangumiService.Instance.ConfigureProxy(imported.ProxyAddress);
                        SteamService.Instance.ConfigureProxy(imported.ProxyAddress);
                        RawgService.Instance.ConfigureProxy(imported.ProxyAddress);
                        SendSettings();
                        Bridge.SendToJs("settingsImported", new { ok = true });
                        LogService.Info($"Settings imported from {dialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("Settings import failed", ex);
                        Bridge.SendToJs("onError", new { message = $"Settings import failed: {ex.Message}" });
                    }
                });
                break;
            }
        }
    }

    private static readonly JsonSerializerOptions SettingsJsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static void SendSettings()
    {
        var settings = SettingsService.Load();
        settings.DbPath = AppDb.DbPath;
        var coversDir = AppPaths.CoversDir;
        Bridge.SendToJs("receiveSettings", new
        {
            settings.Theme,
            settings.DefaultFolder,
            settings.Language,
            settings.DbPath,
            settings.VndbEnabled,
            settings.AutoStart,
            settings.MinimizeToTray,
            settings.StartMinimized,
            settings.MaxBackups,
            settings.BackupInterval,
            settings.Shortcuts,
            settings.ProxyAddress,
            settings.SortBy,
            settings.SortDir,
            settings.GridSize,
            settings.WindowX,
            settings.WindowY,
            settings.WindowWidth,
            settings.WindowHeight,
            settings.WindowMaximized,
            settings.ScanBlacklistExe,
            settings.ScanBlacklistDirs,
            settings.ScanSortBy,
            settings.ScanSortDir,
            settings.ScanSkipExisting,
            settings.ScanRecursive,
            settings.Customization,
            settings.MetadataProvider,
            settings.IgdbClientId,
            settings.IgdbClientSecret,
            settings.RawgApiKey,
            coversPath = coversDir,
            logsPath = LogService.GetLogDir()
        });
    }
}
