using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public class DBColumnCollection : ICollection<DBColumn>
    {
        public int Count => _list.Count;
        public bool IsReadOnly => false;
        private readonly List<DBColumn> _list = new List<DBColumn>();
        private readonly Dictionary<string, DBColumn> _dictionary = new Dictionary<string, DBColumn>();

        public DBColumn this[int index] => _list[index];
        public DBColumn this[string fullName]
        {
            get
            {
                if (_dictionary.TryGetValue(fullName, out var column))
                {
                    return column;
                }
                throw DBInternal.UnknownColumnException(null, fullName);
            }
        }

        public void Add(DBColumn item)
        {
            if (!_dictionary.ContainsKey(item.FullName))
            {
                _dictionary.Add(item.FullName, item);
            }
            // колонки могут быть с одинаковыми названиями
            _list.Add(item);
        }
        public void Clear()
        {
            _list.Clear();
            _dictionary.Clear();
        }
        public bool Contains(DBColumn item)
        {
            return _dictionary.ContainsValue(item);
        }
        public bool Contains(string fullName)
        {
            return _dictionary.ContainsKey(fullName);
        }
        public void CopyTo(DBColumn[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }
        public IEnumerator<DBColumn> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        public DBColumn Find(Predicate<DBColumn> match)
        {
            return _list.Find(match);
        }
        public bool Remove(DBColumn item)
        {
            if (_dictionary.ContainsKey(item.FullName))
            {
                _dictionary.Remove(item.FullName);
            }
            return _list.Remove(item);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        public override string ToString()
        {
            return $"{nameof(Count)} = {Count}";
        }
    }
}
