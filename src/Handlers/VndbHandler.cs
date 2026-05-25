using System.Text.Json;
using VnHub.Common;
using VnHub.Database;
using VnHub.Services;

namespace VnHub.Handlers;

public static class MetadataHandler
{
    private static CancellationTokenSource? _fetchCts;

    public static Task Handle(string action, JsonElement? payload)
    {
        var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
        if (p == null) return Task.CompletedTask;

        var settings = SettingsService.Load();
        if (!settings.VndbEnabled)
        {
            Bridge.SendToJs("vndbResult", new { id = p.Id, found = false, disabled = true });
            return Task.CompletedTask;
        }

        _fetchCts?.Cancel();
        _fetchCts?.Dispose();
        _fetchCts = new CancellationTokenSource();
        var token = _fetchCts.Token;

        _ = Task.Run(async () =>
        {
            var vnEntry = VnRepository.GetById(p.Id);
            if (vnEntry != null)
                await FetchAndApplyMetadata(vnEntry.Id, vnEntry.Title, token);
        });

        return Task.CompletedTask;
    }

    internal static async Task FetchAndApplyMetadata(string vnId, string title, CancellationToken ct)
    {
        try
        {
            var settings = SettingsService.Load();
            var provider = GetProvider(settings);
            var result = await provider.SearchAsync(title, ct);
            if (ct.IsCancellationRequested) return;

            if (result == null)
            {
                Bridge.InvokeOnUiThread(() => Bridge.SendToJs("vndbResult", new { id = vnId, found = false }));
                return;
            }

            var entry = VnRepository.GetById(vnId);
            if (entry == null) return;

            entry.VndbId = result.ExternalId;
            entry.Description = result.Description;
            entry.Rating = result.Rating;

            var existingTags = JsonSerializer.Deserialize<List<string>>(entry.Tags) ?? new();
            foreach (var tag in result.Tags)
            {
                if (!existingTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    existingTags.Add(tag);
            }
            entry.Tags = JsonSerializer.Serialize(existingTags);

            string? coverError = null;
            if (!string.IsNullOrEmpty(result.ImageUrl))
            {
                if (!string.IsNullOrEmpty(entry.CoverPath))
                {
                    var oldCoverPath = Path.Combine(AppPaths.CoversDir, entry.CoverPath);
                    if (File.Exists(oldCoverPath))
                    {
                        try { File.Delete(oldCoverPath); }
                        catch (Exception ex) { LogService.Error("Failed to delete old cover", ex); }
                    }
                }

                var (coverName, err) = await VndbService.DownloadCoverAsync(result.ImageUrl, vnId, ct);
                if (coverName != null)
                    entry.CoverPath = coverName;
                else
                    coverError = err;
            }

            VnHub.Common.Validation.Normalize(entry);
            VnRepository.Update(entry);

            Bridge.InvokeOnUiThread(() =>
            {
                Bridge.SendToJs("vnUpdated", entry);
                Bridge.SendToJs("vndbResult", new { id = vnId, found = true, title = result.Title, coverError });
            });

            LogService.Info($"Metadata applied ({provider.DisplayName}): {title} → {result.ExternalId}");
        }
        catch (OperationCanceledException)
        {
            LogService.Info($"Metadata fetch cancelled for {vnId}");
        }
        catch (Exception ex)
        {
            LogService.Error($"Metadata fetch failed for {vnId}", ex);
            try
            {
                Bridge.InvokeOnUiThread(() =>
                    Bridge.SendToJs("vndbResult", new { id = vnId, found = false, error = ex.Message }));
            }
            catch { /* webview may be disposed */ }
        }
    }

    private static IMetadataProvider GetProvider(AppSettings settings) =>
        settings.MetadataProvider switch
        {
            "igdb" => IgdbService.Instance,
            "anilist" => AniListService.Instance,
            _ => VndbProvider.Instance
        };
}
