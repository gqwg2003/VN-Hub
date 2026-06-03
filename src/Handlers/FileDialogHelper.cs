namespace VnHub.Handlers;

internal static class FileDialogHelper
{
    public const string ImageFilter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All files|*.*";
    public const string ExeFilter = "Executables|*.exe|All files|*.*";
    public const string FontFilter = "Fonts|*.ttf;*.otf;*.woff;*.woff2|All files|*.*";
    public const string JsonSaveFilter = "JSON|*.json";
    public const string JsonOpenFilter = "JSON|*.json|All files|*.*";

    public static void PickFile(string filter, Action<string> onPicked)
    {
        Bridge.InvokeOnUiThread(() =>
        {
            using var dialog = new OpenFileDialog { Filter = filter };
            if (dialog.ShowDialog() == DialogResult.OK)
                onPicked(dialog.FileName);
        });
    }

    public static void SaveFile(string filter, string defaultName, Action<string> onPicked)
    {
        Bridge.InvokeOnUiThread(() =>
        {
            using var dialog = new SaveFileDialog { Filter = filter, FileName = defaultName };
            if (dialog.ShowDialog() == DialogResult.OK)
                onPicked(dialog.FileName);
        });
    }

    public static void PickFolder(string? initialDir, Action<string> onPicked)
    {
        Bridge.InvokeOnUiThread(() =>
        {
            using var dialog = new FolderBrowserDialog();
            if (!string.IsNullOrEmpty(initialDir))
                dialog.InitialDirectory = initialDir;
            if (dialog.ShowDialog() == DialogResult.OK)
                onPicked(dialog.SelectedPath);
        });
    }
}
