using System.Text.Json;
using VnHub.Common;
using VnHub.Database;
using VnHub.Services;

namespace VnHub.Handlers;

public static class SessionHandler
{
    public static async Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "getStats":
            {
                var stats = await Task.Run(() => StatsService.Compute(LibraryService.GetLibrary()));
                Bridge.SendToJs("receiveStats", stats);
                break;
            }

            case "getSessions":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null || !Validation.IsValidVnId(p.Id)) break;
                var sessions = await Task.Run(() => SessionRepository.GetByVnId(p.Id));
                Bridge.SendToJs("receiveSessions", new { id = p.Id, sessions });
                break;
            }

            case "getPlayStats":
            {
                var p = Bridge.Deserialize<Bridge.DaysPayload>(payload);
                int days = p?.Days ?? 30;
                if (days < 1) days = 1;
                if (days > 3650) days = 3650;
                var data = await Task.Run(() => SessionRepository.GetStatsByDays(days));
                Bridge.SendToJs("receivePlayStats", new { days, data });
                break;
            }
        }
    }
}
