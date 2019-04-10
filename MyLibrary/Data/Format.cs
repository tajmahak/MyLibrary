using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MyLibrary.Data
{
    /// <summary>
    /// Представляет набор методов для работы с данными
    /// </summary>
    public static class Format
    {
        /// <summary>
        /// Преобразование объекта в заданный тип
        /// </summary>
        /// <typeparam name="T">Тип выходных данных</typeparam>
        /// <param name="value">Исходный объект</param>
        /// <param name="allowNullString">Указывает, допускается ли получение пустого объекта типа String со значением null</param>
        /// <returns></returns>
        public static T Convert<T>(object value, bool allowNullString = true)
        {
            var type = typeof(T);

            // определение основного типа данных
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }
            else if (type.BaseType == typeof(Enum))
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (IsNull(value))
            {
                if (!allowNullString && type == typeof(string))
                {
                    value = string.Empty;
                }
                else
                {
                    value = default(T);
                }
            }
            else if (value.GetType() != type)
            {
                value = System.Convert.ChangeType(value, type);
            }

            return (T)value;
        }
        /// <summary>
        /// Получение объекта типа String, не допускающего значения null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetNotEmptyString(object value)
        {
            return Convert<string>(value, false);
        }
        /// <summary>
        /// Получение объекта заданного типа, не допускающего значения null
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetNotNullValue(Type type)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }

            if (type.BaseType == typeof(Array))
            {
                return Activator.CreateInstance(type, 0);
            }

            return Activator.CreateInstance(type);
        }
        /// <summary>
        /// Выполняет преобразование исходного списка с использованием заданной функции преобразования элементов
        /// </summary>
        /// <typeparam name="T1">Исходный тип списка</typeparam>
        /// <typeparam name="T2">Конечный тип списка</typeparam>
        /// <param name="srcList">Исходный список</param>
        /// <param name="convertFunc">Функция преобразования элементов</param>
        /// <returns></returns>
        public static List<T2> ConvertList<T1, T2>(List<T1> srcList, Func<T1, T2> convertFunc)
        {
            var destList = new List<T2>(srcList.Count);
            foreach (var srcItem in srcList)
            {
                var destItem = convertFunc(srcItem);
                destList.Add(destItem);
            }
            return destList;
        }


        public static int Compare<T>(T value1, T value2, bool ignoreCase = false) where T : IComparable
        {
            if (IsNull(value1) && IsNull(value2))
                return 0;

            if (IsNull(value1))
                return -1;

            if (IsNull(value2))
                return 1;

            if (ignoreCase)
            {
                var type1 = value1.GetType();
                var type2 = value2.GetType();
                if (type1 == typeof(string) && type2 == typeof(string))
                {
                    //!!!
                    //value1 = ((string)value1).ToUpperInvariant();
                    //value2 = ((string)value2).ToUpperInvariant();
                }
            }
            return value1.CompareTo(value2);
        }

        public static bool IsEquals(object value1, object value2, bool ignoreCase = false)
        {
            if (value1 == null && value2 == null)
                return true;

            if (value1 == null || value2 == null)
                return false;

            var type1 = value1.GetType();
            var type2 = value2.GetType();

            #region Приведение типов из Enum

            if (type1.BaseType == typeof(Enum))
            {
                type1 = Enum.GetUnderlyingType(type1);
                value1 = System.Convert.ChangeType(value1, type1);
            }

            if (type2.BaseType == typeof(Enum))
            {
                type2 = Enum.GetUnderlyingType(type2);
                value2 = System.Convert.ChangeType(value2, type2);
            }

            #endregion

            if (type1 != type2)
            {
                if ((type1.IsPrimitive || type1 == typeof(decimal)) && (type2.IsPrimitive || type2 == typeof(decimal)))
                {
                    value1 = System.Convert.ToDecimal(value1);
                    value2 = System.Convert.ToDecimal(value2);
                }
                else
                {
                    value1 = value1.ToString();
                    value2 = value2.ToString();
                }
            }
            else if (ignoreCase && type1 == typeof(string))
            {
                value1 = ((string)value1).ToUpperInvariant();
                value2 = ((string)value2).ToUpperInvariant();
            }
            else if (type1 == typeof(byte[]))
            {
                return BlobEquals((byte[])value1, (byte[])value2);
            }
            return object.Equals(value1, value2);
        }
        public static bool BlobEquals<T>(T[] blob1, T[] blob2) where T : IEquatable<T>
        {
            if (blob1.Length != blob2.Length)
                return false;

            int length = blob1.Length;
            for (int i = 0; i < length; i++)
            {
                if (!blob1[i].Equals(blob2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsContains(object value1, object value2, bool ignoreCase = false)
        {
            if (value1 is string && value2 is string)
            {
                if (ignoreCase)
                {
                    value1 = ((string)value1).ToUpperInvariant();
                    value2 = ((string)value2).ToUpperInvariant();
                }
                return ((string)value1).Contains((string)value2);
            }
            throw new Exception("Операция проверки содержимого не выполнима.");
        }

        public static bool IsNull(object value)
        {
            return (value == null || value is DBNull);
        }
        public static bool IsEmpty(object value)
        {
            if (IsNull(value))
                return true;

            if (value is string && string.IsNullOrEmpty((string)value))
                return true;

            return false;
        }
        public static bool HasFlag<T>(T value, T flag)
        {
            ulong uValue = System.Convert.ToUInt64(value);
            ulong uFlag = System.Convert.ToUInt64(flag);
            return ((uValue & uFlag) == uFlag);
        }

        public static void StableSort<T>(T[] array)
        {
            StableSort(array, null);
        }
        public static void StableSort<T>(T[] array, Comparison<T> comparison)
        {
            //if (comparison == null)
            //    comparison = (x, y) => ((IComparable)x).CompareTo(y);

            //!!! не реализовано
            Array.Sort<T>(array, comparison);
        }
        public static void StableSort<T>(List<T> list)
        {
            StableSort(list, null);
        }
        public static void StableSort<T>(List<T> list, Comparison<T> comparison)
        {
            //if (comparison == null)
            //    comparison = (x, y) => ((IComparable)x).CompareTo(y);

            //!!! не реализовано
            list.Sort(comparison);
        }

        public static decimal RoundDigit(object value, int decimals = 0)
        {
            decimal digit = Convert<decimal>(value);
            return Math.Round(digit, decimals);
        }
        public static object RoundValue(object value, int decimals = 0)
        {
            if (IsEmpty(value))
                return null;

            decimal digit = Convert<decimal>(value);
            return Math.Round(digit, decimals);
        }

        public static string FormatString<T>(T value, string format) where T : IFormattable
        {
            return value.ToString(format, null);
        }
        public static string FormatDigit(object value, int decimals = 0, bool allowNull = false)
        {
            if (allowNull && IsNull(value))
                return null;

            if (IsNull(value))
                value = decimal.Zero;

            string text = System.Convert.ToDecimal(value).ToString("N" + decimals);
            if (text.Length > 0)
            {
                if (text[0] == '-')
                {
                    // вставка пробела между минусом и значением в отрицательном числе
                    text = text.Insert(1, "\u00A0");
                }
            }
            return text;
        }

        public static string[] Split(string value, params string[] values)
        {
            return value.Split(values, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string FormatFileSize(long value)
        {
            string[] sizes = { "б", "Кб", "Мб", "Гб", "Тб" };
            int order = 0;
            var len = value;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            decimal val = value / (decimal)Math.Pow(1024, order);
            var text = string.Format("{0:0.00} {1}", val, sizes[order]).Replace(',', '.');
            return text;
        }





        /// <summary>
        /// Получение имени экземпляра объекта
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="accessor">Функция для передачи экземпляра объекта. Задаётся: () => member</param>
        /// <returns></returns>
        public static String NameOf<T>(Expression<Func<T>> accessor)
        {
            Expression expression = accessor.Body;
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = expression as MemberExpression;
                if (memberExpression == null)
                    return null;
                return memberExpression.Member.Name;
            }
            return null;
        }
    }
}