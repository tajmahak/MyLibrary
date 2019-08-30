using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public class DBIndexCollection : ICollection<DBIndex>
    {
        public int Count => _list.Count;
        public bool IsReadOnly => false;
        private readonly List<DBIndex> _list = new List<DBIndex>();
        private readonly HashSet<DBIndex> _hashSet = new HashSet<DBIndex>();

        public DBIndex this[int index] => _list[index];

        public void Add(DBIndex item)
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
        public DBIndex Find(Predicate<DBIndex> match)
        {
            return _list.Find(match);
        }
        public bool Contains(DBIndex item)
        {
            return _hashSet.Contains(item);
        }
        public void CopyTo(DBIndex[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }
        public IEnumerator<DBIndex> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        public bool Remove(DBIndex item)
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
    }
}
