namespace MyLibrary.DataBase
{
    /// <summary>
    /// Базовый класс для ORM-таблиц.
    /// </summary>
    public abstract class DBOrmTableBase
    {
        public DBRow Row { get; set; }

        public DBOrmTableBase(DBRow row)
        {
            Row = row;
        }

        public void Delete() => Row.Delete();
        public void SetNotNull(int index) => Row.SetNotNull(index);
        public void SetNotNull(string columnName) => Row.SetNotNull(columnName);
        public void SetNotNull() => Row.SetNotNull();
    }
}
