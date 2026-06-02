using Microsoft.Data.Sqlite;
using VnHub.Models;

namespace VnHub.Database;

public static class VnRepository
{
    public static List<VnEntry> GetAll()
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM vn_entries ORDER BY is_pinned DESC, date_added DESC";
        return ReadEntries(cmd);
    }

    public static List<VnEntry> GetByStatus(VnStatus status)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM vn_entries WHERE status = @s ORDER BY is_pinned DESC, date_added DESC";
        cmd.Parameters.AddWithValue("@s", (int)status);
        return ReadEntries(cmd);
    }

    public static List<VnEntry> GetFavorites()
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM vn_entries WHERE is_favorite = 1 ORDER BY is_pinned DESC, date_added DESC";
        return ReadEntries(cmd);
    }

    public static List<VnEntry> GetPinned()
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM vn_entries WHERE is_pinned = 1 ORDER BY date_added DESC";
        return ReadEntries(cmd);
    }

    public static VnEntry? GetById(string id)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM vn_entries WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        var list = ReadEntries(cmd);
        return list.Count > 0 ? list[0] : null;
    }

    public static void Insert(VnEntry entry)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO vn_entries (id, title, cover_path, exe_path, date_added, status, is_favorite, is_pinned, notes, tags, play_time_seconds, last_launched_at, group_id, vndb_id, description, rating, user_rating, completed_at, story_rating, art_rating, music_rating, character_rating, links, reading_progress, skip_vndb)
            VALUES (@id, @title, @cover, @exe, @date, @status, @fav, @pin, @notes, @tags, @playtime, @lastlaunched, @groupid, @vndbid, @desc, @rating, @userrating, @completedat, @storyrating, @artrating, @musicrating, @charrating, @links, @readprogress, @skipvndb)
            """;
        BindEntry(cmd, entry);
        cmd.ExecuteNonQuery();
    }

    public static void Update(VnEntry entry)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE vn_entries SET
                title = @title, cover_path = @cover, exe_path = @exe,
                status = @status, is_favorite = @fav, is_pinned = @pin,
                notes = @notes, tags = @tags,
                play_time_seconds = @playtime, last_launched_at = @lastlaunched,
                group_id = @groupid, vndb_id = @vndbid,
                description = @desc, rating = @rating, user_rating = @userrating,
                completed_at = @completedat, story_rating = @storyrating,
                art_rating = @artrating, music_rating = @musicrating,
                character_rating = @charrating, links = @links,
                reading_progress = @readprogress, skip_vndb = @skipvndb
            WHERE id = @id
            """;
        BindEntry(cmd, entry);
        cmd.ExecuteNonQuery();
    }

    public static void Delete(string id)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM vn_entries WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static void ToggleFavorite(string id)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE vn_entries SET is_favorite = 1 - is_favorite WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static void TogglePin(string id)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE vn_entries SET is_pinned = 1 - is_pinned WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static void SetStatus(string id, VnStatus status)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        if (status == VnStatus.Completed)
        {
            cmd.CommandText = "UPDATE vn_entries SET status = @s, completed_at = COALESCE(completed_at, @now) WHERE id = @id";
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        }
        else
        {
            cmd.CommandText = "UPDATE vn_entries SET status = @s WHERE id = @id";
        }
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@s", (int)status);
        cmd.ExecuteNonQuery();
    }

    public static List<VnEntry> Search(string query)
    {
        var escaped = EscapeFts(query);
        if (string.IsNullOrEmpty(escaped))
            return GetAll();

        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT e.* FROM vn_entries e
            JOIN vn_fts f ON e.rowid = f.rowid
            WHERE vn_fts MATCH @q
            ORDER BY e.is_pinned DESC, rank
            """;
        cmd.Parameters.AddWithValue("@q", $"{{title notes tags description}}: {escaped}");
        return ReadEntries(cmd);
    }

    public static List<string> GetAllTitles()
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT LOWER(title) FROM vn_entries";
        var list = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0)) continue;
            var title = reader.GetString(0);
            if (!string.IsNullOrEmpty(title))
                list.Add(title);
        }
        return list;
    }

    private static string EscapeFts(string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var escaped = new List<string>();
        foreach (var t in terms)
        {
            var clean = System.Text.RegularExpressions.Regex.Replace(t, @"[""(){}+\-^:*~]", "");
            if (string.IsNullOrWhiteSpace(clean)) continue;
            escaped.Add($"\"{clean}\"*");
        }
        return escaped.Count > 0 ? string.Join(" ", escaped) : "";
    }

    private static void BindEntry(SqliteCommand cmd, VnEntry e)
    {
        cmd.Parameters.AddWithValue("@id", e.Id);
        cmd.Parameters.AddWithValue("@title", e.Title);
        cmd.Parameters.AddWithValue("@cover", (object?)e.CoverPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@exe", (object?)e.ExePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@date", e.DateAdded);
        cmd.Parameters.AddWithValue("@status", (int)e.Status);
        cmd.Parameters.AddWithValue("@fav", e.IsFavorite ? 1 : 0);
        cmd.Parameters.AddWithValue("@pin", e.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@notes", (object?)e.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tags", e.Tags);
        cmd.Parameters.AddWithValue("@playtime", e.PlayTimeSeconds);
        cmd.Parameters.AddWithValue("@lastlaunched", (object?)e.LastLaunchedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@groupid", (object?)e.GroupId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@vndbid", (object?)e.VndbId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@desc", (object?)e.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@rating", (object?)e.Rating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@userrating", (object?)e.UserRating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@completedat", (object?)e.CompletedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@storyrating", (object?)e.StoryRating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@artrating", (object?)e.ArtRating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@musicrating", (object?)e.MusicRating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@charrating", (object?)e.CharacterRating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@links", e.Links);
        cmd.Parameters.AddWithValue("@readprogress", e.ReadingProgress);
        cmd.Parameters.AddWithValue("@skipvndb", e.SkipVndb ? 1 : 0);
    }

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

    public static void AddPlayTime(string id, long seconds)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE vn_entries SET play_time_seconds = play_time_seconds + @s, last_launched_at = @now WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@s", seconds);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    private static readonly Dictionary<string, string> SortColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = "title COLLATE NOCASE",
        ["dateAdded"] = "date_added",
        ["status"] = "status",
        ["playTime"] = "play_time_seconds",
        ["lastLaunched"] = "last_launched_at",
        ["rating"] = "rating",
        ["userRating"] = "user_rating"
    };

    public static List<VnEntry> GetFiltered(
        string? tab,
        string? markedSubTab,
        int status,
        string? groupId,
        string? tag,
        string? search,
        string sortBy,
        string sortDir)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();

        var sortCol = SortColumnMap.TryGetValue(sortBy ?? "title", out var col) ? col : "title COLLATE NOCASE";
        var dir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

        var sql = new System.Text.StringBuilder();
        var where = new List<string>();
        bool hasFts = false;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = EscapeFts(search);
            if (!string.IsNullOrEmpty(escaped))
            {
                hasFts = true;
                sql.Append("SELECT e.* FROM vn_entries e JOIN vn_fts f ON e.rowid = f.rowid ");
                where.Add("vn_fts MATCH @ftsq");
                cmd.Parameters.AddWithValue("@ftsq", $"{{title notes tags description}}: {escaped}");
            }
        }

        if (!hasFts)
            sql.Append("SELECT e.* FROM vn_entries e ");

        switch (tab)
        {
            case "marked":
                switch (markedSubTab)
                {
                    case "favorites": where.Add("e.is_favorite = 1"); break;
                    case "priority": where.Add("e.is_pinned = 1"); break;
                    case "completed": where.Add("e.status = 1"); break;
                    default: where.Add("(e.is_favorite = 1 OR e.is_pinned = 1)"); break;
                }
                break;
            case "reading":
                where.Add("e.status = 0");
                break;
        }

        if (status >= 0)
        {
            where.Add("e.status = @status");
            cmd.Parameters.AddWithValue("@status", status);
        }

        if (!string.IsNullOrEmpty(groupId))
        {
            where.Add("e.group_id = @groupId");
            cmd.Parameters.AddWithValue("@groupId", groupId);
        }

        if (!string.IsNullOrEmpty(tag))
        {
            where.Add("EXISTS (SELECT 1 FROM json_each(e.tags) WHERE lower(trim(value)) = lower(@tag))");
            cmd.Parameters.AddWithValue("@tag", tag);
        }

        if (where.Count > 0)
        {
            sql.Append("WHERE ");
            sql.Append(string.Join(" AND ", where));
            sql.Append(' ');
        }

        sql.Append($"ORDER BY e.is_pinned DESC, ");
        if (hasFts && string.Equals(sortBy, "relevance", StringComparison.OrdinalIgnoreCase))
            sql.Append("rank");
        else
            sql.Append($"e.{sortCol} {dir}");

        cmd.CommandText = sql.ToString();
        return ReadEntries(cmd);
    }

    public static List<TagCount> GetTagCounts()
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT trim(value) AS name, COUNT(*) AS cnt
            FROM vn_entries, json_each(vn_entries.tags)
            WHERE json_valid(vn_entries.tags)
              AND typeof(value) = 'text'
              AND trim(value) <> ''
            GROUP BY lower(trim(value))
            ORDER BY cnt DESC, name COLLATE NOCASE ASC
            """;
        var list = new List<TagCount>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TagCount
            {
                Name = reader.GetString(0),
                Count = reader.GetInt32(1)
            });
        }
        return list;
    }

    public class TagCount
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    private static List<VnEntry> ReadEntries(SqliteCommand cmd)
    {
        var list = new List<VnEntry>();
        using var reader = cmd.ExecuteReader();

        int oId = reader.GetOrdinal("id");
        int oTitle = reader.GetOrdinal("title");
        int oCover = reader.GetOrdinal("cover_path");
        int oExe = reader.GetOrdinal("exe_path");
        int oDate = reader.GetOrdinal("date_added");
        int oStatus = reader.GetOrdinal("status");
        int oFav = reader.GetOrdinal("is_favorite");
        int oPin = reader.GetOrdinal("is_pinned");
        int oNotes = reader.GetOrdinal("notes");
        int oTags = reader.GetOrdinal("tags");
        int oPlaytime = reader.GetOrdinal("play_time_seconds");
        int oLastLaunched = reader.GetOrdinal("last_launched_at");
        int oGroupId = reader.GetOrdinal("group_id");
        int oVndbId = reader.GetOrdinal("vndb_id");
        int oDesc = reader.GetOrdinal("description");
        int oRating = reader.GetOrdinal("rating");
        int oUserRating = reader.GetOrdinal("user_rating");
        int oCompletedAt = reader.GetOrdinal("completed_at");
        int oStoryRating = reader.GetOrdinal("story_rating");
        int oArtRating = reader.GetOrdinal("art_rating");
        int oMusicRating = reader.GetOrdinal("music_rating");
        int oCharRating = reader.GetOrdinal("character_rating");
        int oLinks = reader.GetOrdinal("links");
        int oReadProgress = reader.GetOrdinal("reading_progress");
        int oSkipVndb = reader.GetOrdinal("skip_vndb");

        while (reader.Read())
        {
            list.Add(new VnEntry
            {
                Id = reader.GetString(oId),
                Title = reader.GetString(oTitle),
                CoverPath = reader.IsDBNull(oCover) ? null : reader.GetString(oCover),
                ExePath = reader.IsDBNull(oExe) ? null : reader.GetString(oExe),
                DateAdded = reader.IsDBNull(oDate) ? "" : reader.GetString(oDate),
                Status = (VnStatus)reader.GetInt32(oStatus),
                IsFavorite = reader.GetInt32(oFav) == 1,
                IsPinned = reader.GetInt32(oPin) == 1,
                Notes = reader.IsDBNull(oNotes) ? null : reader.GetString(oNotes),
                Tags = reader.IsDBNull(oTags) ? "[]" : reader.GetString(oTags),
                PlayTimeSeconds = reader.IsDBNull(oPlaytime) ? 0 : reader.GetInt64(oPlaytime),
                LastLaunchedAt = reader.IsDBNull(oLastLaunched) ? null : reader.GetString(oLastLaunched),
                GroupId = reader.IsDBNull(oGroupId) ? null : reader.GetString(oGroupId),
                VndbId = reader.IsDBNull(oVndbId) ? null : reader.GetString(oVndbId),
                Description = reader.IsDBNull(oDesc) ? null : reader.GetString(oDesc),
                Rating = reader.IsDBNull(oRating) ? null : reader.GetDouble(oRating),
                UserRating = reader.IsDBNull(oUserRating) ? null : reader.GetDouble(oUserRating),
                CompletedAt = reader.IsDBNull(oCompletedAt) ? null : reader.GetString(oCompletedAt),
                StoryRating = reader.IsDBNull(oStoryRating) ? null : reader.GetDouble(oStoryRating),
                ArtRating = reader.IsDBNull(oArtRating) ? null : reader.GetDouble(oArtRating),
                MusicRating = reader.IsDBNull(oMusicRating) ? null : reader.GetDouble(oMusicRating),
                CharacterRating = reader.IsDBNull(oCharRating) ? null : reader.GetDouble(oCharRating),
                Links = reader.IsDBNull(oLinks) ? "[]" : reader.GetString(oLinks),
                ReadingProgress = reader.IsDBNull(oReadProgress) ? 0 : reader.GetInt32(oReadProgress),
                SkipVndb = !reader.IsDBNull(oSkipVndb) && reader.GetInt32(oSkipVndb) == 1
            });
        }
        return list;
    }
}
