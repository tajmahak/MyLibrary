using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    internal class DBRowCollection : ICollection<DBRow>
    {
        public int Count => RowList.Count;
        public bool IsReadOnly => false;
        public List<DBRow> RowList { get; private set; } = new List<DBRow>();
        private readonly HashSet<DBRow> _hashTable = new HashSet<DBRow>();

        public void Add(DBRow item)
        {
            if (!_hashTable.Contains(item))
            {
                RowList.Add(item);
                _hashTable.Add(item);
            }
        }
        public void Clear()
        {
            RowList.Clear();
            _hashTable.Clear();
        }
        public bool Contains(DBRow item)
        {
            return _hashTable.Contains(item);
        }
        public void CopyTo(DBRow[] array, int arrayIndex)
        {
            RowList.CopyTo(array, arrayIndex);
        }
        public IEnumerator<DBRow> GetEnumerator()
        {
            return RowList.GetEnumerator();
        }
        public bool Remove(DBRow item)
        {
            if (_hashTable.Contains(item))
            {
                RowList.Remove(item);
                _hashTable.Remove(item);
                return true;
            }
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return RowList.GetEnumerator();
        }
    }
}
