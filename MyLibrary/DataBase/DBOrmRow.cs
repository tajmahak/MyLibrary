namespace MyLibrary.DataBase
{
    /// <summary>
    /// Базовый класс для ORM-таблиц.
    /// </summary>
    public abstract class DBOrmRow
    {
        public DBRow Row { get; set; }

        public DBOrmRow(DBRow row)
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
    public abstract class DBOrmRow<TRow> : DBOrmRow where TRow : DBOrmRow
    {
        public DBOrmRow(DBRow row) : base(row)
        {
        }
    }
}
