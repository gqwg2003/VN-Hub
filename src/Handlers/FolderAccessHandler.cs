using System.Text.Json;
using VnHub.Common;
using VnHub.Database;
using VnHub.Services;

namespace VnHub.Handlers;

public static class FolderAccessHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "openDbFolder":
                OpenFolder(Path.GetDirectoryName(AppDb.DbPath));
                break;

            case "openCoversFolder":
                OpenFolder(AppPaths.EnsureCoversDir());
                break;

            case "openLogsFolder":
                OpenFolder(LogService.GetLogDir());
                break;

            case "clearLogs":
            {
                var count = LogService.ClearLogs();
                Bridge.SendToJs("logsCleaned", new { count });
                LogService.Info($"Logs cleared: {count} files removed");
                break;
            }

            case "readLogs":
            {
                var content = await Task.Run(() => LogService.ReadRecentLog(500));
                Bridge.SendToJs("logsLoaded", new { content });
                break;
            }
        }
    }

    private static void OpenFolder(string? folder)
    {
        if (folder == null || !Directory.Exists(folder)) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }
}
