using System;

namespace MyLibrary.DataBase
{
    /// <summary>
    /// Предоставляет статические методы для внутреннего использования в пространстве имён <see cref="DataBase"/>.
    /// </summary>
    internal static class DBInternal
    {
        public static T PackRow<T>(object row)
        {
            if (typeof(T) == typeof(DBRow))
            {
                return (T)row;
            }
            return (T)Activator.CreateInstance(typeof(T), row);
        }
        public static DBRow UnpackRow(object row)
        {
            if (row == null)
            {
                return null;
            }
            if (row is DBOrmTableBase)
            {
                return (row as DBOrmTableBase).Row;
            }
            return (DBRow)row;
        }
        public static string GetTableNameFromAttribute(Type type)
        {
            DBOrmTableAttribute attribute = null;
            while (true)
            {
                var attrArray = type.GetCustomAttributes(typeof(DBOrmTableAttribute), false);
                if (attrArray.Length == 0)
                {
                    type = type.BaseType;
                    if (type == null)
                    {
                        throw OrmTableNotAttributeException(type);
                    }
                    continue;
                }
                attribute = (DBOrmTableAttribute)attrArray[0];
                break;
            }
            return attribute.TableName;
        }
        public static string[] GetForeignKey(Type type1, Type type2)
        {
            var table = GetTableNameFromAttribute(type2);
            foreach (var property in type1.GetProperties())
            {
                foreach (DBOrmColumnAttribute attribute in property.GetCustomAttributes(typeof(DBOrmColumnAttribute), false))
                {
                    if (attribute.ForeignKey != null)
                    {
                        var split = attribute.ForeignKey.Split('.');
                        if (split[0] == table)
                        {
                            return new string[] { attribute.ColumnName, attribute.ForeignKey };
                        }
                    }
                }
            }

            table = GetTableNameFromAttribute(type1);
            foreach (var property in type2.GetProperties())
            {
                foreach (DBOrmColumnAttribute attribute in property.GetCustomAttributes(typeof(DBOrmColumnAttribute), false))
                {
                    if (attribute.ForeignKey != null)
                    {
                        var split = attribute.ForeignKey.Split('.');
                        if (split[0] == table)
                        {
                            return new string[] { attribute.ForeignKey, attribute.ColumnName };
                        }
                    }
                }
            }

            return null;
        }

        public static Exception ArgumentNullException(string argumentName)
        {
            throw new ArgumentNullException(argumentName);
        }
        public static Exception UnknownTableException(string tableName)
        {
            return new Exception(string.Format("Неизвестная таблица \"{0}\".", tableName));
        }
        public static Exception UnknownColumnException(DBTable table, string columnName)
        {
            string text;
            if (table != null)
            {
                text = string.Format("Таблица \"{0}\" - неизвестный столбец \"{1}\".", table.Name, columnName);
            }
            else
            {
                text = string.Format("Неизвестный столбец \"{0}\".", table.Name);
            }

            return new Exception(text);
        }
        public static Exception DataConvertException(DBColumn column, object value, Exception innerException)
        {
            return new Exception(string.Format("{0}: приведение из \"{1}\" в \"{2}\" невозможно.",
                column.Name,
                column.DataType.Name,
                value.GetType().Name),
                innerException);
        }
        public static Exception SqlExecuteException()
        {
            return new Exception("SQL-команда не может быть выполнена в текущем контексте.");
        }
        public static Exception ProcessRowException()
        {
            return new Exception("Обработка строки невозможна.");
        }
        public static Exception StringFormatException()
        {
            return new Exception("Невозможно привести значение к форматированной строке.");
        }
        public static Exception ProcessViewException()
        {
            return new Exception("Обработка представления невозможна.");
        }
        public static Exception DbSaveException(DBRow row, Exception ex)
        {
            if (row == null)
            {
                return ex;
            }

            throw new Exception(string.Format("Ошибка сохранения БД. \"{0}\" - {1}.", row.Table.Name, ex.Message), ex);
        }
        public static Exception DbSaveWrongRelationsException()
        {
            throw new Exception("Неверные связи между строками.");
        }
        public static Exception StringOverflowException(DBColumn column)
        {
            return new Exception(string.Format("\"{0}\": длина строки превышает допустимую длину.", column.Name));
        }
        public static Exception GenerateSetIDException(DBColumn column)
        {
            return new Exception("Невозможно изменить значение первичного ключа.");
        }
        public static Exception WrongInsertCommandException()
        {
            return new Exception("Insert-команда не содержит ни одного 'Set'.");
        }
        public static Exception WrongUpdateCommandException()
        {
            return new Exception("Update-команда не содержит ни одного 'Set'.");
        }
        public static Exception UnsupportedCommandContextException()
        {
            throw new Exception("Недопустимая операция в текущем контексте команды.");
        }
        public static Exception NotFindRowException()
        {
            throw new Exception("Не найдено ни одной строки.");
        }
        public static Exception RowDeleteException()
        {
            return new Exception("Невозможно удалить строку, т.к. нет привязки к DBSet.");
        }
        public static Exception ParameterValuePairException()
        {
            return new Exception("Неверно заданы параметры запроса.");
        }
        public static Exception OrmTableNotAttributeException(Type type)
        {
            return new Exception(type.FullName + " - отсутствует атрибут таблицы.");
        }
        public static Exception DBFunctionException()
        {
            return new Exception("Функции класса " + nameof(DBFunction) + " не могут быть вызваны напрямую.");
        }
        public static Exception ForeignKeyException()
        {
            return new Exception("Отсутствует внешний ключ.");
        }
        public static Exception ContextInitializeException()
        {
            return new Exception("Экземпляр " + nameof(DBContext) + " уже был инициализирован.");
        }
        public static Exception GetDefaultSqlQueryException(DBTable table)
        {
            return new Exception("Невозможно получить SQL-команду для таблицы '" + table.Name + "', т.к. в ней отсутствует первичный ключ.");
        }
    }
}
