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
        AppServices.Initialize();
        var settings = SettingsService.Load();
        AppServices.Metadata.ConfigureProxies(settings.ProxyAddress);
        BackupService.BackupOnStartup();
        Application.Run(new MainWindow());
    }
}