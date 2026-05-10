namespace VnHub.Common;
public static class PathGuard
{
    public static string? EnsureWithin(string root, string candidate)
    {
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(candidate))
            return null;

        try
        {
            var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var fullCandidate = Path.GetFullPath(candidate);
            if (fullCandidate.Equals(rootFull.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                return fullCandidate;
            if (fullCandidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                return fullCandidate;
        }
        catch
        {
            // ignore: invalid path characters etc.
        }
        return null;
    }

    private static readonly char[] UnsafeArgChars = { '"', '\0', '\r', '\n' };

    public static bool IsSafeArg(string? value)
    {
        return !string.IsNullOrEmpty(value) && value.IndexOfAny(UnsafeArgChars) < 0;
    }

    public static bool IsSafeFileName(string fileName, params string[] allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        if (fileName.Contains("..", StringComparison.Ordinal)) return false;
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
        if (Path.GetFileName(fileName) != fileName) return false;
        if (allowedExtensions.Length > 0)
        {
            var ext = Path.GetExtension(fileName);
            foreach (var allowed in allowedExtensions)
            {
                if (string.Equals(ext, allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        return true;
    }
}
