using System;

namespace MyLibrary.DataBase
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DBOrmColumnAttribute : Attribute
    {
        public DBOrmColumnAttribute(string columnName, bool AllowDbNull = true, bool PrimaryKey = false, string ForeignKey = null)
        {
            ColumnName = columnName;
            this.AllowDbNull = AllowDbNull;
            this.PrimaryKey = PrimaryKey;
            this.ForeignKey = ForeignKey;
        }

        public string ColumnName { get; set; }
        public bool AllowDbNull { get; set; }
        public bool PrimaryKey { get; set; }
        public string ForeignKey { get; set; }
    }
}
