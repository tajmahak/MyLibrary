using System;
using System.Data;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет схему строки для таблицы <see cref="DBTable"/>.
    /// </summary>
    public sealed class DBRow
    {
        public DBTable Table { get; private set; }
        public DataRowState State { get; internal set; }
        internal object[] Values;

        internal DBRow(DBTable table)
        {
            Table = table;
            Values = new object[table.Columns.Count];
            State = DataRowState.Detached;
        }

        public object this[string columnName]
        {
            get
            {
                DBColumn column = Table.Columns[columnName];
                return this[column.OrderIndex];
            }
            set
            {
                DBColumn column = Table.Columns[columnName];
                SetValue(column, value);
            }
        }
        public object this[int columnIndex]
        {
            get => Values[columnIndex];
            set
            {
                DBColumn column = Table.Columns[columnIndex];
                SetValue(column, value);
            }
        }
        public object PrimaryKeyValue => this[Table.PrimaryKeyColumn.OrderIndex];
        public bool PrimaryKeyValueIsTemporary => PrimaryKeyValue is DBTempId;

        public TValue GetValue<TValue>(string columnName)
        {
            DBColumn column = Table.Columns[columnName];
            return GetValue<TValue>(column.OrderIndex);
        }
        public TValue GetValue<TValue>(int columnIndex)
        {
            object value = Values[columnIndex];
            return Data.Convert<TValue>(value);
        }
        public bool GetBoolean(string columnName)
        {
            return GetValue<bool>(columnName);
        }
        public bool GetBoolean(int columnIndex)
        {
            return GetValue<bool>(columnIndex);
        }
        public byte GetByte(string columnName)
        {
            return GetValue<byte>(columnName);
        }
        public byte GetByte(int columnIndex)
        {
            return GetValue<byte>(columnIndex);
        }
        public byte[] GetBytes(string columnName)
        {
            return GetValue<byte[]>(columnName);
        }
        public byte[] GetBytes(int columnIndex)
        {
            return GetValue<byte[]>(columnIndex);
        }
        public DateTime GetDateTime(string columnName)
        {
            return GetValue<DateTime>(columnName);
        }
        public DateTime GetDateTime(int columnIndex)
        {
            return GetValue<DateTime>(columnIndex);
        }
        public decimal GetDecimal(string columnName)
        {
            return GetValue<decimal>(columnName);
        }
        public decimal GetDecimal(int columnIndex)
        {
            return GetValue<decimal>(columnIndex);
        }
        public double GetDouble(string columnName)
        {
            return GetValue<double>(columnName);
        }
        public double GetDouble(int columnIndex)
        {
            return GetValue<double>(columnIndex);
        }
        public short GetInt16(string columnName)
        {
            return GetValue<short>(columnName);
        }
        public short GetInt16(int columnIndex)
        {
            return GetValue<short>(columnIndex);
        }
        public int GetInt32(string columnName)
        {
            return GetValue<int>(columnName);
        }
        public int GetInt32(int columnIndex)
        {
            return GetValue<int>(columnIndex);
        }
        public long GetInt64(string columnName)
        {
            return GetValue<long>(columnName);
        }
        public long GetInt64(int columnIndex)
        {
            return GetValue<long>(columnIndex);
        }
        public float GetSingle(string columnName)
        {
            return GetValue<float>(columnName);
        }
        public float GetSingle(int columnIndex)
        {
            return GetValue<float>(columnIndex);
        }
        public string GetString(string columnName, bool allowNull = false)
        {
            DBColumn column = Table.Columns[columnName];
            return GetString(column.OrderIndex, allowNull);
        }
        public string GetString(string columnName, string format)
        {
            DBColumn column = Table.Columns[columnName];
            return GetString(column.OrderIndex, format);
        }
        public string GetString(int columnIndex, bool allowNull = false)
        {
            string value = GetValue<string>(columnIndex);
            if (!allowNull && value == null)
            {
                return string.Empty;
            }
            return value;
        }
        public string GetString(int columnIndex, string format)
        {
            object value = this[columnIndex];
            if (value is IFormattable formattable)
            {
                return formattable.ToString(format, null);
            }
            throw DBInternal.StringFormatException();
        }
        public TimeSpan GetTimeSpan(string columnName)
        {
            return GetValue<TimeSpan>(columnName);
        }
        public TimeSpan GetTimeSpan(int columnIndex)
        {
            return GetValue<TimeSpan>(columnIndex);
        }

        public bool IsNull(string columnName)
        {
            DBColumn column = Table.Columns[columnName];
            return IsNull(column.OrderIndex);
        }
        public bool IsNull(int columnIndex)
        {
            object value = Values[columnIndex];
            return Data.IsNull(value);
        }

        public void SetNotNull(string columnName)
        {
            DBColumn column = Table.Columns[columnName];
            SetNotNull(column.OrderIndex);
        }
        public void SetNotNull(int columnIndex)
        {
            DBColumn column = Table.Columns[columnIndex];
            SetNotNull(column);
        }
        public void SetNotNull()
        {
            for (int i = 0; i < Table.Columns.Count; i++)
            {
                SetNotNull(i);
            }
        }

        public void Delete()
        {
            State = DataRowState.Deleted;
        }

        private void SetValue(DBColumn column, object value)
        {
            value = value ?? DBNull.Value;

            if (value is DBTempId)
            {
                if (column.IsPrimary)
                {
                    throw DBInternal.GenerateSetIDException(column);
                }
            }
            else if (value is DBNull)
            {
                if (column.NotNull)
                {
                    value = Data.GetNotNullValue(column.DataType);
                }
            }
            else
            {
                if (value.GetType() != column.DataType)
                {
                    try
                    {
                        value = Convert.ChangeType(value, column.DataType);
                    }
                    catch (Exception ex)
                    {
                        throw DBInternal.DataConvertException(column, value, ex);
                    }
                }
            }

            if (column.DataType == typeof(string) && value is string stringValue)
            {
                // проверка на максимальную длину текстовой строки
                if (stringValue.Length > column.Size)
                {
                    throw DBInternal.StringOverflowException(column);
                }
            }

            if (State == DataRowState.Unchanged)
            {
                // проверка значения на разницу с предыдущим значением
                object prevValue = Values[column.OrderIndex];
                bool modified = true;
                if (value.GetType() == prevValue.GetType() && value is IComparable)
                {
                    modified = !Equals(value, prevValue);
                }
                else if (value is byte[] array && prevValue is byte[] prevArray)
                {
                    modified = !Data.Equals(array, prevArray);
                }

                if (modified)
                {
                    State = DataRowState.Modified;
                }
            }

            Values[column.OrderIndex] = value;
        }
        private void SetNotNull(DBColumn column)
        {
            if (column.NotNull)
            {
                object value = Values[column.OrderIndex];
                if (value is DBNull)
                {
                    value = Data.GetNotNullValue(column.DataType);
                    SetValue(column, value);
                }
            }
        }
    }
}
