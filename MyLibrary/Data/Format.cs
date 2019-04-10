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
        public static T Convert<T>(object value)
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
                value = default(T);
            }
            else if (value.GetType() != type)
            {
                value = System.Convert.ChangeType(value, type);
            }

            return (T)value;
        }
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
        public static string GetNotEmptyString(object value)
        {
            string sValue = Convert<string>(value);
            if (IsNull(value))
            {
                return string.Empty;
            }
            return sValue;
        }
        public static int Compare<T>(T value1, T value2) where T : IComparable
        {
            if (IsNull(value1) && IsNull(value2))
                return 0;

            if (IsNull(value1))
                return -1;

            if (IsNull(value2))
                return 1;

            return value1.CompareTo(value2);
        }
        public static bool IsEquals<T>(T value1, T value2) where T : IEquatable<T>
        {
            if (value1 == null && value2 == null)
                return true;

            if (value1 == null || value2 == null)
                return false;

            var type = typeof(T);
            if (type.BaseType == typeof(Array))
            {
                return ArrayEquals((T[])((object)value1), (T[])((object)value2));
            }
            return object.Equals(value1, value2);
        }
        public static bool ArrayEquals<T>(T[] blob1, T[] blob2) where T : IEquatable<T>
        {
            if (blob1.Length != blob2.Length)
                return false;

            int length = blob1.Length;
            for (int i = 0; i < length; ++i)
            {
                if (!blob1[i].Equals(blob2[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsContains(string value1, string value2, bool ignoreCase = false)
        {
            if (ignoreCase)
            {
                value1 = value1.ToUpperInvariant();
                value2 = value2.ToUpperInvariant();
            }
            return value1.Contains(value2);
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

        /// <summary>
        /// Округляет десятичное значение до ближайшего целого.Параметр задает правило округления значения, если оно находится ровно посредине между двумя другими числами
        /// </summary>
        /// <param name="value">Округляемое число</param>
        /// <param name="decimals">Значение, задающее правило округления, если его значение находится ровно посредине между двумя другими числами</param>
        /// <param name="midpointRounding">Значение, задающее правило округления параметра value, если его значение находится ровно посредине между двумя другими числами</param>
        /// <returns></returns>
        public static decimal RoundDigit(object value, int decimals = 0, MidpointRounding midpointRounding = MidpointRounding.ToEven)
        {
            decimal digit = Convert<decimal>(value);
            return Math.Round(digit, decimals, midpointRounding);
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