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
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await MetadataHandler.FetchAndApplyMetadata(entry.Id, entry.Title, CancellationToken.None);
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            LogService.Error($"Metadata fetch failed for {entry.Id}", ex);
                        }
                    });
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

            case "bulkDelete":
            {
                var p = Bridge.Deserialize<BulkIdsPayload>(payload);
                var ids = ValidIds(p?.Ids);
                if (ids.Count == 0) break;
                await Task.Run(() => VnRepository.BulkDelete(ids));
                foreach (var id in ids)
                    Bridge.SendToJs("vnDeleted", new { id });
                LogService.Info($"Bulk deleted {ids.Count} VNs");
                break;
            }

            case "bulkSetStatus":
            {
                var p = Bridge.Deserialize<BulkStatusPayload>(payload);
                var ids = ValidIds(p?.Ids);
                if (ids.Count == 0) break;
                if (p == null || !Validation.IsValidVnStatus(p.Status))
                {
                    LogService.Warn($"bulkSetStatus: invalid status");
                    break;
                }
                var updated = await Task.Run(() =>
                {
                    VnRepository.BulkSetStatus(ids, (VnStatus)p.Status);
                    return VnRepository.GetByIds(ids);
                });
                foreach (var entry in updated)
                    Bridge.SendToJs("vnUpdated", entry);
                break;
            }

            case "bulkSetFavorite":
            {
                var p = Bridge.Deserialize<BulkBoolPayload>(payload);
                var ids = ValidIds(p?.Ids);
                if (ids.Count == 0 || p == null) break;
                var updated = await Task.Run(() =>
                {
                    VnRepository.BulkSetFavorite(ids, p.Value);
                    return VnRepository.GetByIds(ids);
                });
                foreach (var entry in updated)
                    Bridge.SendToJs("vnUpdated", entry);
                break;
            }

            case "bulkSetPin":
            {
                var p = Bridge.Deserialize<BulkBoolPayload>(payload);
                var ids = ValidIds(p?.Ids);
                if (ids.Count == 0 || p == null) break;
                var updated = await Task.Run(() =>
                {
                    VnRepository.BulkSetPin(ids, p.Value);
                    return VnRepository.GetByIds(ids);
                });
                foreach (var entry in updated)
                    Bridge.SendToJs("vnUpdated", entry);
                break;
            }

            case "bulkSetGroup":
            {
                var p = Bridge.Deserialize<BulkGroupPayload>(payload);
                var ids = ValidIds(p?.Ids);
                if (ids.Count == 0) break;
                var groupId = string.IsNullOrEmpty(p?.GroupId) ? null : p!.GroupId;
                var updated = await Task.Run(() =>
                {
                    VnRepository.BulkSetGroup(ids, groupId);
                    return VnRepository.GetByIds(ids);
                });
                foreach (var entry in updated)
                    Bridge.SendToJs("vnUpdated", entry);
                break;
            }

            case "bulkAddTag":
            {
                var p = Bridge.Deserialize<BulkTagPayload>(payload);
                var ids = ValidIds(p?.Ids);
                if (ids.Count == 0 || p == null || string.IsNullOrWhiteSpace(p.Tag)) break;
                var updated = await Task.Run(() =>
                {
                    VnRepository.BulkAddTag(ids, p.Tag);
                    return VnRepository.GetByIds(ids);
                });
                foreach (var entry in updated)
                    Bridge.SendToJs("vnUpdated", entry);
                break;
            }

            case "bulkRemoveTag":
            {
                var p = Bridge.Deserialize<BulkTagPayload>(payload);
                var ids = ValidIds(p?.Ids);
                if (ids.Count == 0 || p == null || string.IsNullOrWhiteSpace(p.Tag)) break;
                var updated = await Task.Run(() =>
                {
                    VnRepository.BulkRemoveTag(ids, p.Tag);
                    return VnRepository.GetByIds(ids);
                });
                foreach (var entry in updated)
                    Bridge.SendToJs("vnUpdated", entry);
                break;
            }
        }
    }

    private static List<string> ValidIds(IEnumerable<string>? ids)
    {
        if (ids == null) return new List<string>();
        return ids.Where(Validation.IsValidVnId).Distinct(StringComparer.Ordinal).ToList();
    }

    private class BulkIdsPayload
    {
        public List<string>? Ids { get; set; }
    }

    private class BulkStatusPayload
    {
        public List<string>? Ids { get; set; }
        public int Status { get; set; }
    }

    private class BulkBoolPayload
    {
        public List<string>? Ids { get; set; }
        public bool Value { get; set; }
    }

    private class BulkGroupPayload
    {
        public List<string>? Ids { get; set; }
        public string? GroupId { get; set; }
    }

    private class BulkTagPayload
    {
        public List<string>? Ids { get; set; }
        public string Tag { get; set; } = "";
    }
}

