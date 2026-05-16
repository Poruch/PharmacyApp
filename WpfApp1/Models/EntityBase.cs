using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace PharmacyApp.Models
{
    public abstract class EntityBase
    {

        // Получение имени таблицы
        public string GetTableName()
        {
            var tableAttr = GetType().GetCustomAttribute<TableAttribute>();
            return tableAttr?.Name ?? GetType().Name;
        }

        public static string GetTableName<T>() where T : EntityBase
        {
            var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
            return tableAttr?.Name ?? typeof(T).Name;
        }

        // Получение свойства-ключа
        private PropertyInfo GetKeyProperty()
        {
            var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var keyProp = props.FirstOrDefault(p => p.IsDefined(typeof(KeyAttribute), false));
            if (keyProp == null)
                keyProp = props.FirstOrDefault(p => p.Name == "Id");
            if (keyProp == null)
                throw new InvalidOperationException($"Не найден первичный ключ для типа {GetType().Name}. Укажите [Key] на свойстве.");
            return keyProp;
        }

        // Получение имени столбца ключа
        private string GetKeyColumnName()
        {
            var keyProp = GetKeyProperty();
            var colAttr = keyProp.GetCustomAttribute<ColumnAttribute>();
            return colAttr?.Name ?? keyProp.Name;
        }

        // Получение значения ключа
        private object GetKeyValue()
        {
            var keyProp = GetKeyProperty();
            return keyProp.GetValue(this) ?? 0;
        }

        // Установка значения ключа (после вставки)
        public void SetKeyValue(int value)
        {
            var keyProp = GetKeyProperty();
            if (keyProp.PropertyType == typeof(int))
                keyProp.SetValue(this, value);
            else if (keyProp.PropertyType == typeof(long))
                keyProp.SetValue(this, (long)value);
            // можно добавить другие типы
        }

        // Получение свойств, помеченных [Column] (включая ключ? но ключ мы обрабатываем отдельно)
        private List<PropertyInfo> GetColumnProperties()
        {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.IsDefined(typeof(ColumnAttribute), inherit: true))
                .ToList();
        }

        // Получить имя столбца из атрибута [Column] или имя свойства
        private string GetColumnName(PropertyInfo prop)
        {
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            return colAttr?.Name ?? prop.Name;
        }

        // Генерация INSERT: только столбцы, помеченные [Column] (исключая ключ)
        public (string sql, Dictionary<string, object?> parameters) GenerateInsertSql()
        {
            var keyProp = GetKeyProperty();
            var props = GetColumnProperties()
                .Where(p => p != keyProp) // исключаем ключ
                .ToList();

            var columns = props.Select(GetColumnName);
            var paramNames = props.Select(p => "@" + p.Name);

            string keyColumn = GetKeyColumnName();

            var sql = $@"
                INSERT INTO {GetTableName()} ({string.Join(", ", columns)})
                OUTPUT INSERTED.[{keyColumn}]
                VALUES ({string.Join(", ", paramNames)});";

            var parameters = new Dictionary<string, object?>();
            foreach (var prop in props)
                parameters.Add("@" + prop.Name, prop.GetValue(this) ?? DBNull.Value);
            return (sql, parameters);
        }

        // Генерация UPDATE: обновляем все колонки, кроме ключа
        public (string sql, Dictionary<string, object?> parameters) GenerateUpdateSql()
        {
            var keyProp = GetKeyProperty();
            var props = GetColumnProperties()
                .Where(p => p != keyProp)
                .ToList();

            var setClauses = props.Select(p => $"{GetColumnName(p)} = @{p.Name}");

            string keyColumn = GetKeyColumnName();
            object keyValue = GetKeyValue();

            var sql = $@"
                UPDATE {GetTableName()}
                SET {string.Join(", ", setClauses)}
                WHERE {keyColumn} = @KeyId";

            var parameters = new Dictionary<string, object?> { ["@KeyId"] = keyValue };
            foreach (var prop in props)
                parameters.Add("@" + prop.Name, prop.GetValue(this) ?? DBNull.Value);
            return (sql, parameters);
        }

        // DELETE использует ключ
        public string GenerateDeleteSql()
        {
            string keyColumn = GetKeyColumnName();
            object keyValue = GetKeyValue();
            // Возвращаем параметризованный SQL, но значение подставим позже
            return $"DELETE FROM {GetTableName()} WHERE {keyColumn} = @KeyId";
        }

        // Для удобства: получить параметр удаления
        public Dictionary<string, object?> GetDeleteParameters()
        {
            return new Dictionary<string, object?> { ["@KeyId"] = GetKeyValue() };
        }
    }
}