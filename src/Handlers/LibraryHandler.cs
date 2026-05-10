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
                var q = Bridge.Deserialize<Bridge.LibraryQueryPayload>(payload);
                List<VnEntry> entries;
                if (q == null)
                {
                    entries = await Task.Run(() => LibraryService.GetLibrary());
                }
                else
                {
                    entries = await Task.Run(() => VnRepository.GetFiltered(
                        q.Tab, q.MarkedSubTab, q.Status, q.GroupId, q.Tag, q.Search, q.SortBy, q.SortDir));
                }
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

            case "getTags":
            {
                var tags = await Task.Run(() => VnRepository.GetTagCounts());
                Bridge.SendToJs("receiveTags", tags);
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
        }
    }
}

