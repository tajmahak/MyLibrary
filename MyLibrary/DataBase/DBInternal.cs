using System;
using System.Linq.Expressions;
using MyLibrary.DataBase.Orm;

namespace MyLibrary.DataBase
{
    internal static class DBInternal
    {
        public static T ConvertValue<T>(object value)
        {
            if (value is DBNull || value == null)
                return default(T);

            var type = typeof(T);
            if (value.GetType() == type)
                return (T)value;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = Nullable.GetUnderlyingType(type);
            else if (type.BaseType == typeof(Enum))
                type = Enum.GetUnderlyingType(type);

            return (T)Convert.ChangeType(value, type);
        }
        public static bool EqualsBlob(byte[] blobA, byte[] blobB)
        {
            if (blobA == null && blobB == null) return true;
            if (blobA == null || blobB == null) return false;
            if (blobA.Length != blobB.Length) return false;

            int length = blobA.Length;
            for (int i = 0; i < length; i++)
                if (blobA[i] != blobB[i])
                    return false;
            return true;
        }
        public static T PackRow<T>(object value)
        {
            if (typeof(T) == typeof(DBRow))
                return (T)value;
            return (T)Activator.CreateInstance(typeof(T), value);
        }
        public static DBRow UnpackRow(object value)
        {
            if (value == null)
                return null;
            if (value is DBOrmTableBase)
                return (value as DBOrmTableBase).Row;
            return (DBRow)value;
        }
        public static string GetTableNameFromAttribute(Type type)
        {
            var attrArray = type.GetCustomAttributes(typeof(DBOrmTableAttribute), false);
            if (attrArray.Length == 0)
            {
                throw DBInternal.OrmTableNotAttributeException(type);
            }
            var attr = (DBOrmTableAttribute)attrArray[0];
            return attr.TableName;
        }

        #region NameOf

        private static String NameOf<T, TT>(this Expression<Func<T, TT>> accessor)
        {
            return NameOf(accessor.Body);
        }
        private static String NameOf<T>(this Expression<Func<T>> accessor)
        {
            return NameOf(accessor.Body);
        }
        private static String NameOf<T, TT>(this T obj, Expression<Func<T, TT>> propertyAccessor)
        {
            return NameOf(propertyAccessor.Body);
        }
        private static String NameOf(Expression expression)
        {
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = expression as MemberExpression;
                if (memberExpression == null)
                    return null;
                return memberExpression.Member.Name;
            }
            return null;
        }

        #endregion
        public static Exception ArgumentNullException<T>(Expression<Func<T>> accessor)
        {
            throw new ArgumentNullException(NameOf(accessor));
        }
        public static Exception UnknownTableException(string tableName)
        {
            return new Exception(string.Format("Неизвестная таблица \"{0}\"", tableName));
        }
        public static Exception UnknownColumnException(DBTable table, string columnName)
        {
            string text;
            if (table != null)
                text = string.Format("Таблица \"{0}\" - неизвестный столбец \"{1}\"", table.Name, columnName);
            else text = string.Format("Неизвестный столбец \"{0}\"", table.Name, columnName);
            return new Exception(text);
        }
        public static Exception DataConvertException(DBColumn column, object value, Exception innerException)
        {
            return new Exception(string.Format("{1}: приведение из \"{2}\" в \"{3}\" невозможно",
                column.Name,
                column.DataType.Name,
                value.GetType().Name),
                innerException);
        }
        public static Exception SqlExecuteException()
        {
            return new Exception("SQL-команда не может быть выполнена в текущем контексте");
        }
        public static Exception ProcessRowException()
        {
            return new Exception("Обработка строки невозможна");
        }
        public static Exception StringFormatException()
        {
            return new Exception("Невозможно привести значение к форматированной строке");
        }
        public static Exception ProcessViewException()
        {
            return new Exception("Обработка представления невозможна");
        }
        public static Exception DbSaveException(DBRow row, Exception ex)
        {
            if (row == null)
                return ex;
            throw new Exception(string.Format("Ошибка сохранения БД. \"{0}\" - {1}", row.Table.Name, ex.Message), ex);
        }
        public static Exception DbSaveWrongRelationsException()
        {
            throw new Exception("Неверные связи между строками");
        }
        public static Exception StringOverflowException(DBColumn column)
        {
            return new Exception(string.Format("\"{0}\": длина строки превышает допустимую длину", column.Name));
        }
        public static Exception GenerateSetIDException(DBColumn column)
        {
            return new Exception("Невозможно изменить значение первичного ключа");
        }
        public static Exception InadequateInsertCommandException()
        {
            return new Exception("Insert-команда не содержит ни одного 'Set'");
        }
        public static Exception InadequateUpdateCommandException()
        {
            return new Exception("Update-команда не содержит ни одного 'Set'");
        }
        public static Exception UnsupportedCommandContextException()
        {
            throw new Exception("Недопустимая операция в текущем контексте команды");
        }
        public static Exception NotFindRowException()
        {
            throw new Exception("Не найдено ни одной строки");
        }
        public static Exception RowDeleteException()
        {
            return new Exception("Невозможно удалить строку, т.к. нет привязки к DBSet");
        }
        public static Exception ParameterValuePairException()
        {
            return new Exception("Неверно заданы параметры запроса");
        }
        public static Exception OrmTableNotAttributeException(Type type)
        {
            return new Exception(type.FullName + " - отсутствует атрибут имени таблицы");
        }
    }
}
