using System;

namespace MyLibrary.DataBase
{
    public sealed class DBColumn
    {
        public DBColumn(DBTable table)
        {
            Table = table;
        }

        public int Index { get; set; }
        public string Name { get; set; }
        public Type DataType { get; set; }
        public bool IsPrimary { get; set; }
        public bool AllowDBNull { get; set; }
        public object DefaultValue { get; set; } = DBNull.Value;
        public int Size { get; set; }
        public string Comment { get; set; }
        public DBTable Table { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
