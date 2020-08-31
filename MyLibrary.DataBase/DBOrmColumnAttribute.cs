using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Указывает, что класс представляет столбец для таблицы <see cref="DBOrmTableAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DBOrmColumnAttribute : Attribute
    {
        public DBOrmColumnAttribute(string columnName, bool NotNull = false, bool PrimaryKey = false, string ForeignKey = null)
        {
            ColumnName = columnName;
            this.NotNull = NotNull;
            this.PrimaryKey = PrimaryKey;
            this.ForeignKey = ForeignKey;
        }

        public string ColumnName { get; set; }
        public bool NotNull { get; set; }
        public bool PrimaryKey { get; set; }
        public string ForeignKey { get; set; }
    }
}
