using System.Text.Json;

namespace VnHub.Services;

internal static class MetadataJson
{
    public static string? StringOrNull(this JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null;

    public static List<string> NamedArray(this JsonElement el, string arrayProp, string nameProp)
    {
        var result = new List<string>();
        if (el.TryGetProperty(arrayProp, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (item.TryGetProperty(nameProp, out var n) && n.GetString() is { } s)
                    result.Add(s);
        return result;
    }

    public static List<string> StringArray(this JsonElement el, string arrayProp)
    {
        var result = new List<string>();
        if (el.TryGetProperty(arrayProp, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (item.GetString() is { } s)
                    result.Add(s);
        return result;
    }
}
