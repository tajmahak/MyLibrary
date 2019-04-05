using System;
using System.Collections.Generic;

namespace MyLibrary.Data
{
    public static class Format
    {
        public static T Convert<T>(object value)
        {
            return Convert<T>(value, true);
        }
        public static T Convert<T>(object value, bool allowNullString)
        {
            var type = typeof(T);

            // определение основного типа данных
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
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
            else
            {
                if (value.GetType() != type)
                {
                    value = System.Convert.ChangeType(value, type);
                }
            }
            return (T)value;
        }

        public static int Compare(object value1, object value2)
        {
            return Compare(value1, value2, false);
        }
        public static int Compare(object value1, object value2, bool ignoreCase)
        {
            if (IsNull(value1) && IsNull(value2))
                return 0;

            if (IsNull(value1))
                return -1;

            if (IsNull(value2))
                return 1;

            var type1 = value1.GetType();
            var type2 = value2.GetType();
            if (value1 is IComparable && value2 is IComparable)
            {
                if (ignoreCase && type1 == typeof(string) && type2 == typeof(string))
                {
                    value1 = ((string)value1).ToUpperInvariant();
                    value2 = ((string)value2).ToUpperInvariant();
                }
                return ((IComparable)value1).CompareTo(value2);
            }
            throw new Exception("Сравнение указанных значений невозможно.");
        }

        public static bool IsEquals(object value1, object value2)
        {
            return IsEquals(value1, value2, false);
        }
        public static bool IsEquals(object value1, object value2, bool ignoreCase)
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

        public static bool IsContains(object value1, object value2)
        {
            return IsContains(value1, value2, false);
        }
        public static bool IsContains(object value1, object value2, bool ignoreCase)
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

        public static string GetNotEmptyString(object value)
        {
            return Convert<string>(value, false);
        }
        public static string FormatString(object value, string format)
        {
            return ((IFormattable)value).ToString(format, null);
        }
        public static decimal RoundDigit(object value)
        {
            return RoundDigit(value, 0);
        }
        public static decimal RoundDigit(object value, int decimals)
        {
            decimal digit = Convert<decimal>(value);
            return Math.Round(digit, decimals);
        }
        public static object RoundValue(object value)
        {
            return RoundValue(value, 0);
        }
        public static object RoundValue(object value, int decimals)
        {
            if (IsEmpty(value))
                return null;

            decimal digit = Convert<decimal>(value);
            return Math.Round(digit, decimals);
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
    }
}