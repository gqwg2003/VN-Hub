using System.Text.Json;
using VnHub.Common;
using VnHub.Database;
using VnHub.Services;

namespace VnHub.Handlers;

public static class MediaHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "pickFolder":
            {
                var settings = SettingsService.Load();
                FileDialogHelper.PickFolder(settings.DefaultFolder,
                    path => Bridge.SendToJs("folderPicked", new { path }));
                break;
            }

            case "pickImage":
            {
                FileDialogHelper.PickFile(FileDialogHelper.ImageFilter,
                    path => Bridge.SendToJs("imagePicked", new { path }));
                break;
            }

            case "pickExe":
            {
                FileDialogHelper.PickFile(FileDialogHelper.ExeFilter, path =>
                {
                    var title = IconService.GetTitleFromExe(path);
                    Bridge.SendToJs("exePicked", new { path, suggestedTitle = title });
                });
                break;
            }

            case "setCover":
            {
                var p = Bridge.Deserialize<Bridge.SetCoverPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var coverName = await Task.Run(() => LibraryService.SetCover(p.Id, p.SourcePath));
                if (coverName != null)
                {
                    var entry = VnRepository.GetById(p.Id);
                    Bridge.SendToJs("vnUpdated", entry);
                }
                break;
            }

            case "extractIcon":
            {
                var p = Bridge.Deserialize<Bridge.ExtractIconPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var iconName = await Task.Run(() => IconService.ExtractIconFromExe(p.ExePath, p.Id));
                if (iconName != null)
                {
                    var entry = VnRepository.GetById(p.Id);
                    if (entry != null && string.IsNullOrEmpty(entry.CoverPath))
                    {
                        entry.CoverPath = iconName;
                        VnRepository.Update(entry);
                        Bridge.SendToJs("vnUpdated", entry);
                    }
                }
                break;
            }

            case "openFolder":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var entry = VnRepository.GetById(p.Id);
                var exe = entry?.ExePath;
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) break;
                if (!PathGuard.IsSafeArg(exe))
                {
                    LogService.Warn($"openFolder: rejected path with unsafe characters for {p.Id}");
                    break;
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{exe}\"",
                    UseShellExecute = false
                });
                break;
            }
        }
    }
}
