using System.Text.Json;
using VnHub.Database;
using VnHub.Models;
using VnHub.Services;

namespace VnHub.Handlers;

public static class ExportHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "exportLibrary":
            {
                FileDialogHelper.SaveFile(FileDialogHelper.JsonSaveFilter, "vnhub-library.json", fileName =>
                {
                    try
                    {
                        var entries = LibraryService.GetLibrary();
                        var json = JsonSerializer.Serialize(entries, Bridge.JsonOpts);
                        File.WriteAllText(fileName, json);
                        Bridge.SendToJs("exportDone", new { path = fileName });
                        LogService.Info($"Library exported to {fileName} ({entries.Count} entries)");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("Export failed", ex);
                        Bridge.SendToJs("onError", new { message = $"Export failed: {ex.Message}" });
                    }
                });
                break;
            }

            case "importLibrary":
            {
                FileDialogHelper.PickFile(FileDialogHelper.JsonOpenFilter, fileName =>
                {
                    try
                    {
                        var json = File.ReadAllText(fileName);
                        var entries = JsonSerializer.Deserialize<List<VnEntry>>(json, Bridge.JsonOpts);
                        if (entries == null)
                        {
                            Bridge.SendToJs("onError", new { message = "Invalid JSON format" });
                            return;
                        }
                        var count = VnRepository.BulkImport(entries);
                        Bridge.SendToJs("importDone", new { count });
                        LogService.Info($"Library imported from {fileName} ({count} new entries)");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("Import failed", ex);
                        Bridge.SendToJs("onError", new { message = $"Import failed: {ex.Message}" });
                    }
                });
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
                FileDialogHelper.SaveFile(FileDialogHelper.JsonSaveFilter, "vnhub-library.json", fileName =>
                {
                    try
                    {
                        var entries = VnRepository.GetAll();
                        var path = ExportService.WriteJson(entries, fileName);
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
        }
    }
}
