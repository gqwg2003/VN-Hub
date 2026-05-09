using System.Text;
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
                var settings = SettingsService.Load();
                settings.DbPath = AppDb.DbPath;
                var coversDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VnHub", "covers");
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
                    coversPath = coversDir,
                    logsPath = LogService.GetLogDir()
                });
                break;
            }

            case "saveSettings":
            {
                var s = Bridge.Deserialize<AppSettings>(payload);
                if (s == null) break;
                SettingsService.Save(s);
                VndbService.ConfigureProxy(s.ProxyAddress);
                Bridge.SendToJs("settingsSaved", new { ok = true });
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
                var coversDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VnHub", "covers");
                Directory.CreateDirectory(coversDir);
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

            case "exportCsv":
            {
                var entries = await Task.Run(() => VnRepository.GetAll());
                var sb = new StringBuilder();
                sb.AppendLine("Title,Status,Rating,UserRating,PlayTimeHours,DateAdded,CompletedAt,IsFavorite,IsPinned,ReadingProgress,VndbId,ExePath");
                foreach (var e in entries)
                {
                    var title = EscapeCsv(e.Title);
                    var hours = Math.Round(e.PlayTimeSeconds / 3600.0, 1);
                    sb.AppendLine($"{title},{e.Status},{e.Rating?.ToString() ?? ""},{e.UserRating?.ToString() ?? ""},{hours},{e.DateAdded:yyyy-MM-dd},{e.CompletedAt ?? ""},{e.IsFavorite},{e.IsPinned},{e.ReadingProgress},{e.VndbId ?? ""},{EscapeCsv(e.ExePath ?? "")}");
                }
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VnHub", $"vnhub_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                Bridge.SendToJs("exportDone", new { path, format = "csv" });
                LogService.Info($"Exported CSV: {path}");
                break;
            }

            case "exportHtml":
            {
                var entries = await Task.Run(() => VnRepository.GetAll());
                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>VN-Hub Export</title>");
                sb.AppendLine("<style>body{font-family:system-ui;background:#1a1a2e;color:#e0e0e0;padding:2rem}table{border-collapse:collapse;width:100%}th,td{border:1px solid #333;padding:8px;text-align:left}th{background:#16213e}tr:nth-child(even){background:#1a1a3e}img{height:40px;border-radius:4px}</style></head><body>");
                sb.AppendLine("<h1>VN-Hub Library</h1>");
                sb.AppendLine($"<p>Exported: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
                sb.AppendLine("<table><thead><tr><th>Title</th><th>Status</th><th>Rating</th><th>User Rating</th><th>Play Time</th><th>Progress</th><th>Date Added</th></tr></thead><tbody>");
                foreach (var e in entries)
                {
                    var hours = Math.Round(e.PlayTimeSeconds / 3600.0, 1);
                    sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(e.Title)}</td><td>{e.Status}</td><td>{e.Rating?.ToString() ?? "-"}</td><td>{e.UserRating?.ToString() ?? "-"}</td><td>{hours}h</td><td>{e.ReadingProgress}%</td><td>{e.DateAdded:yyyy-MM-dd}</td></tr>");
                }
                sb.AppendLine("</tbody></table></body></html>");
                var htmlPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VnHub", $"vnhub_export_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(htmlPath, sb.ToString(), Encoding.UTF8);
                Bridge.SendToJs("exportDone", new { path = htmlPath, format = "html" });
                LogService.Info($"Exported HTML: {htmlPath}");
                break;
            }
        }
    }

    private static string EscapeCsv(string val)
    {
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
            return "\"" + val.Replace("\"", "\"\"") + "\"";
        return val;
    }
}
