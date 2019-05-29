using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MyLibrary.Collections
{
    public class ReadOnlyArray<T> : ICollection<T>, IEnumerable
    {
        public ReadOnlyArray(T[] array)
        {
            Array = array;
        }
        internal ReadOnlyArray(int length)
        {
            Array
            Array = new T[length];
        }
        public static implicit operator ReadOnlyArray<T>(T[] array)
        {
            return new ReadOnlyArray<T>(array);
        }

        public T this[int index] { get => Array[index]; }

        public int Count => Array.Length;
        public bool IsReadOnly => true;
        public bool Contains(T item) => Array.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => Array.CopyTo(array, arrayIndex);

        void ICollection<T>.Add(T item) => throw new InvalidOperationException();
        void ICollection<T>.Clear() => throw new InvalidOperationException();
        bool ICollection<T>.Remove(T item) => throw new InvalidOperationException();

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator() => Array.GetEnumerator();

        public override bool Equals(object obj) => Array.Equals(obj);
        public override int GetHashCode() => Array.GetHashCode();
        public override string ToString() => Array.ToString();

        internal T[] Array { get; set; }
    }
}
