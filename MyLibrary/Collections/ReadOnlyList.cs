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
