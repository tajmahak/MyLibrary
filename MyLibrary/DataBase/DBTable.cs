using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public sealed class DBTable
    {
        public string Name { get; private set; }
        public DBColumn[] Columns { get; private set; }
        public int PrimaryKeyIndex { get; private set; }
        public DBModelBase Model { get; private set; }
        private Dictionary<string, int> ColumnIndexDict;

        public DBTable(DBModelBase model, string name)
        {
            Model = model;
            Name = name;
        }
        public int GetIndex(string columnName)
        {
            int index;
            if (!ColumnIndexDict.TryGetValue(columnName, out index))
                throw DBInternal.UnknownColumnException(this, columnName);
            return index;
        }

        internal void AddColumns(DBColumn[] columns)
        {
            Columns = columns;
            ColumnIndexDict = new Dictionary<string, int>(columns.Length);
            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];

                string columnName;
                if (column.Table.Name == null)
                    columnName = column.Name;
                else columnName = string.Concat(column.Table.Name, '.', column.Name);

                if (!ColumnIndexDict.ContainsKey(columnName))
                {
                    ColumnIndexDict.Add(columnName, i);
                }
                else
                {
                    int index = 1;
                    while (true)
                    {
                        var tempColumnName = string.Concat(columnName, '_', index++);
                        if (!ColumnIndexDict.ContainsKey(tempColumnName))
                        {
                            ColumnIndexDict.Add(tempColumnName, i);
                            break;
                        }
                    }
                }
                if (column.IsPrimary)
                    PrimaryKeyIndex = i;
            }
            if (Name == null)
                PrimaryKeyIndex = -1;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
