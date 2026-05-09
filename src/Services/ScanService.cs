using System.Text.RegularExpressions;

namespace VnHub.Services;

public static class ScanService
{
    private static readonly HashSet<string> DefaultBlacklistExe = new(StringComparer.OrdinalIgnoreCase)
    {
        "uninstall", "unins000", "unins001", "setup", "install",
        "config", "configure", "UnityCrashHandler", "UnityCrashHandler32", "UnityCrashHandler64",
        "vcredist", "dxsetup", "dxwebsetup", "dotNetFx",
        "windowsdesktop-runtime", "updater", "patcher",
        "crashreporter", "bugreport", "7z", "winrar",
        "notification_helper", "createdump"
    };

    private static readonly HashSet<string> DefaultBlacklistDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "_redist", "_CommonRedist", "__support", "DotNetFX", "DirectX",
        "_Redist", "Redist", "CommonRedist", ".unity"
    };

    public static List<ScanResult> ScanFolder(string rootPath, HashSet<string> existingTitles, AppSettings? settings = null)
    {
        var blacklistExe = BuildSet(settings?.ScanBlacklistExe, DefaultBlacklistExe);
        var blacklistDirs = BuildSet(settings?.ScanBlacklistDirs, DefaultBlacklistDirs);
        bool recursive = settings?.ScanRecursive == true;

        var results = new List<ScanResult>();
        if (!Directory.Exists(rootPath)) return results;

        foreach (var dir in Directory.GetDirectories(rootPath))
        {
            var folderName = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(folderName)) continue;
            if (folderName.StartsWith('.') || folderName.StartsWith('_')) continue;

            var exes = FindExecutables(dir, blacklistExe, blacklistDirs);
            if (exes.Count == 0) continue;

            var best = PickBestExe(exes, folderName);
            var title = CleanTitle(folderName);
            var alreadyExists = existingTitles.Contains(title.ToLowerInvariant());

            results.Add(new ScanResult
            {
                FolderPath = dir,
                Title = title,
                Exes = exes,
                SelectedExe = best,
                AlreadyExists = alreadyExists
            });

            if (recursive)
            {
                foreach (var sub in Directory.GetDirectories(dir))
                {
                    var subName = Path.GetFileName(sub);
                    if (string.IsNullOrEmpty(subName) || subName.StartsWith('.') || subName.StartsWith('_')) continue;
                    if (blacklistDirs.Any(bd => subName.Equals(bd, StringComparison.OrdinalIgnoreCase))) continue;

                    var subExes = FindExecutables(sub, blacklistExe, blacklistDirs);
                    if (subExes.Count == 0) continue;

                    var subBest = PickBestExe(subExes, subName);
                    var subTitle = CleanTitle(subName);
                    var subExists = existingTitles.Contains(subTitle.ToLowerInvariant());

                    results.Add(new ScanResult
                    {
                        FolderPath = sub,
                        Title = subTitle,
                        Exes = subExes,
                        SelectedExe = subBest,
                        AlreadyExists = subExists
                    });
                }
            }
        }

        var sortBy = settings?.ScanSortBy ?? "title";
        var sortAsc = (settings?.ScanSortDir ?? "asc") == "asc";
        results = sortBy switch
        {
            "folder" => sortAsc
                ? results.OrderBy(r => r.FolderPath, StringComparer.OrdinalIgnoreCase).ToList()
                : results.OrderByDescending(r => r.FolderPath, StringComparer.OrdinalIgnoreCase).ToList(),
            "exeCount" => sortAsc
                ? results.OrderBy(r => r.Exes.Count).ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList()
                : results.OrderByDescending(r => r.Exes.Count).ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => sortAsc
                ? results.OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList()
                : results.OrderByDescending(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList()
        };

        return results;
    }

    private static HashSet<string> BuildSet(List<string>? custom, HashSet<string> defaults)
    {
        var set = new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase);
        if (custom != null)
        {
            foreach (var item in custom)
            {
                var trimmed = item.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    set.Add(trimmed);
            }
        }
        return set;
    }

    private static List<string> FindExecutables(string dir, HashSet<string> blacklistExe, HashSet<string> blacklistDirs)
    {
        var all = new List<string>();
        try
        {
            all.AddRange(Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories));
        }
        catch { return all; }

        return all
            .Where(exe =>
            {
                var name = Path.GetFileNameWithoutExtension(exe);
                if (blacklistExe.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    return false;

                var relDir = Path.GetDirectoryName(exe)?[dir.Length..] ?? "";
                if (blacklistDirs.Any(bd => relDir.Contains(bd, StringComparison.OrdinalIgnoreCase)))
                    return false;

                return true;
            })
            .ToList();
    }

    private static string PickBestExe(List<string> exes, string folderName)
    {
        if (exes.Count == 1) return exes[0];

        var normalizedFolder = Regex.Replace(folderName, @"[\s_\-.]", "").ToLowerInvariant();

        var nameMatch = exes
            .OrderByDescending(e =>
            {
                var n = Regex.Replace(Path.GetFileNameWithoutExtension(e), @"[\s_\-.]", "").ToLowerInvariant();
                return n == normalizedFolder ? 3 :
                       n.Contains(normalizedFolder) || normalizedFolder.Contains(n) ? 2 : 0;
            })
            .First();

        if (Regex.Replace(Path.GetFileNameWithoutExtension(nameMatch), @"[\s_\-.]", "").ToLowerInvariant()
            .Contains(normalizedFolder) || normalizedFolder.Contains(
                Regex.Replace(Path.GetFileNameWithoutExtension(nameMatch), @"[\s_\-.]", "").ToLowerInvariant()))
        {
            return nameMatch;
        }

        var x64 = exes.FirstOrDefault(e =>
        {
            var n = Path.GetFileNameWithoutExtension(e).ToLowerInvariant();
            return n.Contains("x64") || n.Contains("64bit") || n.EndsWith("64");
        });
        if (x64 != null)
        {
            var base64 = Regex.Replace(Path.GetFileNameWithoutExtension(x64).ToLowerInvariant(), @"(x64|64bit|_64|64)$", "");
            var hasX86 = exes.Any(e =>
            {
                var n = Regex.Replace(Path.GetFileNameWithoutExtension(e).ToLowerInvariant(), @"(x86|32bit|_32|32)$", "");
                return n == base64 && e != x64;
            });
            if (hasX86) return x64;
        }

        var rootDir = exes.Min(e => e.Split(Path.DirectorySeparatorChar).Length);
        var rootExes = exes.Where(e => e.Split(Path.DirectorySeparatorChar).Length == rootDir).ToList();
        if (rootExes.Count == 1) return rootExes[0];

        return (rootExes.Count > 0 ? rootExes : exes)
            .OrderByDescending(e =>
            {
                try { return new FileInfo(e).Length; }
                catch { return 0; }
            })
            .First();
    }

    private static string CleanTitle(string folderName)
    {
        var cleaned = Regex.Replace(folderName, @"\[.*?\]|\(.*?\)", "").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? folderName : cleaned;
    }

    public class ScanResult
    {
        public string FolderPath { get; set; } = "";
        public string Title { get; set; } = "";
        public List<string> Exes { get; set; } = new();
        public string SelectedExe { get; set; } = "";
        public bool AlreadyExists { get; set; }
    }
}
