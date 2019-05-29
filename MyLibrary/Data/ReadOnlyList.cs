using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.Data
{
    public class ReadOnlyList<T> : ICollection<T>, IEnumerable<T>, IEnumerable
    {
        public ReadOnlyList()
        {
            List = new List<T>();
        }
        public ReadOnlyList(IList<T> list)
        {
            List = list;
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

        internal IList<T> List { get; private set; }
    }

    public class ReadOnlyList : ICollection, IEnumerable
    {
        public ReadOnlyList(IList list)
        {
            _list = list;
        }

        public int Count => _list.Count;
        public object SyncRoot => _list.SyncRoot;
        public bool IsSynchronized => _list.IsSynchronized;
        public void CopyTo(Array array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public IEnumerator GetEnumerator() => _list.GetEnumerator();

        public override bool Equals(object obj) => _list.Equals(obj);
        public override int GetHashCode() => _list.GetHashCode();
        public override string ToString() => _list.ToString();

        private IList _list;
    }
}
