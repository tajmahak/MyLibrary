using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
        public static List<TOut> ConvertList<TIn, TOut>(IList<TIn> srcList, Func<TIn, TOut> convertFunc)
        {
            var destList = new List<TOut>(srcList.Count);
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
            if (IsNull(x) && IsNull(y))
                return 0;

            if (IsNull(x))
                return -1;

            if (IsNull(y))
                return 1;

            var type1 = x.GetType();
            var type2 = y.GetType();
            if (x is IComparable && y is IComparable)
            {
                return ((IComparable)x).CompareTo(y);
            }
            throw new Exception("Сравнение указанных значений невозможно.");
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

        public static bool IsEquals<T>(T x, T y) where T : IEquatable<T>
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            var type = typeof(T);
            if (type.BaseType == typeof(Array))
            {
                return ArrayEquals((T[])((object)x), (T[])((object)y));
            }
            return object.Equals(x, y);
        }
        public static bool IsEquals(object x, object y)
        {
            if (IsNull(x) && IsNull(y))
                return true;

            if (IsNull(x) || IsNull(y))
                return false;

            var type1 = x.GetType();
            var type2 = y.GetType();

            #region Приведение типов из Enum

            if (type1.BaseType == typeof(Enum))
            {
                type1 = Enum.GetUnderlyingType(type1);
                x = System.Convert.ChangeType(x, type1);
            }

            if (type2.BaseType == typeof(Enum))
            {
                type2 = Enum.GetUnderlyingType(type2);
                y = System.Convert.ChangeType(y, type2);
            }

            #endregion

            if (type1 != type2)
            {
                if ((type1.IsPrimitive || type1 == typeof(decimal)) && (type2.IsPrimitive || type2 == typeof(decimal)))
                {
                    x = System.Convert.ToDecimal(x);
                    y = System.Convert.ToDecimal(y);
                }
                else
                {
                    x = x.ToString();
                    y = y.ToString();
                }
            }

            if (type1 == typeof(byte[]))
            {
                return ArrayEquals((byte[])x, (byte[])y);
            }
            return object.Equals(x, y);
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

        public static string GetNotEmptyString(object value)
        {
            string sValue = Convert<string>(value);
            if (IsNull(value))
            {
                return string.Empty;
            }
            return sValue;
        }
        public static string GetIgnoreCaseString(object value)
        {
            var sValue = GetNotEmptyString(value);
            return sValue.ToUpperInvariant();
        }
        public static bool IsContainsString(string value1, string value2, bool ignoreCase = false)
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
            {
                return true;
            }
            if (value is string @string)
            {
                return string.IsNullOrEmpty(@string);
            }
            if (value is StringBuilder stringBuilder)
            {
                return stringBuilder.Length == 0;
            }
            return false;
        }

        /// <summary>
        /// Округляет десятичное значение до ближайшего целого. Параметр задает правило округления значения, если оно находится ровно посредине между двумя другими числами
        /// </summary>
        /// <param name="value">Округляемое число</param>
        /// <param name="decimals">Значение, задающее правило округления, если его значение находится ровно посредине между двумя другими числами</param>
        /// <param name="mode">Значение, задающее правило округления параметра value, если его значение находится ровно посредине между двумя другими числами</param>
        /// <returns></returns>
        public static decimal RoundDigit(object value, int decimals = 0, MidpointRounding mode = MidpointRounding.ToEven)
        {
            decimal digit = Convert<decimal>(value);
            return Math.Round(digit, decimals, mode);
        }
        public static decimal? RoundDigitValue(object value, int decimals = 0, MidpointRounding mode = MidpointRounding.ToEven)
        {
            if (IsEmpty(value))
                return null;

            decimal digit = Convert<decimal>(value);
            return Math.Round(digit, decimals, mode);
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

        public static void SetValue(object obj, string memberName, object value)
        {
            var type = obj.GetType();
            MemberInfo[] members;
            while (true)
            {
                members = type.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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

        /// <summary>
        /// Процентное соотношение совпадений двух строк на основе расчёта расстояния Левенштейна.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static decimal LevenshteinDistancePercent(string value1, string value2)
        {
            var distance = (decimal)LevenshteinDistance(value1, value2);
            var maxLength = (decimal)Math.Max(value1.Length, value2.Length);
            var percent = 100m - (distance / maxLength * 100m);
            return percent;
        }
        /// <summary>
        /// Расчёт расстояния Левенштейна между двумя строками 
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static int LevenshteinDistance(string value1, string value2)
        {
            int n = value1.Length;
            int m = value2.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++) ;

            for (int j = 0; j <= m; d[0, j] = j++) ;

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (value2[j - 1] == value1[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}