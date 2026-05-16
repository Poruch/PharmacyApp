using Microsoft.Data.SqlClient;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
using PharmacyApp.Models;

public class EntityRepository
{
    private readonly string _connectionString;

    public EntityRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public int Add<T>(T entity) where T : EntityBase
    {
        var (sql, parameters) = entity.GenerateInsertSql();
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        foreach (var p in parameters)
            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        conn.Open();
        int newId = Convert.ToInt32(cmd.ExecuteScalar());
        entity.SetKeyValue(newId);
        return newId;
    }

    public void Update<T>(T entity) where T : EntityBase
    {
        var (sql, parameters) = entity.GenerateUpdateSql();
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        foreach (var p in parameters)
            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void Delete<T>(T entity) where T : EntityBase
    {
        string sql = entity.GenerateDeleteSql();
        var parameters = entity.GetDeleteParameters();
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        foreach (var p in parameters)
            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        conn.Open();
        cmd.ExecuteNonQuery();
    }



    public List<T> GetAll<T>() where T : EntityBase, new()
    {
        var list = new List<T>();
        string tableName = EntityBase.GetTableName<T>();
        string sql = $"SELECT * FROM {tableName}";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        conn.Open();
        using var reader = cmd.ExecuteReader();

        // Берём только те свойства, которые помечены [Column]
        var properties = typeof(T).GetProperties()
            .Where(p => p.IsDefined(typeof(ColumnAttribute), inherit: true))
            .ToDictionary(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name, p => p);

        while (reader.Read())
        {
            var entity = new T();
            foreach (var (columnName, prop) in properties)
            {
                // Пытаемся прочитать колонку (предполагаем, что она есть в SELECT *)
                object value = reader[columnName];
                if (value != DBNull.Value)
                {
                    var converted = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(entity, converted);
                }
            }
            list.Add(entity);
        }
        return list;
    }
}
