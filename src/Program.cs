using VnHub.Database;
using VnHub.Services;

namespace VnHub;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        LogService.Initialize();
        AppDb.Initialize();
        var settings = SettingsService.Load();
        VndbService.ConfigureProxy(settings.ProxyAddress);
        IgdbService.Instance.ConfigureProxy(settings.ProxyAddress);
        AniListService.Instance.ConfigureProxy(settings.ProxyAddress);
        BackupService.BackupOnStartup();
        Application.Run(new MainWindow());
    }
}