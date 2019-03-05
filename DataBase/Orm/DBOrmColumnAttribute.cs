using System;

namespace MyLibrary.DataBase.Orm
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DBOrmColumnAttribute : Attribute
    {
        public DBOrmColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
        public string ColumnName { get; private set; }
    }
}
