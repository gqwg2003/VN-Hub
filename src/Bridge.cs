using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;
using VnHub.Handlers;
using VnHub.Services;

namespace VnHub;

public static class Bridge
{
    private static WebView2 _webView = null!;

    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, Func<string, JsonElement?, Task>> ActionHandlers = BuildActionHandlers();

    private static Dictionary<string, Func<string, JsonElement?, Task>> BuildActionHandlers()
    {
        var map = new Dictionary<string, Func<string, JsonElement?, Task>>(StringComparer.Ordinal);

        void Register(string[] actions, Func<string, JsonElement?, Task> handler)
        {
            foreach (var action in actions)
                map[action] = handler;
        }

        // Library (CRUD + list/search/tags)
        Register(new[]
        {
            "getLibrary", "addVn", "updateVn", "deleteVn",
            "toggleFavorite", "togglePin", "setStatus",
            "launchVn", "getRunningGames",
            "searchVn", "getTags"
        }, LibraryHandler.Handle);

        // Media (file/folder pickers, covers, icons, OS folder)
        Register(new[]
        {
            "pickFolder", "pickImage", "pickExe",
            "setCover", "extractIcon", "openFolder"
        }, MediaHandler.Handle);

        // Sessions / play stats / aggregate stats
        Register(new[] { "getSessions", "getPlayStats", "getStats" }, SessionHandler.Handle);

        // Scan
        Register(new[] { "scanFolder", "bulkAddScanned" }, ScanHandler.Handle);

        // Groups
        Register(new[] { "getGroups", "addGroup", "updateGroup", "deleteGroup", "setVnGroup" },
            GroupHandler.Handle);

        // Settings
        Register(new[]
        {
            "getSettings", "saveSettings",
            "openDbFolder", "openCoversFolder", "openLogsFolder", "clearLogs",
            "exportLibrary", "importLibrary",
            "setAutoStart",
            "getBackups", "backupNow", "restoreBackup",
            "exportCsv", "exportHtml"
        }, SettingsHandler.Handle);

        // VNDB
        Register(new[] { "fetchVndb" }, VndbHandler.Handle);

        // Utility
        map["openUrl"] = (_, payload) =>
        {
            var u = Deserialize<UrlPayload>(payload);
            if (u != null && Uri.TryCreate(u.Url, UriKind.Absolute, out var uri)
                && (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
            return Task.CompletedTask;
        };

        map["toggleFullscreen"] = (_, _) =>
        {
            InvokeOnUiThread(() =>
            {
                var form = Application.OpenForms.OfType<MainWindow>().FirstOrDefault();
                form?.ToggleFullscreen();
            });
            return Task.CompletedTask;
        };

        return map;
    }

    public static void Init(WebView2 webView)
    {
        _webView = webView;
        _webView.CoreWebView2.WebMessageReceived += async (_, args) =>
        {
            try
            {
                var raw = args.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(raw)) return;
                if (raw.Length > 1_000_000)
                {
                    LogService.Warn($"Bridge: oversized message rejected ({raw.Length} bytes)");
                    return;
                }
                var msg = JsonSerializer.Deserialize<BridgeMessage>(raw, JsonOpts);
                if (msg == null || string.IsNullOrEmpty(msg.Action)) return;

                LogService.Info($"Bridge: {msg.Action}");
                await HandleMessage(msg);
            }
            catch (Exception ex)
            {
                LogService.Error("Bridge message error", ex);
                SendToJs("onError", new { message = "An internal error occurred." });
            }
        };

        LauncherService.GameExited += vnId =>
        {
            try { _webView.Invoke(() => SendToJs("gameStopped", new { id = vnId })); }
            catch { /* webview may be disposed */ }
        };
    }

    private static async Task HandleMessage(BridgeMessage msg)
    {
        if (!ActionHandlers.TryGetValue(msg.Action, out var handler))
        {
            LogService.Warn($"Unknown bridge action: {msg.Action}");
            SendToJs("onError", new { message = $"Unknown action: {msg.Action}" });
            return;
        }

        try
        {
            await handler(msg.Action, msg.Payload);
        }
        catch (Exception ex)
        {
            LogService.Error($"Handler '{msg.Action}' failed", ex);
            SendToJs("onError", new { message = "Operation failed. See logs for details." });
        }
    }

    internal static void SendToJs(string callback, object? data)
    {
        if (_webView == null) return;

        var envelope = new { callback, data };
        var json = JsonSerializer.Serialize(envelope, JsonOpts);

        try
        {
            if (_webView.InvokeRequired)
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(json));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(json);
        }
        catch (ObjectDisposedException) { /* shutting down */ }
        catch (InvalidOperationException) { /* webview may not yet have a handle */ }
    }

    internal static void InvokeOnUiThread(Action action) => _webView.Invoke(action);

    internal static T? Deserialize<T>(JsonElement? element)
    {
        if (element == null) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(element.Value.GetRawText(), JsonOpts);
        }
        catch (JsonException ex)
        {
            LogService.Warn($"Bridge payload deserialization failed for {typeof(T).Name}: {ex.Message}");
            return default;
        }
    }

    // Message DTOs
    internal class BridgeMessage
    {
        public string Action { get; set; } = "";
        public JsonElement? Payload { get; set; }
    }

    internal class AddVnPayload
    {
        public string Title { get; set; } = "";
        public string? Path { get; set; }
        public string? ExePath { get; set; }
        public bool SkipVndb { get; set; }
    }

    internal class IdPayload
    {
        public string Id { get; set; } = "";
    }

    internal class DaysPayload
    {
        public int Days { get; set; }
    }

    internal class SetStatusPayload
    {
        public string Id { get; set; } = "";
        public int Status { get; set; }
    }

    internal class SetCoverPayload
    {
        public string Id { get; set; } = "";
        public string SourcePath { get; set; } = "";
    }

    internal class ExtractIconPayload
    {
        public string Id { get; set; } = "";
        public string ExePath { get; set; } = "";
    }

    internal class SearchPayload
    {
        public string Query { get; set; } = "";
    }

    internal class LibraryQueryPayload
    {
        public string? Tab { get; set; }
        public string? MarkedSubTab { get; set; }
        public int Status { get; set; } = -1;
        public string? GroupId { get; set; }
        public string? Tag { get; set; }
        public string? Search { get; set; }
        public string SortBy { get; set; } = "title";
        public string SortDir { get; set; } = "asc";
    }

    internal class SetVnGroupPayload
    {
        public string Id { get; set; } = "";
        public string? GroupId { get; set; }
    }

    internal class AutoStartPayload
    {
        public bool Enabled { get; set; }
    }

    internal class FileNamePayload
    {
        public string FileName { get; set; } = "";
    }

    internal class UrlPayload
    {
        public string Url { get; set; } = "";
    }

    internal class BulkAddPayload
    {
        public List<BulkAddItem>? Items { get; set; }
    }

    internal class BulkAddItem
    {
        public string Title { get; set; } = "";
        public string ExePath { get; set; } = "";
    }
}
