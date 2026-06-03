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

            case "getSmartGroupLibrary":
            {
                var p = Bridge.Deserialize<Bridge.IdPayload>(payload);
                if (p == null) break;
                Bridge.SendToJs("receiveLibrary", GetSmartGroupMembers(p.Id));
                break;
            }
        }

        return Task.CompletedTask;
    }

    private static List<VnEntry> GetSmartGroupMembers(string groupId)
    {
        var group = GroupRepository.GetById(groupId);
        if (group == null || string.IsNullOrWhiteSpace(group.Filter))
            return new List<VnEntry>();

        GroupFilter? filter;
        try
        {
            filter = JsonSerializer.Deserialize<GroupFilter>(group.Filter, Bridge.JsonOpts);
        }
        catch (JsonException ex)
        {
            LogService.Warn($"Smart group '{group.Name}' has invalid filter: {ex.Message}");
            return new List<VnEntry>();
        }
        if (filter == null) return new List<VnEntry>();

        return LibraryService.GetLibrary().Where(e => Matches(e, filter)).ToList();
    }

    private static bool Matches(VnEntry entry, GroupFilter filter)
    {
        if (filter.Status.HasValue && (int)entry.Status != filter.Status.Value)
            return false;

        if (filter.IsFavorite.HasValue && entry.IsFavorite != filter.IsFavorite.Value)
            return false;

        if (filter.MinRating.HasValue)
        {
            var rating = entry.UserRating ?? entry.Rating;
            if (!rating.HasValue || rating.Value < filter.MinRating.Value)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(filter.Tag) && !HasTag(entry, filter.Tag))
            return false;

        return true;
    }

    private static bool HasTag(VnEntry entry, string tag)
    {
        if (string.IsNullOrWhiteSpace(entry.Tags))
            return false;
        try
        {
            var tags = JsonSerializer.Deserialize<List<string>>(entry.Tags);
            if (tags == null) return false;
            return tags.Any(t => string.Equals(t?.Trim(), tag.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
