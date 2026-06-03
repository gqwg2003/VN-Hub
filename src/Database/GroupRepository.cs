using Microsoft.Data.Sqlite;
using VnHub.Models;

namespace VnHub.Database;

public static class GroupRepository
{
    public static List<VnGroup> GetAll()
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, color, \"filter\", sort_order FROM groups ORDER BY sort_order, name COLLATE NOCASE";
        var list = new List<VnGroup>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new VnGroup
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Color = reader.IsDBNull(2) ? "#6366f1" : reader.GetString(2),
                Filter = reader.IsDBNull(3) ? null : reader.GetString(3),
                SortOrder = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
            });
        }
        return list;
    }

    public static VnGroup? GetById(string id)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, color, \"filter\", sort_order FROM groups WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new VnGroup
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Color = reader.IsDBNull(2) ? "#6366f1" : reader.GetString(2),
            Filter = reader.IsDBNull(3) ? null : reader.GetString(3),
            SortOrder = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
        };
    }

    public static void Insert(VnGroup group)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO groups (id, name, color, \"filter\", sort_order) VALUES (@id, @name, @color, @filter, @sortOrder)";
        cmd.Parameters.AddWithValue("@id", group.Id);
        cmd.Parameters.AddWithValue("@name", group.Name);
        cmd.Parameters.AddWithValue("@color", group.Color);
        cmd.Parameters.AddWithValue("@filter", (object?)group.Filter ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sortOrder", group.SortOrder);
        cmd.ExecuteNonQuery();
    }

    public static void Update(VnGroup group)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE groups SET name = @name, color = @color, \"filter\" = @filter, sort_order = @sortOrder WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", group.Id);
        cmd.Parameters.AddWithValue("@name", group.Name);
        cmd.Parameters.AddWithValue("@color", group.Color);
        cmd.Parameters.AddWithValue("@filter", (object?)group.Filter ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sortOrder", group.SortOrder);
        cmd.ExecuteNonQuery();
    }

    public static void Delete(string id)
    {
        using var conn = AppDb.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            using (var clearCmd = conn.CreateCommand())
            {
                clearCmd.CommandText = "UPDATE vn_entries SET group_id = NULL WHERE group_id = @id";
                clearCmd.Parameters.AddWithValue("@id", id);
                clearCmd.ExecuteNonQuery();
            }

            using (var deleteCmd = conn.CreateCommand())
            {
                deleteCmd.CommandText = "DELETE FROM groups WHERE id = @id";
                deleteCmd.Parameters.AddWithValue("@id", id);
                deleteCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static void SetVnGroup(string vnId, string? groupId)
    {
        using var conn = AppDb.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE vn_entries SET group_id = @gid WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", vnId);
        cmd.Parameters.AddWithValue("@gid", (object?)groupId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}
