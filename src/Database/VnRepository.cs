using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using VnHub.Models;

namespace VnHub.Database;

public static partial class VnRepository
{
    private static readonly Regex FtsEscapeRegex =
        new(@"[""(){}+\-^:*~]", RegexOptions.Compiled);

    private static string EscapeFts(string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var escaped = new List<string>();
        foreach (var t in terms)
        {
            var clean = FtsEscapeRegex.Replace(t, "");
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

    private static string BuildIdInClause(SqliteCommand cmd, IReadOnlyList<string> ids)
    {
        var placeholders = new string[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            var param = "@id" + i;
            placeholders[i] = param;
            cmd.Parameters.AddWithValue(param, ids[i]);
        }
        return string.Join(", ", placeholders);
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
