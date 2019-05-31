using MyLibrary.Data;
using System;
using System.Data;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет схему строки для таблицы <see cref="DBTable"/>.
    /// </summary>
    public sealed class DBRow
    {
        internal DBRow(DBTable table)
        {
            Table = table;
            Values = new object[table.Columns.Count];
            State = DataRowState.Detached;
        }

        public DBTable Table { get; private set; }
        public DataRowState State { get; internal set; }
        internal object[] Values;

        public object this[int index]
        {
            get
            {
                return Values[index];
            }
            set
            {
                SetValue(index, value);
            }
        }
        public object this[string columnName]
        {
            get
            {
                var column = Table[columnName];
                return this[column.OrderIndex];
            }
            set
            {
                var column = Table[columnName];
                SetValue(column.OrderIndex, value);
            }
        }

        public void SetNotNull(int index)
        {
            var column = Table.Columns[index];
            SetNotNull(column);
        }
        public void SetNotNull(string columnName)
        {
            var column = Table[columnName];
            SetNotNull(column.OrderIndex);
        }
        public void SetNotNull()
        {
            for (int i = 0; i < Table.Columns.Count; i++)
            {
                SetNotNull(i);
            }
        }

        public T Get<T>(int index)
        {
            return Format.Convert<T>(Values[index]);
        }
        public T Get<T>(string columnName)
        {
            var column = Table[columnName];
            var value = Values[column.OrderIndex];
            return Format.Convert<T>(value);
        }

        public string GetString(int columnIndex)
        {
            return GetString(columnIndex, false);
        }
        public string GetString(string columnName)
        {
            return GetString(columnName, false);
        }

        public string GetString(int columnIndex, bool allowNull)
        {
            var value = Get<string>(columnIndex);
            if (!allowNull && value == null)
                return string.Empty;
            return value;
        }
        public string GetString(string columnName, bool allowNull)
        {
            var column = Table[columnName];
            return GetString(column.OrderIndex, allowNull);
        }

        public string GetString(int columnIndex, string format)
        {
            var value = this[columnIndex];
            if (!(value is IFormattable))
                throw DBInternal.StringFormatException();
            return ((IFormattable)value).ToString(format, null);
        }
        public string GetString(string columnName, string format)
        {
            var column = Table[columnName];
            return GetString(column.OrderIndex, format);
        }

        public bool IsNull(int index)
        {
            return (Values[index] is DBNull);
        }
        public bool IsNull(string columnName)
        {
            var column = Table[columnName];
            return IsNull(column.OrderIndex);
        }

        public void Delete()
        {
            State = DataRowState.Deleted;
        }

        private void SetValue(int index, object value)
        {
            var column = Table[index];

            value = value ?? DBNull.Value;

            if (value is Guid)
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
                    value = Format.GetNotNullValue(column.DataType);
                }
            }
            else
            {
                var valueType = value.GetType();
                if (valueType != column.DataType)
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

            if (value is string)
            {
                // проверка на максимальную длину текстовой строки
                string stringValue = (string)value;
                if (stringValue.Length > column.Size)
                {
                    throw DBInternal.StringOverflowException(column);
                }
            }

            if (State == DataRowState.Unchanged)
            {
                #region Проверка значения на разницу с предыдущим значением

                object prevValue = Values[index];
                bool isChanged = true;
                if (value.GetType() == prevValue.GetType() && value is IComparable)
                {
                    isChanged = !object.Equals(value, prevValue);
                }
                else if (value is byte[] && prevValue is byte[])
                {
                    isChanged = !Format.ArrayEquals((byte[])value, (byte[])prevValue);
                }

                if (isChanged)
                    State = DataRowState.Modified;

                #endregion
            }

            Values[index] = value;
        }
        private void SetNotNull(DBColumn column)
        {
            if (column.NotNull)
            {
                var value = Values[column.OrderIndex];
                if (value is DBNull)
                {
                    value = Format.GetNotNullValue(column.DataType);
                    SetValue(column.OrderIndex, value);
                }
            }
        }
    }
}
