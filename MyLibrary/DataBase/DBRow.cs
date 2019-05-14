using System;
using System.Data;
using MyLibrary.Data;

namespace MyLibrary.DataBase
{
    public sealed class DBRow
    {
        internal DBRow(DBTable table)
        {
            Table = table;
            Values = new object[table.Columns.Length];
            State = DataRowState.Detached;
        }

        public DBTable Table { get; private set; }
        public bool IsNew
        {
            get
            {
                return (State == DataRowState.Added);
            }
        }
        internal DataRowState State;
        internal object[] Values;

        #region Работа с данными

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
                var index = Table.GetIndex(columnName);
                return this[index];
            }
            set
            {
                var index = Table.GetIndex(columnName);
                SetValue(index, value);
            }
        }

        public void SetNotNull(int index)
        {
            var column = Table.Columns[index];
            var value = Values[index];
            if ((value is DBNull) && !column.AllowDBNull)
            {
                value = Format.GetNotNullValue(column.DataType);
                SetValue(index, value);
            }
        }
        public void SetNotNull(string columnName)
        {
            var index = Table.GetIndex(columnName);
            SetNotNull(index);
        }
        public void SetNotNull()
        {
            for (int i = 0; i < Table.Columns.Length; i++)
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
            var index = Table.GetIndex(columnName);
            return Format.Convert<T>(Values[index]);
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
            var index = Table.GetIndex(columnName);
            return GetString(index, allowNull);
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
            var index = Table.GetIndex(columnName);
            return GetString(index, format);
        }

        public bool IsNull(int index)
        {
            return (Values[index] is DBNull);
        }
        public bool IsNull(string columnName)
        {
            var index = Table.GetIndex(columnName);
            return IsNull(index);
        }

        #endregion

        public void Delete()
        {
            State = DataRowState.Deleted;
        }

        internal void InitializeValues()
        {
            for (int i = 0; i < Values.Length; i++)
            {
                var column = Table.Columns[i];
                Values[i] = (column.IsPrimary) ? Guid.NewGuid() : column.DefaultValue;
            }
        }
        private void SetValue(int index, object value)
        {
            var column = Table.Columns[index];

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
                if (!column.AllowDBNull)
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
                if (stringValue.Length > column.MaxTextLength)
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
    }
}
