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

        public int OrderIndex { get; set; }
        public string Name { get; set; }
        public string FullName => Table.Name == null ? Name : string.Concat(Table.Name, '.', Name);
        public Type DataType { get; set; }
        public bool IsPrimary { get; set; }
        public bool NotNull { get; set; }
        public object DefaultValue { get; set; } = DBNull.Value;
        public int Size { get; set; } = -1;
        public string Description { get; set; }
        public DBTable Table { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
