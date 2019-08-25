using MyLibrary.Collections;
using System;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет схему таблицы БД.
    /// </summary>
    public sealed class DBTable
    {
        public string Name { get; set; }
        public ReadOnlyList<DBColumn> Columns { get; private set; } = new List<DBColumn>();
        public ReadOnlyList<DBIndex> Indexes { get; private set; } = new List<DBIndex>();
        public DBColumn PrimaryKeyColumn { get; set; }
        public DBModelBase Model { get; private set; }
        private readonly Dictionary<string, DBColumn> _columnsDict = new Dictionary<string, DBColumn>();

        public DBTable(DBModelBase model)
        {
            Model = model;
        }

        public DBColumn this[int index] => Columns[index];
        public DBColumn this[string columnName]
        {
            get
            {
                if (!_columnsDict.TryGetValue(columnName, out var column))
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
        public void AddColumn(DBColumn column)
        {
            string columnName;
            if (column.Table.Name == null)
            {
                columnName = column.Name;
            }
            else
            {
                columnName = string.Concat(column.Table.Name, '.', column.Name);
            }

            Columns.List.Add(column);
            if (!_columnsDict.ContainsKey(columnName))
            {
                _columnsDict.Add(columnName, column);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
