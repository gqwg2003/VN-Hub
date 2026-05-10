using System.Text;
using VnHub.Common;
using VnHub.Models;

namespace VnHub.Services;

public static class ExportService
{
    public static string WriteCsv(IEnumerable<VnEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Title,Status,Rating,UserRating,PlayTimeHours,DateAdded,CompletedAt,IsFavorite,IsPinned,ReadingProgress,VndbId,ExePath");
        foreach (var e in entries)
        {
            var hours = Math.Round(e.PlayTimeSeconds / 3600.0, 1);
            sb.Append(EscapeCsv(e.Title)).Append(',')
              .Append(e.Status).Append(',')
              .Append(e.Rating?.ToString() ?? "").Append(',')
              .Append(e.UserRating?.ToString() ?? "").Append(',')
              .Append(hours).Append(',')
              .Append(e.DateAdded ?? "").Append(',')
              .Append(e.CompletedAt ?? "").Append(',')
              .Append(e.IsFavorite).Append(',')
              .Append(e.IsPinned).Append(',')
              .Append(e.ReadingProgress).Append(',')
              .Append(e.VndbId ?? "").Append(',')
              .Append(EscapeCsv(e.ExePath ?? ""))
              .AppendLine();
        }

        AppPaths.EnsureRoot();
        var path = Path.Combine(AppPaths.Root, $"vnhub_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    public static string WriteHtml(IEnumerable<VnEntry> entries)
    {
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

        AppPaths.EnsureRoot();
        var path = Path.Combine(AppPaths.Root, $"vnhub_export_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string EscapeCsv(string val)
    {
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
            return "\"" + val.Replace("\"", "\"\"") + "\"";
        return val;
    }
}
