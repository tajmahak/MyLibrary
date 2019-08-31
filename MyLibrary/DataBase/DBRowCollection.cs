using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    internal class DBRowCollection : ICollection<DBRow>
    {
        public int Count => _list.Count;
        public bool IsReadOnly => false;
        private readonly List<DBRow> _list = new List<DBRow>();
        private readonly HashSet<DBRow> _hashSet = new HashSet<DBRow>();

        public DBRow this[int index] => _list[index];

        public void Add(DBRow item)
        {
            if (!_hashSet.Contains(item))
            {
                _list.Add(item);
                _hashSet.Add(item);
            }
        }
        public void Clear()
        {
            _list.Clear();
            _hashSet.Clear();
        }
        public int Clear(Predicate<DBRow> match)
        {
            var list = _list.FindAll(x => !match(x));
            if (list.Count != _list.Count)
            {
                Clear();
                foreach (var item in list)
                {
                    Add(item);
                }
            }
            return _list.Count - list.Count;
        }
        public bool Contains(DBRow item)
        {
            return _hashSet.Contains(item);
        }
        public void CopyTo(DBRow[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }
        public IEnumerator<DBRow> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        public bool Remove(DBRow item)
        {
            if (_hashSet.Contains(item))
            {
                _list.Remove(item);
                _hashSet.Remove(item);
                return true;
            }
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        public override string ToString()
        {
            return _list.ToString();
        }
    }
}
