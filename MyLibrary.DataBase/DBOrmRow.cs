using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Базовый класс для ORM-таблиц.
    /// </summary>
    public abstract class DBOrmRow
    {
        public DBOrmRow(DBRow row)
        {
            Row = row;
        }

        public DBRow Row { get; set; }
        public object this[string columnName]
        {
            get => Row[columnName];
            set => Row[columnName] = value;
        }
        public object this[int columnIndex]
        {
            get => Row[columnIndex];
            set => Row[columnIndex] = value;
        }

        public void Delete()
        {
            Row.Delete();
        }

        public void SetNotNull(int index)
        {
            Row.SetNotNull(index);
        }

        public void SetNotNull(string columnName)
        {
            Row.SetNotNull(columnName);
        }

        public void SetNotNull()
        {
            Row.SetNotNull();
        }

        public TValue GetValue<TValue>(string columnName)
        {
            return Row.GetValue<TValue>(columnName);
        }

        public bool GetBoolean(string columnName)
        {
            return Row.GetValue<bool>(columnName);
        }

        public byte GetByte(string columnName)
        {
            return Row.GetValue<byte>(columnName);
        }

        public byte[] GetBytes(string columnName)
        {
            return Row.GetValue<byte[]>(columnName);
        }

        public DateTime GetDateTime(string columnName)
        {
            return Row.GetValue<DateTime>(columnName);
        }

        public decimal GetDecimal(string columnName)
        {
            return Row.GetValue<decimal>(columnName);
        }

        public double GetDouble(string columnName)
        {
            return Row.GetValue<double>(columnName);
        }

        public short GetInt16(string columnName)
        {
            return Row.GetValue<short>(columnName);
        }

        public int GetInt32(string columnName)
        {
            return Row.GetValue<int>(columnName);
        }

        public long GetInt64(string columnName)
        {
            return Row.GetValue<long>(columnName);
        }

        public float GetSingle(string columnName)
        {
            return Row.GetValue<float>(columnName);
        }

        public string GetString(string columnName, bool allowNull = false)
        {
            return Row.GetString(columnName, allowNull);
        }

        public string GetString(string columnName, string format)
        {
            return Row.GetString(columnName, format);
        }

        public TimeSpan GetTimeSpan(string columnName)
        {
            return Row.GetValue<TimeSpan>(columnName);
        }
    }

    /// <summary>
    /// Базовый класс для типизированных ORM-таблиц.
    /// </summary>
    public abstract class DBOrmRow<TRow> : DBOrmRow
    {
        public DBOrmRow(DBRow row) : base(row)
        {
        }
    }
}
