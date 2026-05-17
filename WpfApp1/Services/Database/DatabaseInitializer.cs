using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;
using PharmacyApp.Models;

namespace PharmacyApp.Services
{
    public static class DatabaseInitializer
    {
        private static readonly string TargetConnectionString = ConfigManager.ConnectionString;

        /// <summary>
        /// Создаёт все таблицы, если они не существуют, в правильном порядке (с учётом внешних ключей)
        /// </summary>
        public static void EnsureDatabaseCreated()
        {
            CreateAllTablesOrdered();
        }

        /// <summary>
        /// Удаляет все таблицы динамически (сбрасывает базу)
        /// </summary>
        public static void DropAllTables()
        {
            using (var connection = new SqlConnection(TargetConnectionString))
            {
                connection.Open();
                string dropScript = @"
        DECLARE @sql NVARCHAR(MAX) = N'';
        SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
        FROM sys.foreign_keys;
        EXEC sp_executesql @sql;

        SET @sql = N'';
        SELECT @sql += 'DROP TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';'
        FROM sys.tables t
        JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE t.name NOT IN ('__EFMigrationsHistory');
        EXEC sp_executesql @sql;
    ";
                using (var cmd = new SqlCommand(dropScript, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Создаёт таблицы для всех типов с атрибутом [Table], унаследованных от EntityBase (или без него)
        /// </summary>
        public static void CreateAllTablesOrdered()
        {
            var modelTypes = Assembly.GetAssembly(typeof(App))
                             .GetTypes()
                             .Where(t => t.IsClass && t.GetCustomAttribute<TableAttribute>() != null && !t.IsAbstract)
                             .ToList();

            var ordered = TopologicalSort(modelTypes);
            foreach (var type in ordered)
            {
                var method = typeof(DatabaseInitializer).GetMethod(nameof(CreateTableIfNotExists))
                    ?.MakeGenericMethod(type);
                method?.Invoke(null, null);
            }
        }

        /// <summary>
        /// Создаёт таблицу для типа T, если она не существует.
        /// Использует атрибуты [Table], [Column], [Key], [ForeignKey], [MaxLength], [Required].
        /// </summary>
        public static void CreateTableIfNotExists<T>() where T : class, new()
        {
            var type = typeof(T);
            string tableName = GetTableName(type);
            var columns = GetColumnDefinitions(type).ToList();
            var foreignKeys = GetForeignKeys(type).ToList();

            string createTableSql = $@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')
                BEGIN
                    CREATE TABLE [{tableName}] (
                        {string.Join(", ", columns)}
                    );
                END";

            using (var connection = new SqlConnection(TargetConnectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand(createTableSql, connection))
                {
                    Debug.WriteLine(cmd.CommandText);
                    cmd.ExecuteNonQuery();
                }

                foreach (var fk in foreignKeys)
                {
                    string checkFkSql = $@"
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{fk.ConstraintName}')
                           AND EXISTS (
                               SELECT 1 FROM sys.columns c
                               INNER JOIN sys.tables t ON c.object_id = t.object_id
                               WHERE t.name = '{tableName}' AND c.name = '{fk.ForeignKeyColumn}')
                        BEGIN
                            ALTER TABLE [{tableName}] 
                            ADD CONSTRAINT [{fk.ConstraintName}] 
                            FOREIGN KEY ([{fk.ForeignKeyColumn}]) 
                            REFERENCES [{fk.ReferencedTable}]([{fk.ReferencedColumn}])
                            ON DELETE NO ACTION
                        END";
                    using (var cmd = new SqlCommand(checkFkSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static string GetTableName(Type type)
        {
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            return tableAttr?.Name ?? type.Name;
        }

        private static IEnumerable<string> GetColumnDefinitions(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null);

            foreach (var prop in props)
            {
                string columnName = prop.GetCustomAttribute<ColumnAttribute>()!.Name;
                string sqlType = MapCSharpTypeToSql(prop);
                string nullable = IsNullable(prop) ? "NULL" : "NOT NULL";
                string identity = IsAutoIncrementKey(prop) ? "IDENTITY(1,1)" : "";
                string primaryKey = IsPrimaryKey(prop) ? "PRIMARY KEY" : "";

                var parts = new[] { $"[{columnName}]", sqlType, nullable, identity, primaryKey }
                    .Where(p => !string.IsNullOrEmpty(p));
                yield return string.Join(" ", parts);
            }
        }

        private static string MapCSharpTypeToSql(PropertyInfo prop)
        {
            // 1. Если явно задан TypeName в атрибуте [Column], используем его
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.TypeName))
                return columnAttr.TypeName.ToUpperInvariant();

            Type type = prop.PropertyType;
            bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable)
                type = type.GetGenericArguments()[0];

            // 2. Обработка простых типов
            if (type == typeof(int) || type == typeof(short) || type == typeof(byte))
                return "INT";
            if (type == typeof(long))
                return "BIGINT";
            if (type == typeof(decimal))
            {
                // Можно задать точность через атрибут [Column(TypeName = "decimal(18,4)")] или по умолчанию
                return "DECIMAL(18,2)";
            }
            if (type == typeof(float))
                return "REAL";
            if (type == typeof(double))
                return "FLOAT";
            if (type == typeof(bool))
                return "BIT";
            if (type == typeof(DateTime))
                return "DATETIME2(7)";
            if (type == typeof(DateTimeOffset))
                return "DATETIMEOFFSET(7)";
            if (type == typeof(TimeSpan))
                return "TIME(7)";
            if (type == typeof(Guid))
                return "UNIQUEIDENTIFIER";

            // 3. Строковые типы (юникод)
            if (type == typeof(string) || type == typeof(char) || type == typeof(char?))
            {
                int maxLength = GetMaxStringLength(prop);
                if (maxLength == -1 || maxLength > 4000)
                    return "NVARCHAR(MAX)";
                return $"NVARCHAR({maxLength})";
            }

            if (type == typeof(byte[]))
                return "VARBINARY(MAX)";

            if (type.IsEnum)
                return "INT";

            throw new NotSupportedException($"Тип {type.Name} не поддерживается для маппинга в SQL");
        }

        // Вспомогательный метод для получения максимальной длины строки из атрибутов
        private static int GetMaxStringLength(PropertyInfo prop)
        {
            // Проверяем MaxLengthAttribute
            var maxLenAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
            if (maxLenAttr != null && maxLenAttr.Length > 0)
                return maxLenAttr.Length;

            // Проверяем StringLengthAttribute (из System.ComponentModel.DataAnnotations)
            var strLenAttr = prop.GetCustomAttribute<StringLengthAttribute>();
            if (strLenAttr != null && strLenAttr.MaximumLength > 0)
                return strLenAttr.MaximumLength;

            // Значение по умолчанию
            return 255;
        }

        private static bool IsNullable(PropertyInfo prop)
        {
            // Значимые типы: nullable если подлежащий тип != null
            if (prop.PropertyType.IsValueType)
                return Nullable.GetUnderlyingType(prop.PropertyType) != null;

            // Для ссылочных типов проверяем атрибут [Required]
            if (prop.IsDefined(typeof(RequiredAttribute)))
                return false;
            return true;
        }

        private static bool IsPrimaryKey(PropertyInfo prop)
        {
            return prop.IsDefined(typeof(KeyAttribute));
        }

        private static bool IsAutoIncrementKey(PropertyInfo prop)
        {
            if (!IsPrimaryKey(prop)) return false;
            var type = prop.PropertyType;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];
            return type == typeof(int) || type == typeof(long);
        }

        private class ForeignKeyInfo
        {
            public string ForeignKeyColumn { get; set; }
            public string ReferencedTable { get; set; }
            public string ReferencedColumn { get; set; }
            public string ConstraintName { get; set; }
        }

        private static IEnumerable<ForeignKeyInfo> GetForeignKeys(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            string currentTable = GetTableName(type);

            foreach (var prop in properties)
            {
                var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();
                if (fkAttr == null) continue;

                Type referencedType = null;
                string foreignKeyColumn = null;

                // Определяем, что у нас за свойство: навигационное (ссылается на другую сущность) или скалярное (внешний ключ)
                bool isNavigationProperty = !prop.PropertyType.IsValueType && prop.PropertyType != typeof(string);

                if (isNavigationProperty)
                {
                    // Атрибут висит на навигационном свойстве. fkAttr.Name — имя свойства внешнего ключа.
                    // Целевой тип — это тип навигационного свойства (если это коллекция — берём элемент)
                    referencedType = prop.PropertyType;
                    if (referencedType.IsGenericType && referencedType.GetGenericTypeDefinition() == typeof(ICollection<>))
                        referencedType = referencedType.GetGenericArguments()[0];

                    // Ищем столбец внешнего ключа по имени, указанному в атрибуте
                    var fkProperty = properties.FirstOrDefault(p => p.Name == fkAttr.Name);
                    if (fkProperty != null)
                        foreignKeyColumn = fkProperty.GetCustomAttribute<ColumnAttribute>()?.Name ?? fkProperty.Name;
                    else
                        foreignKeyColumn = fkAttr.Name; // fallback
                }
                else
                {
                    // Атрибут висит на свойстве внешнего ключа (обычно int, int? и т.д.)
                    // fkAttr.Name — имя навигационного свойства
                    var navProp = properties.FirstOrDefault(p => p.Name == fkAttr.Name);
                    if (navProp != null)
                    {
                        referencedType = navProp.PropertyType;
                        if (referencedType.IsGenericType && referencedType.GetGenericTypeDefinition() == typeof(ICollection<>))
                            referencedType = referencedType.GetGenericArguments()[0];
                    }
                    // Имя столбца — из текущего свойства
                    foreignKeyColumn = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
                }

                if (referencedType == null) continue;

                string referencedTable = GetTableName(referencedType);
                string referencedColumn = GetPrimaryKeyColumn(referencedType);
                string safeFkColumn = foreignKeyColumn ?? "Unknown";
                string constraintName = $"FK_{currentTable}_{referencedTable}_{safeFkColumn}";
                // Очищаем constraintName от недопустимых символов (например, Nullable`1 и т.п.)
                constraintName = constraintName.Replace("`", "").Replace("<", "").Replace(">", "").Replace(".", "_");

                yield return new ForeignKeyInfo
                {
                    ForeignKeyColumn = safeFkColumn,
                    ReferencedTable = referencedTable,
                    ReferencedColumn = referencedColumn,
                    ConstraintName = constraintName
                };
            }
        }

        private static string GetPrimaryKeyColumn(Type type)
        {
            var keyProp = type.GetProperties()
                .FirstOrDefault(p => p.IsDefined(typeof(KeyAttribute)));
            if (keyProp == null) return "Id"; // по умолчанию
            return keyProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? keyProp.Name;
        }

        // ==================== Топологическая сортировка ====================

        private static List<Type> TopologicalSort(List<Type> types)
        {
            var graph = new Dictionary<Type, List<Type>>();
            foreach (var t in types)
            {
                graph[t] = new List<Type>();
                foreach (var fk in GetForeignKeys(t))
                {
                    var referencedType = types.FirstOrDefault(tt => GetTableName(tt) == fk.ReferencedTable);
                    if (referencedType != null && referencedType != t) // исключаем самоссылку
                        graph[t].Add(referencedType);
                }
            }

            var sorted = new List<Type>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>(); // для обнаружения циклов

            void Visit(Type node)
            {
                if (visiting.Contains(node))
                    throw new InvalidOperationException($"Циклическая зависимость для типа {node.Name}");
                if (visited.Contains(node)) return;

                visiting.Add(node);
                foreach (var dep in graph[node])
                    Visit(dep);
                visiting.Remove(node);

                visited.Add(node);
                sorted.Add(node);
            }

            foreach (var t in types)
                if (!visited.Contains(t))
                    Visit(t);

            return sorted;
        }
    }
}