using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.Collections
{
    internal class HashCollection<T> : ICollection<T>
    {
        private readonly List<T> _list;
        private readonly HashSet<T> _hash;

        public T this[int index] => _list[index];
        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (!_hash.Contains(item))
            {
                _hash.Add(item);
                _list.Add(item);
            }
        }
        public void Clear()
        {
            _hash.Clear();
            _list.Clear();
        }
        public bool Contains(T item)
        {
            return _hash.Contains(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }
        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        public bool Remove(T item)
        {
            _hash.Remove(item);
            return _list.Remove(item);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public bool Exists(Predicate<T> match)
        {
            return _list.Exists(match);
        }
        public T Find(Predicate<T> match)
        {
            return _list.Find(match);
        }
        public List<T> FindAll(Predicate<T> match)
        {
            return _list.FindAll(match);
        }
        public int FindIndex(Predicate<T> match)
        {
            return _list.FindIndex(match);
        }
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return _list.FindIndex(startIndex, match);
        }
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return _list.FindIndex(startIndex, count, match);
        }
        public T FindLast(Predicate<T> match)
        {
            return _list.FindLast(match);
        }
        public int FindLastIndex(Predicate<T> match)
        {
            return _list.FindLastIndex(match);
        }
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return _list.FindLastIndex(startIndex, match);
        }
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return _list.FindLastIndex(startIndex, count, match);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            _list.Sort(index, count, comparer);
        }
        public void Sort(Comparison<T> comparison)
        {
            _list.Sort(comparison);
        }
        public void Sort()
        {
            _list.Sort();
        }
        public void Sort(IComparer<T> comparer)
        {
            _list.Sort(comparer);
        }
        public T[] ToArray()
        {
            return _list.ToArray();
        }
        public List<T> ToList()
        {
            return new List<T>(_list);
        }
    }
}
