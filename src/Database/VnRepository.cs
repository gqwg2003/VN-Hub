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
            int count = 0;
            foreach (var entry in entries)
            {
                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM vn_entries WHERE id = @id";
                checkCmd.Parameters.AddWithValue("@id", entry.Id);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                if (!exists)
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = """
                        INSERT INTO vn_entries (id, title, cover_path, exe_path, date_added, status, is_favorite, is_pinned, notes, tags, play_time_seconds, last_launched_at, group_id, vndb_id, description, rating, user_rating, completed_at, story_rating, art_rating, music_rating, character_rating, links, reading_progress, skip_vndb)
                        VALUES (@id, @title, @cover, @exe, @date, @status, @fav, @pin, @notes, @tags, @playtime, @lastlaunched, @groupid, @vndbid, @desc, @rating, @userrating, @completedat, @storyrating, @artrating, @musicrating, @charrating, @links, @readprogress, @skipvndb)
                        """;
                    BindEntry(cmd, entry);
                    cmd.ExecuteNonQuery();
                    count++;
                }
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
        while (reader.Read())
        {
            list.Add(new VnEntry
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                CoverPath = reader.IsDBNull(reader.GetOrdinal("cover_path")) ? null : reader.GetString(reader.GetOrdinal("cover_path")),
                ExePath = reader.IsDBNull(reader.GetOrdinal("exe_path")) ? null : reader.GetString(reader.GetOrdinal("exe_path")),
                DateAdded = reader.IsDBNull(reader.GetOrdinal("date_added")) ? "" : reader.GetString(reader.GetOrdinal("date_added")),
                Status = (VnStatus)reader.GetInt32(reader.GetOrdinal("status")),
                IsFavorite = reader.GetInt32(reader.GetOrdinal("is_favorite")) == 1,
                IsPinned = reader.GetInt32(reader.GetOrdinal("is_pinned")) == 1,
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                Tags = reader.IsDBNull(reader.GetOrdinal("tags")) ? "[]" : reader.GetString(reader.GetOrdinal("tags")),
                PlayTimeSeconds = reader.IsDBNull(reader.GetOrdinal("play_time_seconds")) ? 0 : reader.GetInt64(reader.GetOrdinal("play_time_seconds")),
                LastLaunchedAt = reader.IsDBNull(reader.GetOrdinal("last_launched_at")) ? null : reader.GetString(reader.GetOrdinal("last_launched_at")),
                GroupId = reader.IsDBNull(reader.GetOrdinal("group_id")) ? null : reader.GetString(reader.GetOrdinal("group_id")),
                VndbId = reader.IsDBNull(reader.GetOrdinal("vndb_id")) ? null : reader.GetString(reader.GetOrdinal("vndb_id")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                Rating = reader.IsDBNull(reader.GetOrdinal("rating")) ? null : reader.GetDouble(reader.GetOrdinal("rating")),
                UserRating = reader.IsDBNull(reader.GetOrdinal("user_rating")) ? null : reader.GetDouble(reader.GetOrdinal("user_rating")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : reader.GetString(reader.GetOrdinal("completed_at")),
                StoryRating = reader.IsDBNull(reader.GetOrdinal("story_rating")) ? null : reader.GetDouble(reader.GetOrdinal("story_rating")),
                ArtRating = reader.IsDBNull(reader.GetOrdinal("art_rating")) ? null : reader.GetDouble(reader.GetOrdinal("art_rating")),
                MusicRating = reader.IsDBNull(reader.GetOrdinal("music_rating")) ? null : reader.GetDouble(reader.GetOrdinal("music_rating")),
                CharacterRating = reader.IsDBNull(reader.GetOrdinal("character_rating")) ? null : reader.GetDouble(reader.GetOrdinal("character_rating")),
                Links = reader.IsDBNull(reader.GetOrdinal("links")) ? "[]" : reader.GetString(reader.GetOrdinal("links")),
                ReadingProgress = reader.IsDBNull(reader.GetOrdinal("reading_progress")) ? 0 : reader.GetInt32(reader.GetOrdinal("reading_progress")),
                SkipVndb = !reader.IsDBNull(reader.GetOrdinal("skip_vndb")) && reader.GetInt32(reader.GetOrdinal("skip_vndb")) == 1
            });
        }
        return list;
    }
}
