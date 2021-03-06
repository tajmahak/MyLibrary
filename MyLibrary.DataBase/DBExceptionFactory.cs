﻿using System;

namespace MyLibrary.DataBase
{
    public class DBExceptionFactory
    {
        public static Exception ArgumentNullException(string argumentName)
        {
            return new ArgumentNullException(argumentName);
        }

        public static Exception UnknownTableException(string tableName)
        {
            return new Exception($"Неизвестная таблица \"{tableName}\".");
        }

        public static Exception UnknownColumnException(DBTable table, string columnName)
        {
            string text;
            if (table != null)
            {
                text = $"Таблица \"{table.Name}\" - неизвестный столбец \"{columnName}\".";
            }
            else
            {
                text = $"Неизвестный столбец \"{table.Name}\".";
            }

            return new Exception(text);
        }

        public static Exception DataConvertException(DBColumn column, object value, Exception innerException)
        {
            return new Exception($"{column.Name}: приведение из '{value.GetType().Name}' в '{column.DataType.Name}' невозможно.",
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
            return new Exception($"Ошибка сохранения БД. '{row.Table.Name}' - {ex.Message}.", ex);
        }

        public static Exception DbSaveWrongRelationsException()
        {
            return new Exception("Неверные связи между строками.");
        }

        public static Exception StringOverflowException(DBColumn column)
        {
            return new Exception($"'{column.FullName}': длина строки превышает допустимую длину.");
        }

        public static Exception GenerateSetIDException(DBColumn column)
        {
            return new Exception($"'{column.FullName}' - невозможно изменить значение первичного ключа.");
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
            return new Exception("Недопустимая операция в текущем контексте команды.");
        }

        public static Exception ParameterValuePairException()
        {
            return new Exception("Неверно заданы параметры запроса.");
        }

        public static Exception OrmTableNotAttributeException(Type type)
        {
            return new Exception($"'{type.FullName}' - отсутствует атрибут таблицы.");
        }

        public static Exception DBFunctionException()
        {
            return new Exception($"Функции класса {nameof(DBFunction)} не могут быть вызваны напрямую.");
        }

        public static Exception ForeignKeyException()
        {
            return new Exception("Отсутствует внешний ключ.");
        }

        public static Exception GetDefaultSqlQueryException(DBTable table)
        {
            return new Exception($"Невозможно получить SQL-команду для таблицы '{table.Name}', т.к. в ней отсутствует первичный ключ.");
        }

        public static Exception ExtractDBRowException(Type type)
        {
            return new Exception($"'{type.Name}' - невозможно извлечь строку.");
        }
    }
}
