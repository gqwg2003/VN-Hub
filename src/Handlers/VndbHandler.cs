using System.Text.Json;
using VnHub.Database;
using VnHub.Services;

namespace VnHub.Handlers;

public static class VndbHandler
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
                await FetchAndApplyVndb(vnEntry.Id, vnEntry.Title, token);
        });

        return Task.CompletedTask;
    }

    internal static async Task FetchAndApplyVndb(string vnId, string title, CancellationToken ct)
    {
        try
        {
            var result = await VndbService.SearchAsync(title, ct);
            if (ct.IsCancellationRequested) return;

            if (result == null)
            {
                Bridge.InvokeOnUiThread(() => Bridge.SendToJs("vndbResult", new { id = vnId, found = false }));
                return;
            }

            var entry = VnRepository.GetById(vnId);
            if (entry == null) return;

            entry.VndbId = result.VndbId;
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
                    var coversDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "VnHub", "covers");
                    var oldCoverPath = Path.Combine(coversDir, entry.CoverPath);
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

            VnRepository.Update(entry);

            Bridge.InvokeOnUiThread(() =>
            {
                Bridge.SendToJs("vnUpdated", entry);
                Bridge.SendToJs("vndbResult", new { id = vnId, found = true, title = result.Title, coverError });
            });

            LogService.Info($"VNDB data applied: {title} → {result.VndbId}");
        }
        catch (OperationCanceledException)
        {
            LogService.Info($"VNDB fetch cancelled for {vnId}");
        }
        catch (Exception ex)
        {
            LogService.Error($"VNDB fetch failed for {vnId}", ex);
            try
            {
                Bridge.InvokeOnUiThread(() =>
                    Bridge.SendToJs("vndbResult", new { id = vnId, found = false, error = ex.Message }));
            }
            catch { /* webview may be disposed */ }
        }
    }
}
