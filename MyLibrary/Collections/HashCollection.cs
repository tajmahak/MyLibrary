using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.Html
{
    public class HashCollection<T> : ICollection<T>
    {
        public T this[int index] => list[index];
        public int Count => list.Count;
        public bool IsReadOnly => false;
        private readonly List<T> list = null;
        private readonly HashSet<T> hash = null;

        public void Add(T item)
        {
            if (!hash.Contains(item))
            {
                hash.Add(item);
                list.Add(item);
            }
        }

        public void Clear()
        {
            hash.Clear();
            list.Clear();
        }

        public bool Contains(T item)
        {
            return hash.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public bool Remove(T item)
        {
            hash.Remove(item);
            return list.Remove(item);
        }

        public bool Exists(Predicate<T> match)
        {
            return list.Exists(match);
        }

        public T Find(Predicate<T> match)
        {
            return list.Find(match);
        }

        public List<T> FindAll(Predicate<T> match)
        {
            return list.FindAll(match);
        }

        public int FindIndex(Predicate<T> match)
        {
            return list.FindIndex(match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return list.FindIndex(startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return list.FindIndex(startIndex, count, match);
        }

        public T FindLast(Predicate<T> match)
        {
            return list.FindLast(match);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return list.FindLastIndex(match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return list.FindLastIndex(startIndex, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return list.FindLastIndex(startIndex, count, match);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            list.Sort(index, count, comparer);
        }

        public void Sort(Comparison<T> comparison)
        {
            list.Sort(comparison);
        }

        public void Sort()
        {
            list.Sort();
        }

        public void Sort(IComparer<T> comparer)
        {
            list.Sort(comparer);
        }

        public T[] ToArray()
        {
            return list.ToArray();
        }

        public List<T> ToList()
        {
            return new List<T>(list);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
