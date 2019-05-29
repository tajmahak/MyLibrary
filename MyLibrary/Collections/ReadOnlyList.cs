using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.Collections
{
    public class ReadOnlyList<T> : ICollection<T>, IEnumerable<T>, IEnumerable
    {
        public ReadOnlyList(List<T> list)
        {
            List = list;
        }
        internal ReadOnlyList()
        {
            List = new List<T>();
        }
        public static implicit operator ReadOnlyList<T>(List<T> list)
        {
            return new ReadOnlyList<T>(list);
        }

        public T this[int index]
        {
            get => List[index];
            internal set => List[index] = value;
        }

        public int Count => List.Count;
        public bool IsReadOnly => true;
        public bool Contains(T item) => List.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);

        public bool Exists(Predicate<T> match) => List.Exists(match);
        public T Find(Predicate<T> match) => List.Find(match);
        public List<T> FindAll(Predicate<T> match) => List.FindAll(match);
        public int FindIndex(Predicate<T> match) => List.FindIndex(match);
        public int FindIndex(int startIndex, Predicate<T> match) => List.FindIndex(startIndex, match);
        public int FindIndex(int startIndex, int count, Predicate<T> match) => List.FindIndex(startIndex, count, match);
        public T FindLast(Predicate<T> match) => List.FindLast(match);
        public int FindLastIndex(Predicate<T> match) => List.FindLastIndex(match);
        public int FindLastIndex(int startIndex, Predicate<T> match) => List.FindLastIndex(startIndex, match);
        public int FindLastIndex(int startIndex, int count, Predicate<T> match) => List.FindLastIndex(startIndex, count, match);
        public void ForEach(Action<T> action) => List.ForEach(action);
        public int IndexOf(T item, int index, int count) => List.IndexOf(item, index, count);
        public int IndexOf(T item, int index) => List.IndexOf(item, index);
        public int IndexOf(T item) => List.IndexOf(item);
        public int LastIndexOf(T item) => List.LastIndexOf(item);
        public int LastIndexOf(T item, int index) => List.LastIndexOf(item, index);
        public int LastIndexOf(T item, int index, int count) => List.LastIndexOf(item, index, count);

        void ICollection<T>.Add(T item) => throw new InvalidOperationException();
        void ICollection<T>.Clear() => throw new InvalidOperationException();
        bool ICollection<T>.Remove(T item) => throw new InvalidOperationException();

        public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

        public override bool Equals(object obj) => List.Equals(obj);
        public override int GetHashCode() => List.GetHashCode();
        public override string ToString() => List.ToString();

        internal List<T> List { get; set; }
    }
}
