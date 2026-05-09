using System.Text.Json;
using VnHub.Database;
using VnHub.Models;
using VnHub.Services;

namespace VnHub.Handlers;

public static class GroupHandler
{
    public static Task Handle(string action, JsonElement? payload)
    {
        switch (action)
        {
            case "getGroups":
                Bridge.SendToJs("receiveGroups", GroupRepository.GetAll());
                break;

            case "addGroup":
            {
                var g = Bridge.Deserialize<VnGroup>(payload);
                if (g == null) break;
                GroupRepository.Insert(g);
                Bridge.SendToJs("receiveGroups", GroupRepository.GetAll());
                LogService.Info($"Group added: {g.Name}");
                break;
            }

            case "updateGroup":
            {
                var g = Bridge.Deserialize<VnGroup>(payload);
                if (g == null) break;
                GroupRepository.Update(g);
                Bridge.SendToJs("receiveGroups", GroupRepository.GetAll());
                break;
            }

            case "deleteGroup":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null) break;
                GroupRepository.Delete(p.Id);
                Bridge.SendToJs("receiveGroups", GroupRepository.GetAll());
                Bridge.SendToJs("receiveLibrary", LibraryService.GetLibrary());
                LogService.Info($"Group deleted: {p.Id}");
                break;
            }

            case "setVnGroup":
            {
                var p = Bridge.Deserialize<Bridge.SetVnGroupPayload>(payload);
                if (p == null) break;
                GroupRepository.SetVnGroup(p.Id, p.GroupId);
                var entry = VnRepository.GetById(p.Id);
                Bridge.SendToJs("vnUpdated", entry);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
