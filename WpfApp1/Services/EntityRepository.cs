using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

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
        string sql = $"SELECT * FROM {EntityBase.GetQuotedTableName<T>()}";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        conn.Open();
        using var reader = cmd.ExecuteReader();

        var columnOrdinals = BuildColumnOrdinals(reader);

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.IsDefined(typeof(ColumnAttribute), inherit: true))
            .Select(p => (Column: p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name, Property: p))
            .ToList();

        while (reader.Read())
        {
            var entity = new T();
            foreach (var (columnName, prop) in properties)
            {
                if (!columnOrdinals.TryGetValue(columnName, out int ordinal))
                    continue;

                if (reader.IsDBNull(ordinal))
                    continue;

                object rawValue = reader.GetValue(ordinal);
                prop.SetValue(entity, ConvertFromDatabase(rawValue, prop.PropertyType));
            }
            list.Add(entity);
        }

        return list;
    }

    private static Dictionary<string, int> BuildColumnOrdinals(SqlDataReader reader)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
            map[reader.GetName(i)] = i;
        return map;
    }

    /// <summary>
    /// Преобразует значение из SqlDataReader в тип свойства (включая nullable, enum, Guid).
    /// </summary>
    internal static object? ConvertFromDatabase(object value, Type targetType)
    {
        if (value is DBNull)
            return null;

        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsEnum)
        {
            return value is string s
                ? Enum.Parse(underlyingType, s, ignoreCase: true)
                : Enum.ToObject(underlyingType, value);
        }

        if (underlyingType == typeof(Guid))
        {
            return value switch
            {
                Guid g => g,
                string s => Guid.Parse(s),
                byte[] bytes => new Guid(bytes),
                _ => Guid.Parse(value.ToString()!)
            };
        }

        if (underlyingType == typeof(bool) && value is not bool)
            return Convert.ToInt32(value) != 0;

        if (value.GetType() == underlyingType || underlyingType.IsInstanceOfType(value))
            return value;

        return Convert.ChangeType(value, underlyingType);
    }
}
