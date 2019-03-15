using System;

namespace MyLibrary.DataBase.Orm
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DBOrmTableAttribute : Attribute
    {
        public DBOrmTableAttribute(string tableName)
        {
            TableName = tableName;
        }
        public string TableName { get; private set; }
    }
}
