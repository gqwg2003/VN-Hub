using System.Globalization;
using System.Text.Json;
using VnHub.Common;
using VnHub.Database;
using VnHub.Models;
using VnHub.Services;

namespace VnHub.Handlers;

public static class LibraryHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "getLibrary":
            {
                var entries = await Task.Run(() => LibraryService.GetLibrary());
                Bridge.SendToJs("receiveLibrary", entries);
                break;
            }

            case "addVn":
            {
                var p = Bridge.Deserialize<Bridge.AddVnPayload>(payload);
                if (p == null) break;
                p.Title = Validation.SanitizeTitle(p.Title);
                var entry = await Task.Run(() => LibraryService.AddVn(p.Title, p.Path, p.ExePath));
                if (p.SkipVndb) entry.SkipVndb = true;
                if (entry.SkipVndb) await Task.Run(() => VnRepository.Update(entry));
                Bridge.SendToJs("vnAdded", entry);
                LogService.Info($"VN added: {entry.Title} ({entry.Id})");

                var settings = SettingsService.Load();
                if (settings.VndbEnabled && !entry.SkipVndb)
                    _ = Task.Run(() => VndbHandler.FetchAndApplyVndb(entry.Id, entry.Title, CancellationToken.None));
                break;
            }

            case "updateVn":
            {
                var entry = Bridge.Deserialize<VnEntry>(payload);
                if (entry == null) break;
                if (!Validation.IsValidVnId(entry.Id))
                {
                    LogService.Warn($"updateVn: invalid id '{entry.Id}'");
                    break;
                }
                Validation.Normalize(entry);

                var updated = await Task.Run(() =>
                {
                    var existing = VnRepository.GetById(entry.Id);
                    if (existing != null)
                    {
                        entry.DateAdded = existing.DateAdded;
                        entry.PlayTimeSeconds = existing.PlayTimeSeconds;
                        entry.LastLaunchedAt = existing.LastLaunchedAt;
                        entry.VndbId = existing.VndbId;
                        entry.Description = existing.Description;
                        entry.Rating = existing.Rating;
                        entry.CompletedAt = existing.CompletedAt;
                    }
                    return LibraryService.UpdateVn(entry);
                });
                Bridge.SendToJs("vnUpdated", updated);
                break;
            }

            case "deleteVn":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                await Task.Run(() => LibraryService.DeleteVn(p.Id));
                Bridge.SendToJs("vnDeleted", new { id = p.Id });
                LogService.Info($"VN deleted: {p.Id}");
                break;
            }

            case "toggleFavorite":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var entry = await Task.Run(() =>
                {
                    LibraryService.ToggleFavorite(p.Id);
                    return VnRepository.GetById(p.Id);
                });
                Bridge.SendToJs("vnUpdated", entry);
                break;
            }

            case "togglePin":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var entry = await Task.Run(() =>
                {
                    LibraryService.TogglePin(p.Id);
                    return VnRepository.GetById(p.Id);
                });
                Bridge.SendToJs("vnUpdated", entry);
                break;
            }

            case "setStatus":
            {
                var p = Bridge.Deserialize<Bridge.SetStatusPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                if (!Validation.IsValidVnStatus(p.Status))
                {
                    LogService.Warn($"setStatus: invalid status {p.Status} for {p.Id}");
                    break;
                }
                var entry = await Task.Run(() =>
                {
                    LibraryService.SetStatus(p.Id, (VnStatus)p.Status);
                    return VnRepository.GetById(p.Id);
                });
                Bridge.SendToJs("vnUpdated", entry);
                break;
            }

            case "launchVn":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var entry = VnRepository.GetById(p.Id);
                var ok = LauncherService.Launch(p.Id, entry?.ExePath);
                Bridge.SendToJs("launchResult", new { success = ok, id = p.Id });
                if (ok)
                {
                    Bridge.SendToJs("gameStarted", new { id = p.Id });
                    LogService.Info($"Launched: {entry?.Title} ({p.Id})");
                }
                break;
            }

            case "getRunningGames":
            {
                var ids = LauncherService.GetRunningIds();
                Bridge.SendToJs("runningGames", new { ids });
                break;
            }

            case "getStats":
            {
                var stats = await Task.Run(() => ComputeStats(LibraryService.GetLibrary()));
                Bridge.SendToJs("receiveStats", stats);
                break;
            }

            case "pickFolder":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new FolderBrowserDialog();
                    var settings = SettingsService.Load();
                    if (!string.IsNullOrEmpty(settings.DefaultFolder))
                        dialog.InitialDirectory = settings.DefaultFolder;

                    if (dialog.ShowDialog() == DialogResult.OK)
                        Bridge.SendToJs("folderPicked", new { path = dialog.SelectedPath });
                });
                break;
            }

            case "pickImage":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All files|*.*"
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                        Bridge.SendToJs("imagePicked", new { path = dialog.FileName });
                });
                break;
            }

            case "setCover":
            {
                var p = Bridge.Deserialize<Bridge.SetCoverPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var coverName = await Task.Run(() => LibraryService.SetCover(p.Id, p.SourcePath));
                if (coverName != null)
                {
                    var entry = VnRepository.GetById(p.Id);
                    Bridge.SendToJs("vnUpdated", entry);
                }
                break;
            }

            case "pickExe":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "Executables|*.exe|All files|*.*"
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        var title = IconService.GetTitleFromExe(dialog.FileName);
                        Bridge.SendToJs("exePicked", new { path = dialog.FileName, suggestedTitle = title });
                    }
                });
                break;
            }

            case "extractIcon":
            {
                var p = Bridge.Deserialize<Bridge.ExtractIconPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var iconName = await Task.Run(() => IconService.ExtractIconFromExe(p.ExePath, p.Id));
                if (iconName != null)
                {
                    var entry = VnRepository.GetById(p.Id);
                    if (entry != null && string.IsNullOrEmpty(entry.CoverPath))
                    {
                        entry.CoverPath = iconName;
                        VnRepository.Update(entry);
                        Bridge.SendToJs("vnUpdated", entry);
                    }
                }
                break;
            }

            case "searchVn":
            {
                var p = Bridge.Deserialize<Bridge.SearchPayload>(payload);
                if (p == null) break;
                var results = await Task.Run(() => LibraryService.Search(p.Query ?? ""));
                Bridge.SendToJs("receiveLibrary", results);
                break;
            }

            case "openFolder":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var entry = VnRepository.GetById(p.Id);
                var exe = entry?.ExePath;
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) break;
                if (exe.IndexOfAny(new[] { '"', '\0', '\r', '\n' }) >= 0)
                {
                    LogService.Warn($"openFolder: rejected path with unsafe characters for {p.Id}");
                    break;
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{exe}\"",
                    UseShellExecute = false
                });
                break;
            }

            case "getSessions":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var sessions = await Task.Run(() => Database.SessionRepository.GetByVnId(p.Id));
                Bridge.SendToJs("receiveSessions", new { id = p.Id, sessions });
                break;
            }

            case "getPlayStats":
            {
                var p = Bridge.Deserialize<Bridge.DaysPayload>(payload);
                int days = p?.Days ?? 30;
                if (days < 1) days = 1;
                if (days > 3650) days = 3650;
                var data = await Task.Run(() => Database.SessionRepository.GetStatsByDays(days));
                Bridge.SendToJs("receivePlayStats", new { days, data });
                break;
            }

            case "scanFolder":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new FolderBrowserDialog();
                    var settings = SettingsService.Load();
                    if (!string.IsNullOrEmpty(settings.DefaultFolder))
                        dialog.InitialDirectory = settings.DefaultFolder;

                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    var selectedPath = dialog.SelectedPath;
                    _ = Task.Run(() =>
                    {
                        var existingTitles = new HashSet<string>(
                            LibraryService.GetLibrary().Select(e => e.Title.ToLowerInvariant()));
                        var results = ScanService.ScanFolder(selectedPath, existingTitles, settings);
                        Bridge.SendToJs("scanResults", new { path = selectedPath, items = results });
                    });
                });
                break;
            }

            case "bulkAddScanned":
            {
                var p = Bridge.Deserialize<Bridge.BulkAddPayload>(payload);
                if (p?.Items == null) break;
                var settings = SettingsService.Load();
                var added = await Task.Run(() =>
                {
                    var list = new List<VnEntry>();
                    foreach (var item in p.Items)
                    {
                        var safeTitle = Validation.SanitizeTitle(item.Title);
                        var entry = LibraryService.AddVn(safeTitle, null, item.ExePath);
                        list.Add(entry);
                        if (settings.VndbEnabled)
                            _ = Task.Run(() => VndbHandler.FetchAndApplyVndb(entry.Id, entry.Title, CancellationToken.None));
                    }
                    return list;
                });
                Bridge.SendToJs("bulkAddDone", new { count = added.Count });
                var library = await Task.Run(() => LibraryService.GetLibrary());
                Bridge.SendToJs("receiveLibrary", library);
                LogService.Info($"Bulk scan added {added.Count} VNs");
                break;
            }
        }
    }

    private static object ComputeStats(List<VnEntry> entries)
    {
        var totalVn = entries.Count;
        var byStatus = new Dictionary<int, int>();
        for (int i = 0; i <= 4; i++) byStatus[i] = 0;
        long totalPlayTime = 0;
        int favCount = 0;
        double ratingSum = 0;
        int ratingCount = 0;
        var monthlyAdds = new Dictionary<string, int>();
        foreach (var e in entries)
        {
            byStatus[(int)e.Status]++;
            totalPlayTime += e.PlayTimeSeconds;
            if (e.IsFavorite) favCount++;
            if (e.UserRating.HasValue) { ratingSum += e.UserRating.Value; ratingCount++; }
            if (!string.IsNullOrEmpty(e.DateAdded))
            {
                try
                {
                    var d = DateTime.Parse(e.DateAdded, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    var key = d.ToString("yyyy-MM");
                    monthlyAdds.TryGetValue(key, out int c);
                    monthlyAdds[key] = c + 1;
                }
                catch { }
            }
        }
        var topPlayed = entries
            .Where(e => e.PlayTimeSeconds > 0)
            .OrderByDescending(e => e.PlayTimeSeconds)
            .Take(10)
            .Select(e => new { id = e.Id, title = e.Title, cover = e.CoverPath, playTime = e.PlayTimeSeconds, userRating = e.UserRating })
            .ToList();
        var topRated = entries
            .Where(e => e.UserRating.HasValue)
            .OrderByDescending(e => e.UserRating!.Value)
            .Take(10)
            .Select(e => new { id = e.Id, title = e.Title, cover = e.CoverPath, playTime = e.PlayTimeSeconds, userRating = e.UserRating })
            .ToList();
        var avgRating = ratingCount > 0 ? Math.Round(ratingSum / ratingCount, 1) : 0.0;
        return new { totalVn, byStatus, totalPlayTime, monthlyAdds, topPlayed, topRated, favCount, avgRating };
    }
}

