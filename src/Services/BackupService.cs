namespace VnHub.Services;

using VnHub.Common;

public static class BackupService
{
    public static void BackupOnStartup()
    {
        try
        {
            var settings = SettingsService.Load();
            var interval = settings.BackupInterval;
            if (interval == "never") return;

            var dbPath = Database.AppDb.DbPath;
            if (!File.Exists(dbPath)) return;

            var backupDir = Path.Combine(Path.GetDirectoryName(dbPath)!, "backups");
            Directory.CreateDirectory(backupDir);

            if (interval != "startup")
            {
                var existing = Directory.GetFiles(backupDir, "vnhub-*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
                if (existing != null)
                {
                    var age = DateTime.Now - existing.LastWriteTime;
                    if (interval == "daily" && age.TotalHours < 24) return;
                    if (interval == "weekly" && age.TotalDays < 7) return;
                }
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var destPath = Path.Combine(backupDir, $"vnhub-{timestamp}.db");

            File.Copy(dbPath, destPath, overwrite: false);
            LogService.Info($"Database backed up to {destPath}");

            var maxBackups = settings.MaxBackups > 0 ? settings.MaxBackups : 5;
            var backups = Directory.GetFiles(backupDir, "vnhub-*.db")
                .OrderByDescending(f => f)
                .ToArray();

            for (int i = maxBackups; i < backups.Length; i++)
            {
                File.Delete(backups[i]);
                LogService.Info($"Old backup removed: {Path.GetFileName(backups[i])}");
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Backup failed", ex);
        }
    }

    public static string? CreateBackupNow()
    {
        try
        {
            var settings = SettingsService.Load();
            var dbPath = Database.AppDb.DbPath;
            if (!File.Exists(dbPath)) return null;

            var backupDir = Path.Combine(Path.GetDirectoryName(dbPath)!, "backups");
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var destPath = Path.Combine(backupDir, $"vnhub-{timestamp}.db");
            File.Copy(dbPath, destPath, overwrite: false);
            LogService.Info($"Manual backup created: {destPath}");

            var maxBackups = settings.MaxBackups > 0 ? settings.MaxBackups : 5;
            var backups = Directory.GetFiles(backupDir, "vnhub-*.db")
                .OrderByDescending(f => f)
                .ToArray();
            for (int i = maxBackups; i < backups.Length; i++)
            {
                File.Delete(backups[i]);
            }

            return destPath;
        }
        catch (Exception ex)
        {
            LogService.Error("Manual backup failed", ex);
            return null;
        }
    }

    public static List<object> GetBackups()
    {
        var list = new List<object>();
        var dbPath = Database.AppDb.DbPath;
        var backupDir = Path.Combine(Path.GetDirectoryName(dbPath)!, "backups");
        if (!Directory.Exists(backupDir)) return list;

        var files = Directory.GetFiles(backupDir, "vnhub-*.db")
            .OrderByDescending(f => f)
            .ToArray();

        foreach (var f in files)
        {
            var info = new FileInfo(f);
            list.Add(new
            {
                fileName = info.Name,
                date = info.LastWriteTime.ToString("o"),
                sizeKb = info.Length / 1024
            });
        }
        return list;
    }

    public static bool RestoreBackup(string fileName)
    {
        try
        {
            if (!PathGuard.IsSafeFileName(fileName, ".db"))
            {
                LogService.Warn($"RestoreBackup: rejected unsafe file name '{fileName}'");
                return false;
            }

            var dbPath = Database.AppDb.DbPath;
            var backupDir = Path.Combine(Path.GetDirectoryName(dbPath)!, "backups");
            var backupPath = Path.Combine(backupDir, fileName);

            var safe = PathGuard.EnsureWithin(backupDir, backupPath);
            if (safe == null || !File.Exists(safe))
            {
                LogService.Warn($"RestoreBackup: file not found or outside backup dir: '{fileName}'");
                return false;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var preRestorePath = Path.Combine(backupDir, $"vnhub-pre-restore-{timestamp}.db");
            File.Copy(dbPath, preRestorePath, overwrite: true);

            File.Copy(safe, dbPath, overwrite: true);
            LogService.Info($"Database restored from {fileName}");
            return true;
        }
        catch (Exception ex)
        {
            LogService.Error("Restore failed", ex);
            return false;
        }
    }
}
