using VnHub.Services;

namespace VnHub;

public partial class MainWindow
{
    private bool _isFullscreen;
    private FormBorderStyle _savedBorderStyle;
    private FormWindowState _savedWindowState;
    private Rectangle _savedBounds;

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
            WindowState = FormWindowState.Minimized;
            if (s.MinimizeToTray)
            {
                Hide();
                if (_trayIcon != null) _trayIcon.Visible = true;
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
