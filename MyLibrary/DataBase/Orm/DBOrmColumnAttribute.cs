using System;

namespace MyLibrary.DataBase.Orm
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DBOrmColumnAttribute : Attribute
    {
        public DBOrmColumnAttribute(string columnName, string foreignKey = null)
        {
            ColumnName = columnName;
            ForeignKey = foreignKey;
        }
        public string ColumnName { get; private set; }
        public string ForeignKey { get; private set; }
    }
}
