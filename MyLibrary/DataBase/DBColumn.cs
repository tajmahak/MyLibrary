using System;

namespace MyLibrary.DataBase
{
    public sealed class DBColumn
    {
        public string Name { get; set; }
        public Type DataType { get; set; }
        public bool IsPrimary { get; set; }
        public bool AllowDBNull { get; set; }
        public object DefaultValue { get; set; }
        public int MaxTextLength { get; set; }
        public string Comment { get; set; }
        public DBTable Table { get; private set; }

        public DBColumn(DBTable table)
        {
            Table = table;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
