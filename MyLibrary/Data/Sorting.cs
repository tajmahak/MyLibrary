using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.Data
{
    /// <summary>
    /// Представляет методы сортировки
    /// </summary>
    public static class Sorting
    {
        /// <summary>
        /// Сортировка вставками (устойчивая)
        /// </summary>
        /// <param name="list"></param>
        /// <param name="comparison"></param>
        public static void StableInsertionSort(this IList list, Comparison<object> comparison)
        {
            // сортировка вставками
            var count = list.Count;
            for (var j = 1; j < count; j++)
            {
                var key = list[j];

                var i = j - 1;
                for (; i >= 0 && comparison(list[i], key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }
        /// <summary>
        /// Сортировка вставками (устойчивая)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="comparison"></param>
        public static void StableInsertionSort<T>(this IList<T> list, Comparison<T> comparison)
        {
            StableInsertionSort((IList)list, (x, y) => comparison((T)x, (T)y));
        }
        /// <summary>
        /// Сортировка вставками (устойчивая)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="comparer"></param>
        public static void StableInsertionSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            StableInsertionSort(list, (x, y) => comparer.Compare(x, y));
        }
        /// <summary>
        /// Сортировка вставками (устойчивая)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void StableInsertionSort<T>(this IList<T> list) where T : IComparable
        {
            StableInsertionSort(list, (x, y) => x.CompareTo(y));
        }
    }
}
