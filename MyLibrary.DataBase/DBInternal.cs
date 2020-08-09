using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Предоставляет статические методы для внутреннего использования в пространстве имён <see cref="DataBase"/>.
    /// </summary>
    internal static class DBInternal
    {
        public static TRow CreateOrmRow<TRow>(DBRow row) where TRow : DBOrmRow
        {
            return (TRow)Activator.CreateInstance(typeof(TRow), row);
        }

        public static DBRow ExtractDBRow(DBOrmRow row)
        {
            if (row == null)
            {
                return null;
            }
            if (row is DBOrmRow ormRow)
            {
                return ormRow.Row;
            }
            throw DBExceptionFactory.ExtractDBRowException(row.GetType());
        }

        public static string GetTableNameFromAttribute(Type type)
        {
            object[] attrArray = type.GetCustomAttributes(typeof(DBOrmTableAttribute), true);
            if (attrArray.Length == 0)
            {
                throw DBExceptionFactory.OrmTableNotAttributeException(type);
            }
            return ((DBOrmTableAttribute)attrArray[0]).TableName;
        }

        public static string[] GetForeignKey(Type type1, Type type2)
        {
            string table = GetTableNameFromAttribute(type2);
            foreach (System.Reflection.PropertyInfo property in type1.GetProperties())
            {
                foreach (DBOrmColumnAttribute attribute in property.GetCustomAttributes(typeof(DBOrmColumnAttribute), false))
                {
                    if (attribute.ForeignKey != null)
                    {
                        string[] split = attribute.ForeignKey.Split('.');
                        if (split[0] == table)
                        {
                            return new string[] { attribute.ColumnName, attribute.ForeignKey };
                        }
                    }
                }
            }

            table = GetTableNameFromAttribute(type1);
            foreach (System.Reflection.PropertyInfo property in type2.GetProperties())
            {
                foreach (DBOrmColumnAttribute attribute in property.GetCustomAttributes(typeof(DBOrmColumnAttribute), false))
                {
                    if (attribute.ForeignKey != null)
                    {
                        string[] split = attribute.ForeignKey.Split('.');
                        if (split[0] == table)
                        {
                            return new string[] { attribute.ForeignKey, attribute.ColumnName };
                        }
                    }
                }
            }

            return null;
        }
    }
}
