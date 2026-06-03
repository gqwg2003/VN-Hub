using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using VnHub.Common;

namespace VnHub;

internal static class WebViewBootstrapper
{
    public static async Task ConfigureAsync(WebView2 webView)
    {
        var env = await CoreWebView2Environment.CreateAsync(
            userDataFolder: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VnHub", "WebView2Data"));

        await webView.EnsureCoreWebView2Async(env);

        var core = webView.CoreWebView2;

        Map(core, "vnhub.local", Path.Combine(AppContext.BaseDirectory, "src", "UI"));
        Map(core, "covers.vnhub.local", AppPaths.EnsureCoversDir());
        Map(core, "fonts.vnhub.local", AppPaths.EnsureFontsDir());
        Map(core, "bg.vnhub.local", AppPaths.EnsureBackgroundsDir());

        Bridge.Init(webView);

        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Navigate("https://vnhub.local/index.html");
    }

    private static void Map(CoreWebView2 core, string host, string folder)
        => core.SetVirtualHostNameToFolderMapping(
            host, folder, CoreWebView2HostResourceAccessKind.Allow);
}
