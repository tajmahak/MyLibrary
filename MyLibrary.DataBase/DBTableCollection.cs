using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public sealed class DBTableCollection : ICollection<DBTable>
    {
        public int Count => _list.Count;
        public bool IsReadOnly => false;
        private readonly List<DBTable> _list = new List<DBTable>();
        private readonly Dictionary<string, DBTable> _dictionary = new Dictionary<string, DBTable>();

        public DBTable this[int index] => _list[index];
        public DBTable this[string name]
        {
            get
            {
                if (_dictionary.TryGetValue(name, out DBTable table))
                {
                    return table;
                }
                throw DBInternal.UnknownTableException(name);
            }
        }

        public void Add(DBTable item)
        {
            _list.Add(item);
            _dictionary.Add(item.Name, item);
        }
        public void Clear()
        {
            _list.Clear();
            _dictionary.Clear();
        }
        public bool Contains(DBTable item)
        {
            return _dictionary.ContainsValue(item);
        }
        public bool Contains(string name)
        {
            return _dictionary.ContainsKey(name);
        }
        public void CopyTo(DBTable[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }
        public IEnumerator<DBTable> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        public bool Remove(DBTable item)
        {
            if (_dictionary.ContainsKey(item.Name))
            {
                _dictionary.Remove(item.Name);
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
