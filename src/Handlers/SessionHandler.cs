using System.Text;
using System.Text.Json;
using VnHub.Common;
using VnHub.Database;
using VnHub.Services;

namespace VnHub.Handlers;

public static class SessionHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "getStats":
            {
                var stats = await Task.Run(() => StatsService.Compute(LibraryService.GetLibrary()));
                Bridge.SendToJs("receiveStats", stats);
                break;
            }

            case "exportStats":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new SaveFileDialog
                    {
                        Filter = "JSON|*.json|CSV|*.csv",
                        FileName = "vnhub-stats.json"
                    };
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    try
                    {
                        var stats = StatsService.Compute(LibraryService.GetLibrary());
                        var json = JsonSerializer.Serialize(stats, Bridge.JsonOpts);
                        if (Path.GetExtension(dialog.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                            File.WriteAllText(dialog.FileName, BuildStatsCsv(json));
                        else
                            File.WriteAllText(dialog.FileName, json);
                        Bridge.SendToJs("statsExported", new { path = dialog.FileName });
                        LogService.Info($"Statistics exported to {dialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("Stats export failed", ex);
                        Bridge.SendToJs("onError", new { message = $"Stats export failed: {ex.Message}" });
                    }
                });
                break;
            }

            case "getSessions":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var sessions = await Task.Run(() => SessionRepository.GetByVnId(p.Id));
                Bridge.SendToJs("receiveSessions", new { id = p.Id, sessions });
                break;
            }

            case "getPlayStats":
            {
                var p = Bridge.Deserialize<Bridge.DaysPayload>(payload);
                int days = p?.Days ?? 30;
                if (days < 1) days = 1;
                if (days > 3650) days = 3650;
                var data = await Task.Run(() => SessionRepository.GetStatsByDays(days));
                Bridge.SendToJs("receivePlayStats", new { days, data });
                break;
            }
        }
    }

    private static string BuildStatsCsv(string json)
    {
        var sb = new StringBuilder();
        sb.AppendLine("metric,value");
        using var doc = JsonDocument.Parse(json);
        FlattenScalars(doc.RootElement, "", sb);
        return sb.ToString();
    }

    private static void FlattenScalars(JsonElement element, string prefix, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                    FlattenScalars(prop.Value, prefix.Length == 0 ? prop.Name : $"{prefix}.{prop.Name}", sb);
                break;
            case JsonValueKind.Array:
                break;
            default:
                sb.Append(CsvEscape(prefix)).Append(',').AppendLine(CsvEscape(element.ToString()));
                break;
        }
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return '"' + value.Replace("\"", "\"\"") + '"';
        return value;
    }
}
