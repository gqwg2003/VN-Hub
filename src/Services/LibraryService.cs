using VnHub.Common;
using VnHub.Database;
using VnHub.Models;

namespace VnHub.Services;

public static class LibraryService
{
    public static List<VnEntry> GetLibrary() => VnRepository.GetAll();

    public static List<VnEntry> GetByStatus(VnStatus status) => VnRepository.GetByStatus(status);

    public static List<VnEntry> GetFavorites() => VnRepository.GetFavorites();

    public static List<VnEntry> GetPinned() => VnRepository.GetPinned();

    public static VnEntry AddVn(string title, string? folderPath, string? exePath)
    {
        var resolvedExe = exePath ?? FindExecutable(folderPath);

        if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrEmpty(resolvedExe))
            title = IconService.GetTitleFromExe(resolvedExe);
        if (string.IsNullOrWhiteSpace(title))
            title = "Untitled";

        var entry = new VnEntry
        {
            Title = title,
            ExePath = resolvedExe
        };

        if (!string.IsNullOrEmpty(resolvedExe))
        {
            var iconName = IconService.ExtractIconFromExe(resolvedExe, entry.Id);
            if (iconName != null)
                entry.CoverPath = iconName;
        }

        VnRepository.Insert(entry);
        return entry;
    }

    public static VnEntry? UpdateVn(VnEntry entry)
    {
        Validation.Normalize(entry);
        VnRepository.Update(entry);
        return entry;
    }

    public static void DeleteVn(string id) => VnRepository.Delete(id);

    public static void ToggleFavorite(string id) => VnRepository.ToggleFavorite(id);

    public static void TogglePin(string id) => VnRepository.TogglePin(id);

    public static void SetStatus(string id, VnStatus status) => VnRepository.SetStatus(id, status);

    public static List<VnEntry> Search(string query) => VnRepository.Search(query);

    public static string? SetCover(string vnId, string sourcePath)
    {
        var coversDir = AppPaths.EnsureCoversDir();

        var safeId = Path.GetFileName(vnId);
        if (string.IsNullOrEmpty(safeId) || safeId != vnId)
        {
            LogService.Warn($"SetCover: invalid vnId '{vnId}'");
            return null;
        }

        var ext = Path.GetExtension(sourcePath);
        var destName = $"{safeId}{ext}";
        var destPath = Path.Combine(coversDir, destName);

        if (PathGuard.EnsureWithin(coversDir, destPath) == null)
        {
            LogService.Warn($"SetCover: path traversal attempt with vnId '{vnId}'");
            return null;
        }

        var entry = VnRepository.GetById(vnId);
        if (entry != null && !string.IsNullOrEmpty(entry.CoverPath))
        {
            var oldPath = Path.Combine(coversDir, Path.GetFileName(entry.CoverPath));
            if (File.Exists(oldPath) && !string.Equals(oldPath, destPath, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(oldPath); }
                catch (Exception ex) { LogService.Error("Failed to delete old cover", ex); }
            }
        }

        File.Copy(sourcePath, destPath, overwrite: true);

        if (entry != null)
        {
            entry.CoverPath = destName;
            VnRepository.Update(entry);
        }
        return destName;
    }

    private static string? FindExecutable(string? folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return null;

        var exes = Directory.GetFiles(folderPath, "*.exe", SearchOption.TopDirectoryOnly);
        return exes.Length > 0 ? exes[0] : null;
    }
}
