using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет схему столбца в таблице <see cref="DBTable"/>.
    /// </summary>
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
        public string Description { get; set; }
        public DBTable Table { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
