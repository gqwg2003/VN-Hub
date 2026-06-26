using Microsoft.Data.Sqlite;
using VnHub.Models;

namespace VnHub.Database;

public static partial class VnRepository
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

    public static List<VnEntry> GetByIds(IReadOnlyList<string> ids)
    {
        if (ids.Count == 0) return new List<VnEntry>();
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        var inClause = BuildIdInClause(cmd, ids);
        cmd.CommandText = $"SELECT * FROM vn_entries WHERE id IN ({inClause})";
        return ReadEntries(cmd);
    }

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

        sql.Append("ORDER BY e.is_pinned DESC, ");
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
}
