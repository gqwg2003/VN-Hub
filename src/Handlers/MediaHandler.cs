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
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new FolderBrowserDialog();
                    var settings = SettingsService.Load();
                    if (!string.IsNullOrEmpty(settings.DefaultFolder))
                        dialog.InitialDirectory = settings.DefaultFolder;

                    if (dialog.ShowDialog() == DialogResult.OK)
                        Bridge.SendToJs("folderPicked", new { path = dialog.SelectedPath });
                });
                break;
            }

            case "pickImage":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All files|*.*"
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                        Bridge.SendToJs("imagePicked", new { path = dialog.FileName });
                });
                break;
            }

            case "pickExe":
            {
                Bridge.InvokeOnUiThread(() =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "Executables|*.exe|All files|*.*"
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        var title = IconService.GetTitleFromExe(dialog.FileName);
                        Bridge.SendToJs("exePicked", new { path = dialog.FileName, suggestedTitle = title });
                    }
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
