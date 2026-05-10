namespace VnHub.Database;

public static class AchievementRepository
{
    public static Dictionary<string, string> GetAll()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT key, unlocked_at FROM achievements";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetString(0)] = reader.GetString(1);
        }
        return result;
    }

    public static void Unlock(string key, string unlockedAtIso)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO achievements (key, unlocked_at) VALUES (@k, @t)";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@t", unlockedAtIso);
        cmd.ExecuteNonQuery();
    }
}
