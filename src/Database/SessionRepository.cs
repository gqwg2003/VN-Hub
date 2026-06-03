using Microsoft.Data.Sqlite;

namespace VnHub.Database;

public sealed record DayStat(string Day, long Seconds);

public static class SessionRepository
{
    public static void Insert(string vnId, DateTime startedAt, DateTime endedAt, long seconds)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO play_sessions (vn_id, started_at, ended_at, seconds)
            VALUES (@vnId, @start, @end, @sec)
            """;
        cmd.Parameters.AddWithValue("@vnId", vnId);
        cmd.Parameters.AddWithValue("@start", startedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@end", endedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@sec", seconds);
        cmd.ExecuteNonQuery();
    }

    public static List<object> GetByVnId(string vnId)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT started_at, ended_at, seconds FROM play_sessions WHERE vn_id = @id ORDER BY started_at DESC LIMIT 50";
        cmd.Parameters.AddWithValue("@id", vnId);
        var list = new List<object>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new
            {
                startedAt = reader.GetString(0),
                endedAt = reader.GetString(1),
                seconds = reader.GetInt64(2)
            });
        }
        return list;
    }

    public static List<DayStat> GetWeeklyStats()
    {
        return GetStatsByDays(7);
    }

    public static List<DayStat> GetMonthlyStats()
    {
        return GetStatsByDays(30);
    }

    public static List<DayStat> GetStatsByDays(int days)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        if (days <= 0)
        {
            cmd.CommandText = """
                SELECT date(started_at, 'localtime') as day, SUM(seconds) as total
                FROM play_sessions
                GROUP BY day ORDER BY day
                """;
        }
        else
        {
            cmd.CommandText = """
                SELECT date(started_at, 'localtime') as day, SUM(seconds) as total
                FROM play_sessions
                WHERE started_at >= date('now', @cutoff)
                GROUP BY day ORDER BY day
                """;
            cmd.Parameters.AddWithValue("@cutoff", $"-{days} days");
        }
        return ReadDayStats(cmd);
    }

    private static List<DayStat> ReadDayStats(SqliteCommand cmd)
    {
        var list = new List<DayStat>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new DayStat(reader.GetString(0), reader.GetInt64(1)));
        }
        return list;
    }
}
