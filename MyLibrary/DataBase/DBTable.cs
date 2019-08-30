using System;

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
        public DBModelBase Model { get; private set; }

        public DBTable(DBModelBase model)
        {
            Model = model;
        }

        public DBColumn this[int index] => Columns[index];
        public DBColumn this[string columnName]
        {
            get
            {
                var column = Columns[columnName];
                if (column == null)
                {
                    throw DBInternal.UnknownColumnException(this, columnName);
                }
                return column;
            }
        }

        public DBRow CreateRow()
        {
            var row = new DBRow(this);
            for (var i = 0; i < row.Values.Length; i++)
            {
                var column = Columns[i];
                row.Values[i] = (column.IsPrimary) ? Guid.NewGuid() : column.DefaultValue;
            }
            return row;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
