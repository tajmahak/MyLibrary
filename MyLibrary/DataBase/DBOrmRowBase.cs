using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Базовый класс для ORM-таблиц.
    /// </summary>
    public abstract class DBOrmRowBase
    {
        public DBRow Row { get; set; }

        public DBOrmRowBase(DBRow row)
        {
            Row = row;
        }

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
        public TValue Get<TValue>(string columnName)
        {
            return Row.Get<TValue>(columnName);
        }
        public TValue Get<TValue>(int columnIndex)
        {
            return Row.Get<TValue>(columnIndex);
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
    }

    /// <summary>
    /// Базовый класс для типизированных ORM-таблиц.
    /// </summary>
    public abstract class DBOrmRowBase<T> : DBOrmRowBase
    {
        public DBOrmRowBase(DBRow row) : base(row)
        {
        }

        public TOut Convert<TOut>(Func<DBOrmRowBase<T>, TOut> convertFunc)
        {
            return convertFunc(this);
        }
    }
}
