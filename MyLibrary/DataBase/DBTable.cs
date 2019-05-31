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
        public DBTable(DBModelBase model, string name)
        {
            Columns = new List<DBColumn>();
            Model = model;
            Name = name;
        }

        public string Name { get; private set; }
        public ReadOnlyList<DBColumn> Columns { get; private set; }
        public DBColumn PrimaryKeyColumn { get; set; }
        public DBModelBase Model { get; private set; }

        public DBRow CreateRow()
        {
            var row = new DBRow(this);
            for (int i = 0; i < row.Values.Length; i++)
            {
                var column = Columns[i];
                row.Values[i] = (column.IsPrimary) ? Guid.NewGuid() : column.DefaultValue;
            }
            return row;
        }
        public DBColumn GetColumn(string columnName)
        {
            if (!_columnsDict.TryGetValue(columnName, out var column))
            {
                throw DBInternal.UnknownColumnException(this, columnName);
            }
            return column;
        }
        protected internal void AddColumn(DBColumn column)
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

        private Dictionary<string, DBColumn> _columnsDict = new Dictionary<string, DBColumn>();
    }
}
