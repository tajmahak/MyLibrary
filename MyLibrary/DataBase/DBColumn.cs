﻿using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Представляет схему столбца в таблице <see cref="DBTable"/>.
    /// </summary>
    public sealed class DBColumn
    {
        public int OrderIndex { get; set; }
        public string Name { get; set; }
        public Type DataType { get; set; }
        public bool IsPrimary { get; set; }
        public bool NotNull { get; set; }
        public object DefaultValue { get; set; } = DBNull.Value;
        public int Size { get; set; } = -1;
        public string Description { get; set; }
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
