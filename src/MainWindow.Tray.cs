using VnHub.Database;
using VnHub.Services;

namespace VnHub;

public partial class MainWindow
{
    private NotifyIcon? _trayIcon;

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

    private void DisposeTrayIcon()
    {
        LauncherService.GameExited -= OnGameFinished;
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }
}
