using Microsoft.Data.Sqlite;

namespace VnHub.Database;

public static class AppDb
{
    private static string _dbPath = null!;

    public static string DbPath => _dbPath;

    public static void Initialize(string? overridePath = null)
    {
        var folder = overridePath ?? VnHub.Common.AppPaths.Root;
        Directory.CreateDirectory(folder);

        _dbPath = Path.Combine(folder, "vnhub.db");
        RunMigrations();
    }

    public static SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();

        return conn;
    }

    private static void RunMigrations()
    {
        using var conn = Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS vn_entries (
                id          TEXT PRIMARY KEY,
                title       TEXT NOT NULL,
                cover_path  TEXT,
                exe_path    TEXT,
                date_added  TEXT,
                status      INTEGER DEFAULT 4,
                is_favorite INTEGER DEFAULT 0,
                is_pinned   INTEGER DEFAULT 0,
                notes       TEXT,
                tags        TEXT DEFAULT '[]'
            );
            """;
        cmd.ExecuteNonQuery();

        // Add play_time_seconds and last_launched_at columns (migration)
        AddColumnIfMissing(conn, "vn_entries", "play_time_seconds", "INTEGER DEFAULT 0");
        AddColumnIfMissing(conn, "vn_entries", "last_launched_at", "TEXT");
        AddColumnIfMissing(conn, "vn_entries", "group_id", "TEXT");
        AddColumnIfMissing(conn, "vn_entries", "vndb_id", "TEXT");
        AddColumnIfMissing(conn, "vn_entries", "description", "TEXT");
        AddColumnIfMissing(conn, "vn_entries", "rating", "REAL");
        AddColumnIfMissing(conn, "vn_entries", "user_rating", "REAL");
        AddColumnIfMissing(conn, "vn_entries", "completed_at", "TEXT");
        AddColumnIfMissing(conn, "vn_entries", "story_rating", "REAL");
        AddColumnIfMissing(conn, "vn_entries", "art_rating", "REAL");
        AddColumnIfMissing(conn, "vn_entries", "music_rating", "REAL");
        AddColumnIfMissing(conn, "vn_entries", "character_rating", "REAL");
        AddColumnIfMissing(conn, "vn_entries", "links", "TEXT DEFAULT '[]'");
        AddColumnIfMissing(conn, "vn_entries", "reading_progress", "INTEGER DEFAULT 0");
        AddColumnIfMissing(conn, "vn_entries", "skip_vndb", "INTEGER DEFAULT 0");
        // Groups table
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS groups (
                id    TEXT PRIMARY KEY,
                name  TEXT NOT NULL,
                color TEXT DEFAULT '#6366f1'
            );
            """;
        cmd.ExecuteNonQuery();

        // Play sessions table
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS play_sessions (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                vn_id      TEXT NOT NULL,
                started_at TEXT NOT NULL,
                ended_at   TEXT NOT NULL,
                seconds    INTEGER NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();

        // Achievements table
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS achievements (
                key         TEXT PRIMARY KEY,
                unlocked_at TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();

        // FTS5 virtual table (with description)
        cmd.CommandText = """
            CREATE VIRTUAL TABLE IF NOT EXISTS vn_fts USING fts5(
                title, notes, tags, description, content='vn_entries', content_rowid='rowid'
            );
            """;
        cmd.ExecuteNonQuery();

        MigrateFtsDescription(conn);

        // Triggers to keep FTS in sync
        cmd.CommandText = """
            CREATE TRIGGER IF NOT EXISTS vn_entries_ai AFTER INSERT ON vn_entries BEGIN
                INSERT INTO vn_fts(rowid, title, notes, tags, description)
                VALUES (new.rowid, new.title, new.notes, new.tags, new.description);
            END;
            """;
        cmd.ExecuteNonQuery();

        cmd.CommandText = """
            CREATE TRIGGER IF NOT EXISTS vn_entries_ad AFTER DELETE ON vn_entries BEGIN
                INSERT INTO vn_fts(vn_fts, rowid, title, notes, tags, description)
                VALUES ('delete', old.rowid, old.title, old.notes, old.tags, old.description);
            END;
            """;
        cmd.ExecuteNonQuery();

        cmd.CommandText = """
            CREATE TRIGGER IF NOT EXISTS vn_entries_au AFTER UPDATE ON vn_entries BEGIN
                INSERT INTO vn_fts(vn_fts, rowid, title, notes, tags, description)
                VALUES ('delete', old.rowid, old.title, old.notes, old.tags, old.description);
                INSERT INTO vn_fts(rowid, title, notes, tags, description)
                VALUES (new.rowid, new.title, new.notes, new.tags, new.description);
            END;
            """;
        cmd.ExecuteNonQuery();
    }

    private static void MigrateFtsDescription(SqliteConnection conn)
    {
        try
        {
            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT description FROM vn_fts LIMIT 0";
            checkCmd.ExecuteNonQuery();
        }
        catch
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DROP TRIGGER IF EXISTS vn_entries_ai";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DROP TRIGGER IF EXISTS vn_entries_ad";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DROP TRIGGER IF EXISTS vn_entries_au";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DROP TABLE IF EXISTS vn_fts";
            cmd.ExecuteNonQuery();
            cmd.CommandText = """
                CREATE VIRTUAL TABLE vn_fts USING fts5(
                    title, notes, tags, description, content='vn_entries', content_rowid='rowid'
                );
                """;
            cmd.ExecuteNonQuery();
            cmd.CommandText = """
                INSERT INTO vn_fts(rowid, title, notes, tags, description)
                SELECT rowid, title, notes, tags, description FROM vn_entries;
                """;
            cmd.ExecuteNonQuery();
        }
    }

    private static void AddColumnIfMissing(SqliteConnection conn, string table, string column, string type)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1) == column) return;
        }
        reader.Close();
        cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
        cmd.ExecuteNonQuery();
    }
}
