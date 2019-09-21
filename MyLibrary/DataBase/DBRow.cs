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
                var column = Table.Columns[columnName];
                return this[column.OrderIndex];
            }
            set
            {
                var column = Table.Columns[columnName];
                SetValue(column, value);
            }
        }
        public object this[int columnIndex]
        {
            get => Values[columnIndex];
            set
            {
                var column = Table.Columns[columnIndex];
                SetValue(column, value);
            }
        }
        public object PrimaryKeyValue => this[Table.PrimaryKeyColumn.OrderIndex];

        public TValue Get<TValue>(string columnName)
        {
            var column = Table.Columns[columnName];
            return Get<TValue>(column.OrderIndex);
        }
        public TValue Get<TValue>(int columnIndex)
        {
            var value = Values[columnIndex];
            return Format.Convert<TValue>(value);
        }

        public string GetString(string columnName, bool allowNull = false)
        {
            var column = Table.Columns[columnName];
            return GetString(column.OrderIndex, allowNull);
        }
        public string GetString(int columnIndex, bool allowNull = false)
        {
            var value = Get<string>(columnIndex);
            if (!allowNull && value == null)
            {
                return string.Empty;
            }
            return value;
        }

        public string GetString(string columnName, string format)
        {
            var column = Table.Columns[columnName];
            return GetString(column.OrderIndex, format);
        }
        public string GetString(int columnIndex, string format)
        {
            var value = this[columnIndex];
            if (value is IFormattable formattable)
            {
                return formattable.ToString(format, null);
            }
            throw DBInternal.StringFormatException();
        }

        public bool IsNull(string columnName)
        {
            var column = Table.Columns[columnName];
            return IsNull(column.OrderIndex);
        }
        public bool IsNull(int columnIndex)
        {
            var value = Values[columnIndex];
            return Format.IsNull(value);
        }

        public void SetNotNull(string columnName)
        {
            var column = Table.Columns[columnName];
            SetNotNull(column.OrderIndex);
        }
        public void SetNotNull(int columnIndex)
        {
            var column = Table.Columns[columnIndex];
            SetNotNull(column);
        }
        public void SetNotNull()
        {
            for (var i = 0; i < Table.Columns.Count; i++)
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
                    value = Format.GetNotNullValue(column.DataType);
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
                var prevValue = Values[column.OrderIndex];
                var modified = true;
                if (value.GetType() == prevValue.GetType() && value is IComparable)
                {
                    modified = !Equals(value, prevValue);
                }
                else if (value is byte[] array && prevValue is byte[] prevArray)
                {
                    modified = !Format.IsEqualsArray(array, prevArray);
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
                var value = Values[column.OrderIndex];
                if (value is DBNull)
                {
                    value = Format.GetNotNullValue(column.DataType);
                    SetValue(column, value);
                }
            }
        }
    }
}
