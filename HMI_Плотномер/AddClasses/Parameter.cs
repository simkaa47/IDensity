using IDensity.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace IDensity.AddClasses
{
    /// <summary>
    /// Хранит информацию о том или ином параметре: адрес Modbus, описание
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class Parameter<T> : PropertyChangedBase, IDataErrorInfo, ICloneable where T : IComparable
    {
        #region Событие ошибки работы с ДБ
        /// <summary>
        /// Событие ошибки работы с ДБ
        /// </summary>
        public event Action<string> SqlErrorEvent;
        #endregion
        public static string DbName { get; set; } = "application";
        public static string TableName { get; set; } = "params";

        #region Строка подлючения
        string ConnectionString { get => $"Data Source={DbName}.db;Mode=ReadWriteCreate"; }

        #endregion
        bool isChanged;
        Dictionary<string, string> errorsDict = new Dictionary<string, string>();
        public string this[string columnName] => errorsDict.ContainsKey(columnName) ? errorsDict[columnName] : null;

        public string Error { get; }

        bool _validationOk = true;
        public bool ValidationOk { get => _validationOk; set => Set(ref _validationOk, value);  } 

        #region Идетификатор параметра
        public string Id { get; }
        #endregion
        
        public Parameter(string id, string description, T minValue, T maxValue, int regNum, string regType)
        {
            this.Id = id;
            this.Description = description;
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.RegNum = regNum;
            this.RegType = regType;
            timer = new Timer(5000);
            timer.Elapsed += OnTimerElapsed;
            GetFromSql();
            AddToParamList();
            PropertyChanged += (o, e) => OnSqlPropertyChanged(e.PropertyName);
        }
        #region Упрощенный конструктор
        public Parameter()
        {

        }
        #endregion       

        #region Описание
        string _description = "";
        public string Description{ get => _description; set => Set(ref _description, value); }       
        #endregion

        #region Величина
        T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (Set(ref _value, value) && !isChanged)
                {
                    WriteValue = value;
                }
            }
        }
        #endregion

        #region Значение записи
        T _writeValue;
        public T WriteValue
        {
            get => _writeValue;
            set
            {
                if (value.CompareTo(this.Value) != 0)
                {
                    isChanged = true;
                    RestartTimer();
                }
                if (value.CompareTo(MinValue) < 0 || value.CompareTo(MaxValue) > 0)
                {
                    errorsDict["WriteValue"] = $"Значение параметра записи \"{Description}\" больше максимального или меньше минимального предела!";
                    ValidationOk = false;                    
                } 
                else
                {
                    errorsDict["WriteValue"] = null;
                    ValidationOk = true;
                }
                Set(ref _writeValue, value);
            }
        }
        #endregion

        #region Тип регистра
        string _regType = "hold";
        public string RegType{get => _regType;set=>Set(ref _regType, value); }
        #endregion

        #region Адрес регистра
        int _regNum;
        public int RegNum{ get => _regNum; set => Set(ref _regNum, value); }  
       
        #endregion

        #region Минимальная величина
        T _minValue;
        public T MinValue{get => _minValue;set=>Set(ref _minValue, value);}
        #endregion

        #region Максмальная величина
        T _maxValue;
        public T MaxValue{get => _maxValue;set=>Set(ref _maxValue, value);} 
        #endregion

        #region Только чтение
        bool _onlyRead;
        public bool OnlyRead { get => _onlyRead; set => Set(ref _onlyRead, value); }
        #endregion
        public object Clone()
        {
            return this.MemberwiseClone();
        }        


        #region Таймер
        Timer timer;

        void RestartTimer()
        {
            if (timer.Enabled) timer.Stop();
            timer.Start();
        }

        void OnTimerElapsed(Object source, ElapsedEventArgs e)
        {
            timer.Stop();
            isChanged = false;
            WriteValue = Value;
        }
        #endregion
        #region Добавить в глобальный лист параметров
        void AddToParamList()
        {
            if (ParamList.ParameterList == null) return;
            if (!ParamList.ParameterList.Where(p => p is Parameter<T>)
                .Any(p => (p as Parameter<T>).Description == this.Description)) ParamList.ParameterList.Add(this);
        }
        #endregion

        #region  Функция, вызываемая при изменении свойства
        /// <summary>
        /// Функция, вызываемая при изменении свойства
        /// </summary>
        void OnSqlPropertyChanged(string propName)
        {
            switch (propName)
            {
                case "Id":
                    UpdateSql("Id", Id);
                    return;
                case "Description":
                    UpdateSql("Description", Description);
                    return;
                case "MinValue":
                    UpdateSql("MinValue", MinValue);
                    return;
                case "MaxValue":
                    UpdateSql("MaxValue", MaxValue);
                    return;
                case "RegNum":
                    UpdateSql("RegNum", RegNum);
                    return;
                case "RegType":
                    UpdateSql("RegType", RegType);
                    return;
                default:
                    break;
            }

        } 
        #endregion

        #region Методы, работающин с Sqlite
        #region прочитать из ДБ
        void GetFromSql()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    CreateTable();// Если базы данных не существует, создаем ее
                    SqliteCommand command = new SqliteCommand($"SELECT * FROM {TableName} WHERE Id = '{Id}' ;", connection);
                    SqliteDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        MinValue = reader.GetString(2).Convert<T>();
                        MaxValue = reader.GetString(3).Convert<T>();
                        RegNum = reader.GetInt32(4);
                        RegType = reader.GetString(5);
                        Description = reader.GetString(6);
                    }
                    else
                    {
                        InsertToTable();
                    }
                }
                catch (Exception ex)
                {
                    SqlErrorEvent?.Invoke(ex.Message);
                }
            }
        }
        #endregion
        #region Создание таблицы в базе данных
        /// <summary>
        /// Создание таблицы в базе данных
        /// </summary>
        void CreateTable()
        {
            string cmd = "CREATE TABLE IF NOT EXISTS  " + TableName + "(_id INTEGER NOT NULL PRIMARY KEY UNIQUE, Id TEXT NOT NULL, MinValue TEXT NOT NULL, MaxValue TEXT NOT NULL, RegNum INTEGER, RegType, Description TEXT);";
            SqliteCommand command = new SqliteCommand(cmd);
            SqlExecuteCmd(command);
        }
        #endregion
        #region Добвление в таблицу
        void InsertToTable()
        {
            SqliteCommand command = new SqliteCommand();
            string cmd = $"INSERT INTO {TableName}(Id, MinValue, MaxValue, RegNum, RegType, Description) VALUES(@id, @minVal, @maxVal, @regNum, @regType, @desc);";            
            command.Parameters.Add(new SqliteParameter("@id", Id));
            command.Parameters.Add(new SqliteParameter("@minVal", MinValue.ToString()));
            command.Parameters.Add(new SqliteParameter("@maxVal", MaxValue.ToString()));
            command.Parameters.Add(new SqliteParameter("@regNum", RegNum));
            command.Parameters.Add(new SqliteParameter("@regType", RegType));
            command.Parameters.Add(new SqliteParameter("@desc", Description));            
            command.CommandText = cmd;
            SqlExecuteCmd(command);
        }
        #endregion
        #region Общий метод для отлавливания исключений
        void DoSqlCommand(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                SqlErrorEvent?.Invoke(ex.Message);
            }
        }
        #endregion
        #region Выполнить sql команду
        /// <summary>
        /// Выполнить sql команду
        /// </summary>
        /// <param name="command"></param>
        void SqlExecuteCmd(SqliteCommand command)
        {
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                DoSqlCommand(() =>
                {
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                }
                );
            }
        }
        #endregion
        #region Редактирование строки        
        void UpdateSql(string setName, object setValue)
        {
            SqliteCommand command = new SqliteCommand();
            command.Parameters.Add(new SqliteParameter("@idValue", Id));
            command.Parameters.Add(new SqliteParameter("@setValue", setValue));
            command.CommandText = $"UPDATE {TableName} SET {setName} = @setValue WHERE Id = '{Id}';";
            SqlExecuteCmd(command);
        }
        
        #endregion
        #endregion
    }
    #region Расширение для парсинга из строки
    public static class StringExtension
    {
        #region Джененрик метод для парсинга из строки
        public static T Convert<T>(this string input)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null)
                {
                    // Cast ConvertFromString(string text) : object to (T)
                    return (T)converter.ConvertFromString(input);
                }
                return default(T);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
        #endregion

    } 
    #endregion
}
