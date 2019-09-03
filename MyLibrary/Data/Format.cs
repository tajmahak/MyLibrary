using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            {
                return 0;
            }

            if (IsNull(x))
            {
                return -1;
            }

            if (IsNull(y))
            {
                return 1;
            }

            return x.CompareTo(y);
        }
        public static int Compare(object x, object y)
        {
            if (IsNull(x) && IsNull(y))
            {
                return 0;
            }

            if (IsNull(x))
            {
                return -1;
            }

            if (IsNull(y))
            {
                return 1;
            }

            if (x is IComparable && y is IComparable)
            {
                return ((IComparable)x).CompareTo(y);
            }
            throw new Exception("Сравнение указанных значений невозможно.");
        }

        public static bool IsNull(object value)
        {
            return value == null || value is DBNull;
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
            return Equals(value, Activator.CreateInstance(value.GetType()));
        }

        public static bool IsEquals<T>(T x, T y) where T : IEquatable<T>
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            var type = typeof(T);
            if (type.BaseType == typeof(Array))
            {
                return IsEqualsArray((T[])(object)x, (T[])(object)y);
            }
            return Equals(x, y);
        }
        public static bool IsEquals(object x, object y)
        {
            if (IsNull(x) && IsNull(y))
            {
                return true;
            }

            if (IsNull(x) || IsNull(y))
            {
                return false;
            }

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
                return IsEqualsArray((byte[])x, (byte[])y);
            }
            return Equals(x, y);
        }
        public static bool IsEqualsString(string str1, string str2, bool ignoreCase = false)
        {
            if (ignoreCase)
            {
                if (!IsEmpty(str1))
                {
                    str1 = str1.ToUpper();
                }
                if (!IsEmpty(str2))
                {
                    str2 = str2.ToUpper();
                }
            }
            return IsEquals(str1, str2);
        }
        public static bool IsEqualsArray<T>(T[] blob1, T[] blob2) where T : IEquatable<T>
        {
            if (blob1.Length != blob2.Length)
            {
                return false;
            }

            var length = blob1.Length;
            for (var i = 0; i < length; ++i)
            {
                if (!blob1[i].Equals(blob2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsContainsString(string str1, string str2, bool ignoreCase = false)
        {
            if (ignoreCase)
            {
                str1 = str1.ToUpper();
                str2 = str2.ToUpper();
            }
            return str1.Contains(str2);
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
        public static string ConvertToNotNullString(object value)
        {
            value = Convert<string>(value);
            return IsNull(value) ? string.Empty : (string)value;
        }
        public static string[] Split(string value, params string[] values)
        {
            return value.Split(values, StringSplitOptions.RemoveEmptyEntries);
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
            {
                throw new NotImplementedException();
            }

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

        public static byte[] CompressText(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            using (var mem = new MemoryStream())
            {
                using (var stream = new DeflateStream(mem, CompressionMode.Compress))
                {
                    stream.Write(data, 0, data.Length);
                }
                data = mem.ToArray();
            }
            return data;
        }
        public static string DecompressText(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            using (var stream = new DeflateStream(mem, CompressionMode.Decompress))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var text = reader.ReadToEnd();
                return text;
            }
        }
        public static string ToBase64(byte[] data)
        {
            return System.Convert.ToBase64String(data);
        }
        public static byte[] FromBase64(string data)
        {
            return System.Convert.FromBase64String(data);
        }
        public static string ToHexText(byte[] data)
        {
            var str = new StringBuilder(data.Length * 2);
            for (var i = 0; i < data.Length; i++)
            {
                str.Append(data[i].ToString("x2"));
            }
            return str.ToString();
        }

        public static string FormattedDigit(object value, int decimals = 0, bool allowNull = false)
        {
            if (allowNull && IsNull(value))
            {
                return null;
            }

            if (IsNull(value))
            {
                value = decimal.Zero;
            }

            var text = System.Convert.ToDecimal(value).ToString($"N{decimals}");
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
        public static string FormattedFileSize(long value)
        {
            string[] sizes = { "б", "Кб", "Мб", "Гб", "Тб" };
            var order = 0;
            var len = value;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            var val = value / (decimal)Math.Pow(1024, order);
            var text = $"{val:0.00} {sizes[order]}".Replace(',', '.');
            return text;
        }

        public static decimal Round(object value, int decimals = 0, MidpointRounding mode = MidpointRounding.ToEven)
        {
            var digit = Convert<decimal>(value);
            return Math.Round(digit, decimals, mode);
        }
        public static decimal? RoundNullableDigit(object value, int decimals = 0, MidpointRounding mode = MidpointRounding.ToEven)
        {
            if (IsEmpty(value))
            {
                return null;
            }
            return Round(value, decimals, mode);
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
            var n = value1.Length;
            var m = value2.Length;
            var d = new int[n + 1, m + 1];

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
            for (var i = 0; i <= n; d[i, 0] = i++)
            {
                ;
            }

            for (var j = 0; j <= m; d[0, j] = j++)
            {
                ;
            }

            // Step 3
            for (var i = 1; i <= n; i++)
            {
                //Step 4
                for (var j = 1; j <= m; j++)
                {
                    // Step 5
                    var cost = (value2[j - 1] == value1[i - 1]) ? 0 : 1;

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