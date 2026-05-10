using System.Text.Json;
using VnHub.Common;
using VnHub.Models;
using VnHub.Services;

namespace VnHub.Handlers;

public static class ScanHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
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
}
