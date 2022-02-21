using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IDensity.Models.SQL
{
    /// <summary>
    /// Представляет собой набор статических методов для записи классов в таблицу
    /// </summary>
    class SqlMethods
    {
        #region Событие ошибки работы с ДБ
        public static event Action<string> SqlErrorEvent;
        #endregion

        #region Имя ДБ
        static private string DBName { get; } = "application";
        #endregion

        #region Строка подлючения
        static string ConnectionString { get => $"Data Source={DBName}.db;Mode=ReadWriteCreate;"; }

        #endregion

        #region Записать данные в таблицу
        public static void WritetoDb<T>(T cell)
        {
            try
            {
                Type type = typeof(T);
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    CreateTable<T>(type.Name+"s");// Если базы данных не существует, создаем ее
                    InsertToTable<T>(cell);
                }
            }
            catch (Exception ex)
            {
                SqlErrorEvent?.Invoke(ex.Message);


            }
        }
        #endregion

        #region Создание таблицы в базе данных
        /// <summary>
        /// Создание таблицы в базе данных
        /// </summary>
        static void CreateTable<T>(string tableName)
        {
            var props = typeof(T).GetProperties();
            string cmd = "CREATE TABLE IF NOT EXISTS  " + tableName + "(";
            foreach (var item in props)
            {
                cmd += item.Name;
                cmd += " " + GetSqlTypeName(item.PropertyType);
                cmd += ", ";
            }
            cmd = cmd.Substring(0, cmd.Length - 2);
            cmd += ");";
            SqliteCommand command = new SqliteCommand(cmd);
            SqlExecuteCmd(command);
        }
        #endregion

        #region Сопоставление имен system и sql
        /// <summary>
        /// Сопоставление имен system и sql
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public static string GetSqlTypeName(Type propertyType)
        {
            switch (propertyType.Name.ToLower())
            {
                case "int":
                case "int32":
                case "int64":
                    return "INTEGER NOT NULL default 0";
                case "datetime": return "TEXT";
                case "string": return "TEXT";
                case "double": return "float NOT NULL";
                case "single":
                case "float": return "real NOT NULL";
                case "boolean": return "INTEGER";
                default:
                    throw new Exception($"Для типа {propertyType.Name} не определен соответствующий SQL  тип!");
            }
        }
        #endregion

        #region Выполнить sql команду
        /// <summary>
        /// Выполнить sql команду
        /// </summary>
        /// <param name="command"></param>
        static void SqlExecuteCmd(SqliteCommand command)
        {
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {                
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                //INSERT INTO TimePoints (time, y1, y2, y3, y4, y5, y6, y7, y8, y9, y10) VALUES ('18.02.2002 18:15:14', -Inf, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
        }
        #endregion
        #region Добвление в таблицу
        static void InsertToTable<T>(T cell)
        {
            string tableName = typeof(T).Name+"s";
            SqliteCommand command = new SqliteCommand();
            Type myType = cell.GetType();
            string cmd = $"INSERT INTO {tableName}(";
            string values = "VALUES(";
            var props = myType.GetProperties();
            string paramName = null;
            foreach (var item in props)
            {
                cmd = cmd + item.Name + ", ";
                paramName = $"@{item.Name}";
                object value = myType.GetProperty(item.Name).GetValue(cell);
                if (item.PropertyType.Name.ToLower() == "single")
                {
                    if (value is float)
                    {
                        float valFloat = (float)value;
                        if (float.IsNaN(valFloat)) value = 0;
                        if (float.IsPositiveInfinity(valFloat)) value = float.MaxValue;
                        if (float.IsNegativeInfinity(valFloat)) value = float.MinValue;
                    }
                }
                command.Parameters.Add(new SqliteParameter(paramName, value));
                values += paramName;
                values += ", ";
            }
            cmd = cmd.Substring(0, cmd.Length - 2);
            cmd += ")";
            values = values.Substring(0, values.Length - 2);
            values += ")";
            cmd = cmd + values;
            cmd += ";";
            command.CommandText = cmd;
            SqlExecuteCmd(command);
        }
        #endregion

        #region Чтение из ДБ
        public static List<T> ReadFromSql<T>(string commandString)
        {
            List<T> list = new List<T>();
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand(commandString, connection);
                SqliteDataReader reader = command.ExecuteReader();
                Type type = typeof(T);
                var props = type.GetProperties();
                while (reader.Read())
                {
                    T cell = (T)Activator.CreateInstance(type);
                    for (int i = 0; i < props.Length; i++)
                    {
                        type.GetProperty(props[i].Name).SetValue(cell, Parse(props[i].PropertyType, reader.GetValue(i).ToString()));
                    }
                    list.Add(cell);
                }
            }
            return list;
        } 
        #endregion

        #region Преобразование из строки в зависимости от типа
        static object Parse(Type type, string par)
        {
            switch (type.Name.ToLower())
            {
                case "boolean": return par.ToLower() == "1";
                case "int32": return int.Parse(par);
                case "byte": return byte.Parse(par);
                case "datetime": return DateTime.Parse(par);
                case "single":
                case "float": return float.Parse(par.Replace(",", "."), CultureInfo.InvariantCulture);
                default: return par;
            }
        }
        #endregion
    }
}
