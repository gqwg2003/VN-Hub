using System.Text.Json;

namespace VnHub.Common;

public static class JsonHelpers
{
    public static readonly JsonSerializerOptions CommonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static readonly JsonSerializerOptions IndentedOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static T? DeserializeOrDefault<T>(string? json, T? defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(json)) return defaultValue;
        try { return JsonSerializer.Deserialize<T>(json, CommonOpts); }
        catch { return defaultValue; }
    }

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, CommonOpts);
}
