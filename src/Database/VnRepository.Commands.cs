using Microsoft.Data.Sqlite;
using VnHub.Models;

namespace VnHub.Database;

public static partial class VnRepository
{
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
}
