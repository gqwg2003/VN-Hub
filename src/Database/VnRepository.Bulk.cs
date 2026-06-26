using System.Text.Json;
using Microsoft.Data.Sqlite;
using VnHub.Models;

namespace VnHub.Database;

public static partial class VnRepository
{
    public static int BulkImport(List<VnEntry> entries)
    {
        using var conn = AppDb.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            var existing = new HashSet<string>(StringComparer.Ordinal);
            const int batchSize = 500;
            for (int start = 0; start < entries.Count; start += batchSize)
            {
                var chunk = entries.Skip(start).Take(batchSize).ToList();
                using var checkCmd = conn.CreateCommand();
                var placeholders = new string[chunk.Count];
                for (int i = 0; i < chunk.Count; i++)
                {
                    var param = "@id" + i;
                    placeholders[i] = param;
                    checkCmd.Parameters.AddWithValue(param, chunk[i].Id);
                }
                checkCmd.CommandText = $"SELECT id FROM vn_entries WHERE id IN ({string.Join(", ", placeholders)})";
                using var reader = checkCmd.ExecuteReader();
                while (reader.Read())
                    existing.Add(reader.GetString(0));
            }

            int count = 0;
            foreach (var entry in entries)
            {
                if (existing.Contains(entry.Id))
                    continue;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO vn_entries (id, title, cover_path, exe_path, date_added, status, is_favorite, is_pinned, notes, tags, play_time_seconds, last_launched_at, group_id, vndb_id, description, rating, user_rating, completed_at, story_rating, art_rating, music_rating, character_rating, links, reading_progress, skip_vndb)
                    VALUES (@id, @title, @cover, @exe, @date, @status, @fav, @pin, @notes, @tags, @playtime, @lastlaunched, @groupid, @vndbid, @desc, @rating, @userrating, @completedat, @storyrating, @artrating, @musicrating, @charrating, @links, @readprogress, @skipvndb)
                    """;
                BindEntry(cmd, entry);
                cmd.ExecuteNonQuery();
                count++;
            }
            transaction.Commit();
            return count;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static void BulkDelete(IReadOnlyList<string> ids)
    {
        if (ids.Count == 0) return;
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        var inClause = BuildIdInClause(cmd, ids);
        cmd.CommandText = $"DELETE FROM vn_entries WHERE id IN ({inClause})";
        cmd.ExecuteNonQuery();
    }

    public static void BulkSetStatus(IReadOnlyList<string> ids, VnStatus status)
    {
        if (ids.Count == 0) return;
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        var inClause = BuildIdInClause(cmd, ids);
        if (status == VnStatus.Completed)
        {
            cmd.CommandText = $"UPDATE vn_entries SET status = @s, completed_at = COALESCE(completed_at, @now) WHERE id IN ({inClause})";
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        }
        else
        {
            cmd.CommandText = $"UPDATE vn_entries SET status = @s WHERE id IN ({inClause})";
        }
        cmd.Parameters.AddWithValue("@s", (int)status);
        cmd.ExecuteNonQuery();
    }

    public static void BulkSetFavorite(IReadOnlyList<string> ids, bool value)
    {
        if (ids.Count == 0) return;
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        var inClause = BuildIdInClause(cmd, ids);
        cmd.CommandText = $"UPDATE vn_entries SET is_favorite = @v WHERE id IN ({inClause})";
        cmd.Parameters.AddWithValue("@v", value ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public static void BulkSetPin(IReadOnlyList<string> ids, bool value)
    {
        if (ids.Count == 0) return;
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        var inClause = BuildIdInClause(cmd, ids);
        cmd.CommandText = $"UPDATE vn_entries SET is_pinned = @v WHERE id IN ({inClause})";
        cmd.Parameters.AddWithValue("@v", value ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public static void BulkSetGroup(IReadOnlyList<string> ids, string? groupId)
    {
        if (ids.Count == 0) return;
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        var inClause = BuildIdInClause(cmd, ids);
        cmd.CommandText = $"UPDATE vn_entries SET group_id = @g WHERE id IN ({inClause})";
        cmd.Parameters.AddWithValue("@g", (object?)groupId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public static void BulkAddTag(IReadOnlyList<string> ids, string tag)
    {
        if (ids.Count == 0 || string.IsNullOrWhiteSpace(tag)) return;
        var trimmed = tag.Trim();
        UpdateTags(ids, current =>
        {
            if (current.Any(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)))
                return current;
            current.Add(trimmed);
            return current;
        });
    }

    public static void BulkRemoveTag(IReadOnlyList<string> ids, string tag)
    {
        if (ids.Count == 0 || string.IsNullOrWhiteSpace(tag)) return;
        var trimmed = tag.Trim();
        UpdateTags(ids, current =>
            current.Where(x => !string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    private static void UpdateTags(IReadOnlyList<string> ids, Func<List<string>, List<string>> transform)
    {
        using var conn = AppDb.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            using var selectCmd = conn.CreateCommand();
            selectCmd.Transaction = transaction;
            var inClause = BuildIdInClause(selectCmd, ids);
            selectCmd.CommandText = $"SELECT id, tags FROM vn_entries WHERE id IN ({inClause})";

            var current = new List<(string Id, string Tags)>();
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetString(0);
                    var tags = reader.IsDBNull(1) ? "[]" : reader.GetString(1);
                    current.Add((id, tags));
                }
            }

            foreach (var (id, tagsJson) in current)
            {
                var list = ParseTags(tagsJson);
                var updated = transform(list);
                var json = JsonSerializer.Serialize(updated);

                using var updateCmd = conn.CreateCommand();
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = "UPDATE vn_entries SET tags = @tags WHERE id = @id";
                updateCmd.Parameters.AddWithValue("@tags", json);
                updateCmd.Parameters.AddWithValue("@id", id);
                updateCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static List<string> ParseTags(string tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson)) return new List<string>();
        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(tagsJson);
            if (parsed == null) return new List<string>();
            return parsed.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
