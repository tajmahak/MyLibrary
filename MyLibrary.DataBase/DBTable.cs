namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет схему таблицы БД.
    /// </summary>
    public sealed class DBTable
    {
        public string Name { get; set; }
        public DBColumnCollection Columns { get; private set; } = new DBColumnCollection();
        public DBIndexCollection Indexes { get; private set; } = new DBIndexCollection();
        public DBColumn PrimaryKeyColumn { get; set; }

        public DBRow CreateRow()
        {
            DBRow row = new DBRow(this);
            for (int i = 0; i < row.Values.Length; i++)
            {
                DBColumn column = Columns[i];
                row.Values[i] = column.IsPrimary ? new DBTempId() : column.DefaultValue;
            }
            return row;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
