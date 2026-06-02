using Microsoft.Web.WebView2.WinForms;
using VnHub.Common;
using VnHub.Database;
using VnHub.Services;

namespace VnHub;

public class MainWindow : Form
{
    private readonly WebView2 _webView;
    private NotifyIcon? _trayIcon;
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
            await InitWebView();
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

    private void RestoreWindowBounds()
    {
        var s = SettingsService.Load();
        if (s.WindowWidth.HasValue && s.WindowHeight.HasValue)
        {
            StartPosition = FormStartPosition.Manual;
            var bounds = new Rectangle(
                s.WindowX ?? 0, s.WindowY ?? 0,
                s.WindowWidth.Value, s.WindowHeight.Value);

            if (Screen.AllScreens.Any(scr => scr.WorkingArea.IntersectsWith(bounds)))
            {
                Location = bounds.Location;
                Size = bounds.Size;
            }
            if (s.WindowMaximized)
                WindowState = FormWindowState.Maximized;
        }
        if (s.StartMinimized)
        {
            if (s.MinimizeToTray)
            {
                WindowState = FormWindowState.Minimized;
                Hide();
                if (_trayIcon != null) _trayIcon.Visible = true;
            }
            else
            {
                WindowState = FormWindowState.Minimized;
            }
        }
    }

    private void SaveWindowBounds()
    {
        var s = SettingsService.Load();
        s.WindowMaximized = WindowState == FormWindowState.Maximized;
        var restore = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        s.WindowX = restore.X;
        s.WindowY = restore.Y;
        s.WindowWidth = restore.Width;
        s.WindowHeight = restore.Height;
        SettingsService.Save(s);
    }

    private void InitTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => ShowFromTray());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => { _forceClose = true; Close(); });

        _trayIcon = new NotifyIcon
        {
            Icon = Icon,
            Text = "VN-Hub",
            ContextMenuStrip = menu,
            Visible = false
        };
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();

        LauncherService.GameExited += OnGameFinished;
    }

    private void OnGameFinished(string vnId)
    {
        // _trayIcon is set to null after Dispose() in OnFormClosing, so this also covers the disposed case.
        if (_trayIcon == null) return;
        try
        {
            var entry = VnRepository.GetById(vnId);
            if (entry != null && _trayIcon != null)
            {
                _trayIcon.Visible = true;
                _trayIcon.ShowBalloonTip(
                    5000,
                    "VN-Hub",
                    $"{entry.Title} — session finished",
                    ToolTipIcon.Info);
            }
        }
        catch { /* ignore if disposed */ }
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        if (_trayIcon != null) _trayIcon.Visible = false;
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

        LauncherService.GameExited -= OnGameFinished;

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
        base.OnFormClosing(e);
    }

    private async Task InitWebView()
    {
        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
            userDataFolder: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VnHub", "WebView2Data"));

        await _webView.EnsureCoreWebView2Async(env);

        var uiPath = Path.Combine(AppContext.BaseDirectory, "src", "UI");
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "vnhub.local", uiPath,
            Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

        var coversPath = AppPaths.EnsureCoversDir();
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "covers.vnhub.local", coversPath,
            Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

        var fontsPath = AppPaths.EnsureFontsDir();
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "fonts.vnhub.local", fontsPath,
            Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

        var backgroundsPath = AppPaths.EnsureBackgroundsDir();
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "bg.vnhub.local", backgroundsPath,
            Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

        Bridge.Init(_webView);

        _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

        _webView.CoreWebView2.Navigate("https://vnhub.local/index.html");
    }

    private bool _isFullscreen;
    private FormBorderStyle _savedBorderStyle;
    private FormWindowState _savedWindowState;
    private Rectangle _savedBounds;

    public void ToggleFullscreen()
    {
        if (_isFullscreen)
        {
            FormBorderStyle = _savedBorderStyle;
            WindowState = _savedWindowState;
            if (_savedWindowState == FormWindowState.Normal)
                Bounds = _savedBounds;
        }
        else
        {
            _savedBorderStyle = FormBorderStyle;
            _savedWindowState = WindowState;
            _savedBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }
        _isFullscreen = !_isFullscreen;
    }
}
