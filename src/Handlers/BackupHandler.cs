using System.Text.Json;
using VnHub.Database;
using VnHub.Services;

namespace VnHub.Handlers;

public static class BackupHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "getBackups":
            {
                var backups = BackupService.GetBackups();
                Bridge.SendToJs("receiveBackups", backups);
                break;
            }

            case "backupNow":
            {
                var path = await Task.Run(() => BackupService.CreateBackupNow());
                if (path != null)
                    Bridge.SendToJs("backupDone", new { ok = true, path });
                else
                    Bridge.SendToJs("onError", new { message = "Backup failed" });
                break;
            }

            case "restoreBackup":
            {
                var p = Bridge.Deserialize<Bridge.FileNamePayload>(payload);
                if (p == null) break;
                if (!Common.PathGuard.IsSafeFileName(p.FileName, ".db"))
                {
                    Bridge.SendToJs("onError", new { message = "Invalid backup file name." });
                    break;
                }
                var ok = await Task.Run(() => BackupService.RestoreBackup(p.FileName));
                if (ok)
                {
                    Bridge.SendToJs("backupRestored", new { ok = true });
                    var lib = await Task.Run(() => LibraryService.GetLibrary());
                    Bridge.SendToJs("receiveLibrary", lib);
                }
                else
                {
                    Bridge.SendToJs("onError", new { message = "Restore failed" });
                }
                break;
            }
        }
    }
}
