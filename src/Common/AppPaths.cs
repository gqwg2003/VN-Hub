namespace VnHub.Common;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VnHub");

    public static string CoversDir { get; } = Path.Combine(Root, "covers");
    public static string LogsDir { get; } = Path.Combine(Root, "logs");

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
}
