using Microsoft.Web.WebView2.WinForms;
using VnHub.Services;

namespace VnHub;

public partial class MainWindow : Form
{
    private readonly WebView2 _webView;
    private bool _forceClose;

    public MainWindow()
    {
        Text = "VN-Hub";
        Width = 1280;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 13, 26); // --bg-base

        var iconPath = Path.Combine(AppContext.BaseDirectory, "src", "UI", "Assets", "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        InitTrayIcon();

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(_webView);

        Load += async (_, _) =>
        {
            RestoreWindowBounds();
            await WebViewBootstrapper.ConfigureAsync(_webView);
        };

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                var settings = SettingsService.Load();
                if (settings.MinimizeToTray)
                {
                    Hide();
                    if (_trayIcon != null) _trayIcon.Visible = true;
                }
            }
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_forceClose && e.CloseReason == CloseReason.UserClosing)
        {
            var settings = SettingsService.Load();
            if (settings.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                if (_trayIcon != null) _trayIcon.Visible = true;
                return;
            }
        }

        SaveWindowBounds();
        LauncherService.SaveAllPlayTime();
        LogService.Info("=== VN-Hub shutting down ===");

        DisposeTrayIcon();
        base.OnFormClosing(e);
    }
}
