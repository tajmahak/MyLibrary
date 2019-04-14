using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

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

        public static int Compare<T>(T x, T y) where T : IComparable
        {
            if (IsNull(x) && IsNull(y))
                return 0;

            if (IsNull(x))
                return -1;

            if (IsNull(y))
                return 1;

            return x.CompareTo(y);
        }
        public static int Compare(object x, object y)
        {

            //if (IsNull(value1) && IsNull(value2))
            //    return 0;

            //if (IsNull(value1))
            //    return -1;

            //if (IsNull(value2))
            //    return 1;

            //var type1 = value1.GetType();
            //var type2 = value2.GetType();
            //if (value1 is IComparable && value2 is IComparable)
            //{
            //    if (ignoreCase && type1 == typeof(string) && type2 == typeof(string))
            //    {
            //        value1 = ((string)value1).ToUpperInvariant();
            //        value2 = ((string)value2).ToUpperInvariant();
            //    }
            //    return ((IComparable)value1).CompareTo(value2);
            //}
            //throw new Exception("Сравнение указанных значений невозможно.");




            return 0;
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

        public static void StableInsertionSort(this IList list, Comparison<object> comparison)
        {
            // сортировка вставками
            int count = list.Count;
            for (int j = 1; j < count; j++)
            {
                object key = list[j];

                int i = j - 1;
                for (; i >= 0 && comparison(list[i], key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }
        public static void StableInsertionSort<T>(this IList<T> list, Comparison<T> comparison)
        {
            StableInsertionSort((IList)list, (x, y) => comparison((T)x, (T)y));
        }
        public static void StableInsertionSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            StableInsertionSort(list, (x, y) => comparer.Compare(x, y));
        }
        public static void StableInsertionSort<T>(this IList<T> list) where T : IComparable
        {
            StableInsertionSort(list, (x, y) => x.CompareTo(y));
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

        public static void SetValue(object obj, string memberName, object value)
        {
            var type = obj.GetType();
            MemberInfo[] members;
            while (true)
            {
                members = type.GetMember(memberName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (members.Length == 0 && type.BaseType != null)
                {
                    type = type.BaseType;
                    continue;
                }
                break;
            }

            if (members.Length != 1)
                throw new NotImplementedException();

            var member = members[0];
            if (member is FieldInfo)
            {
                var field = member as FieldInfo;
                field.SetValue(obj, value);
            }
            else if (member is PropertyInfo)
            {
                var property = member as PropertyInfo;
                property.SetValue(obj, value, null);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}