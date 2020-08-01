using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Указывает, что класс представляет ORM-таблицу.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DBOrmTableAttribute : Attribute
    {
        public string TableName { get; private set; }

        public DBOrmTableAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
}
