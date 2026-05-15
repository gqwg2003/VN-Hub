namespace VnHub.Common;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VnHub");

    public static string CoversDir { get; } = Path.Combine(Root, "covers");
    public static string LogsDir { get; } = Path.Combine(Root, "logs");
    public static string FontsDir { get; } = Path.Combine(Root, "fonts");
    public static string BackgroundsDir { get; } = Path.Combine(Root, "backgrounds");

    public static string EnsureRoot()
    {
        Directory.CreateDirectory(Root);
        return Root;
    }

    public static string EnsureCoversDir()
    {
        Directory.CreateDirectory(CoversDir);
        return CoversDir;
    }

    public static string EnsureLogsDir()
    {
        Directory.CreateDirectory(LogsDir);
        return LogsDir;
    }

    public static string EnsureFontsDir()
    {
        Directory.CreateDirectory(FontsDir);
        return FontsDir;
    }

    public static string EnsureBackgroundsDir()
    {
        Directory.CreateDirectory(BackgroundsDir);
        return BackgroundsDir;
    }
}
